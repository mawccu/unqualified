using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// What is on the battered computer: a cheap job site with two real vacancies
/// (a call center and a wedding crew), and the CV editor where the crew decides
/// how much to lie. The draft lives here (not in the UI) so getting up does not
/// lose it. Submitting stores claim ids and each player's language ids in
/// CrewState, which the shift reads back.
/// </summary>
public class ComputerScreen : MonoBehaviour
{
    static readonly Color Page = new(0.97f, 0.97f, 0.95f);
    static readonly Color Chrome = new(0.82f, 0.82f, 0.80f);
    static readonly Color Brand = new(0.13f, 0.32f, 0.78f);
    static readonly Color Yellow = new(1f, 0.85f, 0.2f);
    static readonly Color Body = new(0.16f, 0.16f, 0.18f);
    static readonly Color Faint = new(0.45f, 0.45f, 0.48f);
    static readonly Color Alarm = new(0.75f, 0.15f, 0.12f);

    static string[] Titles => CvLies.Titles;
    static string[] Experience => CvLies.Experience;
    const string LanguageConsequence = "Calls in this language may be assigned or transferred to you.";

    /// The draft, kept between visits to the desk.
    public static class Draft
    {
        public static string Name = "";
        public static int Title;
        public static int Experience;
        public static readonly HashSet<string> Claims = new();
        /// This player's claimed languages besides English (ids).
        public static readonly HashSet<string> Languages = new();
        public static string Job = Jobs.CallCenter;
        public static bool Loaded;
        public static bool Truthful => Lies == 0;

        /// The lies this draft tells, counted the way the shift will count them.
        public static int Lies
        {
            get
            {
                int claims = 0;
                foreach (var (id, _, _) in global::Claims.For(Job))
                    if (Claims.Contains(id))
                        claims++;
                return CvLies.Count(Title, Experience, claims, Job == Jobs.CallCenter ? Languages.Count : 0);
            }
        }
    }

    [SerializeField] Hud hud;

    VisualElement _window, _content;
    ScrollView _pageScroll;
    ComputerDesk _desk;
    Label _preview, _meterTier, _meterPay, _meterEffects;
    VisualElement _meterFill, _meter;

    public bool IsOpen => _window != null && _window.style.display == DisplayStyle.Flex;

    public void Configure(Hud owner) => hud = owner;

    public void Open(ComputerDesk desk)
    {
        _desk = desk;
        if (_window == null)
            BuildWindow();
        _window.style.display = DisplayStyle.Flex;
        if (string.IsNullOrEmpty(Draft.Name))
            Draft.Name = string.IsNullOrEmpty(CrewState.Current.cvName) ? "Your Name" : CrewState.Current.cvName;
        if (!Draft.Loaded)
        {
            // Start from what the CV already says (a repeat visit), not a blank page.
            var crew = CrewState.Current;
            Draft.Loaded = true;
            Draft.Title = Mathf.Max(0, System.Array.IndexOf(Titles, crew.cvTitle));
            Draft.Experience = Mathf.Max(0, System.Array.IndexOf(Experience, crew.cvExperience));
            Draft.Claims.UnionWith(crew.claims);
            Draft.Languages.UnionWith(crew.LanguagesOf(GameSession.OfflineActor));
        }
        ShowListings();
    }

    public void Close()
    {
        if (_window != null)
            _window.style.display = DisplayStyle.None;
    }

    // Window -----------------------------------------------------------------------------

    void BuildWindow()
    {
        var overlay = hud.Overlay;
        _window = new VisualElement();
        Hud.Fill(_window);
        _window.style.alignItems = Align.Center;
        _window.style.justifyContent = Justify.Center;
        _window.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
        overlay.Add(_window);

        var browser = new VisualElement();
        browser.style.width = 1240;
        browser.style.height = 760;
        browser.style.maxWidth = Length.Percent(96);
        browser.style.maxHeight = Length.Percent(94);
        browser.style.flexShrink = 0;
        browser.style.backgroundColor = Page;
        Hud.Round(browser, 6);
        Hud.SetBorder(browser, 10, new Color(0.72f, 0.69f, 0.6f));
        browser.style.overflow = Overflow.Hidden;
        _window.Add(browser);

        // Browser bar.
        var bar = Row(browser);
        bar.style.flexShrink = 0;
        bar.style.backgroundColor = Chrome;
        Hud.Pad(bar, 8, 14);
        var url = Hud.Text(bar, "http://www.jobjob.biz/listings?sort=desperate", 14, Faint);
        url.style.flexGrow = 1;
        url.style.backgroundColor = Color.white;
        Hud.Pad(url, 4, 10);
        Hud.Round(url, 4);
        var logoff = Button(bar, "Log off  (Esc)", false, () => _desk?.StandUp());
        logoff.style.marginLeft = 12;

        // Site header.
        var header = Row(browser);
        header.style.flexShrink = 0;
        header.style.backgroundColor = Brand;
        Hud.Pad(header, 14, 26);
        var logo = Hud.Text(header, "<b>JobJob</b><color=#FFD933>.biz</color>", 34, Color.white);
        logo.style.flexGrow = 1;
        Hud.Text(header, "Jobs for everyone*", 16, Yellow);

        _pageScroll = new ScrollView(ScrollViewMode.Vertical);
        _pageScroll.name = "computer-page-scroll";
        _pageScroll.style.flexGrow = 1;
        _pageScroll.style.flexBasis = 0;
        _pageScroll.style.minHeight = 0;
        _pageScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
        _pageScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        browser.Add(_pageScroll);

        _content = _pageScroll.contentContainer;
        _content.style.flexShrink = 0;
        Hud.Pad(_content, 22, 30);

        var footer = Hud.Text(browser, "*not everyone.   JobJob.biz is not responsible for anything that happens at any job.", 12, Faint);
        Hud.Pad(footer, 8, 30);
        footer.style.backgroundColor = new Color(0.92f, 0.92f, 0.9f);
        footer.style.flexShrink = 0;
    }

    void ClearPage()
    {
        _content.Clear();
        _preview = null;
        _meter = null;
        _pageScroll.scrollOffset = Vector2.zero;
    }

    // Pages ------------------------------------------------------------------------------

    void ShowListings()
    {
        ClearPage();
        Hud.Text(_content, "Today's listings", 26, Body).style.unityFontStyleAndWeight = FontStyle.Bold;
        Hud.Text(_content, "5 jobs near you.  3 of them are not for you.", 14, Faint).style.marginBottom = 14;

        var calls = Listing(_content, Jobs.Title(Jobs.CallCenter), $"{Jobs.Place(Jobs.CallCenter)}   ·   Evening shift", true);
        Hud.Text(calls, $"Pay: <b>from ${CrewState.CallCenterShiftPay} per shift</b>. The better your CV, the higher your grade.", 16, Body);
        Hud.Text(calls, "Duties: answer customer calls about deliveries, smart-home setup and refunds.", 16, Body);
        Hud.Text(calls, "<color=#BF2620><b>Requires: 2+ years in customer support. A second language is a big plus.</b></color>", 16, Body).style.marginTop = 4;
        ApplyRow(calls, Jobs.CallCenter);

        var wedding = Listing(_content, Jobs.Title(Jobs.Wedding), $"{Jobs.Place(Jobs.Wedding)}   ·   Tonight", true);
        Hud.Text(wedding, $"Pay: <b>from ${CrewState.WeddingShiftPay} per shift</b>. Experienced staff earn more.", 16, Body);
        Hud.Text(wedding, "Duties: dessert display, guest seating, ceremony sound.", 16, Body);
        Hud.Text(wedding, "<color=#BF2620><b>Requires: 5+ years of professional event experience.</b></color>", 16, Body).style.marginTop = 4;
        ApplyRow(wedding, Jobs.Wedding);

        Listing(_content, "Sushi Chef", "Applications closed", false);
        Listing(_content, "Locksmith", "Coming soon", false);
        Listing(_content, "Air Traffic Controller", "Position filled (thankfully)", false);
    }

    void ApplyRow(VisualElement card, string job)
    {
        var crew = CrewState.Current;
        if (crew.hired && crew.Job == job)
        {
            Hud.Text(card, "<color=#2E8B3E><b>Application accepted.</b> You start tonight.</color>", 16, Body).style.marginTop = 10;
            var edit = Button(card, "Update your CV", false, () => ShowCv(job));
            edit.style.alignSelf = Align.FlexStart;
            edit.style.marginTop = 8;
            return;
        }
        var apply = Button(card, crew.hired ? "Apply for this instead" : "Apply now", true, () => ShowCv(job));
        apply.style.alignSelf = Align.FlexStart;
        apply.style.marginTop = 10;
    }

    void ShowCv(string job)
    {
        Draft.Job = job;
        ClearPage();
        var top = Row(_content);
        var heading = Hud.Text(top, $"Build your CV: {Jobs.Title(job)}", 26, Body);
        heading.style.unityFontStyleAndWeight = FontStyle.Bold;
        heading.style.flexGrow = 1;
        Button(top, "Back to listings", false, ShowListings);
        Hud.Text(_content, job == Jobs.CallCenter
            ? "Brightline wants 2+ years of support experience, and loves a second language. Your CV, as of this morning, is empty."
            : "Wedding Crew Staff wants 5+ years of experience. Your CV, as of this morning, is empty.", 14, Faint).style.marginBottom = 12;

        var columns = Row(_content);
        columns.style.alignItems = Align.FlexStart;
        columns.style.flexWrap = Wrap.Wrap;
        columns.style.flexShrink = 0;

        var form = new VisualElement();
        form.style.width = 600;
        form.style.maxWidth = Length.Percent(100);
        form.style.flexShrink = 0;
        form.style.marginRight = 24;
        columns.Add(form);

        Caption("Your name", form);
        var name = new TextField { value = Draft.Name, maxLength = 32 };
        name.style.fontSize = 16;
        name.style.marginLeft = 0;
        name.RegisterValueChangedCallback(e =>
        {
            Draft.Name = e.newValue;
            RefreshPreview();
        });
        form.Add(name);

        Caption("Current title", form);
        Choices(form, Titles, () => Draft.Title, i => Draft.Title = i);
        Caption("Experience", form);
        Choices(form, Experience, () => Draft.Experience, i => Draft.Experience = i);

        Caption("Skills and specialisms", form);
        foreach (var (id, text, consequence) in Claims.For(job))
            ClaimToggle(form, id, text, consequence);

        // Languages are yours alone: teammates fill in their own. Only jobs that
        // use them ask (a language nobody tests would be a free lie).
        if (job == Jobs.CallCenter)
        {
            Caption("Languages (yours, not the crew's)", form);
            var english = new Button { text = "☑  English (native)" };
            StyleChoice(english, true);
            english.SetEnabled(false);
            english.style.alignSelf = Align.FlexStart;
            form.Add(english);
            foreach (var language in CallLanguage.Claimable)
                LanguageToggle(form, language);
        }

        var actions = Row(form);
        actions.style.marginTop = 16;
        Button(actions, "Tell the truth", false, () =>
        {
            Draft.Title = 0;
            Draft.Experience = 0;
            Draft.Claims.Clear();
            Draft.Languages.Clear();
            ShowCv(job);
        }).style.marginRight = 10;
        Button(actions, "Submit application", true, () => StartCoroutine(Submit()));

        // Live preview: a sheet of paper.
        var sheet = new VisualElement();
        sheet.style.flexGrow = 1;
        sheet.style.backgroundColor = Color.white;
        Hud.SetBorder(sheet, 1, new Color(0f, 0f, 0f, 0.15f));
        Hud.Pad(sheet, 22, 26);
        columns.Add(sheet);
        Hud.Text(sheet, "CURRICULUM VITAE", 12, Faint).style.letterSpacing = 3;
        _preview = Hud.Text(sheet, "", 16, Body);
        _preview.style.marginTop = 8;

        // The trade, live: every lie raises the pay grade and the heat.
        _meter = new VisualElement();
        _meter.style.marginTop = 16;
        _meter.style.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
        Hud.Round(_meter, 6);
        Hud.Pad(_meter, 12, 16);
        sheet.Add(_meter);
        Hud.Text(_meter, "LIE METER", 11, new Color(1f, 1f, 1f, 0.55f)).style.letterSpacing = 3;
        _meterTier = Hud.Text(_meter, "", 20, Color.white);
        _meterTier.style.unityFontStyleAndWeight = FontStyle.Bold;
        var track = new VisualElement();
        track.style.height = 10;
        track.style.marginTop = 6;
        track.style.backgroundColor = new Color(1f, 1f, 1f, 0.12f);
        Hud.Round(track, 5);
        _meterFill = new VisualElement();
        _meterFill.style.height = 10;
        Hud.Round(_meterFill, 5);
        track.Add(_meterFill);
        _meter.Add(track);
        _meterPay = Hud.Text(_meter, "", 16, Yellow);
        _meterPay.style.marginTop = 8;
        _meterEffects = Hud.Text(_meter, "", 13, new Color(1f, 1f, 1f, 0.8f));
        _meterEffects.style.marginTop = 4;
        RefreshPreview();
    }

    void RefreshMeter()
    {
        if (_meter == null)
            return;
        int lies = Draft.Lies;
        int basePay = Draft.Job == Jobs.CallCenter ? CrewState.CallCenterShiftPay : CrewState.WeddingShiftPay;
        _meterTier.text = $"{CvLies.Tier(lies)}  <size=14><color=#FFFFFF99>({lies} {(lies == 1 ? "lie" : "lies")})</color></size>";
        float fill = Mathf.Clamp01(lies / 9f);
        _meterFill.style.width = Length.Percent(Mathf.Max(3f, fill * 100f));
        _meterFill.style.backgroundColor = Color.Lerp(new Color(0.4f, 0.85f, 0.5f), new Color(0.95f, 0.25f, 0.2f), fill);
        _meterPay.text = lies == 0 ? $"Pay: ${basePay} per shift"
            : $"Pay: ${basePay}  →  <b>${CvLies.Pay(basePay, lies)}</b> per shift  (+{Mathf.RoundToInt(CvLies.PayPerLie * 100f)}% a lie)";
        _meterEffects.text = string.Join("\n", CvLies.Effects(lies, Draft.Job).ConvertAll(e => "·  " + e));
    }

    void RefreshPreview()
    {
        RefreshMeter();
        if (_preview == null)
            return;
        string name = string.IsNullOrWhiteSpace(Draft.Name) ? "Your Name" : Draft.Name;
        var text = $"<size=28><b>{Escape(name)}</b></size>\n{Titles[Draft.Title]}\n\n";
        if (Draft.Truthful)
        {
            text += "<b>No experience. Available immediately.</b>\n\n<color=#777777>Honest, at least.</color>";
        }
        else
        {
            text += $"<b>Experience</b>\n{Experience[Draft.Experience]} in {(Draft.Job == Jobs.CallCenter ? "customer support" : "events and hospitality")}\n\n";
            var claims = Claims.For(Draft.Job);
            if (System.Array.Exists(claims, c => Draft.Claims.Contains(c.id)))
            {
                text += "<b>Specialisms</b>\n";
                foreach (var (id, claim, _) in claims)
                    if (Draft.Claims.Contains(id))
                        text += $"·  {claim}\n";
                text += "\n";
            }
            if (Draft.Job == Jobs.CallCenter)
            {
                text += "<b>Languages</b>\nEnglish (native)";
                foreach (var language in CallLanguage.Claimable)
                    if (Draft.Languages.Contains(language))
                        text += $", {CallLanguage.Name(language)} (fluent)";
                text += "\n\n";
            }
            text += "<color=#BF2620>You will be expected to do all of this.</color>";
        }
        _preview.text = text;
    }

    IEnumerator Submit()
    {
        _desk?.PressEnter();
        yield return new WaitForSecondsRealtime(0.15f);
        ClearPage();
        var sending = Hud.Text(_content, "Sending application", 26, Body);
        sending.style.unityFontStyleAndWeight = FontStyle.Bold;
        sending.style.marginTop = 220;
        sending.style.alignSelf = Align.Center;
        for (int i = 0; i < 6; i++)
        {
            sending.text = "Sending application" + new string('.', i % 4);
            yield return new WaitForSecondsRealtime(0.3f);
        }

        // Stored as ids, not text: the shift reads these back.
        var crew = CrewState.Current;
        crew.cvName = string.IsNullOrWhiteSpace(Draft.Name) ? "Crew" : Draft.Name.Trim();
        crew.cvTitle = Titles[Draft.Title];
        crew.cvExperience = Experience[Draft.Experience];
        crew.claims = new List<string>();
        foreach (var (id, _, _) in Claims.For(Draft.Job))
            if (Draft.Claims.Contains(id))
                crew.claims.Add(id);
        crew.SetLanguages(GameSession.OfflineActor, Draft.Languages);
        crew.job = Draft.Job;
        crew.hired = true;
        crew.Save();
        OpeningDirector.Instance?.OnHired();

        ClearPage();
        var done = new VisualElement();
        done.style.alignSelf = Align.Center;
        done.style.alignItems = Align.Center;
        done.style.marginTop = 150;
        _content.Add(done);
        var title = Hud.Text(done, "You're hired.", 40, new Color(0.18f, 0.55f, 0.24f));
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        Hud.Text(done, Draft.Truthful
            ? "As temporary general staff. Bring your own shoes."
            : "We were impressed by your extensive experience.", 20, Body).style.marginTop = 8;
        Hud.Text(done, $"Your shift: {Jobs.Title(Draft.Job)}, {Jobs.Place(Draft.Job)}. Tonight. Do not be late.", 16, Faint).style.marginTop = 18;
        var close = Button(done, "Log off and get ready", true, () => _desk?.StandUp());
        close.style.marginTop = 26;
    }

    // Small parts --------------------------------------------------------------------------

    static VisualElement Row(VisualElement parent)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        parent.Add(row);
        return row;
    }

    static void Caption(string text, VisualElement parent)
    {
        var label = Hud.Text(parent, text, 14, Faint);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginTop = 12;
        label.style.marginBottom = 4;
    }

    static VisualElement Listing(VisualElement parent, string title, string subtitle, bool open)
    {
        var card = new VisualElement();
        card.style.backgroundColor = open ? Color.white : new Color(0.93f, 0.93f, 0.91f);
        Hud.SetBorder(card, open ? 2 : 1, open ? Brand : new Color(0f, 0f, 0f, 0.1f));
        Hud.Round(card, 6);
        Hud.Pad(card, 12, 18);
        card.style.marginBottom = 10;
        parent.Add(card);
        var t = Hud.Text(card, title, open ? 22 : 17, open ? Brand : Faint);
        t.style.unityFontStyleAndWeight = FontStyle.Bold;
        Hud.Text(card, subtitle, 14, Faint).style.marginBottom = open ? 8 : 0;
        return card;
    }

    void Choices(VisualElement parent, string[] options, System.Func<int> get, System.Action<int> set)
    {
        var row = Row(parent);
        row.style.flexWrap = Wrap.Wrap;
        var buttons = new List<Button>();
        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            var b = new Button { text = options[i] };
            buttons.Add(b);
            b.clicked += () =>
            {
                set(index);
                for (int k = 0; k < buttons.Count; k++)
                    StyleChoice(buttons[k], k == get());
                RefreshPreview();
            };
            StyleChoice(b, i == get());
            row.Add(b);
        }
    }

    void ClaimToggle(VisualElement parent, string id, string text, string consequence)
    {
        var b = new Button();
        b.style.flexDirection = FlexDirection.Column;
        b.style.alignItems = Align.FlexStart;
        b.style.marginBottom = 4;
        var label = Hud.Text(b, "", 15, Body);
        var note = Hud.Text(b, consequence, 12, Alarm);
        void Refresh()
        {
            bool on = Draft.Claims.Contains(id);
            label.text = (on ? "☑  " : "☐  ") + text;
            note.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            StyleChoice(b, on);
            label.style.color = on ? Color.white : Body;
            note.style.color = on ? Yellow : Alarm;
        }
        b.clicked += () =>
        {
            if (!Draft.Claims.Remove(id))
                Draft.Claims.Add(id);
            Refresh();
            RefreshPreview();
        };
        Refresh();
        parent.Add(b);
    }

    void LanguageToggle(VisualElement parent, string language)
    {
        var b = new Button();
        b.name = "language-" + language;
        b.style.flexDirection = FlexDirection.Column;
        b.style.alignItems = Align.FlexStart;
        b.style.marginBottom = 4;
        var row = Row(b);
        var label = Hud.Text(row, "", 15, Body);
        var native = Hud.LocalText(row, CallLanguage.NativeName(language), 15, Faint, language);
        native.style.marginLeft = 10;
        var note = Hud.Text(b, LanguageConsequence, 12, Alarm);
        void Refresh()
        {
            bool on = Draft.Languages.Contains(language);
            label.text = (on ? "☑  " : "☐  ") + $"{CallLanguage.Name(language)} (fluent)";
            note.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            StyleChoice(b, on);
            label.style.color = on ? Color.white : Body;
            native.style.color = on ? Yellow : Faint;
            note.style.color = on ? Yellow : Alarm;
        }
        b.clicked += () =>
        {
            if (!Draft.Languages.Remove(language))
                Draft.Languages.Add(language);
            Refresh();
            RefreshPreview();
        };
        Refresh();
        parent.Add(b);
    }

    static void StyleChoice(Button b, bool on)
    {
        b.style.backgroundColor = on ? Brand : Color.white;
        b.style.color = on ? Color.white : Body;
        b.style.fontSize = 14;
        Hud.SetBorder(b, 1, on ? Brand : new Color(0f, 0f, 0f, 0.2f));
        Hud.Round(b, 5);
        Hud.Pad(b, 6, 12);
        b.style.marginLeft = 0;
        b.style.marginRight = 6;
        b.style.marginTop = 2;
    }

    static Button Button(VisualElement parent, string text, bool primary, System.Action onClick)
    {
        var b = new Button(onClick) { text = text };
        b.style.fontSize = 16;
        b.style.unityFontStyleAndWeight = FontStyle.Bold;
        b.style.backgroundColor = primary ? new Color(0.98f, 0.62f, 0.12f) : Color.white;
        b.style.color = primary ? Color.white : Body;
        Hud.SetBorder(b, 1, primary ? new Color(0.85f, 0.5f, 0.08f) : new Color(0f, 0f, 0f, 0.25f));
        Hud.Round(b, 5);
        Hud.Pad(b, 8, 16);
        b.style.marginLeft = 0;
        parent.Add(b);
        return b;
    }

    static string Escape(string s) => s.Replace("<", "‹").Replace(">", "›");
}
