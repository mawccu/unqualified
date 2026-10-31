using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// The workshop's money and parts: every change is whole and happens once.
/// Orders are keyed by id (double clicks, resent requests, reloads), parcels
/// unpack once, builds reserve, return or consume exactly their parts, one
/// gadget per player, and old saves load with their money and lies intact.
public class WorkshopLedgerTests
{
    static CrewState Crew(int savings = 200)
    {
        var crew = new CrewState { savings = savings, billReceived = true, hired = true };
        return crew;
    }

    static List<PartStack> Cart(params (string part, int n)[] lines) => lines.Select(l => new PartStack(l.part, l.n)).ToList();

    [Test]
    public void ACheckoutChargesOnceEvenWhenSentTwice()
    {
        var crew = Crew(100);
        var cart = Cart((PartCatalog.Board, 1), (PartCatalog.Speaker, 2));
        Assert.AreEqual(ShopResult.Ok, WorkshopLedger.Checkout(crew, "Q-1", cart, out var order));
        Assert.AreEqual(100 - 14 - 18, crew.savings);
        Assert.AreEqual(32, order.total);
        Assert.AreEqual(ShopResult.AlreadyPlaced, WorkshopLedger.Checkout(crew, "Q-1", cart, out var again), "the same order id again");
        Assert.AreSame(order, again);
        Assert.AreEqual(68, crew.savings, "charged once");
        Assert.AreEqual(1, crew.orders.Count);
    }

    [Test]
    public void ACheckoutThatCannotBePaidChangesNothing()
    {
        var crew = Crew(20);
        Assert.AreEqual(ShopResult.NotEnoughMoney, WorkshopLedger.Checkout(crew, "Q-1", Cart((PartCatalog.Board, 2)), out _));
        Assert.AreEqual(ShopResult.EmptyCart, WorkshopLedger.Checkout(crew, "Q-2", Cart((PartCatalog.Board, 0)), out _));
        Assert.AreEqual(ShopResult.UnknownPart, WorkshopLedger.Checkout(crew, "Q-3", Cart(("flux_capacitor", 1)), out _));
        Assert.AreEqual(20, crew.savings);
        Assert.AreEqual(0, crew.orders.Count);
    }

    [Test]
    public void TheShopSellsALimitedStockEachDay()
    {
        var crew = Crew(500);
        Assert.AreEqual(ShopResult.Ok, WorkshopLedger.Checkout(crew, "Q-1", Cart((PartCatalog.Motor, PartCatalog.DailyStock)), out _));
        Assert.AreEqual(0, WorkshopLedger.StockLeft(crew, PartCatalog.Motor));
        Assert.AreEqual(ShopResult.OutOfStock, WorkshopLedger.Checkout(crew, "Q-2", Cart((PartCatalog.Motor, 1)), out _));
        crew.day++;
        Assert.AreEqual(PartCatalog.DailyStock, WorkshopLedger.StockLeft(crew, PartCatalog.Motor), "restocked the next day");
        Assert.AreEqual(ShopResult.Ok, WorkshopLedger.Checkout(crew, "Q-3", Cart((PartCatalog.Motor, 1)), out _));
    }

    [Test]
    public void AParcelIsDeliveredAndUnpackedOnce()
    {
        var crew = Crew(100);
        WorkshopLedger.Checkout(crew, "Q-1", Cart((PartCatalog.Battery, 2)), out _);
        Assert.IsFalse(WorkshopLedger.OpenParcel(crew, "Q-1"), "not here yet");
        Assert.IsTrue(WorkshopLedger.Deliver(crew, "Q-1"));
        Assert.IsFalse(WorkshopLedger.Deliver(crew, "Q-1"), "one knock");
        Assert.IsTrue(WorkshopLedger.OpenParcel(crew, "Q-1"));
        Assert.IsFalse(WorkshopLedger.OpenParcel(crew, "Q-1"), "an empty box gives nothing");
        Assert.AreEqual(2, WorkshopLedger.Count(crew, PartCatalog.Battery));
    }

    [Test]
    public void TheStarterKitComesOnceAfterTheFirstPaidShiftForTheJob()
    {
        var crew = Crew(0);
        crew.job = Jobs.CallCenter;
        Assert.IsFalse(WorkshopLedger.GiveStarterKit(crew), "not before a shift");
        crew.shiftsCompleted = 1;
        Assert.IsTrue(WorkshopLedger.GiveStarterKit(crew));
        Assert.IsFalse(WorkshopLedger.GiveStarterKit(crew), "once per life");
        var kit = crew.orders.Single();
        Assert.AreEqual(0, kit.total, "a gift, not a deduction");
        WorkshopLedger.Deliver(crew, kit.id);
        WorkshopLedger.OpenParcel(crew, kit.id);
        Assert.IsTrue(WorkshopLedger.CanBuild(crew, PartCatalog.Recipe(PartCatalog.WhisperBox)), "enough for the call center's gadget");
    }

    static CrewState WithParts(params string[] parts)
    {
        var crew = Crew();
        foreach (var p in parts)
        {
            var s = crew.parts.Find(x => x.part == p);
            if (s == null) crew.parts.Add(new PartStack(p, 1)); else s.count++;
        }
        return crew;
    }

    [Test]
    public void ABuildReservesItsPartsAndCancellingGivesThemBackOnce()
    {
        var recipe = PartCatalog.Recipe(PartCatalog.WhisperBox);
        var crew = WithParts(recipe.Parts);
        Assert.AreEqual(BuildResult.Ok, WorkshopLedger.BeginBuild(crew, recipe.Id, "peer_1"));
        Assert.AreEqual(0, crew.parts.Sum(s => s.count), "out of storage, on the bench");
        Assert.AreEqual(BuildResult.BuildInProgress, WorkshopLedger.BeginBuild(crew, recipe.Id, "peer_1"));
        Assert.AreEqual(BuildResult.Ok, WorkshopLedger.CancelBuild(crew));
        Assert.AreEqual(BuildResult.NoBuild, WorkshopLedger.CancelBuild(crew), "a second cancel returns nothing");
        Assert.AreEqual(recipe.Parts.Length, crew.parts.Sum(s => s.count));
    }

    [Test]
    public void FinishingABuildMakesExactlyOneGadgetFromItsParts()
    {
        var recipe = PartCatalog.Recipe(PartCatalog.SteadyHand);
        var crew = WithParts(recipe.Parts.Append(PartCatalog.Speaker).ToArray());
        Assert.AreEqual(BuildResult.Ok, WorkshopLedger.BeginBuild(crew, recipe.Id, "peer_1"));
        Assert.AreEqual(BuildResult.Ok, WorkshopLedger.CompleteBuild(crew, out var gadget));
        Assert.AreEqual(BuildResult.NoBuild, WorkshopLedger.CompleteBuild(crew, out _), "no second gadget");
        Assert.AreEqual(1, crew.gadgets.Count);
        Assert.AreEqual(recipe.Id, gadget.recipe);
        Assert.AreEqual(1f, gadget.charge);
        Assert.AreEqual(1, crew.parts.Sum(s => s.count), "only the spare speaker is left");
        Assert.IsFalse(crew.build.Active);
    }

    [Test]
    public void ABuildNeedsEveryPart()
    {
        var crew = WithParts(PartCatalog.Casing, PartCatalog.Board, PartCatalog.Battery);
        Assert.AreEqual(BuildResult.MissingParts, WorkshopLedger.BeginBuild(crew, PartCatalog.WhisperBox, "peer_1"));
        Assert.AreEqual(3, crew.parts.Sum(s => s.count), "nothing reserved");
        Assert.AreEqual(BuildResult.UnknownRecipe, WorkshopLedger.BeginBuild(crew, "jetpack", "peer_1"));
    }

    [Test]
    public void OnlyOnePersonWorksOnABuildAndLeavingFreesIt()
    {
        var crew = WithParts(PartCatalog.Recipe(PartCatalog.WhisperBox).Parts);
        WorkshopLedger.BeginBuild(crew, PartCatalog.WhisperBox, "peer_1");
        Assert.IsTrue(WorkshopLedger.CanEdit(crew, "peer_1"));
        Assert.IsFalse(WorkshopLedger.CanEdit(crew, "peer_2"));
        WorkshopLedger.ReleaseBuild(crew, "peer_1");
        Assert.IsTrue(WorkshopLedger.CanEdit(crew, "peer_2"), "a disconnect frees the lock and keeps the build");
        Assert.IsTrue(crew.build.Active);
    }

    [Test]
    public void TakingAGadgetApartGivesItsPartsBackButNotWhilePacked()
    {
        var recipe = PartCatalog.Recipe(PartCatalog.WhisperBox);
        var crew = WithParts(recipe.Parts);
        WorkshopLedger.BeginBuild(crew, recipe.Id, "peer_1");
        WorkshopLedger.CompleteBuild(crew, out var gadget);
        WorkshopLedger.Equip(crew, gadget.id, "peer_1");
        Assert.AreEqual(BuildResult.Equipped, WorkshopLedger.Dismantle(crew, gadget.id));
        WorkshopLedger.Unequip(crew, gadget.id);
        Assert.AreEqual(BuildResult.Ok, WorkshopLedger.Dismantle(crew, gadget.id));
        Assert.AreEqual(BuildResult.UnknownGadget, WorkshopLedger.Dismantle(crew, gadget.id));
        Assert.AreEqual(recipe.Parts.Length, crew.parts.Sum(s => s.count), "all parts back, no more");
        Assert.AreEqual(0, crew.gadgets.Count);
    }

    [Test]
    public void EachPlayerPacksOneGadgetAndNoTwoShareOne()
    {
        var crew = Crew();
        crew.gadgets.Add(new GadgetInstance { id = "G-1", recipe = PartCatalog.WhisperBox });
        crew.gadgets.Add(new GadgetInstance { id = "G-2", recipe = PartCatalog.SteadyHand });
        Assert.AreEqual(BuildResult.Ok, WorkshopLedger.Equip(crew, "G-1", "peer_1"));
        Assert.AreEqual(BuildResult.TakenBySomeoneElse, WorkshopLedger.Equip(crew, "G-1", "peer_2"));
        Assert.AreEqual(BuildResult.Ok, WorkshopLedger.Equip(crew, "G-2", "peer_1"), "switching");
        Assert.AreEqual("G-2", WorkshopLedger.EquippedBy(crew, "peer_1").id);
        Assert.IsFalse(crew.gadgets[0].Equipped, "the first went back on the rack");
        Assert.AreEqual(BuildResult.Ok, WorkshopLedger.Equip(crew, "G-1", "peer_2"));
    }

    [Test]
    public void ChargeIsNeverSpentPastEmptyAndTheRackFillsItForFree()
    {
        var gadget = new GadgetInstance { id = "G-1", recipe = PartCatalog.WhisperBox, charge = 0.3f };
        Assert.IsTrue(WorkshopLedger.Spend(gadget, WorkshopLedger.WhisperCost));
        Assert.IsFalse(WorkshopLedger.Spend(gadget, WorkshopLedger.WhisperCost), "0.1 left is not a hint");
        Assert.AreEqual(0.1f, gadget.charge, 1e-4f, "a refused hint costs nothing");
        var crew = Crew();
        crew.gadgets.Add(gadget);
        int savings = crew.savings;
        WorkshopLedger.Charge(crew, PartCatalog.ChargeSeconds);
        Assert.AreEqual(1f, gadget.charge);
        Assert.AreEqual(savings, crew.savings, "charging is free");
    }

    [Test]
    public void ASaveFromBeforeTheWorkshopLoadsWithEverythingItHad()
    {
        const string old = "{\"day\":4,\"savings\":310,\"rentDue\":840,\"daysLeft\":4,\"introSeen\":true,\"billReceived\":true,\"hired\":true,\"cvName\":\"Sam\",\"claims\":[\"claim_technical\"],\"shiftsCompleted\":3,\"job\":\"call_center\",\"languageProfiles\":[{\"playerId\":\"peer_1\",\"languages\":[\"es\"]}]}";
        var crew = CrewState.FromJson(old);
        Assert.AreEqual(310, crew.savings);
        Assert.AreEqual(3, crew.shiftsCompleted);
        Assert.IsTrue(crew.HasClaim(Claims.Technical));
        CollectionAssert.AreEqual(new[] { "es" }, crew.LanguagesOf("peer_1"));
        Assert.IsNotNull(crew.parts);
        Assert.IsNotNull(crew.orders);
        Assert.IsNotNull(crew.gadgets);
        Assert.IsFalse(crew.build.Active);
        Assert.IsFalse(crew.starterKitGiven);
    }

    [Test]
    public void ReloadingMidOrderMidBuildAndPackedLosesAndDuplicatesNothing()
    {
        var recipe = PartCatalog.Recipe(PartCatalog.WhisperBox);
        var crew = WithParts(recipe.Parts);
        crew.savings = 100;
        WorkshopLedger.Checkout(crew, "Q-1", Cart((PartCatalog.Sensor, 1)), out _);   // paid, on its way
        WorkshopLedger.Checkout(crew, "Q-2", Cart((PartCatalog.Motor, 1)), out _);
        WorkshopLedger.Deliver(crew, "Q-2");                                          // at the door
        WorkshopLedger.BeginBuild(crew, recipe.Id, "peer_1");
        crew.build.SetPlaced(AssemblyBench.MatSocket, PartCatalog.Casing);
        crew.build.SetPlaced(AssemblyBench.BoardSocket, PartCatalog.Board);
        crew.build.screws = 1;
        crew.gadgets.Add(new GadgetInstance { id = "G-9", recipe = PartCatalog.SteadyHand, charge = 0.4f });
        WorkshopLedger.Equip(crew, "G-9", "peer_1");

        var back = CrewState.FromJson(JsonUtility.ToJson(crew));
        Assert.AreEqual(crew.savings, back.savings);
        Assert.AreEqual(PartOrder.Paid, back.orders.Find(o => o.id == "Q-1").state, "still on its way: delivered again after the reload");
        Assert.AreEqual(PartOrder.Delivered, back.orders.Find(o => o.id == "Q-2").state, "still waiting by the door");
        Assert.IsTrue(back.build.Active);
        Assert.AreEqual("", back.build.editor, "nobody holds the bench after a reload");
        Assert.AreEqual(PartCatalog.Board, back.build.PartIn(AssemblyBench.BoardSocket));
        Assert.AreEqual(1, back.build.screws);
        Assert.AreEqual(recipe.Parts.Length, back.build.reserved.Count);
        Assert.AreEqual(0, back.parts.Sum(s => s.count));
        Assert.AreEqual("peer_1", back.gadgets.Single().equippedBy);
        Assert.AreEqual(0.4f, back.gadgets.Single().charge, 1e-4f);
        // Unpacking the waiting parcel after the reload still works once.
        Assert.IsTrue(WorkshopLedger.OpenParcel(back, "Q-2"));
        Assert.IsFalse(WorkshopLedger.OpenParcel(back, "Q-2"));
    }

    [Test]
    public void RentIsPaidFromSavingsAndTheNextOneIsDueAWeekLater()
    {
        var crew = Crew(900);
        crew.daysLeft = 1;
        Assert.IsTrue(WorkshopLedger.PayRent(crew));
        Assert.AreEqual(60, crew.savings);
        Assert.AreEqual(8, crew.daysLeft);
        Assert.AreEqual(1, crew.rentPaid);
        Assert.IsFalse(WorkshopLedger.PayRent(crew), "can't pay what you don't have");
        Assert.AreEqual(60, crew.savings);
    }

    [Test]
    public void LateRentCanBePushedOnceForAFee()
    {
        var crew = Crew(300);
        crew.daysLeft = 3;
        Assert.IsFalse(WorkshopLedger.AskForExtension(crew), "only when it's due");
        crew.daysLeft = 0;
        Assert.IsTrue(WorkshopLedger.RentOverdue(crew));
        Assert.IsTrue(WorkshopLedger.AskForExtension(crew));
        Assert.AreEqual(CrewState.StartingRent + WorkshopLedger.ExtensionFee, crew.rentDue);
        Assert.AreEqual(WorkshopLedger.ExtensionDays, crew.daysLeft);
        Assert.IsFalse(WorkshopLedger.RentOverdue(crew));
        crew.daysLeft = 0;
        Assert.IsFalse(WorkshopLedger.AskForExtension(crew), "once a week");
    }

    [Test]
    public void TheFirstGadgetCostsAboutAQuarterOfADecentShift()
    {
        // A decent shift: two lies, base pay, no bonus or deduction.
        int wedding = CvLies.Pay(CrewState.WeddingShiftPay, 2), calls = CvLies.Pay(CrewState.CallCenterShiftPay, 2);
        float whisper = PartCatalog.PriceOf(PartCatalog.Recipe(PartCatalog.WhisperBox)) / (float)calls;
        float steady = PartCatalog.PriceOf(PartCatalog.Recipe(PartCatalog.SteadyHand)) / (float)wedding;
        Assert.That(whisper, Is.InRange(0.2f, 0.32f), $"WhisperBox is {whisper:P0} of a call center shift");
        Assert.That(steady, Is.InRange(0.2f, 0.32f), $"SteadyHand is {steady:P0} of a wedding shift");
    }
}
