using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Everything drawn over the game: crosshair, prompts, short feedback, the
/// objective card (tasks at work, the next step at home), subtitles, the bill
/// held up to read, a status line, banners, fades, the F3 frame overlay and the
/// pause menu. Built in code on a UIDocument so the look lives in one place.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public partial class Hud : MonoBehaviour
{
    public static readonly Color Panel = new(0.07f, 0.08f, 0.09f, 0.72f);
    public static readonly Color Ink = new(0.96f, 0.94f, 0.9f);
    public static readonly Color Muted = new(0.96f, 0.94f, 0.9f, 0.62f);
    public static readonly Color Accent = new(0.36f, 0.85f, 0.74f);
    public static readonly Color Warn = new(0.98f, 0.66f, 0.3f);
    public static readonly Color Done = new(0.55f, 0.88f, 0.45f);

    public event Action ResumeRequested, RestartRequested, NewGameRequested, QuitRequested;

    VisualElement _root, _crosshair, _ring, _card, _cardBody, _menu, _banner, _document, _fade, _subtitleBox, _overlay;
    Label _prompt, _toast, _status, _stats, _sensitivityValue, _fovValue, _bobValue, _eyebrow, _title, _bannerTitle, _bannerText;
    Label _subtitleSpeaker, _subtitleText, _skipHint, _help, _documentText;
    float _toastLeft, _subtitleLeft;
    float _statsTimer;
    int _frames;
    Coroutine _fadeRoutine;

    /// A layer above the HUD and below the menu, for full screen interfaces (the computer).
    public VisualElement Overlay
    {
        get
        {
            Build();
            return _overlay;
        }
    }

    void OnEnable() => Build();

    /// Builds once. Public calls may arrive before OnEnable (script order), so each one checks.
    void Build()
    {
        if (_root != null)
            return;
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.Clear();
        _root.pickingMode = PickingMode.Ignore;
        _root.style.flexGrow = 1;

        BuildCrosshair();
        _prompt = Pill(_root, 16);
        _prompt.style.top = Length.Percent(55);
        _toast = Pill(_root, 16);
        _toast.style.top = Length.Percent(63);
        _toast.style.color = Warn;
        BuildCard();
        BuildStatus();
        BuildHelp();
        BuildStats();
        BuildBanner();
        BuildDocument();
        BuildSubtitles();
        _overlay = new VisualElement { pickingMode = PickingMode.Ignore };
        Fill(_overlay);
        _root.Add(_overlay);
        BuildShiftUi();
        BuildFade();
        BuildMenu();
        SetPrompt("");
        _toast.style.display = DisplayStyle.None;
    }

    void Update()
    {
        if (_root == null)
            return;
        UpdateShiftUi();
        float dt = Time.unscaledDeltaTime;
        if (_toastLeft > 0f)
        {
            _toastLeft -= dt;
            _toast.style.opacity = Mathf.Clamp01(_toastLeft / 0.4f);
            if (_toastLeft <= 0f)
                _toast.style.display = DisplayStyle.None;
        }
        if (_subtitleLeft > 0f)
        {
            _subtitleLeft -= Time.deltaTime;
            if (_subtitleLeft <= 0f)
                ClearSubtitle();
        }
        _frames++;
        _statsTimer += dt;
        if (_statsTimer >= 0.5f)
        {
            if (_stats.style.display == DisplayStyle.Flex)
            {
                float fps = _frames / _statsTimer;
                _stats.text = $"{fps:0} fps   {1000f / Mathf.Max(fps, 0.01f):0.0} ms   vsync {QualitySettings.vSyncCount}   {Screen.width}x{Screen.height}";
            }
            _frames = 0;
            _statsTimer = 0f;
        }
    }

    // Prompts and feedback ----------------------------------------------------------

    public void SetPrompt(string text)
    {
        Build();
        _prompt.text = text;
        _prompt.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
    }

    public void ShowToast(string text)
    {
        Build();
        _toast.text = text;
        _toast.style.display = DisplayStyle.Flex;
        _toast.style.opacity = 1f;
        _toastLeft = 2.4f;
    }

    public void SetAim(bool actionable)
    {
        Build();
        _ring.style.display = actionable ? DisplayStyle.Flex : DisplayStyle.None;
        _crosshair.style.backgroundColor = actionable ? Accent : new Color(1f, 1f, 1f, 0.85f);
    }

    public void SetCrosshairVisible(bool visible)
    {
        Build();
        _crosshair.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (!visible)
            _ring.style.display = DisplayStyle.None;
    }

    // The card ---------------------------------------------------------------------------

    /// The top-left card: an eyebrow line, a bold title and optional lines.
    public void SetCard(string eyebrow, string title, params string[] lines)
    {
        Build();
        _card.style.display = DisplayStyle.Flex;
        _eyebrow.text = eyebrow;
        _title.text = title;
        _cardBody.Clear();
        foreach (var line in lines)
            Text(_cardBody, line, 14, Muted).style.marginTop = 6;
    }

    public void HideCard()
    {
        Build();
        _card.style.display = DisplayStyle.None;
    }

    /// The shift's tasks in the card. Tasks in assigned are marked as yours.
    public void SetTasks(string eyebrow, IReadOnlyList<TaskBoard.Result> results, ICollection<string> assigned)
    {
        Build();
        _card.style.display = DisplayStyle.Flex;
        _eyebrow.text = eyebrow;
        _title.text = "Tonight's essentials";
        _cardBody.Clear();
        foreach (var r in results)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 10;
            var dot = new VisualElement();
            dot.style.width = dot.style.height = 10;
            dot.style.marginTop = 6;
            dot.style.marginRight = 10;
            dot.style.flexShrink = 0;
            Round(dot, 5);
            dot.style.backgroundColor = r.Status switch
            {
                TaskBoard.Status.Done => Done,
                TaskBoard.Status.NeedsFixing => Warn,
                TaskBoard.Status.InProgress => Accent,
                TaskBoard.Status.NotBuilt => new Color(1f, 1f, 1f, 0.18f),
                _ => new Color(1f, 1f, 1f, 0.45f),
            };
            row.Add(dot);
            var text = new VisualElement();
            text.style.flexShrink = 1;
            var nameRow = new VisualElement();
            nameRow.style.flexDirection = FlexDirection.Row;
            nameRow.style.alignItems = Align.Center;
            text.Add(nameRow);
            var name = Text(nameRow, r.Name, 16, Ink);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            if (r.Status == TaskBoard.Status.NotBuilt)
                name.style.color = Muted;
            if (assigned != null && assigned.Contains(r.Id))
            {
                var chip = Text(nameRow, "YOURS", 10, new Color(0.05f, 0.1f, 0.09f));
                chip.style.backgroundColor = Warn;
                chip.style.unityFontStyleAndWeight = FontStyle.Bold;
                chip.style.marginLeft = 8;
                Pad(chip, 2, 6);
                Round(chip, 4);
            }
            var status = Text(text, r.Status == TaskBoard.Status.NotStarted || r.Status == TaskBoard.Status.InProgress ? $"{r.Goal}.  {r.Text}" : r.Text, 13, Muted);
            if (r.Status == TaskBoard.Status.NeedsFixing)
                status.style.color = Warn;
            if (r.Status == TaskBoard.Status.Done)
                status.style.color = Done;
            row.Add(text);
            _cardBody.Add(row);
        }
    }

    /// One more line under the card's tasks.
    public void AddCardLine(string richText)
    {
        Build();
        var line = Text(_cardBody, richText, 13, Muted);
        line.style.marginTop = 8;
    }

    /// The line at the bottom left (money and rent at home, the coordinator at work).
    public void SetStatus(string richText)
    {
        Build();
        _status.text = richText;
        _status.style.display = string.IsNullOrEmpty(richText) ? DisplayStyle.None : DisplayStyle.Flex;
    }

    public void SetHelp(string text)
    {
        Build();
        _help.text = text;
    }

    // Story ---------------------------------------------------------------------------------

    public void ShowSubtitle(string speaker, string text, float seconds, bool skippable = false)
    {
        Build();
        _subtitleSpeaker.text = speaker.ToUpperInvariant();
        _subtitleText.text = text;
        _subtitleBox.style.display = DisplayStyle.Flex;
        _skipHint.style.display = skippable ? DisplayStyle.Flex : DisplayStyle.None;
        _subtitleLeft = seconds;
    }

    public void ClearSubtitle()
    {
        Build();
        _subtitleBox.style.display = DisplayStyle.None;
        _subtitleLeft = 0f;
    }

    public void ShowBanner(string title, string text)
    {
        Build();
        _bannerTitle.text = title;
        _bannerText.text = text;
        _banner.style.display = DisplayStyle.Flex;
    }

    public void HideBanner()
    {
        Build();
        _banner.style.display = DisplayStyle.None;
    }

    /// A paper held up to read (rich text), or null to put it away.
    public void ShowDocument(string richText)
    {
        Build();
        _document.style.display = richText == null ? DisplayStyle.None : DisplayStyle.Flex;
        if (richText != null)
            _documentText.text = richText;
    }

    /// Fades the screen to black (1) or back (0) over seconds of real time.
    public void Fade(float to, float seconds)
    {
        Build();
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(to, seconds));
    }

    public void SetBlack(float alpha)
    {
        Build();
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);
        _fade.style.opacity = alpha;
        _fade.style.display = alpha > 0f ? DisplayStyle.Flex : DisplayStyle.None;
    }

    IEnumerator FadeRoutine(float to, float seconds)
    {
        _fade.style.display = DisplayStyle.Flex;
        float from = _fade.resolvedStyle.opacity;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            _fade.style.opacity = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        _fade.style.opacity = to;
        if (to <= 0f)
            _fade.style.display = DisplayStyle.None;
        _fadeRoutine = null;
    }

    // Menu ------------------------------------------------------------------------------------

    public void SetMenuVisible(bool visible)
    {
        Build();
        _menu.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        RefreshSettingValues();
    }

    public void ToggleStats()
    {
        Build();
        _stats.style.display = _stats.style.display == DisplayStyle.Flex ? DisplayStyle.None : DisplayStyle.Flex;
    }

    // Building ----------------------------------------------------------------------------------

    void BuildCrosshair()
    {
        _crosshair = new VisualElement { pickingMode = PickingMode.Ignore };
        Centre(_crosshair, 6, 6);
        Round(_crosshair, 3);
        _crosshair.style.backgroundColor = new Color(1f, 1f, 1f, 0.85f);
        _root.Add(_crosshair);
        _ring = new VisualElement { pickingMode = PickingMode.Ignore };
        Centre(_ring, 26, 26);
        Round(_ring, 13);
        SetBorder(_ring, 2, Accent);
        _ring.style.display = DisplayStyle.None;
        _root.Add(_ring);
    }

    void BuildCard()
    {
        _card = Card(_root);
        _card.style.position = Position.Absolute;
        _card.style.left = 28;
        _card.style.top = 28;
        _card.style.width = 400;
        _eyebrow = Text(_card, "", 12, Accent);
        _eyebrow.style.letterSpacing = 2;
        _eyebrow.style.unityFontStyleAndWeight = FontStyle.Bold;
        _title = Text(_card, "", 21, Ink);
        _title.style.unityFontStyleAndWeight = FontStyle.Bold;
        _title.style.marginTop = 2;
        _cardBody = new VisualElement();
        _card.Add(_cardBody);
        _card.style.display = DisplayStyle.None;
    }

    void BuildStatus()
    {
        _status = Pill(_root, 14);
        _status.style.left = 28;
        _status.style.bottom = 28;
        _status.style.top = StyleKeyword.Auto;
        _status.style.translate = new Translate(0, 0);
        _status.style.unityTextAlign = TextAnchor.MiddleLeft;
        _status.style.display = DisplayStyle.None;
    }

    void BuildHelp()
    {
        _help = Text(_root, "WASD move    Mouse look    Space jump    Shift sprint    C crouch    E use / pick up    Click place    Q drop    Esc menu    F3 frame rate", 13, new Color(1f, 1f, 1f, 0.45f));
        _help.style.position = Position.Absolute;
        _help.style.bottom = 30;
        _help.style.right = 28;
    }

    void BuildStats()
    {
        _stats = Text(_root, "", 13, Ink);
        _stats.style.position = Position.Absolute;
        _stats.style.top = 28;
        _stats.style.right = 28;
        _stats.style.backgroundColor = Panel;
        Pad(_stats, 8, 12);
        Round(_stats, 8);
        _stats.style.display = DisplayStyle.None;
    }

    void BuildBanner()
    {
        _banner = Card(_root);
        _banner.style.position = Position.Absolute;
        _banner.style.top = 40;
        _banner.style.left = Length.Percent(50);
        _banner.style.translate = new Translate(Length.Percent(-50), 0);
        _banner.style.alignItems = Align.Center;
        _bannerTitle = Text(_banner, "", 26, Done);
        _bannerTitle.style.letterSpacing = 4;
        _bannerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        _bannerText = Text(_banner, "", 15, Muted);
        _bannerText.style.unityTextAlign = TextAnchor.MiddleCenter;
        _banner.style.display = DisplayStyle.None;
    }

    void BuildDocument()
    {
        // A sheet of paper held up on the right of the screen.
        _document = new VisualElement { pickingMode = PickingMode.Ignore };
        _document.style.position = Position.Absolute;
        _document.style.right = 60;
        _document.style.top = Length.Percent(22);
        _document.style.width = 330;
        _document.style.backgroundColor = new Color(0.96f, 0.95f, 0.9f);
        _document.style.rotate = new Rotate(2.5f);
        Pad(_document, 22, 24);
        Round(_document, 3);
        SetBorder(_document, 1, new Color(0f, 0f, 0f, 0.15f));
        _documentText = Text(_document, "", 15, new Color(0.15f, 0.15f, 0.17f));
        _root.Add(_document);
        _document.style.display = DisplayStyle.None;
    }

    void BuildSubtitles()
    {
        _subtitleBox = new VisualElement { pickingMode = PickingMode.Ignore };
        _subtitleBox.style.position = Position.Absolute;
        _subtitleBox.style.bottom = 96;
        _subtitleBox.style.left = Length.Percent(50);
        _subtitleBox.style.translate = new Translate(Length.Percent(-50), 0);
        _subtitleBox.style.maxWidth = 900;
        _subtitleBox.style.alignItems = Align.Center;
        _subtitleBox.style.backgroundColor = new Color(0f, 0f, 0f, 0.62f);
        Pad(_subtitleBox, 10, 22);
        Round(_subtitleBox, 10);
        _subtitleSpeaker = Text(_subtitleBox, "", 12, Warn);
        _subtitleSpeaker.style.letterSpacing = 2;
        _subtitleSpeaker.style.unityFontStyleAndWeight = FontStyle.Bold;
        _subtitleText = Text(_subtitleBox, "", 21, Ink);
        _subtitleText.style.unityTextAlign = TextAnchor.MiddleCenter;
        _skipHint = Text(_subtitleBox, "Enter  skip", 11, new Color(1f, 1f, 1f, 0.4f));
        _skipHint.style.marginTop = 4;
        _root.Add(_subtitleBox);
        _subtitleBox.style.display = DisplayStyle.None;
    }

    void BuildFade()
    {
        _fade = new VisualElement { pickingMode = PickingMode.Ignore };
        Fill(_fade);
        _fade.style.backgroundColor = Color.black;
        _fade.style.opacity = 0f;
        _fade.style.display = DisplayStyle.None;
        _root.Add(_fade);
    }

    void BuildMenu()
    {
        _menu = new VisualElement();
        Fill(_menu);
        _menu.style.backgroundColor = new Color(0.03f, 0.03f, 0.04f, 0.6f);
        _menu.style.alignItems = Align.Center;
        _menu.style.justifyContent = Justify.Center;
        _root.Add(_menu);

        var card = Card(_menu);
        card.style.width = 440;
        Pad(card, 30, 34);
        var title = Text(card, "UNQUALIFIED", 34, Ink);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.letterSpacing = 6;
        Text(card, "Paused.", 15, Muted).style.marginBottom = 22;

        MenuButton(card, "Resume", true, () => ResumeRequested?.Invoke());
        MenuButton(card, "Restart this scene", false, () => RestartRequested?.Invoke());
        MenuButton(card, "New game (back to the apartment)", false, () => NewGameRequested?.Invoke());
        _sensitivityValue = Stepper(card, "Mouse sensitivity", () => GameSettings.MouseSensitivity -= 0.1f, () => GameSettings.MouseSensitivity += 0.1f);
        _fovValue = Stepper(card, "Field of view", () => GameSettings.HorizontalFov -= 5f, () => GameSettings.HorizontalFov += 5f);
        _bobValue = Stepper(card, "Camera bob", () => GameSettings.CameraBob -= 0.25f, () => GameSettings.CameraBob += 0.25f);
        // For testers: the game's log (and the save) live here; send Player.log with any bug report.
        MenuButton(card, "Report a problem (open the log folder)", false, TesterSupport.OpenLogFolder);
        MenuButton(card, $"Quit      <size=12><color=#FFFFFF66>v{Application.version}</color></size>", false, () => QuitRequested?.Invoke());
        GameSettings.Changed += RefreshSettingValues;
        _menu.style.display = DisplayStyle.None;
    }

    void OnDestroy() => GameSettings.Changed -= RefreshSettingValues;

    void RefreshSettingValues()
    {
        if (_sensitivityValue == null)
            return;
        _sensitivityValue.text = GameSettings.MouseSensitivity.ToString("0.0");
        _fovValue.text = $"{GameSettings.HorizontalFov:0}°";
        _bobValue.text = GameSettings.CameraBob <= 0f ? "Off" : $"{GameSettings.CameraBob * 100f:0}%";
    }

    // Small building blocks (shared with the computer screen) ---------------------------------

    public static Label Text(VisualElement parent, string text, int size, Color colour)
    {
        var label = new Label(text) { pickingMode = PickingMode.Ignore, enableRichText = true };
        label.style.fontSize = size;
        label.style.color = colour;
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.marginLeft = label.style.marginRight = label.style.marginTop = label.style.marginBottom = 0;
        label.style.paddingLeft = label.style.paddingRight = label.style.paddingTop = label.style.paddingBottom = 0;
        parent.Add(label);
        return label;
    }

    /// Text in a customer's language. Arabic needs the advanced text generator
    /// (letter shaping) and right-to-left order; order numbers inside it stay readable.
    public static Label LocalText(VisualElement parent, string text, int size, Color colour, string language)
    {
        var label = Text(parent, text, size, colour);
        SetLanguage(label, language);
        return label;
    }

    public static void SetLanguage(Label label, string language)
    {
        label.style.unityTextGenerator = TextGeneratorType.Advanced;
        bool rtl = CallLanguage.RightToLeft(language);
        label.languageDirection = rtl ? LanguageDirection.RTL : LanguageDirection.LTR;
        label.style.unityTextAlign = rtl ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
    }

    static Label Pill(VisualElement parent, int size)
    {
        var label = Text(parent, "", size, Ink);
        label.style.position = Position.Absolute;
        label.style.left = Length.Percent(50);
        label.style.translate = new Translate(Length.Percent(-50), 0);
        label.style.backgroundColor = Panel;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.whiteSpace = WhiteSpace.NoWrap;
        Pad(label, 9, 18);
        Round(label, 18);
        return label;
    }

    static VisualElement Card(VisualElement parent)
    {
        var card = new VisualElement { pickingMode = PickingMode.Ignore };
        card.style.backgroundColor = Panel;
        Pad(card, 18, 22);
        Round(card, 14);
        SetBorder(card, 1, new Color(1f, 1f, 1f, 0.07f));
        parent.Add(card);
        return card;
    }

    public static Button MenuButton(VisualElement parent, string text, bool primary, Action onClick)
    {
        var button = new Button(onClick) { text = text };
        button.style.height = 46;
        button.style.marginTop = button.style.marginBottom = 5;
        button.style.marginLeft = button.style.marginRight = 0;
        button.style.fontSize = 17;
        button.style.unityFontStyleAndWeight = FontStyle.Bold;
        button.style.color = primary ? new Color(0.05f, 0.1f, 0.09f) : Ink;
        button.style.backgroundColor = primary ? Accent : new Color(1f, 1f, 1f, 0.08f);
        SetBorder(button, 0, Color.clear);
        Round(button, 10);
        button.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = primary ? new Color(0.48f, 0.92f, 0.82f) : new Color(1f, 1f, 1f, 0.16f));
        button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = primary ? Accent : new Color(1f, 1f, 1f, 0.08f));
        parent.Add(button);
        return button;
    }

    static Label Stepper(VisualElement parent, string name, Action down, Action up)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginTop = 8;
        row.style.marginBottom = 4;
        parent.Add(row);
        var label = Text(row, name, 15, Ink);
        label.style.flexGrow = 1;
        StepButton(row, "−", down);
        var value = Text(row, "", 15, Ink);
        value.style.width = 54;
        value.style.unityTextAlign = TextAnchor.MiddleCenter;
        StepButton(row, "+", up);
        return value;
    }

    static void StepButton(VisualElement parent, string text, Action onClick)
    {
        var b = new Button(onClick) { text = text };
        b.style.width = 36;
        b.style.height = 32;
        b.style.fontSize = 18;
        b.style.color = Ink;
        b.style.backgroundColor = new Color(1f, 1f, 1f, 0.1f);
        b.style.marginLeft = b.style.marginRight = 0;
        SetBorder(b, 0, Color.clear);
        Round(b, 8);
        parent.Add(b);
    }

    public static void Fill(VisualElement e)
    {
        e.style.position = Position.Absolute;
        e.style.left = e.style.right = e.style.top = e.style.bottom = 0;
    }

    static void Centre(VisualElement e, float w, float h)
    {
        e.style.position = Position.Absolute;
        e.style.width = w;
        e.style.height = h;
        e.style.left = Length.Percent(50);
        e.style.top = Length.Percent(50);
        e.style.translate = new Translate(-w * 0.5f, -h * 0.5f);
    }

    public static void Round(VisualElement e, float r) =>
        e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r;

    public static void Pad(VisualElement e, float v, float h)
    {
        e.style.paddingTop = e.style.paddingBottom = v;
        e.style.paddingLeft = e.style.paddingRight = h;
    }

    public static void SetBorder(VisualElement e, float width, Color colour)
    {
        e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = width;
        e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = colour;
    }
}
