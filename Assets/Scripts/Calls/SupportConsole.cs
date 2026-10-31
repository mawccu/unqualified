using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The support desk's screen, in four fixed columns:
/// the conversation (the caller and everything said, in the customer's own
/// language; Arabic shaped and right to left); the case (what has been found out,
/// what to do next, what to say, how to finish); the company system (order search
/// and actions, the help pages, notes and history); and the phone, a rail on the
/// right that never moves or scrolls: status, Answer, Mute, Hold, Transfer, and End
/// call apart from the rest. Every action goes through the shift.
///
/// No free translation: customers' lines stay in their language unless the
/// accessibility setting "Translated call captions" is on (labelled as captions).
/// A call in a language nobody on shift claimed gets the phrasebook (general
/// terms), not an interpreter. The WhisperBox's hints have their own strip.
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
    static readonly Color RailColour = new(0.17f, 0.2f, 0.25f);
    static readonly Color RailInk = new(0.92f, 0.94f, 0.96f);
    static readonly Color RailFaint = new(0.68f, 0.72f, 0.78f);
    static readonly Color Pointer = new(1f, 0.78f, 0.15f);
    const float EndGuardSeconds = 4f, PointSeconds = 2.6f;

    [SerializeField] Hud hud;

    VisualElement _window, _transcript, _case, _popup, _systemBody, _railInfo;
    ScrollView _transcriptScroll;
    Label _state, _patienceLabel, _caller, _callerLanguage, _message, _clock, _captions, _holdLabel, _railHint;
    VisualElement _patienceFill;
    Button _answer, _mute, _hold, _transfer, _end;
    Label _answerWhy, _muteWhy, _holdWhy, _transferWhy, _endWhy;
    TextField _search;
    SupportDesk _desk;
    string _tab = "account";
    string _record = "";
    int _shownLines = -1, _shownCall = -1, _helpReadCall = -1, _endArmedCall = -1;
    CallState _shownState;
    bool _shownMuted, _shownResolved, _phrasebookOpen;
    float _refresh, _endArmedAt;
    // The training step and the control being pointed at for a moment.
    int _trainingStep = -1;
    string _pointAt = "";
    float _pointUntil;

    public bool IsOpen => _window != null && _window.style.display == DisplayStyle.Flex;
    public static SupportConsole Instance { get; private set; }

    // The WhisperBox's last hint, shown in a strip over the company system.
    VisualElement _whisper;
    Label _whisperText, _whisperWords;
    string _highlightPage;
    int _whisperCall = -1;

    public void Configure(Hud owner) => hud = owner;

    void Awake() => Instance = this;

    /// A hint from the WhisperBox for the call on screen: shown until the call changes.
    public void ShowWhisper(WhisperHints.Hint hint)
    {
        if (_window == null)
            return;
        var call = Call;
        _whisperCall = call?.Id ?? -1;
        _whisperText.text = "<b>WHISPERBOX ACTIVE</b> (a hint in your ear, not a translator):  " + hint.Text;
        _whisperWords.style.display = hint.Words != null ? DisplayStyle.Flex : DisplayStyle.None;
        if (hint.Words != null)
        {
            _whisperWords.text = $"{hint.Words}  =  {hint.WordsEnglish}";
            Hud.SetLanguage(_whisperWords, hint.WordsLanguage);
        }
        _whisper.style.display = DisplayStyle.Flex;
        _highlightPage = hint.Page;
        if (hint.Page != null)
            _tab = "help";
        Redraw(true);
    }

    void HideWhisperIfStale()
    {
        if (_whisper == null || _whisper.style.display == DisplayStyle.None)
            return;
        var call = Call;
        if (call == null || call.Id != _whisperCall)
        {
            _whisper.style.display = DisplayStyle.None;
            _highlightPage = null;
        }
    }

    static CallCenterShift Shift => CallCenterShift.Instance;
    string Me => _desk != null ? _desk.WorkerId : GameSession.OfflineActor;
    CallCase Call => Shift != null ? Shift.CallOf(Me) : null;

    public void Open(SupportDesk desk)
    {
        _desk = desk;
        if (_window == null)
            Build();
        _window.style.display = DisplayStyle.Flex;
        hud.FocusMode = true;
        Shift.Changed += OnChanged;
        GameSettings.Changed += OnSettings;
        Redraw(true);
        // The captions toggle used to sit in this screen's toolbar; it is a setting now.
        if (GameSettings.TranslateCalls && PlayerPrefs.GetInt("captions_moved_seen", 0) == 0)
        {
            PlayerPrefs.SetInt("captions_moved_seen", 1);
            ShowMessage("Translated captions are now an accessibility setting (Esc, Settings). You had them on, so they're still on.");
        }
    }

    public void Close()
    {
        if (_window == null)
            return;
        _window.style.display = DisplayStyle.None;
        hud.FocusMode = false;
        ClosePopup();
        if (Shift != null)
            Shift.Changed -= OnChanged;
        GameSettings.Changed -= OnSettings;
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
        if (Instance == this)
            Instance = null;
        if (Shift != null)
            Shift.Changed -= OnChanged;
        GameSettings.Changed -= OnSettings;
    }

    void OnChanged(CallCase call)
    {
        if (call == null || call.Owner == Me || call.Id == _shownCall)
            Redraw(false);
    }

    // Switching captions redraws the lines already on screen at once.
    void OnSettings() => Redraw(true);

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
        _window.style.paddingTop = Length.Percent(6);
        _window.style.backgroundColor = new Color(0f, 0f, 0f, 0.3f);
        hud.Overlay.Add(_window);

        var screen = new VisualElement();
        screen.style.width = 1480;
        screen.style.height = 780;
        screen.style.maxWidth = Length.Percent(97);
        screen.style.maxHeight = Length.Percent(80);
        screen.style.backgroundColor = Page;
        Hud.Round(screen, 6);
        Hud.SetBorder(screen, 10, new Color(0.1f, 0.1f, 0.11f));
        screen.style.overflow = Overflow.Hidden;
        _window.Add(screen);

        // Top bar: brand, queue, captions, help, stand up.
        var bar = Row(screen);
        bar.style.flexShrink = 0;
        bar.style.backgroundColor = Bar;
        Hud.Pad(bar, 8, 16);
        var brand = Hud.Text(bar, "<b>Brightline</b> Desk", 20, Color.white);
        brand.style.marginRight = 18;
        _clock = Hud.Text(bar, "", 14, new Color(1f, 1f, 1f, 0.75f));
        var spacer = new VisualElement();
        spacer.style.flexGrow = 1;
        bar.Add(spacer);
        _captions = Hud.Text(bar, "", 13, new Color(1f, 0.9f, 0.6f));
        _captions.style.marginRight = 14;
        var how = Small(bar, "How does this work?", OpenGuide);
        how.name = "how-it-works";
        how.style.marginRight = 8;
        Small(bar, "Stand up (Esc)", () => _desk?.StandUp());

        var columns = Row(screen);
        columns.style.flexGrow = 1;
        columns.style.alignItems = Align.Stretch;
        columns.style.minHeight = 0;

        // 1. The caller and the conversation.
        var left = Column(columns, 350);
        var callerCard = new VisualElement();
        Hud.Pad(callerCard, 12, 16);
        callerCard.style.borderBottomWidth = 1;
        callerCard.style.borderBottomColor = Line;
        left.Add(callerCard);
        _caller = Hud.Text(callerCard, "", 20, Body);
        _caller.style.unityFontStyleAndWeight = FontStyle.Bold;
        _callerLanguage = Hud.Text(callerCard, "", 14, Faint);
        // Mixed Arabic and English on one line: shaped, left to right overall.
        _callerLanguage.style.unityTextGenerator = TextGeneratorType.Advanced;
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

        // 2. The case: what you know, what's next, what to say, how to finish.
        var middle = Column(columns, 360);
        middle.style.backgroundColor = new Color(0.9f, 0.92f, 0.94f);
        var middleScroll = new ScrollView(ScrollViewMode.Vertical);
        middleScroll.style.flexGrow = 1;
        middleScroll.style.minHeight = 0;
        middle.Add(middleScroll);
        _case = middleScroll.contentContainer;
        Hud.Pad(_case, 10, 12);

        // 3. The company system.
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
        _whisper = new VisualElement();
        _whisper.name = "whisper-strip";
        _whisper.style.flexShrink = 0;
        _whisper.style.backgroundColor = new Color(0.3f, 0.2f, 0.45f);
        Hud.Pad(_whisper, 7, 14);
        _whisperText = Hud.Text(_whisper, "", 14, Color.white);
        _whisperText.style.whiteSpace = WhiteSpace.Normal;
        _whisperWords = Hud.Text(_whisper, "", 15, new Color(1f, 0.9f, 0.55f));
        _whisperWords.style.marginTop = 2;
        _whisper.style.display = DisplayStyle.None;
        right.Add(_whisper);
        var systemScroll = new ScrollView(ScrollViewMode.Vertical);
        systemScroll.style.flexGrow = 1;
        systemScroll.style.minHeight = 0;
        right.Add(systemScroll);
        _systemBody = systemScroll.contentContainer;
        Hud.Pad(_systemBody, 12, 16);
        _message = Hud.Text(right, "", 14, Body);
        _message.name = "system-message";
        _message.style.flexShrink = 0;
        _message.style.backgroundColor = new Color(1f, 0.95f, 0.8f);
        Hud.Pad(_message, 8, 14);
        _message.style.display = DisplayStyle.None;

        // 4. The phone: a fixed rail on the right.
        BuildRail(columns);

        // The transfer list and the guide float over everything.
        _popup = new VisualElement();
        Hud.Fill(_popup);
        _popup.style.alignItems = Align.Center;
        _popup.style.justifyContent = Justify.Center;
        _popup.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
        _popup.style.display = DisplayStyle.None;
        screen.Add(_popup);
    }

    void BuildRail(VisualElement columns)
    {
        var rail = new VisualElement();
        rail.name = "phone-rail";
        rail.style.width = 210;
        rail.style.flexShrink = 0;
        rail.style.backgroundColor = RailColour;
        rail.style.overflow = Overflow.Hidden;
        Hud.Pad(rail, 10, 12);
        columns.Add(rail);
        var title = Hud.Text(rail, "PHONE", 11, RailFaint);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.letterSpacing = 2;
        _state = Hud.Text(rail, "", 15, Color.white);
        _state.name = "phone-state";
        _state.style.unityFontStyleAndWeight = FontStyle.Bold;
        _state.style.unityTextAlign = TextAnchor.MiddleCenter;
        _state.style.marginTop = 6;
        Hud.Pad(_state, 6, 8);
        Hud.Round(_state, 4);
        _railInfo = new VisualElement();
        _railInfo.style.marginTop = 5;
        _railInfo.style.flexShrink = 0;
        rail.Add(_railInfo);
        _patienceLabel = Hud.Text(rail, "", 11, RailFaint);
        _patienceLabel.style.marginTop = 6;
        var track = new VisualElement();
        track.style.height = 7;
        track.style.marginTop = 3;
        track.style.backgroundColor = new Color(1f, 1f, 1f, 0.15f);
        Hud.Round(track, 3);
        _patienceFill = new VisualElement();
        _patienceFill.style.height = 7;
        Hud.Round(_patienceFill, 3);
        track.Add(_patienceFill);
        rail.Add(track);
        _holdLabel = Hud.Text(rail, "", 12, new Color(1f, 0.8f, 0.5f));
        _holdLabel.style.marginTop = 4;
        _holdLabel.style.minHeight = 16;

        // The buttons stay in these places whatever the call is doing.
        (_answer, _answerWhy) = RailButton(rail, "phone-answer", "Answer", () => Send("answer"));
        (_mute, _muteWhy) = RailButton(rail, "phone-mute", "Mute", () =>
        {
            PlayerPrefs.SetInt("phone_hint_seen", 1);
            Send("mute");
        });
        (_hold, _holdWhy) = RailButton(rail, "phone-hold", "Hold", () =>
        {
            PlayerPrefs.SetInt("phone_hint_seen", 1);
            var call = Call;
            Send(call != null && call.State == CallState.OnHold ? "resume" : "hold");
        });
        (_transfer, _transferWhy) = RailButton(rail, "phone-transfer", "Transfer…", OpenTransfer);
        var gap = new VisualElement();
        gap.style.flexGrow = 1;
        gap.style.minHeight = 6;
        rail.Add(gap);
        // Gives way first when the screen is short: End call must always show.
        _railHint = Hud.Text(rail, "<b>Mute</b>: they can't hear you; the call carries on.\n<b>Hold</b>: the call pauses; they wait patiently for about 30 s.", 11, RailFaint);
        _railHint.name = "phone-hint";
        _railHint.style.flexShrink = 1;
        _railHint.style.minHeight = 0;
        _railHint.style.overflow = Overflow.Hidden;
        _railHint.style.marginBottom = 8;
        _railHint.style.backgroundColor = new Color(1f, 1f, 1f, 0.06f);
        Hud.Pad(_railHint, 6, 8);
        Hud.Round(_railHint, 4);
        // Apart from the rest, and ending is not the same as fixing.
        var line = new VisualElement();
        line.style.height = 1;
        line.style.backgroundColor = new Color(1f, 1f, 1f, 0.15f);
        line.style.marginBottom = 4;
        line.style.flexShrink = 0;
        rail.Add(line);
        (_end, _endWhy) = RailButton(rail, "phone-end", "End call", EndPressed);
    }

    (Button, Label) RailButton(VisualElement rail, string name, string text, System.Action onClick)
    {
        var b = new Button(onClick) { text = text };
        b.name = name;
        StyleButton(b, Color.white, Body);
        b.style.unityFontStyleAndWeight = FontStyle.Bold;
        b.style.fontSize = 15;
        b.style.height = 34;
        b.style.flexShrink = 0;
        b.style.marginTop = 6;
        b.style.marginRight = 0;
        rail.Add(b);
        var why = Hud.Text(rail, "", 11, RailFaint);
        why.style.height = 14;
        why.style.flexShrink = 0;
        why.style.marginTop = 1;
        return (b, why);
    }

    // Drawing ---------------------------------------------------------------------------------

    void Redraw(bool full)
    {
        if (_window == null || Shift == null)
            return;
        HideWhisperIfStale();
        var call = Call;
        _clock.text = $"Queue: {Shift.Router.Waiting.Count()}";
        _captions.text = GameSettings.TranslateCalls ? "Captions on (accessibility)" : "";
        DrawRail(call);

        bool changed = call == null ? _shownCall != -1
            : call.Id != _shownCall || call.Transcript.Count != _shownLines || call.State != _shownState || call.Muted != _shownMuted || call.Resolved != _shownResolved;
        if (changed || full)
        {
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
            _shownResolved = call != null && call.Resolved;
            DrawCaller(call);
            DrawTranscript(call);
            DrawCase(call);
            DrawSystem(call);
        }
        PointAtTrainingControl(call);
    }

    void DrawRail(CallCase call)
    {
        (string text, Color colour) state = call == null ? ("NO CALL", new Color(1f, 1f, 1f, 0.12f))
            : call.State switch
            {
                CallState.Ringing => ("RINGING", Danger),
                CallState.OnHold => ("ON HOLD", HoldColour),
                CallState.Active => (call.Muted ? "ON CALL · MUTED" : "ON CALL", Good),
                _ => ("ENDED", new Color(1f, 1f, 1f, 0.12f)),
            };
        _state.text = state.text;
        _state.style.backgroundColor = state.colour;

        _railInfo.Clear();
        if (call != null)
        {
            bool heard = call.State != CallState.Ringing || call.Transcript.Count > 0;
            string language = call.LanguageKnown || heard ? CallLanguage.Name(call.Language) : "Language unknown";
            Hud.Text(_railInfo, language + (call.Training ? "  ·  training" : ""), 14, RailInk).style.unityFontStyleAndWeight = FontStyle.Bold;
            if (RoutedByCv(call))
                Hud.Text(_railInfo, $"Routed to you: your CV says you speak {CallLanguage.Name(call.Language)}.", 11, new Color(1f, 0.8f, 0.5f));
        }
        else
            Hud.Text(_railInfo, "The lamp on the desk phone flashes when one comes in.", 11, RailFaint);

        float patience = call != null ? Mathf.Clamp01(call.Patience) : 0f;
        _patienceLabel.text = call != null ? $"Customer patience {patience * 100f:0}%" : "";
        _patienceFill.style.width = Length.Percent(patience * 100f);
        _patienceFill.style.backgroundColor = Color.Lerp(Danger, Good, patience);
        if (call != null && call.State == CallState.OnHold)
        {
            float left = Shift.Router.HoldLeft(call);
            _holdLabel.text = left > 0f ? $"On hold: patient for {left:0} s more" : "On hold too long: they're getting angry";
            _holdLabel.style.color = left > 8f ? new Color(1f, 0.8f, 0.5f) : new Color(1f, 0.55f, 0.45f);
        }
        else
            _holdLabel.text = "";

        bool ringing = call != null && call.State == CallState.Ringing;
        bool live = call != null && (call.State == CallState.Active || call.State == CallState.OnHold);
        RailState(_answer, _answerWhy, ringing ? "" : call == null ? "Nothing ringing" : "Already answered", ringing ? Good : (Color?)null);
        _mute.text = call != null && call.Muted ? "Unmute" : "Mute";
        RailState(_mute, _muteWhy, live ? "" : call == null ? "No call" : "Answer first", call != null && call.Muted ? HoldColour : (Color?)null);
        _hold.text = call != null && call.State == CallState.OnHold ? "Resume" : "Hold";
        RailState(_hold, _holdWhy, live ? "" : call == null ? "No call" : "Answer first", call != null && call.State == CallState.OnHold ? HoldColour : (Color?)null);
        RailState(_transfer, _transferWhy, !live ? (call == null ? "No call" : "Answer first") : call.Training ? "Not on a training call" : "", null);
        bool armed = call != null && _endArmedCall == call.Id && Time.unscaledTime - _endArmedAt < EndGuardSeconds;
        _end.text = armed ? "End anyway (unfixed)" : "End call";
        // End stays usable on an unfixed call (with a warning and a second press).
        _end.SetEnabled(live);
        _end.style.opacity = live ? 1f : 0.45f;
        _endWhy.text = !live ? (call == null ? "No call" : "Answer first") : call.Resolved ? "" : "Not fixed: counts against you";
        _end.style.backgroundColor = armed ? Danger : Color.white;
        _end.style.color = armed ? Color.white : Danger;
        Hud.SetBorder(_end, 2, Danger);
        _railHint.style.display = PlayerPrefs.GetInt("phone_hint_seen", 0) == 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    static void RailState(Button b, Label why, string reason, Color? active)
    {
        bool enabled = reason == "";
        b.SetEnabled(enabled);
        b.style.backgroundColor = active ?? Color.white;
        b.style.color = active.HasValue ? Color.white : Body;
        b.style.opacity = enabled ? 1f : 0.45f;
        why.text = enabled ? "" : reason;
    }

    /// The call reached you because of a language on your CV (not English).
    bool RoutedByCv(CallCase call)
    {
        var me = Shift.Router.Worker(Me);
        return call.Language != CallLanguage.English && call.LanguageKnown && me != null && me.Speaks(call.Language);
    }

    void EndPressed()
    {
        var call = Call;
        if (call == null)
            return;
        // A fixed call just ends; an unfixed one needs a second press, so a stray
        // click never hangs up on someone.
        if (!call.Resolved && !(_endArmedCall == call.Id && Time.unscaledTime - _endArmedAt < EndGuardSeconds))
        {
            _endArmedCall = call.Id;
            _endArmedAt = Time.unscaledTime;
            ShowMessage("Their problem isn't fixed. Ending now counts against you (and the supervisor may hear). Press End again within 4 s to hang up, or finish the case first.");
            DrawRail(call);
            return;
        }
        _endArmedCall = -1;
        Send("end");
    }

    void DrawCaller(CallCase call)
    {
        if (call == null)
        {
            _caller.text = "Waiting for a call";
            Hud.SetLanguage(_caller, CallLanguage.English);
            _callerLanguage.text = Shift.Router.Waiting.Any() ? "Callers are waiting for someone free who speaks their language." : "The lamp on your phone flashes red when one comes in.";
            return;
        }
        bool heard = call.State != CallState.Ringing || call.Transcript.Count > 0;
        _caller.text = heard ? call.CustomerName : call.Training ? "Training call" : "Incoming call";
        Hud.SetLanguage(_caller, heard ? call.Language : CallLanguage.English);
        string language = call.LanguageKnown || heard ? $"{CallLanguage.NativeName(call.Language)}  ({CallLanguage.Name(call.Language)})" : "Language unknown (skipped the menu)";
        if (call.Training)
            language += "\nTRAINING CALL: paid, patient, and nobody is marking you";
        var from = call.Transfers.LastOrDefault(t => t.Reason != "disconnected");
        if (from != null)
            language += $"\nTransferred from {Shift.Router.Worker(from.From)?.Name}: {from.Reason}";
        if (call.Callback)
            language += "\nCALLBACK: they were told this was fixed";
        if (call.PhraseAid && call.Language != CallLanguage.English && heard)
            language += "\nNobody on shift claimed this language. There's a phrasebook in the case panel.";
        _callerLanguage.text = language;
    }

    void DrawTranscript(CallCase call)
    {
        _transcript.Clear();
        if (call == null)
            return;
        bool captions = GameSettings.TranslateCalls;
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
            var source = CallTranslation.For(line, captions);
            if (source != CallTranslation.Source.None)
            {
                string english = call.English(line);
                if (english != "")
                {
                    var sub = Hud.Text(bubble, CallTranslation.Label(source) + english, 12, new Color(0.35f, 0.3f, 0.6f));
                    sub.name = "caption";
                    sub.style.unityFontStyleAndWeight = FontStyle.Italic;
                    sub.style.marginTop = 3;
                }
            }
        }
    }

    void DrawCase(CallCase call)
    {
        _case.Clear();
        if (call != null && call.Training)
            DrawTraining(call);

        // What to do next (never which fix).
        var next = Hud.Text(_case, "<b>NEXT</b>   " + CaseFlow.NextGoal(call), 15, Body);
        next.name = "next-goal";
        next.style.backgroundColor = Color.white;
        Hud.SetBorder(next, 1, Line);
        Hud.Round(next, 5);
        Hud.Pad(next, 7, 10);
        next.style.marginBottom = 4;
        if (call == null || call.State == CallState.Ringing)
            return;

        DrawSummary(call);
        if (call.Language != CallLanguage.English)
            DrawPhrasebook(call);

        // What to say, grouped by what you are doing; what you have found out is ticked.
        string Done(string fact) => call.Facts.Contains(fact) ? "✓  " : "";
        Section(_case, "OPEN");
        Say(Done("greeted") + "Greet: \"How can I help?\"", "greet");
        Say(Done("order_heard") + "Ask for the order number", "ask_order");
        Section(_case, "ASK ABOUT");
        foreach (var (fact, label) in CallConversation.Questions)
            Say(Done(fact) + label, "ask", fact);
        Section(_case, "SMOOTH IT OVER");
        Say("Apologise", "apologise");
        Say(call.Def.PromiseIntent, "promise");
        Say("Ask a colleague for help (mute or hold first)", "colleague");
        Section(_case, "FINISH THE CASE");
        Say("\"Is there anything else?\" (closes it if fixed)", "close");
        Say("Escalate to second line (honest)", "escalate");
        var anyway = Say("Mark as resolved anyway", "close_anyway");
        anyway.style.color = Danger;
    }

    /// What you have found out, from the customer and from the record, kept apart.
    void DrawSummary(CallCase call)
    {
        var def = call.Def;
        var facts = call.Facts;
        var box = new VisualElement();
        box.name = "case-summary";
        box.style.backgroundColor = Color.white;
        Hud.SetBorder(box, 1, Line);
        Hud.Round(box, 5);
        Hud.Pad(box, 8, 10);
        box.style.marginTop = 4;
        _case.Add(box);

        Section(box, "THE CUSTOMER SAYS");
        Fact(box, "Problem", facts.Contains("greeted") ? $"{def.Product}: {Problem(def.Family)}" : "not heard yet");
        bool needsOrder = def.Requires.Contains("order_found");
        if (facts.Contains("order_heard"))
        {
            var row = Row(box);
            Hud.Text(row, $"<b>Order</b>   #{def.OrderId}", 14, Body).style.flexGrow = 1;
            var use = new Button(UseOrderInSearch) { text = "Use in search" };
            use.name = "use-order";
            StyleButton(use, Brand, Color.white);
            use.style.fontSize = 12;
            Hud.Pad(use, 3, 8);
            row.Add(use);
        }
        else
            Fact(box, "Order", needsOrder || !facts.Contains("greeted") ? "not asked yet" : "not needed (nothing wrong with the order)");
        Fact(box, "Detail", facts.Contains(def.DetailFact) ? CallConversation.FactLabel(def.DetailFact) + " (see the conversation)" : "not asked yet");

        Section(box, "THE RECORD SAYS");
        Fact(box, "Order record", facts.Contains("order_found") ? WhisperHints.RecordFlag(def) : needsOrder ? "not looked up yet" : "no order involved");
        if (call.ActionTaken != "")
        {
            string label = CaseLibrary.Actions(def.Family).FirstOrDefault(a => a.id == call.ActionTaken).label ?? call.ActionTaken;
            Fact(box, "Remedy", call.Resolved ? $"{label}: worked" : $"{label}: didn't fix it");
        }
        if (call.Attempts > 0 && !call.Resolved)
            Fact(box, "Wrong fixes", call.Attempts.ToString());
        if (call.Promises.Count > 0)
            Fact(box, "Promised", string.Join("; ", call.Promises.Select(p => p.Text)));
    }

    static string Problem(CaseFamily family) => family switch
    {
        CaseFamily.Delivery => "delivery problem",
        CaseFamily.Setup => "won't work",
        _ => "wants money back",
    };

    static void Fact(VisualElement parent, string label, string value) =>
        Hud.Text(parent, $"<b>{label}</b>   {value}", 14, Body).style.marginBottom = 2;

    void DrawPhrasebook(CallCase call)
    {
        var terms = Phrasebook.For(call.Language);
        if (terms.Count == 0)
            return;
        var toggle = new Button(() =>
        {
            _phrasebookOpen = !_phrasebookOpen;
            Redraw(true);
        }) { text = (_phrasebookOpen ? "▾ " : "▸ ") + $"Phrasebook: {CallLanguage.Name(call.Language)} (general words, not this call)" };
        toggle.name = "phrasebook";
        StyleButton(toggle, new Color(0.96f, 0.94f, 1f), Body);
        toggle.style.fontSize = 13;
        toggle.style.marginTop = 6;
        toggle.style.unityTextAlign = TextAnchor.MiddleLeft;
        _case.Add(toggle);
        if (!_phrasebookOpen)
            return;
        var list = new VisualElement();
        list.style.backgroundColor = Color.white;
        Hud.Pad(list, 6, 10);
        _case.Add(list);
        foreach (var (term, english) in terms)
        {
            var row = Row(list);
            var t = Hud.LocalText(row, term, 14, Body, call.Language);
            t.style.width = Length.Percent(50);
            Hud.Text(row, english, 13, Faint).style.marginLeft = 8;
        }
    }

    void DrawTraining(CallCase call)
    {
        var step = CaseFlow.Training(call, _helpReadCall == call.Id);
        if (step.Number != _trainingStep)
        {
            // A new step: point at its control for a moment, then stop.
            _trainingStep = step.Number;
            _pointAt = step.Control;
            _pointUntil = Time.unscaledTime + PointSeconds;
        }
        var box = new VisualElement();
        box.name = "training";
        box.style.backgroundColor = new Color(1f, 0.96f, 0.82f);
        Hud.SetBorder(box, 2, Pointer);
        Hud.Round(box, 5);
        Hud.Pad(box, 8, 10);
        box.style.marginBottom = 6;
        _case.Add(box);
        Hud.Text(box, $"<b>TRAINING</b>   step {step.Number} of {CaseFlow.TrainingSteps}", 12, Faint);
        var text = Hud.Text(box, step.Text, 15, Body);
        text.name = "training-step";
        text.style.marginTop = 3;
        var row = Row(box);
        row.style.marginTop = 6;
        var again = Small(row, "Show me", () =>
        {
            _pointAt = CaseFlow.Training(Call, _helpReadCall == Call?.Id).Control;
            _pointUntil = Time.unscaledTime + PointSeconds;
            PointAtTrainingControl(Call);
        });
        again.style.marginRight = 6;
        var skip = Small(row, "Skip training", () => Shift.SkipTraining());
        skip.name = "skip-training";
    }

    /// Outlines the training step's control for a moment (after every redraw, as
    /// the case panel is rebuilt).
    void PointAtTrainingControl(CallCase call)
    {
        if (_pointAt == "")
            return;
        var target = _window.Q(_pointAt);
        if (target == null)
            return;
        // Off again when the moment passes, and at once if training ended (skipped).
        bool on = call != null && call.Training && Time.unscaledTime < _pointUntil;
        Hud.SetBorder(target, on ? 3 : 1, on ? Pointer : new Color(0f, 0f, 0f, 0.18f));
        if (_pointAt == "phone-end")
            Hud.SetBorder(target, on ? 3 : 2, on ? Pointer : Danger);
        if (!on)
            _pointAt = "";
    }

    Button Say(string label, string intent, string arg = "")
    {
        var b = new Button(() => Send(intent, arg)) { text = label };
        b.name = "say-" + intent + (arg != "" ? "-" + arg : "");
        StyleButton(b, Color.white, Body);
        // Compact, so the choices fit with little scrolling.
        b.style.fontSize = 13;
        Hud.Pad(b, 4, 9);
        b.style.unityTextAlign = TextAnchor.MiddleLeft;
        b.style.marginBottom = 3;
        _case.Add(b);
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
            if (call != null)
                _helpReadCall = call.Id;
            foreach (var (title, body) in CaseLibrary.HelpPages)
            {
                var page = new VisualElement();
                page.name = "help-" + title;
                _systemBody.Add(page);
                if (title == _highlightPage)
                {
                    // The page the WhisperBox pointed at.
                    page.style.backgroundColor = new Color(1f, 0.95f, 0.7f);
                    Hud.SetBorder(page, 2, new Color(0.3f, 0.2f, 0.45f));
                    Hud.Round(page, 4);
                    Hud.Pad(page, 4, 8);
                }
                var t = Hud.Text(page, title, 17, Brand);
                t.style.unityFontStyleAndWeight = FontStyle.Bold;
                t.style.marginTop = 6;
                Hud.Text(page, body, 14, Body).style.marginBottom = 8;
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

    /// Copies the order number the customer gave into the search and runs it: the
    /// job is finding the record, not retyping digits.
    void UseOrderInSearch()
    {
        var call = Call;
        if (call == null || !call.Facts.Contains("order_heard"))
            return;
        _tab = "account";
        _search ??= MakeSearch();
        _search.value = call.Def.OrderId;
        Search();
    }

    void Search()
    {
        var call = Call;
        if (call == null)
        {
            ShowMessage("There's no call open: nobody to look up.");
            return;
        }
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

    // Transfer and the guide -------------------------------------------------------------------

    void OpenTransfer()
    {
        var call = Call;
        if (call == null)
            return;
        var box = PopupBox("Transfer this caller to…");
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
        CancelButton(box);
    }

    /// "How does this work?": the screen's parts, phone controls, finishing a case,
    /// and replaying the training call.
    void OpenGuide()
    {
        var box = PopupBox("How the desk works");
        string[] parts =
        {
            "<b>Left</b>: the caller and everything said, in their language.",
            "<b>Middle</b>: the case. NEXT says what kind of thing to do now; the summary keeps what you've found out (the customer's side and the record's side); then what you can say, and how to finish.",
            "<b>Right</b>: the company system. Search the order, read the help pages, take an action on the account.",
            "<b>Phone</b> (far right): Answer, Mute (they can't hear you; the call carries on), Hold (the call pauses; they wait about 30 s patiently), Transfer, and End call.",
            "<b>Ending is not fixing</b>: End call on an unfixed problem counts against you. Close with \"Is there anything else?\" once it's fixed, or escalate honestly.",
            "<b>Other languages</b>: calls in a language on your CV come to you. Nobody translates them; there's a phrasebook of general words. Translated captions are an accessibility setting (Esc, Settings).",
        };
        foreach (var part in parts)
            Hud.Text(box, part, 14, Body).style.marginBottom = 6;
        var row = Row(box);
        row.style.marginTop = 6;
        bool canReplay = Shift.Current == CallCenterShift.Phase.Calls && Call == null;
        var replay = Small(row, "Replay the training call", () =>
        {
            ClosePopup();
            ShowMessage(Shift.StartTraining() ? "" : "Finish the call you're on first.");
        });
        replay.name = "replay-training";
        replay.SetEnabled(canReplay);
        replay.style.marginRight = 8;
        if (!canReplay)
            Hud.Text(row, Call != null ? "(after this call)" : "(during the shift)", 12, Faint);
        CancelButton(box);
    }

    VisualElement PopupBox(string heading)
    {
        _popup.Clear();
        var box = new VisualElement();
        box.style.width = 620;
        box.style.backgroundColor = Color.white;
        Hud.Round(box, 8);
        Hud.Pad(box, 16, 20);
        _popup.Add(box);
        var title = Hud.Text(box, heading, 20, Body);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 6;
        _popup.style.display = DisplayStyle.Flex;
        return box;
    }

    void CancelButton(VisualElement box)
    {
        var cancel = new Button(ClosePopup) { text = "Close (Esc)" };
        StyleButton(cancel, new Color(0.9f, 0.9f, 0.9f), Body);
        cancel.style.marginTop = 8;
        box.Add(cancel);
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
        label.style.marginTop = 8;
        label.style.marginBottom = 4;
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
