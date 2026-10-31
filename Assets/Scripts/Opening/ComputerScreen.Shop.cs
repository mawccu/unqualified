using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The computer's other two sites. Questionable Components sells the workshop's
/// parts: pick quantities into a cart (nothing is charged), review it with the
/// rent in view, then pay once. The cart carries an order id from the moment it
/// is made, so a double click or a second request cannot buy twice. FlatPay is
/// where the rent actually gets paid.
/// </summary>
public partial class ComputerScreen
{
    const string SiteJobs = "jobs", SiteShop = "shop", SiteRent = "rent";
    static readonly Color ShopBrand = new(0.16f, 0.2f, 0.18f);
    static readonly Color ShopAccent = new(0.55f, 0.9f, 0.35f);
    static readonly Color RentBrand = new(0.38f, 0.2f, 0.55f);
    static readonly Color Good = new(0.18f, 0.55f, 0.24f);

    /// The cart survives getting up from the desk, like the CV draft.
    public static class Cart
    {
        public static readonly Dictionary<string, int> Lines = new();
        /// Made with the cart; a new one only after this one is paid for.
        public static string OrderId = "";
        public static bool Empty => Lines.Values.Sum() == 0;

        public static List<PartStack> Stacks() => Lines.Where(l => l.Value > 0).Select(l => new PartStack(l.Key, l.Value)).ToList();

        public static void Set(string part, int count)
        {
            if (string.IsNullOrEmpty(OrderId))
                OrderId = WorkshopLedger.NewOrderId();
            Lines[part] = Mathf.Max(0, count);
        }

        public static void Clear()
        {
            Lines.Clear();
            OrderId = "";
        }
    }

    static string _lastSite = SiteJobs;
    string _site = "";

    public void ShowSite(string site)
    {
        switch (site)
        {
            case SiteShop: ShowShop(); break;
            case SiteRent: ShowRent(); break;
            default: ShowListings(); break;
        }
    }

    void SetSite(string site)
    {
        _lastSite = site;
        if (_site == site)
            return;
        _site = site;
        foreach (var pair in _bookmarks)
        {
            bool on = pair.Key == site;
            pair.Value.style.backgroundColor = on ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            pair.Value.style.unityFontStyleAndWeight = on ? FontStyle.Bold : FontStyle.Normal;
            pair.Value.style.color = Body;
        }
        _header.Clear();
        switch (site)
        {
            case SiteShop:
                _url.text = "http://questionable-components.shop/parts?ask=no";
                _header.style.backgroundColor = ShopBrand;
                Hud.Text(_header, "<b>Questionable</b> <color=#8CE65A>Components</color>", 32, Color.white).style.flexGrow = 1;
                Hud.Text(_header, "Parts that fell off something. No refunds, no questions.", 15, ShopAccent);
                break;
            case SiteRent:
                _url.text = "https://flatpay.example/flat-4b";
                _header.style.backgroundColor = RentBrand;
                Hud.Text(_header, "<b>FlatPay</b>", 32, Color.white).style.flexGrow = 1;
                Hud.Text(_header, "Pay your landlord. He is watching.", 15, new Color(0.95f, 0.85f, 1f));
                break;
            default:
                _url.text = "http://www.jobjob.biz/listings?sort=desperate";
                _header.style.backgroundColor = Brand;
                Hud.Text(_header, "<b>JobJob</b><color=#FFD933>.biz</color>", 34, Color.white).style.flexGrow = 1;
                Hud.Text(_header, "Jobs for everyone*", 16, Yellow);
                break;
        }
    }

    /// Cash, rent and what is left after it: the same numbers on every page that spends.
    VisualElement MoneyStrip(VisualElement parent, int spending)
    {
        var crew = CrewState.Current;
        var strip = Row(parent);
        strip.style.flexWrap = Wrap.Wrap;
        strip.style.backgroundColor = new Color(1f, 0.97f, 0.86f);
        Hud.SetBorder(strip, 1, new Color(0.85f, 0.7f, 0.3f));
        Hud.Round(strip, 5);
        Hud.Pad(strip, 8, 14);
        strip.style.marginBottom = 14;
        int after = crew.savings - spending;
        string rent = crew.billReceived ? $"Rent <b>${crew.rentDue}</b> due by <b>Day {crew.day + crew.daysLeft - 1}</b>" : "No rent bill yet";
        string left = spending > 0 ? $"   ·   after this order: <b>${after}</b>" : "";
        string warn = crew.billReceived && after < crew.rentDue ? $"   ·   <color=#BF2620><b>${crew.rentDue - after} short of the rent</b></color>" : "";
        Hud.Text(strip, $"Cash: <b>${crew.savings}</b>   ·   {rent}{left}{warn}", 15, Body);
        return strip;
    }

    // The parts shop -------------------------------------------------------------------------

    void ShowShop()
    {
        // Changing a quantity redraws the page: keep it scrolled where it was.
        var keep = _site == SiteShop ? _pageScroll.scrollOffset : Vector2.zero;
        SetSite(SiteShop);
        ClearPage();
        if (keep != Vector2.zero)
            _pageScroll.schedule.Execute(() => _pageScroll.scrollOffset = keep).StartingIn(0);
        var crew = CrewState.Current;
        var title = Hud.Text(_content, "Parts", 26, Body);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        Hud.Text(_content, $"Delivered to your front door in about {PartCatalog.DeliverySeconds:0} seconds. We don't ask what it's for. Cash is the crew's: the same money pays the rent.", 14, Faint).style.marginBottom = 10;
        MoneyStrip(_content, Cart.Empty ? 0 : WorkshopLedger.Total(Cart.Stacks()));

        var columns = Row(_content);
        columns.style.alignItems = Align.FlexStart;
        columns.style.flexWrap = Wrap.Wrap;
        columns.style.flexShrink = 0;

        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        grid.style.width = 760;
        grid.style.maxWidth = Length.Percent(100);
        grid.style.flexShrink = 1;
        columns.Add(grid);
        foreach (var part in PartCatalog.Parts)
            ProductCard(grid, part, crew);

        var side = new VisualElement();
        side.style.width = 360;
        side.style.maxWidth = Length.Percent(100);
        side.style.flexGrow = 1;
        side.style.marginLeft = 12;
        columns.Add(side);
        CartPanel(side, crew);
        Blueprints(side, crew);
        OrdersPanel(side, crew);
    }

    void ProductCard(VisualElement parent, PartDef part, CrewState crew)
    {
        var card = new VisualElement();
        card.name = "product-" + part.Id;
        card.style.width = 240;
        card.style.marginRight = 10;
        card.style.marginBottom = 10;
        card.style.backgroundColor = Color.white;
        Hud.SetBorder(card, 1, new Color(0f, 0f, 0f, 0.12f));
        Hud.Round(card, 6);
        Hud.Pad(card, 10, 12);
        parent.Add(card);

        var top = Row(card);
        var icon = new VisualElement();
        icon.style.width = 38;
        icon.style.height = 38;
        icon.style.backgroundColor = part.Colour;
        icon.style.marginRight = 10;
        Hud.Round(icon, part.Port == PartCatalog.PortSensor ? 19 : 5);
        var glyph = Hud.Text(icon, part.Port switch { PartCatalog.PortSensor => "●", PartCatalog.PortMotor => "■", PartCatalog.PortAudio => "▲", _ => part.Name.Substring(0, 1) }, 18, Color.white);
        glyph.style.unityTextAlign = TextAnchor.MiddleCenter;
        glyph.style.flexGrow = 1;
        top.Add(icon);
        var name = Hud.Text(top, $"<b>{part.Name}</b>\n<color=#BF5A12><b>${part.Price}</b></color>", 15, Body);
        name.style.flexShrink = 1;
        name.style.whiteSpace = WhiteSpace.Normal;

        var blurb = Hud.Text(card, part.Blurb, 12, Faint);
        blurb.style.whiteSpace = WhiteSpace.Normal;
        blurb.style.marginTop = 6;
        int owned = WorkshopLedger.Count(crew, part.Id);
        int stock = WorkshopLedger.StockLeft(crew, part.Id);
        Hud.Text(card, $"In your storage: <b>{owned}</b>   ·   stock today: {stock}", 12, Body).style.marginTop = 4;
        Hud.Text(card, "Used in: " + string.Join(", ", PartCatalog.UsedIn(part.Id)), 12, Body);

        var qty = Row(card);
        qty.style.marginTop = 6;
        Cart.Lines.TryGetValue(part.Id, out int inCart);
        var minus = SmallButton(qty, "−", () => { Cart.Set(part.Id, inCart - 1); ShowShop(); });
        minus.name = $"cart-minus-{part.Id}";
        minus.SetEnabled(inCart > 0);
        var count = Hud.Text(qty, $"{inCart} in cart", 14, Body);
        count.style.marginLeft = 8;
        count.style.marginRight = 8;
        var plus = SmallButton(qty, "+", () => { Cart.Set(part.Id, inCart + 1); ShowShop(); });
        plus.name = $"cart-plus-{part.Id}";
        plus.SetEnabled(inCart < stock);
    }

    void CartPanel(VisualElement parent, CrewState crew)
    {
        var panel = Panel(parent, "Your cart");
        var lines = Cart.Stacks();
        if (lines.Count == 0)
        {
            Hud.Text(panel, "Empty. Nothing is charged until you check out.", 13, Faint);
            return;
        }
        foreach (var line in lines)
        {
            var part = PartCatalog.Part(line.part);
            Hud.Text(panel, $"{line.count} × {part.Name}   <b>${part.Price * line.count}</b>", 14, Body);
        }
        int total = WorkshopLedger.Total(lines);
        int after = crew.savings - total;
        Hud.Text(panel, $"<b>Total ${total}</b>", 17, Body).style.marginTop = 6;
        Hud.Text(panel, $"Cash ${crew.savings} → <b>${after}</b> after paying", 13, after < 0 ? Alarm : Body);
        if (crew.billReceived)
            Hud.Text(panel, after >= crew.rentDue
                ? $"<color=#2E8B3E>Still covers the rent (${crew.rentDue}).</color>"
                : $"<color=#BF2620>Leaves you ${crew.rentDue - after} short of the ${crew.rentDue} rent (due Day {crew.day + crew.daysLeft - 1}).</color>", 13, Body);
        var row = Row(panel);
        row.style.marginTop = 8;
        var review = Button(row, "Review and pay", true, ShowCheckout);
        review.name = "cart-review";
        review.SetEnabled(after >= 0);
        Button(row, "Empty cart", false, () => { Cart.Clear(); ShowShop(); }).style.marginLeft = 6;
        if (after < 0)
            Hud.Text(panel, "Not enough cash for this cart.", 13, Alarm);
    }

    /// What you know how to build, and what is still missing from storage for it.
    void Blueprints(VisualElement parent, CrewState crew)
    {
        var panel = Panel(parent, "Blueprints you know");
        foreach (var recipe in PartCatalog.Recipes)
        {
            var needs = WorkshopLedger.Needs(crew, recipe);
            var missing = needs.Where(n => n.have < n.need).ToList();
            string state = missing.Count == 0 ? "<color=#2E8B3E>all parts in storage</color>" : "missing: " + string.Join(", ", missing.Select(m => PartCatalog.Part(m.part).Name));
            Hud.Text(panel, $"<b>{recipe.Name}</b> (for {PartCatalog.JobLabel(recipe.Job)}, ${PartCatalog.PriceOf(recipe)} of parts)\n<size=12>{state}</size>", 14, Body).style.marginTop = 4;
            if (missing.Count > 0)
            {
                var add = Button(panel, "Add the missing parts to the cart", false, () =>
                {
                    foreach (var m in missing)
                    {
                        Cart.Lines.TryGetValue(m.part, out int n);
                        Cart.Set(m.part, Mathf.Max(n, m.need - m.have));
                    }
                    ShowShop();
                });
                add.name = "cart-add-" + recipe.Id;
                add.style.fontSize = 12;
                add.style.alignSelf = Align.FlexStart;
            }
        }
    }

    void OrdersPanel(VisualElement parent, CrewState crew)
    {
        var mine = crew.orders.Where(o => o.id != WorkshopLedger.StarterOrder || o.state != PartOrder.Opened).Reverse().Take(5).ToList();
        if (mine.Count == 0)
            return;
        var panel = Panel(parent, "Your orders");
        foreach (var order in mine)
        {
            string state = order.state switch
            {
                PartOrder.Paid => "on its way",
                PartOrder.Delivered => "<color=#BF5A12><b>at your front door</b></color>",
                _ => "unpacked",
            };
            Hud.Text(panel, $"{order.id}  ·  {order.lines.Sum(l => l.count)} part(s), ${order.total}  ·  {state}", 13, Body);
        }
    }

    void ShowCheckout()
    {
        ClearPage();
        var crew = CrewState.Current;
        var lines = Cart.Stacks();
        if (lines.Count == 0)
        {
            ShowShop();
            return;
        }
        int total = WorkshopLedger.Total(lines);
        var title = Hud.Text(_content, "Check out", 26, Body);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        MoneyStrip(_content, total);
        var panel = Panel(_content, $"Order {Cart.OrderId}");
        panel.style.maxWidth = 620;
        foreach (var line in lines)
        {
            var part = PartCatalog.Part(line.part);
            Hud.Text(panel, $"{line.count} × {part.Name}   ${part.Price} each   <b>${part.Price * line.count}</b>", 15, Body);
        }
        Hud.Text(panel, $"<b>Total: ${total}</b>", 20, Body).style.marginTop = 8;
        Hud.Text(panel, $"Cash now ${crew.savings}  →  after paying <b>${crew.savings - total}</b>", 15, Body);
        if (crew.billReceived)
            Hud.Text(panel, $"Rent: ${crew.rentDue}, due by Day {crew.day + crew.daysLeft - 1}. This is the same money.", 14, crew.savings - total >= crew.rentDue ? Faint : Alarm);
        Hud.Text(panel, "Delivered to your front door while you're home. Parcels wait there if you go out.", 13, Faint).style.marginTop = 4;
        var row = Row(panel);
        row.style.marginTop = 12;
        Button pay = null;
        pay = Button(row, $"Pay ${total} now", true, () =>
        {
            pay.SetEnabled(false);   // one click is one order
            PlaceOrder();
        });
        pay.name = "checkout-pay";
        Button(row, "Back to the shop", false, ShowShop).style.marginLeft = 8;
    }

    /// The single place money leaves for parts.
    void PlaceOrder()
    {
        _desk?.PressEnter();
        var crew = CrewState.Current;
        var result = WorkshopLedger.Checkout(crew, Cart.OrderId, Cart.Stacks(), out var order);
        if (result == ShopResult.Ok)
        {
            crew.Save();
            WorkshopRoom.Instance?.OrderPlaced(order);
        }
        ClearPage();
        var done = new VisualElement();
        done.style.alignSelf = Align.Center;
        done.style.alignItems = Align.Center;
        done.style.marginTop = 120;
        _content.Add(done);
        string headline = result switch
        {
            ShopResult.Ok => "Order placed.",
            ShopResult.AlreadyPlaced => "Already ordered.",
            ShopResult.NotEnoughMoney => "Card declined.",
            ShopResult.OutOfStock => "Sold out.",
            _ => "Something went wrong.",
        };
        string detail = result switch
        {
            ShopResult.Ok => $"{order.id}: ${order.total} paid. It will be at your front door in about {PartCatalog.DeliverySeconds:0} seconds.",
            ShopResult.AlreadyPlaced => $"{order.id} went through the first time. You were not charged again.",
            ShopResult.NotEnoughMoney => "Not enough cash. Nothing was charged.",
            ShopResult.OutOfStock => "Not that many left today. Nothing was charged.",
            _ => "Nothing was charged.",
        };
        var title = Hud.Text(done, headline, 38, result == ShopResult.Ok || result == ShopResult.AlreadyPlaced ? Good : Alarm);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        Hud.Text(done, detail, 17, Body).style.marginTop = 8;
        Hud.Text(done, $"Cash left: ${crew.savings}", 15, Faint).style.marginTop = 6;
        if (result == ShopResult.Ok || result == ShopResult.AlreadyPlaced)
            Cart.Clear();
        var back = Button(done, "Back to the shop", true, ShowShop);
        back.style.marginTop = 22;
    }

    // Rent -----------------------------------------------------------------------------------

    void ShowRent()
    {
        SetSite(SiteRent);
        ClearPage();
        var crew = CrewState.Current;
        var title = Hud.Text(_content, "Flat 4B: rent", 26, Body);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        if (!crew.billReceived)
        {
            Hud.Text(_content, "No bill on file. Enjoy it while it lasts.", 15, Faint);
            return;
        }
        MoneyStrip(_content, 0);
        var panel = Panel(_content, "This week");
        panel.style.maxWidth = 620;
        bool overdue = WorkshopLedger.RentOverdue(crew);
        Hud.Text(panel, $"Rent owed: <b>${crew.rentDue}</b>", 20, Body);
        Hud.Text(panel, overdue ? "<color=#BF2620><b>Overdue.</b> The landlord wants it before you go anywhere.</color>"
            : crew.daysLeft == 1 ? "<color=#BF5A12><b>Due today</b> (Day " + crew.day + ").</color>"
            : $"Due by <b>Day {crew.day + crew.daysLeft - 1}</b> ({crew.daysLeft} days, counting today).", 15, Body);
        if (crew.rentPaid > 0)
            Hud.Text(panel, $"Weeks paid so far: {crew.rentPaid}", 13, Faint);
        var row = Row(panel);
        row.style.marginTop = 12;
        Button pay = null;
        pay = Button(row, $"Pay ${crew.rentDue} now", true, () =>
        {
            pay.SetEnabled(false);
            _desk?.PressEnter();
            if (WorkshopLedger.PayRent(crew))
            {
                crew.Save();
                GameSession.Instance?.Hud.ShowToast("Rent paid. The landlord grunts, somewhere.");
            }
            ShowRent();
        });
        pay.name = "rent-pay";
        pay.SetEnabled(crew.savings >= crew.rentDue);
        if (crew.savings < crew.rentDue)
            Hud.Text(panel, $"You are ${crew.rentDue - crew.savings} short.", 14, Alarm).style.marginTop = 6;
        if (crew.daysLeft <= 1 && crew.extensionsThisWeek == 0)
        {
            var ask = Button(panel, $"Beg for {WorkshopLedger.ExtensionDays} more days (+${WorkshopLedger.ExtensionFee} late fee)", false, () =>
            {
                if (WorkshopLedger.AskForExtension(crew))
                {
                    crew.Save();
                    GameSession.Instance?.Hud.ShowToast("Two more days. He added a fee and a sigh.");
                }
                ShowRent();
            });
            ask.name = "rent-extension";
            ask.style.alignSelf = Align.FlexStart;
            ask.style.marginTop = 10;
        }
    }

    // Small parts ----------------------------------------------------------------------------

    static VisualElement Panel(VisualElement parent, string title)
    {
        var panel = new VisualElement();
        panel.style.backgroundColor = Color.white;
        Hud.SetBorder(panel, 1, new Color(0f, 0f, 0f, 0.12f));
        Hud.Round(panel, 6);
        Hud.Pad(panel, 10, 14);
        panel.style.marginBottom = 10;
        parent.Add(panel);
        var t = Hud.Text(panel, title, 16, Body);
        t.style.unityFontStyleAndWeight = FontStyle.Bold;
        t.style.marginBottom = 4;
        return panel;
    }

    static Button SmallButton(VisualElement parent, string text, System.Action onClick)
    {
        var b = new Button(onClick) { text = text };
        b.style.fontSize = 18;
        b.style.width = 34;
        b.style.height = 30;
        b.style.marginLeft = 0;
        b.style.marginRight = 0;
        b.style.unityFontStyleAndWeight = FontStyle.Bold;
        Hud.Round(b, 5);
        parent.Add(b);
        return b;
    }
}
