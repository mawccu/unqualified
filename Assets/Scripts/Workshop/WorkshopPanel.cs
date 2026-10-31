using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The workshop's paper interfaces, drawn over the world while you stand at
/// them: the blueprint board over the bench (pick a recipe, see what is missing,
/// start or cancel a build, take a gadget apart) and the charging rack (charge,
/// which job each gadget is for, what gives it away, pack it for the next
/// shift). Opening one frees the mouse and holds the player still; Esc or Close
/// gives control back.
/// </summary>
public class WorkshopPanel : MonoBehaviour
{
    static readonly Color Paper = new(0.96f, 0.94f, 0.88f);
    static readonly Color Ink = new(0.15f, 0.15f, 0.17f);
    static readonly Color Faint = new(0.42f, 0.42f, 0.45f);
    static readonly Color Good = new(0.18f, 0.55f, 0.24f);
    static readonly Color Alarm = new(0.75f, 0.15f, 0.12f);
    static readonly Color Orange = new(0.95f, 0.55f, 0.12f);

    [SerializeField] Hud hud;

    public static WorkshopPanel Instance { get; private set; }
    public bool IsOpen => _window != null && _window.style.display == DisplayStyle.Flex;
    public string Showing { get; private set; } = "";

    VisualElement _window, _sheet;
    ScrollView _scroll;
    PlayerActor _player;
    Action _redraw;
    float _refresh;
    string _confirmDismantle = "";
    readonly System.Collections.Generic.List<(VisualElement fill, Label text, GadgetInstance gadget)> _bars = new();

    public void Configure(Hud owner) => hud = owner;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!IsOpen)
            return;
        _refresh += Time.unscaledDeltaTime;
        if (_refresh > 0.25f)
        {
            // Charge bars move while you watch (only the bars: redrawing would eat clicks).
            _refresh = 0f;
            foreach (var (fill, text, gadget) in _bars)
            {
                fill.style.width = Length.Percent(Mathf.Max(2f, gadget.charge * 100f));
                fill.style.backgroundColor = Color.Lerp(Alarm, Good, gadget.charge);
                text.text = ChargeText(gadget);
            }
        }
    }

    void Build()
    {
        _window = new VisualElement();
        Hud.Fill(_window);
        _window.style.alignItems = Align.Center;
        _window.style.justifyContent = Justify.Center;
        _window.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
        hud.Overlay.Add(_window);
        var frame = new VisualElement();
        frame.style.width = 760;
        frame.style.maxWidth = Length.Percent(94);
        frame.style.maxHeight = Length.Percent(88);
        frame.style.backgroundColor = Paper;
        Hud.Round(frame, 4);
        Hud.SetBorder(frame, 8, new Color(0.55f, 0.42f, 0.28f));
        frame.style.overflow = Overflow.Hidden;
        _window.Add(frame);
        _scroll = new ScrollView(ScrollViewMode.Vertical);
        _scroll.name = "workshop-panel-scroll";
        _scroll.style.flexGrow = 1;
        _scroll.style.minHeight = 0;
        frame.Add(_scroll);
        _sheet = _scroll.contentContainer;
        Hud.Pad(_sheet, 18, 24);
        _window.style.display = DisplayStyle.None;
    }

    void Open(string what, PlayerActor player, Action redraw)
    {
        if (_window == null)
            Build();
        _player = player;
        Showing = what;
        _redraw = redraw;
        _confirmDismantle = "";
        _window.style.display = DisplayStyle.Flex;
        hud.FocusMode = true;
        player.Lock();
        player.WantsCursor = true;
        player.EscapeHandler = () =>
        {
            Close();
            return true;
        };
        hud.SetCrosshairVisible(false);
        hud.SetPrompt("");
        GameSession.Instance.RefreshCursor();
        _scroll.scrollOffset = Vector2.zero;
        redraw();
    }

    public void Close()
    {
        if (!IsOpen)
            return;
        _window.style.display = DisplayStyle.None;
        hud.FocusMode = false;
        Showing = "";
        var player = _player;
        _player = null;
        if (player == null)
            return;
        player.WantsCursor = false;
        player.EscapeHandler = null;
        player.Unlock();
        hud.SetCrosshairVisible(true);
        GameSession.Instance.RefreshCursor();
    }

    // The blueprint board --------------------------------------------------------------

    public void OpenBench(PlayerActor player) => Open("bench", player, DrawBench);

    void DrawBench()
    {
        _sheet.Clear();
        _bars.Clear();
        var crew = CrewState.Current;
        var bench = AssemblyBench.Instance;
        Header("BLUEPRINTS", "Pick something to build. Starting takes its parts out of storage and lays them in the tray.");
        Line(StorageLine(crew), 13, Faint).style.marginBottom = 8;

        if (bench != null && bench.HasBuild)
        {
            var recipe = bench.Recipe;
            var card = Card();
            Line($"<b>On the mat: {recipe.Name}</b>", 18, Ink, card);
            foreach (var (done, text) in bench.Steps())
                Line((done ? "<color=#2E8B3E>✓</color>  " : "○  ") + text, 14, done ? Faint : Ink, card);
            Line("Parts on the mat stay there if you walk away, leave the flat or quit.", 12, Faint, card).style.marginTop = 6;
            var row = Row(card);
            Button(row, "Back to work", true, Close).name = "bench-close";
            var cancel = Button(row, "Cancel the build (parts back to storage)", false, () =>
            {
                bench.Cancel();
                DrawBench();
            });
            cancel.name = "bench-cancel";
            cancel.style.marginLeft = 8;
        }
        else
        {
            foreach (var recipe in PartCatalog.Recipes)
            {
                var card = Card();
                var needs = WorkshopLedger.Needs(crew, recipe);
                bool ready = needs.All(n => n.have >= n.need);
                Line($"<b>{recipe.Name}</b>   <size=13><color=#6B6B70>for {PartCatalog.JobLabel(recipe.Job)}</color></size>", 19, Ink, card);
                Line(recipe.Blurb, 13, Ink, card).style.marginBottom = 4;
                foreach (var (part, need, have) in needs)
                {
                    var def = PartCatalog.Part(part);
                    string port = def.Port != "" ? $"  → {PartCatalog.PortLabel(def.Port)} port" : "";
                    Line((have >= need ? "<color=#2E8B3E>✓</color>  " : "<color=#BF2620>✗</color>  ") + $"{def.Name}{port}   <color=#6B6B70>({have} in storage)</color>", 13, Ink, card);
                }
                var row = Row(card);
                var start = Button(row, ready ? $"Build a {recipe.Name}" : "Missing parts (Questionable Components sells them)", ready, () =>
                {
                    var result = bench.Begin(recipe.Id, _player);
                    if (result == BuildResult.Ok)
                    {
                        hud.ShowToast($"Parts for the {recipe.Name} are in the tray. Casing on the mat first.");
                        Close();
                    }
                    else
                        DrawBench();
                });
                start.name = "bench-start-" + recipe.Id;
                start.SetEnabled(ready && bench != null);
            }
        }

        if (crew.gadgets.Count > 0)
        {
            Section("TAKE A GADGET APART");
            foreach (var gadget in crew.gadgets)
            {
                var recipe = PartCatalog.Recipe(gadget.recipe);
                var row = Row(Card());
                Line($"<b>{recipe.Name}</b> {gadget.id}" + (gadget.Equipped ? "   <color=#BF5A12>(packed: unpack it at the rack first)</color>" : ""), 14, Ink, row).style.flexGrow = 1;
                if (gadget.Equipped)
                    continue;
                if (_confirmDismantle == gadget.id)
                {
                    string parts = string.Join(", ", WorkshopLedger.Salvage(gadget).Select(p => PartCatalog.Part(p).Name.ToLowerInvariant()));
                    Line($"You get back: {parts}", 12, Faint, row).style.maxWidth = 260;
                    var yes = Button(row, "Take it apart", true, () =>
                    {
                        if (WorkshopLedger.Dismantle(crew, gadget.id) == BuildResult.Ok)
                        {
                            crew.Save();
                            GadgetRack.Instance?.Refresh();
                            PartShelf.Instance?.Refresh();
                            hud.ShowToast($"{recipe.Name} taken apart. The parts are back in the bins.");
                        }
                        _confirmDismantle = "";
                        DrawBench();
                    });
                    yes.name = "dismantle-yes-" + gadget.id;
                }
                else
                    Button(row, "Dismantle…", false, () => { _confirmDismantle = gadget.id; DrawBench(); }).name = "dismantle-" + gadget.id;
            }
        }
        Button(Row(_sheet), "Close  (Esc)", false, Close).style.marginTop = 10;
    }

    // The charging rack -----------------------------------------------------------------

    public void OpenRack(PlayerActor player) => Open("rack", player, DrawRack);

    static string ChargeText(GadgetInstance gadget) => gadget.charge >= 1f ? "Charge: full" : $"Charge: {gadget.charge * 100f:0}%, charging";

    void DrawRack()
    {
        _sheet.Clear();
        _bars.Clear();
        var crew = CrewState.Current;
        string me = _player != null ? _player.ActorId : GameSession.OfflineActor;
        Header("CHARGING RACK", $"One gadget each, packed in the bag by the door. Tonight: {Jobs.Title(crew.Job)} ({PartCatalog.JobLabel(crew.Job)}). Charging is free.");
        if (crew.gadgets.Count == 0)
        {
            Line("Nothing here yet. Build something at the bench.", 15, Faint);
        }
        foreach (var gadget in crew.gadgets)
        {
            var recipe = PartCatalog.Recipe(gadget.recipe);
            var card = Card();
            card.name = "rack-" + gadget.id;
            var top = Row(card);
            Line($"<b>{recipe.Name}</b>  <size=12><color=#6B6B70>{gadget.id}</color></size>", 19, Ink, top).style.flexGrow = 1;
            string where = gadget.equippedBy == me ? "<color=#2E8B3E><b>PACKED</b> for your next shift</color>"
                : gadget.Equipped ? "<color=#BF5A12>packed by a teammate</color>" : "on the rack";
            Line(where, 13, Ink, top);
            // Charge bar.
            var track = new VisualElement();
            track.style.height = 10;
            track.style.marginTop = 6;
            track.style.backgroundColor = new Color(0f, 0f, 0f, 0.12f);
            Hud.Round(track, 5);
            var fill = new VisualElement();
            fill.style.height = 10;
            fill.style.width = Length.Percent(Mathf.Max(2f, gadget.charge * 100f));
            fill.style.backgroundColor = Color.Lerp(Alarm, Good, gadget.charge);
            Hud.Round(fill, 5);
            track.Add(fill);
            card.Add(track);
            _bars.Add((fill, Line(ChargeText(gadget), 12, Faint, card), gadget));
            bool fits = recipe.Job == crew.Job;
            Line(fits ? $"<color=#2E8B3E>Useful tonight</color> at {PartCatalog.JobLabel(recipe.Job)}."
                : $"<color=#BF2620>Useless tonight:</color> it's for {PartCatalog.JobLabel(recipe.Job)} and you're hired for {PartCatalog.JobLabel(crew.Job)}.", 13, Ink, card).style.marginTop = 4;
            Line("<b>Use:</b> " + recipe.Use, 13, Ink, card);
            Line("<b>Gives you away:</b> " + recipe.Risk, 13, Ink, card);
            if (gadget.timesSeen > 0)
                Line($"Spotted at work {gadget.timesSeen} time(s) so far.", 12, Alarm, card);
            var row = Row(card);
            if (gadget.equippedBy == me)
            {
                Button(row, "Unpack it", false, () =>
                {
                    WorkshopLedger.Unequip(crew, gadget.id);
                    crew.Save();
                    GadgetRack.Instance?.Refresh();
                    DrawRack();
                }).name = "rack-unpack-" + gadget.id;
            }
            else
            {
                var pack = Button(row, "Pack it for the next shift", true, () =>
                {
                    var result = WorkshopLedger.Equip(crew, gadget.id, me);
                    if (result == BuildResult.Ok)
                    {
                        crew.Save();
                        GadgetRack.Instance?.Refresh();
                        hud.ShowToast(fits ? $"{recipe.Name} packed. The gadget key ({GameSettings.GadgetKeyName}) uses it at work."
                            : $"{recipe.Name} packed, but it won't do anything at {PartCatalog.JobLabel(crew.Job)}.");
                    }
                    DrawRack();
                });
                pack.name = "rack-pack-" + gadget.id;
                pack.SetEnabled(!gadget.Equipped);
            }
        }
        Button(Row(_sheet), "Close  (Esc)", false, Close).style.marginTop = 10;
    }

    // Small parts ------------------------------------------------------------------------

    static string StorageLine(CrewState crew)
    {
        var stocked = crew.parts.Where(s => s.count > 0).Select(s => $"{s.count} × {PartCatalog.Part(s.part)?.Name.ToLowerInvariant()}").ToList();
        return "In storage: " + (stocked.Count == 0 ? "nothing" : string.Join(", ", stocked));
    }

    void Header(string title, string sub)
    {
        var t = Line(title, 13, Faint);
        t.style.letterSpacing = 3;
        t.style.unityFontStyleAndWeight = FontStyle.Bold;
        Line(sub, 15, Ink).style.marginBottom = 8;
    }

    void Section(string text)
    {
        var t = Line(text, 12, Faint);
        t.style.letterSpacing = 2;
        t.style.unityFontStyleAndWeight = FontStyle.Bold;
        t.style.marginTop = 12;
        t.style.marginBottom = 4;
    }

    VisualElement Card()
    {
        var card = new VisualElement();
        card.style.backgroundColor = Color.white;
        Hud.SetBorder(card, 1, new Color(0f, 0f, 0f, 0.12f));
        Hud.Round(card, 4);
        Hud.Pad(card, 10, 14);
        card.style.marginBottom = 8;
        _sheet.Add(card);
        return card;
    }

    static VisualElement Row(VisualElement parent)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.flexWrap = Wrap.Wrap;
        row.style.marginTop = 6;
        parent.Add(row);
        return row;
    }

    Label Line(string text, int size, Color colour, VisualElement parent = null)
    {
        var label = Hud.Text(parent ?? _sheet, text, size, colour);
        label.style.whiteSpace = WhiteSpace.Normal;
        return label;
    }

    static Button Button(VisualElement parent, string text, bool primary, Action onClick)
    {
        var b = new Button(onClick) { text = text };
        b.style.fontSize = 14;
        b.style.unityFontStyleAndWeight = FontStyle.Bold;
        b.style.backgroundColor = primary ? Orange : Color.white;
        b.style.color = primary ? Color.white : Ink;
        Hud.SetBorder(b, 1, primary ? new Color(0.8f, 0.45f, 0.08f) : new Color(0f, 0f, 0f, 0.25f));
        Hud.Round(b, 5);
        Hud.Pad(b, 6, 12);
        b.style.marginLeft = 0;
        parent.Add(b);
        return b;
    }
}
