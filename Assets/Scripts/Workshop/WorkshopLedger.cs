using System;
using System.Collections.Generic;
using System.Linq;

public enum ShopResult { Ok, AlreadyPlaced, EmptyCart, NotEnoughMoney, OutOfStock, UnknownPart }

public enum BuildResult { Ok, BuildInProgress, MissingParts, UnknownRecipe, NoBuild, Busy, Equipped, UnknownGadget, TakenBySomeoneElse }

/// <summary>
/// Every change to the crew's money, parts and gadgets goes through here, so the
/// rules are in one place and each change is whole: money never leaves without
/// an order, an order is delivered and unpacked once (by its id), a build takes
/// its parts out of storage when it starts and either gives them back or turns
/// them into exactly one gadget. Nothing here saves: the caller saves once after
/// a change, so a crash leaves the save from before or after it, never between.
/// Once online, only the host calls these.
/// </summary>
public static class WorkshopLedger
{
    /// The late fee for asking the landlord for two more days.
    public const int ExtensionFee = 60;
    public const int ExtensionDays = 2;
    /// Charge one WhisperBox hint uses (five per full battery).
    public const float WhisperCost = 0.2f;

    // Storage ----------------------------------------------------------------------------

    public static int Count(CrewState crew, string part) => crew.parts.Where(s => s.part == part).Sum(s => s.count);

    static void Add(List<PartStack> stacks, string part, int n)
    {
        var stack = stacks.Find(s => s.part == part);
        if (stack == null)
            stacks.Add(new PartStack(part, n));
        else
            stack.count += n;
        stacks.RemoveAll(s => s.count <= 0);
    }

    /// The parts a recipe still needs from storage: (part, needed, in storage).
    public static List<(string part, int need, int have)> Needs(CrewState crew, RecipeDef recipe)
    {
        var list = new List<(string, int, int)>();
        foreach (var group in recipe.Parts.GroupBy(p => p))
            list.Add((group.Key, group.Count(), Count(crew, group.Key)));
        return list;
    }

    public static bool CanBuild(CrewState crew, RecipeDef recipe) => Needs(crew, recipe).All(n => n.have >= n.need);

    // Shopping ---------------------------------------------------------------------------

    /// How many of a part the shop will still sell today.
    public static int StockLeft(CrewState crew, string part)
    {
        if (crew.stockDay != crew.day)
            return PartCatalog.DailyStock;
        return Math.Max(0, PartCatalog.DailyStock - crew.boughtToday.Where(s => s.part == part).Sum(s => s.count));
    }

    public static int Total(IEnumerable<PartStack> cart) => cart.Sum(l => PartCatalog.Part(l.part)?.Price * l.count ?? 0);

    public static string NewOrderId() => "Q-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();

    /// Pays for a cart and places one order under orderId. The same id again is
    /// refused (a double click, a resent request): the first order stands, nothing
    /// more is charged.
    public static ShopResult Checkout(CrewState crew, string orderId, IEnumerable<PartStack> cart, out PartOrder order)
    {
        order = crew.orders.Find(o => o.id == orderId);
        if (order != null)
            return ShopResult.AlreadyPlaced;
        var lines = new List<PartStack>();
        foreach (var line in cart)
        {
            if (line.count <= 0)
                continue;
            if (PartCatalog.Part(line.part) == null)
                return ShopResult.UnknownPart;
            Add(lines, line.part, line.count);
        }
        if (lines.Count == 0)
            return ShopResult.EmptyCart;
        foreach (var line in lines)
            if (line.count > StockLeft(crew, line.part))
                return ShopResult.OutOfStock;
        int total = Total(lines);
        if (total > crew.savings)
            return ShopResult.NotEnoughMoney;

        // Commit, all at once.
        if (crew.stockDay != crew.day)
        {
            crew.stockDay = crew.day;
            crew.boughtToday.Clear();
        }
        foreach (var line in lines)
            Add(crew.boughtToday, line.part, line.count);
        crew.savings -= total;
        order = new PartOrder { id = orderId, lines = lines, total = total, day = crew.day, state = PartOrder.Paid };
        crew.orders.Add(order);
        return ShopResult.Ok;
    }

    /// The courier knocked: the order is a parcel at the door now. Once.
    public static bool Deliver(CrewState crew, string orderId)
    {
        var order = crew.orders.Find(o => o.id == orderId);
        if (order == null || order.state != PartOrder.Paid)
            return false;
        order.state = PartOrder.Delivered;
        return true;
    }

    /// Unpacking a parcel puts its parts in storage. Once per order, whatever happens to the box.
    public static bool OpenParcel(CrewState crew, string orderId)
    {
        var order = crew.orders.Find(o => o.id == orderId);
        if (order == null || order.state != PartOrder.Delivered)
            return false;
        order.state = PartOrder.Opened;
        foreach (var line in order.lines)
            Add(crew.parts, line.part, line.count);
        return true;
    }

    /// After the first paid shift, a box of salvaged parts turns up: enough for
    /// the gadget that helps with the job you were hired for. Once per life.
    public const string StarterOrder = "STARTER";

    public static bool GiveStarterKit(CrewState crew)
    {
        if (crew.starterKitGiven || crew.shiftsCompleted < 1)
            return false;
        crew.starterKitGiven = true;
        var recipe = PartCatalog.RecipeFor(crew.Job);
        var lines = new List<PartStack>();
        foreach (var part in recipe.Parts)
            Add(lines, part, 1);
        crew.orders.Add(new PartOrder { id = StarterOrder, from = "Your cousin Dev", lines = lines, total = 0, day = crew.day, state = PartOrder.Paid });
        return true;
    }

    // Building ---------------------------------------------------------------------------

    /// Starts a build: the recipe's parts leave storage and wait on the bench.
    public static BuildResult BeginBuild(CrewState crew, string recipeId, string actorId)
    {
        var recipe = PartCatalog.Recipe(recipeId);
        if (recipe == null)
            return BuildResult.UnknownRecipe;
        if (crew.build.Active)
            return BuildResult.BuildInProgress;
        if (!CanBuild(crew, recipe))
            return BuildResult.MissingParts;
        foreach (var part in recipe.Parts)
            Add(crew.parts, part, -1);
        crew.build = new BuildState { recipe = recipeId, reserved = recipe.Parts.ToList(), editor = actorId ?? "" };
        return BuildResult.Ok;
    }

    /// Who may handle the build's tools now: nobody else while someone is working on it.
    public static bool CanEdit(CrewState crew, string actorId) =>
        crew.build.Active && (string.IsNullOrEmpty(crew.build.editor) || crew.build.editor == actorId);

    public static void ClaimBuild(CrewState crew, string actorId)
    {
        if (crew.build.Active && string.IsNullOrEmpty(crew.build.editor))
            crew.build.editor = actorId;
    }

    public static void ReleaseBuild(CrewState crew, string actorId)
    {
        if (crew.build.editor == actorId)
            crew.build.editor = "";
    }

    /// Gives the reserved parts back, once, and clears the bench.
    public static BuildResult CancelBuild(CrewState crew)
    {
        if (!crew.build.Active)
            return BuildResult.NoBuild;
        foreach (var part in crew.build.reserved)
            Add(crew.parts, part, 1);
        crew.build = new BuildState();
        return BuildResult.Ok;
    }

    /// The test passed: the reserved parts become one gadget. The bench is cleared.
    public static BuildResult CompleteBuild(CrewState crew, out GadgetInstance gadget)
    {
        gadget = null;
        if (!crew.build.Active)
            return BuildResult.NoBuild;
        gadget = new GadgetInstance
        {
            id = "G-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant(),
            recipe = crew.build.recipe,
            charge = 1f,
            builtDay = crew.day,
        };
        crew.gadgets.Add(gadget);
        crew.build = new BuildState();
        return BuildResult.Ok;
    }

    /// What taking a gadget apart gives back (all its parts: a gadget is never worth more in pieces).
    public static List<string> Salvage(GadgetInstance gadget) => PartCatalog.Recipe(gadget.recipe)?.Parts.ToList() ?? new List<string>();

    public static BuildResult Dismantle(CrewState crew, string gadgetId)
    {
        var gadget = crew.gadgets.Find(g => g.id == gadgetId);
        if (gadget == null)
            return BuildResult.UnknownGadget;
        if (gadget.Equipped)
            return BuildResult.Equipped;
        crew.gadgets.Remove(gadget);
        foreach (var part in Salvage(gadget))
            Add(crew.parts, part, 1);
        return BuildResult.Ok;
    }

    // Loadout ----------------------------------------------------------------------------

    /// One gadget per player. Packing another puts the first back on the rack.
    public static BuildResult Equip(CrewState crew, string gadgetId, string playerId)
    {
        var gadget = crew.gadgets.Find(g => g.id == gadgetId);
        if (gadget == null)
            return BuildResult.UnknownGadget;
        if (gadget.Equipped && gadget.equippedBy != playerId)
            return BuildResult.TakenBySomeoneElse;
        foreach (var other in crew.gadgets)
            if (other.equippedBy == playerId)
                other.equippedBy = "";
        gadget.equippedBy = playerId;
        return BuildResult.Ok;
    }

    public static BuildResult Unequip(CrewState crew, string gadgetId)
    {
        var gadget = crew.gadgets.Find(g => g.id == gadgetId);
        if (gadget == null)
            return BuildResult.UnknownGadget;
        gadget.equippedBy = "";
        return BuildResult.Ok;
    }

    public static GadgetInstance EquippedBy(CrewState crew, string playerId) => crew.gadgets.Find(g => g.equippedBy == playerId);

    /// Spends charge only if there is enough; never goes below empty.
    public static bool Spend(GadgetInstance gadget, float amount)
    {
        if (gadget == null || gadget.charge + 1e-4f < amount)
            return false;
        gadget.charge = Math.Max(0f, gadget.charge - amount);
        return true;
    }

    /// Free charging on the rack at home (equipped ones too: they are packed beside it).
    public static void Charge(CrewState crew, float seconds)
    {
        foreach (var gadget in crew.gadgets)
            gadget.charge = Math.Min(1f, gadget.charge + seconds / PartCatalog.ChargeSeconds);
    }

    // Rent -------------------------------------------------------------------------------

    public static bool RentOverdue(CrewState crew) => crew.billReceived && crew.daysLeft <= 0;

    /// Pays this week's rent from savings; the next one is due a week after this one.
    public static bool PayRent(CrewState crew)
    {
        if (!crew.billReceived || crew.savings < crew.rentDue)
            return false;
        crew.savings -= crew.rentDue;
        crew.rentPaid++;
        crew.rentDue = CrewState.StartingRent;
        crew.daysLeft += CrewState.RentDays;
        crew.extensionsThisWeek = 0;
        return true;
    }

    /// Two more days for a late fee, once per rent week.
    public static bool AskForExtension(CrewState crew)
    {
        if (!crew.billReceived || crew.extensionsThisWeek > 0 || crew.daysLeft > 1)
            return false;
        crew.extensionsThisWeek++;
        crew.rentDue += ExtensionFee;
        crew.daysLeft += ExtensionDays;
        return true;
    }
}
