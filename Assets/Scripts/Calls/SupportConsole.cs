using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The support desk's screen. Left: the caller and everything said so far, in
/// the customer's own language (Arabic shaped and right to left). Middle: what
/// to say (response intents instead of a microphone) and the phone controls.
/// Right: the company system (order search and actions), the help pages, and
/// the call's notes and transfer history. Every action goes through the shift.
/// </summary>
public class SupportConsole : MonoBehaviour
{
    static readonly Color Page = new(0.95f, 0.96f, 0.97f);
    static readonly Color Bar = new(0.12f, 0.2f, 0.3f);
    static readonly Color Body = new(0.15f, 0.17f, 0.2f);
    static readonly Color Faint = new(0.42f, 0.45f, 0.5f);
    static readonly Color Line = new(0f, 0f, 0f, 0.12f);
    static readonly Color Brand = new(0.1f, 0.45f, 0.75f);
    static readonly Color Good = new(0.18f, 0.6f, 0.3f);
    static readonly Color Danger = new(0.75f, 0.2f, 0.15f);
    static readonly Color HoldColour = new(0.9f, 0.55f, 0.1f);

    [SerializeField] Hud hud;

    VisualElement _window, _transcript, _respond, _system, _popup, _systemBody;
    ScrollView _transcriptScroll;
    Label _state, _patienceLabel, _caller, _callerLanguage, _aid, _message, _clock;
    VisualElement _patienceFill, _callerCard;
    Button _translate;
    TextField _search;
    SupportDesk _desk;
    string _tab = "account";
    string _record = "";
    int _shownLines = -1, _shownCall = -1;
    CallState _shownState;
    bool _shownMuted;
    float _refresh;

    public bool IsOpen => _window != null && _window.style.display == DisplayStyle.Flex;

    public void Configure(Hud owner) => hud = owner;

    static CallCenterShift Shift => CallCenterShift.Instance;
    string Me => _desk != null ? _desk.WorkerId : GameSession.OfflineActor;
    CallCase Call => Shift != null ? Shift.CallOf(Me) : null;

    public void Open(SupportDesk desk)
    {
        _desk = desk;
        if (_window == null)
            Build();
        _window.style.display = DisplayStyle.Flex;
        Shift.Changed += OnChanged;
        Redraw(true);
    }

    public void Close()
    {
        if (_window == null)
            return;
        _window.style.display = DisplayStyle.None;
        ClosePopup();
        if (Shift != null)
            Shift.Changed -= OnChanged;
    }

    /// Esc closes a popup first; returns true when it did something.
    public bool HandleEscape()
    {
        if (_popup != null && _popup.style.display == DisplayStyle.Flex)
        {
            ClosePopup();
            return true;
        }
        return false;
    }

    void OnDestroy()
    {
        if (Shift != null)
            Shift.Changed -= OnChanged;
    }

    void OnChanged(CallCase call)
    {
        if (call == null || call.Owner == Me || call.Id == _shownCall)
            Redraw(false);
    }

    void Update()
    {
        if (!IsOpen)
            return;
        _refresh += Time.unscaledDeltaTime;
        if (_refresh > 0.25f)
        {
            _refresh = 0f;
            Redraw(false);
        }
    }

    // Layout ----------------------------------------------------------------------------------

    void Build()
    {
        _window = new VisualElement();
        Hud.Fill(_window);
        _window.style.alignItems = Align.Center;
        // Kept clear of the bottom of the screen, where colleagues' lines appear as subtitles.
        _window.style.justifyContent = Justify.FlexStart;
        _window.style.paddingTop = Length.Percent(8);
        _window.style.backgroundColor = new Color(0f, 0f, 0f, 0.3f);
        hud.Overlay.Add(_window);

        var screen = new VisualElement();
        screen.style.width = 1280;
        screen.style.height = 760;
        screen.style.maxWidth = Length.Percent(97);
        screen.style.maxHeight = Length.Percent(76);
        screen.style.backgroundColor = Page;
        Hud.Round(screen, 6);
        Hud.SetBorder(screen, 10, new Color(0.1f, 0.1f, 0.11f));
        screen.style.overflow = Overflow.Hidden;
        _window.Add(screen);

        // Top bar: brand, call state, patience, clock, subtitles, stand up.
        var bar = Row(screen);
        bar.style.flexShrink = 0;
        bar.style.backgroundColor = Bar;
        Hud.Pad(bar, 8, 16);
        var brand = Hud.Text(bar, "<b>Brightline</b> Desk", 20, Color.white);
        brand.style.marginRight = 18;
        _state = Hud.Text(bar, "", 15, Color.white);
        _state.style.unityFontStyleAndWeight = FontStyle.Bold;
        Hud.Pad(_state, 4, 12);
        Hud.Round(_state, 4);
        _state.style.marginRight = 14;
        var patience = new VisualElement();
        patience.style.width = 150;
        patience.style.marginRight = 8;
        _patienceLabel = Hud.Text(patience, "", 11, new Color(1f, 1f, 1f, 0.7f));
        var track = new VisualElement();
        track.style.height = 7;
        track.style.backgroundColor = new Color(1f, 1f, 1f, 0.15f);
        Hud.Round(track, 3);
        _patienceFill = new VisualElement();
        _patienceFill.style.height = 7;
        Hud.Round(_patienceFill, 3);
        track.Add(_patienceFill);
        patience.Add(track);
        bar.Add(patience);
        var spacer = new VisualElement();
        spacer.style.flexGrow = 1;
        bar.Add(spacer);
        _clock = Hud.Text(bar, "", 14, new Color(1f, 1f, 1f, 0.75f));
        _clock.style.marginRight = 14;
        _translate = Small(bar, "", () =>
        {
            GameSettings.TranslateCalls = !GameSettings.TranslateCalls;
            _shownLines = -1;
            Redraw(false);
        });
        _translate.style.marginRight = 8;
        Small(bar, "Stand up (Esc)", () => _desk?.StandUp());

        var columns = Row(screen);
        columns.style.flexGrow = 1;
        columns.style.alignItems = Align.Stretch;
        columns.style.minHeight = 0;

        // Left: the caller and the conversation.
        var left = Column(columns, 400);
        _callerCard = new VisualElement();
        Hud.Pad(_callerCard, 12, 16);
        _callerCard.style.borderBottomWidth = 1;
        _callerCard.style.borderBottomColor = Line;
        left.Add(_callerCard);
        _caller = Hud.Text(_callerCard, "", 20, Body);
        _caller.style.unityFontStyleAndWeight = FontStyle.Bold;
        _callerLanguage = Hud.Text(_callerCard, "", 14, Faint);
        // Mixed Arabic and English on one line: shaped, left to right overall.
        _callerLanguage.style.unityTextGenerator = TextGeneratorType.Advanced;
        _aid = Hud.Text(_callerCard, "", 12, Color.white);
        _aid.style.alignSelf = Align.FlexStart;
        _aid.style.marginTop = 6;
        _aid.style.backgroundColor = new Color(0.45f, 0.3f, 0.75f);
        Hud.Pad(_aid, 3, 8);
        Hud.Round(_aid, 4);
        _transcriptScroll = new ScrollView(ScrollViewMode.Vertical);
        _transcriptScroll.style.flexGrow = 1;
        _transcriptScroll.style.minHeight = 0;
        _transcriptScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        left.Add(_transcriptScroll);
        _transcript = _transcriptScroll.contentContainer;
        Hud.Pad(_transcript, 10, 14);
        // Newest line in view: scroll once the new lines have been laid out.
        _transcript.RegisterCallback<GeometryChangedEvent>(_ =>
            _transcriptScroll.scrollOffset = new Vector2(0f, Mathf.Max(0f, _transcript.layout.height - _transcriptScroll.contentViewport.layout.height)));

        // Middle: what to say, and the phone.
        var middle = Column(columns, 330);
        middle.style.backgroundColor = new Color(0.9f, 0.92f, 0.94f);
        var middleScroll = new ScrollView(ScrollViewMode.Vertical);
        middleScroll.style.flexGrow = 1;
        middleScroll.style.minHeight = 0;
        middle.Add(middleScroll);
        _respond = middleScroll.contentContainer;
        Hud.Pad(_respond, 10, 12);

        // Right: the company system.
        var right = Column(columns, 0);
        right.style.flexGrow = 1;
        var tabs = Row(right);
        tabs.style.flexShrink = 0;
        Hud.Pad(tabs, 8, 12);
        tabs.style.borderBottomWidth = 1;
        tabs.style.borderBottomColor = Line;
        foreach (var (id, label) in new[] { ("account", "Account"), ("help", "Help pages"), ("notes", "Notes & history") })
        {
            var tab = Small(tabs, label, () =>
            {
                _tab = id;
                Redraw(true);
            });
            tab.name = "tab-" + id;
            tab.style.marginRight = 6;
        }
        var systemScroll = new ScrollView(ScrollViewMode.Vertical);
        systemScroll.style.flexGrow = 1;
        systemScroll.style.minHeight = 0;
        right.Add(systemScroll);
        _systemBody = systemScroll.contentContainer;
        Hud.Pad(_systemBody, 12, 16);
        _message = Hud.Text(right, "", 14, Body);
        _message.style.flexShrink = 0;
        _message.style.backgroundColor = new Color(1f, 0.95f, 0.8f);
        Hud.Pad(_message, 8, 14);
        _message.style.display = DisplayStyle.None;

        // The transfer list floats over everything.
        _popup = new VisualElement();
        Hud.Fill(_popup);
        _popup.style.alignItems = Align.Center;
        _popup.style.justifyContent = Justify.Center;
        _popup.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
        _popup.style.display = DisplayStyle.None;
        screen.Add(_popup);
    }

    // Drawing ---------------------------------------------------------------------------------

    void Redraw(bool full)
    {
        if (_window == null || Shift == null)
            return;
        var call = Call;
        _clock.text = $"Queue: {Shift.Router.Waiting.Count()}";
        _translate.text = GameSettings.TranslateCalls ? "☑ Translate customer" : "☐ Translate customer";

        // Call state chip and patience.
        (string text, Color colour) state = call == null ? ("NO CALL", new Color(1f, 1f, 1f, 0.15f))
            : call.State switch
            {
                CallState.Ringing => ("RINGING", Danger),
                CallState.OnHold => ("ON HOLD", HoldColour),
                CallState.Active => (call.Muted ? "ON CALL · MUTED" : "ON CALL", Good),
                _ => ("ENDED", new Color(1f, 1f, 1f, 0.15f)),
            };
        _state.text = state.text;
        _state.style.backgroundColor = state.colour;
        float patience = call != null ? Mathf.Clamp01(call.Patience) : 0f;
        _patienceLabel.text = call != null ? $"Customer patience {patience * 100f:0}%" : "";
        _patienceFill.style.width = Length.Percent(patience * 100f);
        _patienceFill.style.backgroundColor = Color.Lerp(Danger, Good, patience);

        bool changed = call == null ? _shownCall != -1 : call.Id != _shownCall || call.Transcript.Count != _shownLines || call.State != _shownState || call.Muted != _shownMuted;
        if (!changed && !full)
            return;
        if (call == null || call.Id != _shownCall)
        {
            _record = "";
            if (_search != null)
                _search.value = "";
        }
        _shownCall = call?.Id ?? -1;
        _shownLines = call?.Transcript.Count ?? -1;
        _shownState = call?.State ?? CallState.Ended;
        _shownMuted = call != null && call.Muted;
        DrawCaller(call);
        DrawTranscript(call);
        DrawRespond(call);
        DrawSystem(call);
    }

    void DrawCaller(CallCase call)
    {
        if (call == null)
        {
            _caller.text = "Waiting for a call";
            Hud.SetLanguage(_caller, CallLanguage.English);
            _callerLanguage.text = Shift.Router.Waiting.Any() ? "Callers are waiting for someone free who speaks their language." : "The lamp on your phone flashes red when one comes in.";
            _aid.style.display = DisplayStyle.None;
            return;
        }
        bool heard = call.State != CallState.Ringing || call.Transcript.Count > 0;
        _caller.text = heard ? call.CustomerName : "Incoming call";
        Hud.SetLanguage(_caller, heard ? call.Language : CallLanguage.English);
        string language = call.LanguageKnown || heard ? $"{CallLanguage.NativeName(call.Language)}  ({CallLanguage.Name(call.Language)})" : "Language unknown (skipped the menu)";
        var from = call.Transfers.LastOrDefault(t => t.Reason != "disconnected");
        if (from != null)
            language += $"\nTransferred from {Shift.Router.Worker(from.From)?.Name}: {from.Reason}";
        if (call.Callback)
            language += "\nCALLBACK: they were told this was fixed";
        _callerLanguage.text = language;
        _aid.text = "PHRASE AID: nobody on shift claimed this language";
        _aid.style.display = call.PhraseAid && call.Language != CallLanguage.English ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void DrawTranscript(CallCase call)
    {
        _transcript.Clear();
        if (call == null)
            return;
        bool translate = GameSettings.TranslateCalls || (call.PhraseAid && call.Language != CallLanguage.English);
        foreach (var line in call.Transcript)
        {
            bool customer = line.Speaker == "customer";
            var bubble = new VisualElement();
            bubble.style.marginBottom = 8;
            bubble.style.maxWidth = Length.Percent(92);
            bubble.style.alignSelf = customer ? Align.FlexStart : Align.FlexEnd;
            bubble.style.backgroundColor = customer ? Color.white : new Color(0.86f, 0.92f, 0.98f);
            Hud.SetBorder(bubble, 1, Line);
            Hud.Round(bubble, 8);
            Hud.Pad(bubble, 7, 10);
            _transcript.Add(bubble);
            string who = customer ? call.CustomerName : line.Agent == Me ? "You" : Shift.Router.Worker(line.Agent)?.Name ?? "Agent";
            var name = Hud.LocalText(bubble, who, 11, Faint, customer ? call.Language : CallLanguage.English);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            var text = Hud.LocalText(bubble, line.Text, 16, Body, customer ? line.Language : CallLanguage.English);
            text.name = customer ? "customer-line" : "agent-line";
            if (customer && translate)
            {
                string english = call.English(line);
                if (english != "")
                {
                    var sub = Hud.Text(bubble, (call.PhraseAid ? "Phrase aid: " : "≈ ") + english, 12, new Color(0.35f, 0.3f, 0.6f));
                    sub.style.unityFontStyleAndWeight = FontStyle.Italic;
                    sub.style.marginTop = 3;
                }
            }
        }
    }

    void DrawRespond(CallCase call)
    {
        _respond.Clear();
        Section(_respond, "PHONE");
        var phone = Row(_respond);
        phone.style.flexWrap = Wrap.Wrap;
        if (call != null && call.State == CallState.Ringing)
            Action(phone, "Answer", Good, () => Send("answer"));
        if (call != null && (call.State == CallState.Active || call.State == CallState.OnHold))
        {
            Action(phone, call.State == CallState.OnHold ? "Resume" : "Hold", HoldColour, () => Send(call.State == CallState.OnHold ? "resume" : "hold"));
            Action(phone, call.Muted ? "Unmute" : "Mute", new Color(0.45f, 0.47f, 0.5f), () => Send("mute"));
            Action(phone, "Transfer…", Brand, OpenTransfer);
        }
        if (call == null)
            Hud.Text(_respond, "No call.", 14, Faint);
        if (call == null || call.State == CallState.Ringing)
            return;

        Section(_respond, "SAY");
        Say("Greet: \"How can I help?\"", "greet");
        Say("Ask for the order number", "ask_order");
        foreach (var (fact, label) in CallConversation.Questions)
            Say(label, "ask", fact);
        Say("Apologise", "apologise");
        Say(call.Def.PromiseIntent, "promise");
        Say("Ask a colleague for help", "colleague");

        Section(_respond, "FINISH");
        Say("\"Is there anything else?\" (close if fixed)", "close");
        var anyway = Say("Mark as resolved anyway", "close_anyway");
        anyway.style.color = Danger;
        Say("Escalate to second line (honest)", "escalate");
    }

    Button Say(string label, string intent, string arg = "")
    {
        var b = new Button(() => Send(intent, arg)) { text = label };
        b.name = "say-" + intent + (arg != "" ? "-" + arg : "");
        StyleButton(b, Color.white, Body);
        b.style.unityTextAlign = TextAnchor.MiddleLeft;
        b.style.marginBottom = 4;
        _respond.Add(b);
        return b;
    }

    void DrawSystem(CallCase call)
    {
        _systemBody.Clear();
        foreach (var id in new[] { "account", "help", "notes" })
        {
            var tab = _window.Q<Button>("tab-" + id);
            if (tab != null)
                StyleButton(tab, _tab == id ? Brand : Color.white, _tab == id ? Color.white : Body);
        }
        if (_tab == "help")
        {
            foreach (var (title, body) in CaseLibrary.HelpPages)
            {
                var t = Hud.Text(_systemBody, title, 17, Brand);
                t.style.unityFontStyleAndWeight = FontStyle.Bold;
                t.style.marginTop = 6;
                Hud.Text(_systemBody, body, 14, Body).style.marginBottom = 8;
            }
            return;
        }
        if (_tab == "notes")
        {
            if (call == null)
            {
                Hud.Text(_systemBody, "No call open.", 14, Faint);
                return;
            }
            Section(_systemBody, $"CALL {call.Id}  ·  {call.Def.Product}");
            foreach (var note in call.Notes)
                Hud.Text(_systemBody, "·  " + note, 14, Body);
            if (call.Notes.Count == 0)
                Hud.Text(_systemBody, "Nothing written yet.", 14, Faint);
            Section(_systemBody, "TRANSFERS");
            foreach (var t in call.Transfers)
                Hud.Text(_systemBody, $"·  {Shift.Router.Worker(t.From)?.Name} → {(t.To == "" ? "queue" : Shift.Router.Worker(t.To)?.Name)}  ({t.Reason})", 14, Body);
            if (call.Transfers.Count == 0)
                Hud.Text(_systemBody, "None.", 14, Faint);
            Section(_systemBody, "LEARNED SO FAR");
            Hud.Text(_systemBody, call.Facts.Count == 0 ? "Nothing yet." : string.Join(",  ", call.Facts.Select(CallConversation.FactLabel)), 14, Body);
            if (call.Attempts > 0)
                Hud.Text(_systemBody, $"Wrong fixes tried: {call.Attempts}", 14, Danger).style.marginTop = 6;
            return;
        }

        // Account: search, record, actions.
        Section(_systemBody, "ORDER SEARCH");
        var row = Row(_systemBody);
        _search ??= MakeSearch();
        row.Add(_search);
        Action(row, "Search", Brand, Search);
        if (call == null || call.State == CallState.Ringing)
            return;
        if (_record == "" && call.Facts.Contains("order_found"))
            _record = call.Def.Record;
        if (_record != "")
        {
            var record = Hud.Text(_systemBody, _record, 15, Body);
            record.style.backgroundColor = Color.white;
            Hud.SetBorder(record, 1, Line);
            Hud.Pad(record, 10, 12);
            record.style.marginTop = 8;
            record.name = "order-record";
        }
        Section(_systemBody, call.Def.Family == CaseFamily.Setup ? "TROUBLESHOOTING" : "ACCOUNT ACTIONS");
        var actions = Row(_systemBody);
        actions.style.flexWrap = Wrap.Wrap;
        foreach (var (id, label) in CaseLibrary.Actions(call.Def.Family))
        {
            var b = Action(actions, label, call.ActionTaken == id ? Good : new Color(0.32f, 0.36f, 0.42f), () => Send("act", id, label));
            b.name = "act-" + id;
        }
    }

    TextField MakeSearch()
    {
        var field = new TextField { maxLength = 12 };
        field.name = "order-search";
        field.style.width = 180;
        field.style.fontSize = 16;
        field.style.marginLeft = 0;
        field.style.marginRight = 8;
        field.RegisterCallback<KeyDownEvent>(e =>
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                Search();
        }, TrickleDown.TrickleDown);
        return field;
    }

    void Search()
    {
        var call = Call;
        if (call == null)
            return;
        string result = Shift.Do(Me, "lookup", _search.value);
        if (call.Facts.Contains("order_found") && result == call.Def.Record)
        {
            _record = result;
            ShowMessage("");
        }
        else
            ShowMessage(result);
        Redraw(true);
    }

    void Send(string intent, string arg = "", string label = "")
    {
        string result = Shift.Do(Me, intent, arg, label);
        ShowMessage(result);
        Redraw(true);
    }

    void ShowMessage(string text)
    {
        _message.text = text ?? "";
        _message.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
    }

    // Transfer ---------------------------------------------------------------------------------

    void OpenTransfer()
    {
        var call = Call;
        if (call == null)
            return;
        _popup.Clear();
        var box = new VisualElement();
        box.style.width = 520;
        box.style.backgroundColor = Color.white;
        Hud.Round(box, 8);
        Hud.Pad(box, 16, 20);
        _popup.Add(box);
        var title = Hud.Text(box, "Transfer this caller to…", 20, Body);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        Hud.Text(box, $"They speak {CallLanguage.Name(call.Language)}. Everything on the call goes with them.", 13, Faint).style.marginBottom = 10;
        foreach (var w in Shift.Router.Workers.Where(w => w.Id != Me))
        {
            string langs = string.Join(", ", w.Languages.Select(CallLanguage.Name));
            string status = !w.Online ? "offline" : w.OnBreak ? "on break" : Shift.Router.IsFree(w) ? "free" : "on a call (it will wait for them)";
            string tags = (w.Technical ? "  ·  technical" : "") + (w.Simulated ? "" : "  ·  teammate");
            var b = new Button(() =>
            {
                ClosePopup();
                Send("transfer", w.Id);
            }) { text = $"{w.Name}   ·   {langs}{tags}   ({status})" };
            b.name = "transfer-" + w.Id;
            StyleButton(b, w.Speaks(call.Language) ? new Color(0.88f, 0.95f, 0.88f) : Color.white, Body);
            b.style.unityTextAlign = TextAnchor.MiddleLeft;
            b.style.marginBottom = 5;
            b.SetEnabled(w.Online && !w.OnBreak);
            box.Add(b);
        }
        var cancel = new Button(ClosePopup) { text = "Cancel (Esc)" };
        StyleButton(cancel, new Color(0.9f, 0.9f, 0.9f), Body);
        cancel.style.marginTop = 8;
        box.Add(cancel);
        _popup.style.display = DisplayStyle.Flex;
    }

    void ClosePopup()
    {
        if (_popup != null)
            _popup.style.display = DisplayStyle.None;
    }

    // Small parts ----------------------------------------------------------------------------

    static VisualElement Row(VisualElement parent)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        parent.Add(row);
        return row;
    }

    static VisualElement Column(VisualElement parent, float width)
    {
        var c = new VisualElement();
        if (width > 0f)
        {
            c.style.width = width;
            c.style.flexShrink = 0;
        }
        c.style.minHeight = 0;
        c.style.borderRightWidth = 1;
        c.style.borderRightColor = Line;
        parent.Add(c);
        return c;
    }

    static void Section(VisualElement parent, string text)
    {
        var label = Hud.Text(parent, text, 11, Faint);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.letterSpacing = 2;
        label.style.marginTop = 10;
        label.style.marginBottom = 5;
    }

    static Button Action(VisualElement parent, string text, Color colour, System.Action onClick)
    {
        var b = new Button(onClick) { text = text };
        StyleButton(b, colour, Color.white);
        b.style.unityFontStyleAndWeight = FontStyle.Bold;
        b.style.marginRight = 6;
        b.style.marginBottom = 5;
        parent.Add(b);
        return b;
    }

    static Button Small(VisualElement parent, string text, System.Action onClick)
    {
        var b = new Button(onClick) { text = text };
        StyleButton(b, Color.white, Body);
        b.style.fontSize = 13;
        parent.Add(b);
        return b;
    }

    static void StyleButton(Button b, Color background, Color ink)
    {
        b.style.backgroundColor = background;
        b.style.color = ink;
        b.style.fontSize = 14;
        b.style.whiteSpace = WhiteSpace.Normal;
        Hud.SetBorder(b, 1, new Color(0f, 0f, 0f, 0.18f));
        Hud.Round(b, 5);
        Hud.Pad(b, 6, 10);
        b.style.marginLeft = 0;
        b.style.marginTop = 0;
    }
}
