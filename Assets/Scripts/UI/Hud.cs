using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Everything drawn over the game: crosshair, prompts, short feedback, the task
/// list, the coordinator's whereabouts, the F3 frame overlay and the pause menu.
/// Built in code on a UIDocument so the look lives in one place.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class Hud : MonoBehaviour
{
    static readonly Color Panel = new(0.07f, 0.08f, 0.09f, 0.72f);
    static readonly Color Ink = new(0.96f, 0.94f, 0.9f);
    static readonly Color Muted = new(0.96f, 0.94f, 0.9f, 0.62f);
    static readonly Color Accent = new(0.36f, 0.85f, 0.74f);
    static readonly Color Warn = new(0.98f, 0.66f, 0.3f);
    static readonly Color Done = new(0.55f, 0.88f, 0.45f);

    public event Action ResumeRequested, RestartRequested, QuitRequested;

    VisualElement _root, _crosshair, _ring, _tasks, _menu, _banner;
    Label _prompt, _toast, _coordinator, _stats, _sensitivityValue, _fovValue;
    float _toastLeft;
    float _statsTimer;
    int _frames;

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
        _prompt = Pill(_root, 15);
        _prompt.style.top = Length.Percent(55);
        _toast = Pill(_root, 15);
        _toast.style.top = Length.Percent(63);
        _toast.style.color = Warn;
        BuildTasks();
        BuildCoordinator();
        BuildHelp();
        BuildStats();
        BuildBanner();
        BuildMenu();
        SetPrompt("");
        _toast.style.display = DisplayStyle.None;
    }

    void Update()
    {
        if (_root == null)
            return;
        if (_toastLeft > 0f)
        {
            _toastLeft -= Time.unscaledDeltaTime;
            _toast.style.opacity = Mathf.Clamp01(_toastLeft / 0.4f);
            if (_toastLeft <= 0f)
                _toast.style.display = DisplayStyle.None;
        }
        _frames++;
        _statsTimer += Time.unscaledDeltaTime;
        if (_statsTimer >= 0.5f && _stats.style.display == DisplayStyle.Flex)
        {
            float fps = _frames / _statsTimer;
            _stats.text = $"{fps:0} fps   {1000f / Mathf.Max(fps, 0.01f):0.0} ms   vsync {QualitySettings.vSyncCount}   {Screen.width}x{Screen.height}";
            _frames = 0;
            _statsTimer = 0f;
        }
        else if (_statsTimer >= 0.5f)
        {
            _frames = 0;
            _statsTimer = 0f;
        }
    }

    // Public API ----------------------------------------------------------------

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
        _toastLeft = 2.2f;
    }

    public void SetAim(bool actionable)
    {
        Build();
        _ring.style.display = actionable ? DisplayStyle.Flex : DisplayStyle.None;
        _crosshair.style.backgroundColor = actionable ? Accent : new Color(1f, 1f, 1f, 0.85f);
    }

    public void SetCoordinator(string state)
    {
        Build();
        _coordinator.text = $"<color=#F2C88C>Coordinator</color>   {state}";
    }

    public void SetTasks(IReadOnlyList<TaskBoard.Result> results)
    {
        Build();
        _tasks.Clear();
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
            var name = Text(text, r.Name, 16, Ink);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            if (r.Status == TaskBoard.Status.NotBuilt)
                name.style.color = Muted;
            var status = Text(text, r.Status == TaskBoard.Status.NotStarted || r.Status == TaskBoard.Status.InProgress ? $"{r.Goal}.  {r.Text}" : r.Text, 13, Muted);
            if (r.Status == TaskBoard.Status.NeedsFixing)
                status.style.color = Warn;
            if (r.Status == TaskBoard.Status.Done)
                status.style.color = Done;
            row.Add(text);
            _tasks.Add(row);
        }
        _banner.style.display = TaskBoard.AllDone(results) ? DisplayStyle.Flex : DisplayStyle.None;
    }

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

    // Building ------------------------------------------------------------------

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

    void BuildTasks()
    {
        var card = Card(_root);
        card.style.position = Position.Absolute;
        card.style.left = 28;
        card.style.top = 28;
        card.style.width = 380;
        var eyebrow = Text(card, "WEDDING CREW", 12, Accent);
        eyebrow.style.letterSpacing = 3;
        eyebrow.style.unityFontStyleAndWeight = FontStyle.Bold;
        Text(card, "Tonight's essentials", 20, Ink).style.unityFontStyleAndWeight = FontStyle.Bold;
        _tasks = new VisualElement();
        card.Add(_tasks);
    }

    void BuildCoordinator()
    {
        _coordinator = Pill(_root, 14);
        _coordinator.style.left = 28;
        _coordinator.style.bottom = 28;
        _coordinator.style.top = StyleKeyword.Auto;
        _coordinator.style.translate = new Translate(0, 0);
        _coordinator.style.unityTextAlign = TextAnchor.MiddleLeft;
    }

    void BuildHelp()
    {
        var help = Text(_root, "WASD move    Mouse look    Shift sprint    E pick up    Click place    Q drop    Esc menu    F3 frame rate    F5 restart", 13, new Color(1f, 1f, 1f, 0.45f));
        help.style.position = Position.Absolute;
        help.style.bottom = 30;
        help.style.right = 28;
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
        var title = Text(_banner, "SHIFT COMPLETE", 26, Done);
        title.style.letterSpacing = 4;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        Text(_banner, "Nobody suspects a thing. Yet.", 15, Muted);
        _banner.style.display = DisplayStyle.None;
    }

    void BuildMenu()
    {
        _menu = new VisualElement();
        _menu.style.position = Position.Absolute;
        _menu.style.left = _menu.style.right = _menu.style.top = _menu.style.bottom = 0;
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
        Text(card, "Paused. Wedding Crew shift.", 15, Muted).style.marginBottom = 22;

        MenuButton(card, "Resume", true, () => ResumeRequested?.Invoke());
        MenuButton(card, "Restart the shift", false, () => RestartRequested?.Invoke());
        _sensitivityValue = Stepper(card, "Mouse sensitivity", () => GameSettings.MouseSensitivity -= 0.1f, () => GameSettings.MouseSensitivity += 0.1f);
        _fovValue = Stepper(card, "Field of view", () => GameSettings.HorizontalFov -= 5f, () => GameSettings.HorizontalFov += 5f);
        MenuButton(card, "Quit", false, () => QuitRequested?.Invoke());
        GameSettings.Changed += RefreshSettingValues;
        _menu.style.display = DisplayStyle.None;
    }

    void OnDisable() => GameSettings.Changed -= RefreshSettingValues;

    void RefreshSettingValues()
    {
        if (_sensitivityValue == null)
            return;
        _sensitivityValue.text = GameSettings.MouseSensitivity.ToString("0.0");
        _fovValue.text = $"{GameSettings.HorizontalFov:0}°";
    }

    // Small building blocks -----------------------------------------------------

    static Label Text(VisualElement parent, string text, int size, Color colour)
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

    static void MenuButton(VisualElement parent, string text, bool primary, Action onClick)
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

    static void Centre(VisualElement e, float w, float h)
    {
        e.style.position = Position.Absolute;
        e.style.width = w;
        e.style.height = h;
        e.style.left = Length.Percent(50);
        e.style.top = Length.Percent(50);
        e.style.translate = new Translate(-w * 0.5f, -h * 0.5f);
    }

    static void Round(VisualElement e, float r) =>
        e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r;

    static void Pad(VisualElement e, float v, float h)
    {
        e.style.paddingTop = e.style.paddingBottom = v;
        e.style.paddingLeft = e.style.paddingRight = h;
    }

    static void SetBorder(VisualElement e, float width, Color colour)
    {
        e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = width;
        e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = colour;
    }
}
