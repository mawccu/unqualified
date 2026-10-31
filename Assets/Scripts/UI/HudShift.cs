using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// The shift's part of the HUD: phase and clock, the coordinator's suspicion,
/// a wobble meter while carrying something wide, short reply choices when she
/// questions you, a readable page (guest book), and the end-of-shift payout.
/// </summary>
public partial class Hud
{
    VisualElement _phaseBox, _suspicionBox, _suspicionFill, _wobbleBox, _wobbleFill, _choices, _page, _payout, _gadgetBox, _gadgetFill;
    Label _phaseLabel, _clockLabel, _suspicionLabel, _pageText, _payoutText, _gadgetLabel, _gadgetWarn;
    Action<int> _choiceCallback;
    float _choiceLeft;
    Action _payoutContinue, _payoutRetry;
    Button _payoutRetryButton;

    void BuildShiftUi()
    {
        // Phase and clock, top centre.
        _phaseBox = new VisualElement { pickingMode = PickingMode.Ignore };
        _phaseBox.style.position = Position.Absolute;
        _phaseBox.style.top = 24;
        _phaseBox.style.left = Length.Percent(50);
        _phaseBox.style.translate = new Translate(Length.Percent(-50), 0);
        _phaseBox.style.alignItems = Align.Center;
        _phaseBox.style.backgroundColor = Panel;
        Pad(_phaseBox, 8, 20);
        Round(_phaseBox, 12);
        _phaseLabel = Text(_phaseBox, "", 13, Accent);
        _phaseLabel.style.letterSpacing = 2;
        _phaseLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _clockLabel = Text(_phaseBox, "", 22, Ink);
        _clockLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _root.Add(_phaseBox);
        _phaseBox.style.display = DisplayStyle.None;

        // Suspicion, top right.
        _suspicionBox = new VisualElement { pickingMode = PickingMode.Ignore };
        _suspicionBox.style.position = Position.Absolute;
        _suspicionBox.style.top = 28;
        _suspicionBox.style.right = 28;
        _suspicionBox.style.width = 250;
        _suspicionBox.style.backgroundColor = Panel;
        Pad(_suspicionBox, 10, 14);
        Round(_suspicionBox, 10);
        _suspicionLabel = Text(_suspicionBox, "", 13, Ink);
        var track = new VisualElement();
        track.style.height = 8;
        track.style.marginTop = 6;
        track.style.backgroundColor = new Color(1f, 1f, 1f, 0.12f);
        Round(track, 4);
        _suspicionFill = new VisualElement();
        _suspicionFill.style.height = 8;
        Round(_suspicionFill, 4);
        track.Add(_suspicionFill);
        _suspicionBox.Add(track);
        _root.Add(_suspicionBox);
        _suspicionBox.style.display = DisplayStyle.None;

        // Wobble meter under the crosshair.
        _wobbleBox = new VisualElement { pickingMode = PickingMode.Ignore };
        _wobbleBox.style.position = Position.Absolute;
        _wobbleBox.style.left = Length.Percent(50);
        _wobbleBox.style.top = Length.Percent(50);
        _wobbleBox.style.translate = new Translate(-60, 26);
        _wobbleBox.style.width = 120;
        _wobbleBox.style.height = 6;
        _wobbleBox.style.backgroundColor = new Color(0f, 0f, 0f, 0.4f);
        Round(_wobbleBox, 3);
        _wobbleFill = new VisualElement();
        _wobbleFill.style.height = 6;
        Round(_wobbleFill, 3);
        _wobbleBox.Add(_wobbleFill);
        _root.Add(_wobbleBox);
        _wobbleBox.style.display = DisplayStyle.None;

        // The packed gadget, bottom left above the status line: key, name, charge, state, and a warning when someone is close.
        _gadgetBox = new VisualElement { pickingMode = PickingMode.Ignore };
        _gadgetBox.style.position = Position.Absolute;
        _gadgetBox.style.left = 28;
        _gadgetBox.style.bottom = 74;
        _gadgetBox.style.width = 300;
        _gadgetBox.style.backgroundColor = Panel;
        Pad(_gadgetBox, 8, 12);
        Round(_gadgetBox, 10);
        _gadgetLabel = Text(_gadgetBox, "", 14, Ink);
        var gadgetTrack = new VisualElement();
        gadgetTrack.style.height = 6;
        gadgetTrack.style.marginTop = 5;
        gadgetTrack.style.backgroundColor = new Color(1f, 1f, 1f, 0.12f);
        Round(gadgetTrack, 3);
        _gadgetFill = new VisualElement();
        _gadgetFill.style.height = 6;
        Round(_gadgetFill, 3);
        gadgetTrack.Add(_gadgetFill);
        _gadgetBox.Add(gadgetTrack);
        _gadgetWarn = Text(_gadgetBox, "", 13, new Color(1f, 0.45f, 0.35f));
        _gadgetWarn.style.marginTop = 4;
        _root.Add(_gadgetBox);
        _gadgetBox.style.display = DisplayStyle.None;

        // Reply choices, lower centre.
        _choices = new VisualElement { pickingMode = PickingMode.Ignore };
        _choices.style.position = Position.Absolute;
        _choices.style.bottom = 170;
        _choices.style.left = Length.Percent(50);
        _choices.style.translate = new Translate(Length.Percent(-50), 0);
        _choices.style.backgroundColor = Panel;
        Pad(_choices, 12, 18);
        Round(_choices, 12);
        _root.Add(_choices);
        _choices.style.display = DisplayStyle.None;

        // A page held up to read (the guest book).
        _page = new VisualElement { pickingMode = PickingMode.Ignore };
        _page.style.position = Position.Absolute;
        _page.style.right = 60;
        _page.style.top = Length.Percent(18);
        _page.style.width = 380;
        _page.style.backgroundColor = new Color(0.97f, 0.95f, 0.88f);
        Pad(_page, 20, 24);
        Round(_page, 4);
        _pageText = Text(_page, "", 15, new Color(0.15f, 0.15f, 0.17f));
        _root.Add(_page);
        _page.style.display = DisplayStyle.None;

        // Payout / exposure panel.
        _payout = new VisualElement();
        Fill(_payout);
        _payout.style.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 0.75f);
        _payout.style.alignItems = Align.Center;
        _payout.style.justifyContent = Justify.Center;
        var card = Card(_payout);
        card.style.width = 560;
        Pad(card, 28, 32);
        _payoutText = Text(card, "", 16, Ink);
        var buttons = new VisualElement();
        buttons.style.marginTop = 18;
        card.Add(buttons);
        MenuButton(buttons, "Go home and rest", true, () => _payoutContinue?.Invoke());
        _payoutRetryButton = MenuButton(buttons, "Try this shift again", false, () => _payoutRetry?.Invoke());
        _root.Add(_payout);
        _payout.style.display = DisplayStyle.None;
    }

    void UpdateShiftUi()
    {
        if (_choiceCallback == null)
            return;
        _choiceLeft -= Time.unscaledDeltaTime;
        int picked = -1;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) picked = 0;
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) picked = 1;
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) picked = 2;
        }
        if (picked >= 0 || _choiceLeft <= 0f)
            PickChoice(picked);
    }

    /// Replies on 1, 2, 3. The callback gets the index, or -1 if time ran out.
    public void ShowChoices(IReadOnlyList<string> options, float seconds, Action<int> picked)
    {
        Build();
        _choices.Clear();
        var title = Text(_choices, "REPLY", 11, Warn);
        title.style.letterSpacing = 2;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        for (int i = 0; i < options.Count; i++)
            Text(_choices, $"<b>{i + 1}</b>   {options[i]}", 17, Ink).style.marginTop = 6;
        _choices.style.display = DisplayStyle.Flex;
        _choiceCallback = picked;
        _choiceLeft = seconds;
    }

    /// Picks a reply as if its key was pressed (tests, the playtest driver).
    public void PickChoice(int index)
    {
        var callback = _choiceCallback;
        _choiceCallback = null;
        _choices.style.display = DisplayStyle.None;
        callback?.Invoke(index);
    }

    public bool ChoicesOpen => _choiceCallback != null;

    public void SetPhase(string phase, string clock)
    {
        Build();
        _phaseBox.style.display = string.IsNullOrEmpty(phase) ? DisplayStyle.None : DisplayStyle.Flex;
        _phaseLabel.text = phase;
        _clockLabel.text = clock;
        _clockLabel.style.display = string.IsNullOrEmpty(clock) ? DisplayStyle.None : DisplayStyle.Flex;
    }

    public void SetSuspicion(float value, Suspicion.Level level, string doing, string who = "Coordinator")
    {
        Build();
        _suspicionBox.style.display = DisplayStyle.Flex;
        string word = level switch
        {
            Suspicion.Level.Watching => "watching you",
            Suspicion.Level.Questioning => "suspicious",
            Suspicion.Level.Warning => "one more and you're out",
            Suspicion.Level.Exposed => "EXPOSED",
            _ => "calm",
        };
        _suspicionLabel.text = $"<color=#F2C88C>{who}</color>  {word}" + (string.IsNullOrEmpty(doing) ? "" : $"\n<size=12><color=#FFFFFFAA>{doing}</color></size>");
        _suspicionFill.style.width = Length.Percent(Mathf.Clamp(value, 0f, 100f));
        _suspicionFill.style.backgroundColor = level switch
        {
            Suspicion.Level.Calm => Done,
            Suspicion.Level.Watching => new Color(0.95f, 0.85f, 0.35f),
            Suspicion.Level.Questioning => Warn,
            _ => new Color(0.95f, 0.3f, 0.25f),
        };
    }

    /// The gadget chip: null hides it. charge 0..1; warning is shown in red under the bar.
    public void SetGadget(string richText, float charge, string warning)
    {
        Build();
        if (richText == null)
        {
            _gadgetBox.style.display = DisplayStyle.None;
            return;
        }
        _gadgetBox.style.display = DisplayStyle.Flex;
        _gadgetLabel.text = richText;
        _gadgetFill.style.width = Length.Percent(Mathf.Clamp01(charge) * 100f);
        _gadgetFill.style.backgroundColor = charge > 0.5f ? Done : charge > 0.2f ? new Color(0.95f, 0.85f, 0.35f) : new Color(0.95f, 0.3f, 0.25f);
        _gadgetWarn.text = warning ?? "";
        _gadgetWarn.style.display = string.IsNullOrEmpty(warning) ? DisplayStyle.None : DisplayStyle.Flex;
    }

    public string GadgetText => _gadgetBox != null && _gadgetBox.style.display == DisplayStyle.Flex ? _gadgetLabel.text + " | " + _gadgetWarn.text : "";

    /// 0..1, or a negative number to hide it.
    public void SetWobble(float wobble)
    {
        Build();
        if (wobble < 0f)
        {
            _wobbleBox.style.display = DisplayStyle.None;
            return;
        }
        _wobbleBox.style.display = DisplayStyle.Flex;
        _wobbleFill.style.width = Length.Percent(Mathf.Clamp01(wobble) * 100f);
        _wobbleFill.style.backgroundColor = wobble < 0.45f ? Done : wobble < 0.75f ? new Color(0.95f, 0.85f, 0.35f) : new Color(0.95f, 0.3f, 0.25f);
    }

    public void ShowPage(string richText)
    {
        Build();
        _page.style.display = richText == null ? DisplayStyle.None : DisplayStyle.Flex;
        if (richText != null)
            _pageText.text = richText;
    }

    /// The end of the shift: the money and the reasons. Retry is offered when given.
    public void ShowPayout(string richText, Action goHome, Action retry, string retryLabel = "Try this shift again")
    {
        Build();
        _payoutText.text = richText;
        _payoutRetryButton.text = retryLabel;
        _payoutContinue = goHome;
        _payoutRetry = retry;
        _payout.style.display = DisplayStyle.Flex;
    }

    public bool PayoutOpen => _payout != null && _payout.style.display == DisplayStyle.Flex;
}
