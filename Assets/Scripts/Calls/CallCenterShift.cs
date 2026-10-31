using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The call center shift: the router, the people on the phones, the stream of
/// customers, the supervisor's checks, and the pay at the end. It is the
/// authority for every call: consoles and coworkers ask it to do things (answer,
/// ask, look up, fix, transfer) and it applies them to the router.
///
/// What the CV said changes the shift: claimed languages bring calls in those
/// languages to you (and colleagues transfer them to you), the technical claim
/// routes the hard variants to you, customer service makes the supervisor listen
/// in more often, and pressure adds a rush. Each claim has a bonus if you pull
/// it off. A call in a foreign language is not suspicious; failing again and
/// again at a skill you claimed, promising what the system cannot do, and
/// marking unfixed calls as resolved are.
///
/// Multiplayer is not implemented: the coworkers are simulated workers in the
/// same router a teammate would join.
/// </summary>
public class CallCenterShift : MonoBehaviour
{
    public enum Phase { Briefing, Calls, WrapUp, Payout, Exposed }

    const float ShiftSeconds = 540f;
    const float HardStopSeconds = 60f;
    const int MaxDeductions = 90;
    const float CallbackDelay = 40f;
    const float ReviewDelay = 18f;

    [SerializeField] GameSession session;
    [SerializeField] SupportDesk[] desks;
    [SerializeField] Supervisor supervisor;
    [SerializeField] Transform breakSpot;

    public static CallCenterShift Instance { get; private set; }
    public CallRouter Router { get; private set; }
    public Phase Current { get; private set; } = Phase.Briefing;
    public bool Paid => _paid;
    public float Clock { get; private set; }
    public string PlayerId => GameSession.OfflineActor;
    public Supervisor Boss => supervisor;
    public IReadOnlyList<SupportDesk> Desks => desks;
    public IReadOnlyList<CallCase> Finished => _finished;
    /// The CV's lies: they set the pay grade and the heat (see CvLies).
    public int Lies { get; private set; }
    int Pay => CvLies.Pay(CrewState.CallCenterShiftPay, Lies);
    /// Something about a call changed (consoles redraw).
    public event Action<CallCase> Changed;

    readonly List<CallCase> _finished = new();
    readonly List<(float at, string caseId, string language, bool known, bool rush)> _schedule = new();
    readonly HashSet<int> _rush = new();
    readonly Dictionary<string, int> _skillFailures = new();
    readonly List<string> _notes = new();
    readonly Dictionary<string, SupportDesk> _deskOf = new();
    bool _paid, _started, _warned;
    int _next;
    float _wrapStarted, _cardTimer;

    public void Configure(GameSession owner, SupportDesk[] allDesks, Supervisor boss, Transform breakArea)
    {
        session = owner;
        desks = allDesks;
        supervisor = boss;
        breakSpot = breakArea;
    }

    void Awake()
    {
        Instance = this;
        Router = new CallRouter();
        var crew = CrewState.Current;
        Lies = CvLies.Count(crew, PlayerId);
        Router.Impatience = CvLies.Impatience(Lies);
        // The local player first; the coworkers are simulated (each claimed only English).
        var you = Router.AddWorker(PlayerId, string.IsNullOrEmpty(crew.cvName) ? "You" : crew.cvName,
            crew.LanguagesOf(PlayerId), crew.HasClaim(Claims.Technical), false);
        desks[0].Assign(you.Id, you.Name, true);
        _deskOf[you.Id] = desks[0];
        foreach (var desk in desks.Skip(1))
        {
            var mate = desk.GetComponent<Coworker>();
            if (mate == null)
                continue;
            var w = Router.AddWorker(mate.WorkerId, mate.DisplayName, null, mate.Technical, true);
            desk.Assign(w.Id, w.Name, false);
            _deskOf[w.Id] = desk;
            if (mate.StartsOnBreak)
                Router.SetBreak(w.Id, true);
        }
        Router.Assigned += (call, worker) =>
        {
            if (worker.Id == PlayerId)
                session.Hud.ShowToast(call.Transfers.Count > 0 ? "A transferred call is ringing at your desk." : "Your phone is ringing.");
            Changed?.Invoke(call);
        };
        Router.Transferred += (call, record) =>
        {
            DeskOf(record.From)?.Phone.FlashTransfer();
            Changed?.Invoke(call);
        };
        Router.Ended += OnEnded;
        BuildSchedule(crew);
        GadgetKit.Attach(session, Jobs.CallCenter);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public SupportDesk DeskOf(string workerId) => workerId != null && _deskOf.TryGetValue(workerId, out var d) ? d : null;
    public CallCase CallOf(string workerId) => Router.CallOf(workerId);

    // The stream of customers ----------------------------------------------------------------

    void BuildSchedule(CrewState crew)
    {
        // Every language turns up; whether it reaches you depends on your CV.
        // Some callers skip the language menu, so a colleague hears them first.
        // A bolder CV gets a busier evening: the same callers, closer together.
        float pace = CvLies.TimeScale(Lies);
        void Add(float at, string id, string language, bool known = true, bool rush = false) => _schedule.Add((at * pace, id, language, known, rush));
        Add(4, "delivery_kettle", CallLanguage.English);
        Add(26, "refund_toaster", CallLanguage.English);
        Add(44, "setup_plug", CallLanguage.Spanish, known: false);
        Add(80, "delivery_speaker", CallLanguage.English);
        Add(112, "refund_headphones", CallLanguage.Arabic);
        Add(150, "setup_vacuum", CallLanguage.Spanish);
        Add(196, "delivery_kettle", CallLanguage.Arabic, known: false);
        Add(240, "refund_toaster", CallLanguage.Spanish);
        Add(286, "setup_plug", CallLanguage.English);
        Add(330, "delivery_speaker", CallLanguage.Spanish, known: false);
        Add(372, "refund_headphones", CallLanguage.English);
        Add(410, "setup_vacuum", CallLanguage.Arabic);
        if (crew.HasClaim(Claims.Pressure))
        {
            Add(300, "delivery_kettle", CallLanguage.English, rush: true);
            Add(306, "refund_toaster", CallLanguage.English, rush: true);
            Add(312, "setup_plug", CallLanguage.English, rush: true);
        }
        _schedule.Sort((a, b) => a.at.CompareTo(b.at));
    }

    void Start()
    {
        ProceduralAudio.Ambience(gameObject, ProceduralAudio.RoomTone, 0.3f);
        session.Hud.SetHelp("WASD move    E use    Sit at your desk to take calls    Esc stand up / menu");
        StartCoroutine(Briefing());
    }

    IEnumerator Briefing()
    {
        var crew = CrewState.Current;
        Current = Phase.Briefing;
        session.Hud.SetBlack(1f);
        session.Hud.Fade(0f, 0.8f);
        yield return new WaitForSeconds(0.8f);
        supervisor.Greet(session.Player.transform);
        var langs = crew.LanguagesOf(PlayerId);
        yield return Say("Supervisor", "Welcome to Brightline. Your desk is the one with your name on it.", 3.2f);
        if (langs.Count > 0)
            yield return Say("Supervisor", $"Your CV says you're fluent in {string.Join(" and ", langs.Select(CallLanguage.Name))}. Those callers come to you.", 3.6f);
        else
            yield return Say("Supervisor", "English only? Fine. If a caller doesn't speak it, use the phrase aid.", 3.4f);
        if (crew.HasClaim(Claims.Technical))
            yield return Say("Supervisor", "And as our technical expert, you'll get the tricky ones.", 3f);
        if (crew.HasClaim(Claims.Service))
            yield return Say("Supervisor", "Award-winning service, it says. I'll be listening in to learn from you.", 3.4f);
        if (Lies >= 5)
            yield return Say("Supervisor", "Quite a CV. We hired you at a senior grade, so I'll expect senior work.", 3.4f);
        yield return Say("Supervisor", "Answer, find the order, fix it, close it. Don't promise what the system can't do.", 3.6f);
        supervisor.ResumePatrol();
        Begin();
    }

    void Begin()
    {
        if (_started)
            return;
        _started = true;
        Current = Phase.Calls;
        Clock = 0f;
    }

    IEnumerator Say(string speaker, string line, float seconds)
    {
        session.Hud.ShowSubtitle(speaker, line, seconds + 0.2f);
        yield return new WaitForSeconds(seconds);
    }

    void Update()
    {
        if (Current == Phase.Calls || Current == Phase.WrapUp)
        {
            Clock += Time.deltaTime;
            Router.Tick(Time.deltaTime);
            while (_next < _schedule.Count && _schedule[_next].at <= Clock)
            {
                var s = _schedule[_next++];
                var call = Router.Ring(CaseLibrary.Get(s.caseId), s.language, languageKnown: s.known);
                if (s.rush)
                    _rush.Add(call.Id);
                Changed?.Invoke(call);
            }
            foreach (var call in Router.Calls.Where(c => c.Live && c.State == CallState.OnHold && !c.HoldComplained && Router.Now - c.HoldSince > 25f))
            {
                call.HoldComplained = true;
                call.CustomerSays("hold_long", call.Owner);
                Changed?.Invoke(call);
            }
            if (Current == Phase.Calls && Clock >= ShiftSeconds)
            {
                Current = Phase.WrapUp;
                _wrapStarted = Clock;
                session.Hud.ShowBanner("END OF SHIFT", "Finish the call you are on. The phones are off.");
            }
            if (Current == Phase.WrapUp)
            {
                bool yours = CallOf(PlayerId) != null;
                if (!yours || Clock - _wrapStarted > HardStopSeconds)
                    Payout();
            }
        }
        foreach (var desk in desks)
            desk.Phone.Show(desk.WorkerId != null ? Router.CallOf(desk.WorkerId) : null);
        UpdateHud();
    }

    void UpdateHud()
    {
        var hud = session.Hud;
        if (Current == Phase.Payout || Current == Phase.Exposed)
            return;
        float shown = Mathf.Min(Clock, ShiftSeconds) / ShiftSeconds * 4f * 60f;   // 18:00 to 22:00
        hud.SetPhase(Current == Phase.Briefing ? "BRIEFING" : Current == Phase.WrapUp ? "WRAP UP" : "ON THE PHONES", $"{18 + (int)(shown / 60f)}:{(int)(shown % 60f):00}");
        hud.SetSuspicion(supervisor.Suspicion.Value, supervisor.Suspicion.State, supervisor.Doing, "Supervisor");
        // A reference binder in your hand: its page.
        hud.ShowPage(ReferenceShelf.PageFor(session.Player.HeldProp));
        _cardTimer -= Time.unscaledDeltaTime;
        if (_cardTimer > 0f)
            return;
        _cardTimer = 0.25f;
        var mine = CallOf(PlayerId);
        int waiting = Router.Waiting.Count();
        string line = mine == null ? "No call. Waiting for the next one."
            : mine.State == CallState.Ringing ? $"<color=#FF6B5B><b>RINGING</b></color>  {CallLanguage.Name(mine.RoutedLanguage)}{(mine.Transfers.Count > 0 ? " (transferred)" : "")}"
            : mine.State == CallState.OnHold ? $"<color=#FFB13B><b>ON HOLD</b></color>  {CallLanguage.Name(mine.Language)} caller"
            : $"<color=#5BEA7A><b>ON A CALL</b></color>  {(CallLanguage.RightToLeft(mine.Language) ? "caller" : mine.CustomerName)}, {CallLanguage.Name(mine.Language)}";
        int resolved = _finished.Count(c => c.Outcome == CallOutcome.Resolved && OwnerAtEnd(c) == PlayerId);
        // At the desk the console says all of this; the card would only cover it.
        if (PlayerSeated)
            hud.HideCard();
        else
            hud.SetCard("BRIGHTLINE HOME SUPPORT", "Take calls", line, $"Queue: {waiting}    Fixed by you: {resolved}", "Sit at your desk to take calls.");
    }

    bool PlayerSeated => desks[0].Occupied;

    static string OwnerAtEnd(CallCase c) => c.Owner;

    // Doing things on a call (the authority side) --------------------------------------------

    /// One action by a worker on their current call. Returns a line for their console.
    public string Do(string workerId, string intent, string arg = "", string label = "")
    {
        var call = Router.CallOf(workerId);
        if (call == null)
            return "No call.";
        if (Current != Phase.Calls && Current != Phase.WrapUp)
            return "";
        string result = "";
        int attemptsBefore = call.Attempts;
        bool live = call.State == CallState.Active || call.State == CallState.OnHold;
        var desk = DeskOf(workerId);
        switch (intent)
        {
            case "answer":
                if (!Router.Answer(call.Id, workerId))
                    return "";
                CallConversation.Arrived(call, workerId);
                desk?.Answered(call);
                break;
            case "hold":
                Router.SetHold(call.Id, workerId, true);
                call.CustomerSays("hold", workerId);
                desk?.PressButton(0);
                break;
            case "resume":
                Router.SetHold(call.Id, workerId, false);
                desk?.PressButton(0);
                break;
            case "mute":
                call.Muted = !call.Muted;
                desk?.PressButton(1);
                break;
            case "transfer":
                return TransferCall(workerId, call, arg);
            default:
                if (!live)
                    return "Answer the call first.";
                if (call.State == CallState.OnHold && intent != "colleague" && intent != "lookup")
                    return "They're on hold. Take them off hold first.";
                result = Talk(workerId, call, intent, arg, label);
                break;
        }
        if (call.Transcript.Count > 0 && call.Transcript[^1].Speaker == "customer")
            desk?.Phone.Murmur(1.4f);
        if (!call.Live && result == "")
            result = call.Outcome switch
            {
                CallOutcome.Resolved => $"Call {call.Id} closed: resolved. Quality {CallConversation.Quality(call) * 100f:0}%.",
                CallOutcome.FakeClosure => $"Call {call.Id} closed and marked resolved. It wasn't.",
                CallOutcome.Escalated => $"Call {call.Id} escalated to second line.",
                _ => $"Call {call.Id} ended.",
            };
        Witness(workerId, call, intent, attemptsBefore);
        Changed?.Invoke(call);
        return result;
    }

    string Talk(string workerId, CallCase call, string intent, string arg, string label)
    {
        switch (intent)
        {
            case "greet": return CallConversation.Greet(call, workerId);
            case "ask_order": return CallConversation.AskOrder(call, workerId);
            case "ask": return CallConversation.Ask(call, workerId, arg);
            case "lookup":
                var r = CallConversation.LookUp(call, workerId, arg, out var record);
                return r != "" ? r : record;
            case "apologise": return CallConversation.Apologise(call, workerId);
            case "promise": return CallConversation.MakePromise(call, workerId);
            case "colleague":
                var heard = CallConversation.AskColleague(call, workerId, out var advice);
                return (heard != "" ? heard + "\n" : "") + "Dana leans over: " + advice;
            case "act": return CallConversation.Act(call, workerId, arg, label);
            case "close": return CallConversation.Close(Router, call, workerId);
            case "close_anyway": return CallConversation.CloseAnyway(Router, call, workerId);
            case "escalate": return CallConversation.Escalate(Router, call, workerId);
        }
        return "";
    }

    string TransferCall(string workerId, CallCase call, string toId)
    {
        var to = Router.Worker(toId);
        var result = Router.Transfer(call.Id, workerId, toId, to != null && to.Speaks(call.Language) && call.Language != CallLanguage.English ? $"speaks {CallLanguage.Name(call.Language)}" : "transfer");
        DeskOf(workerId)?.PressButton(2);
        switch (result)
        {
            case TransferResult.Ok:
            case TransferResult.Queued:
                string who = toId == PlayerId && to.Name == "You" ? "a colleague" : to.Name;
                call.Say("agent", workerId, "transfer", $"I'm putting you through to {who}. They'll have all your details.");
                call.Note($"Transferred to {to.Name}");
                DeskOf(workerId)?.HungUp();
                // Handing away a language you claimed counts against that claim.
                if (workerId == PlayerId && call.Language != CallLanguage.English && Router.Worker(PlayerId).Speaks(call.Language))
                    SkillFailed("lang_" + call.Language, $"passing on {CallLanguage.Name(call.Language)} callers despite the CV");
                Changed?.Invoke(call);
                return result == TransferResult.Ok ? $"Transferred to {to.Name}." : $"{to.Name} is busy. The call waits for them.";
            case TransferResult.Bouncing: return $"{to?.Name} just sent you this caller. Sending them straight back would bounce them.";
            case TransferResult.TooMany: return "This caller has been transferred too many times already. Deal with it here.";
            case TransferResult.Offline: return $"{to?.Name} is not on the phones.";
            case TransferResult.NotYours: return "Answer it first.";
            default: return "Can't transfer that.";
        }
    }

    /// While the supervisor is listening in, she hears what happens.
    void Witness(string workerId, CallCase call, string intent, int attemptsBefore)
    {
        if (workerId != PlayerId)
            return;
        bool listening = supervisor.ListeningTo == workerId;
        var s = supervisor.Suspicion;
        if (!listening)
            return;
        if (call.Attempts > attemptsBefore)
            s.Raise($"wrong_{call.Id}_{call.Attempts}", "tried a fix that didn't fit the problem", 6f);
        if (intent == "promise" && call.Promises.Any(p => !p.Supported && !p.Checked))
        {
            foreach (var p in call.Promises) p.Checked = true;
            s.Raise($"promise_{call.Id}", $"promised a customer something the system can't do (\"{call.Def.PromiseIntent.ToLowerInvariant()}\")", 14f);
            supervisor.Remark("We don't promise that. Ever.");
        }
        if (intent == "colleague" && call.State == CallState.Active && !call.Muted)
            s.Raise($"overheard_{call.Id}", "asked a colleague with the customer listening", 5f);
        if (intent == "close_anyway" && call.Outcome == CallOutcome.FakeClosure)
        {
            s.Raise($"fake_{call.Id}", "marked an unfixed call as resolved", 22f);
            supervisor.Remark("That wasn't fixed. I heard it.");
        }
        if (intent == "close" && call.Outcome == CallOutcome.Resolved && call.Attempts == 0)
            s.Ease(4f);
    }

    void OnEnded(CallCase call)
    {
        _finished.Add(call);
        var desk = DeskOf(call.Owner);
        desk?.HungUp();
        Changed?.Invoke(call);
        if (call.Owner != PlayerId)
            return;
        var me = Router.Worker(PlayerId);
        // A claimed skill that keeps failing is what gives a liar away.
        bool failed = call.Outcome != CallOutcome.Resolved || call.Attempts >= 2;
        if (failed && call.Language != CallLanguage.English && me.Speaks(call.Language))
            SkillFailed("lang_" + call.Language, $"keeps struggling with {CallLanguage.Name(call.Language)} callers despite the CV");
        if (failed && call.Def.Technical && me.Technical)
            SkillFailed("technical", "the 'technical expert' keeps fumbling technical calls");
        if (call.Outcome == CallOutcome.HungUp)
            session.Hud.ShowToast($"{call.CustomerName} hung up.");
        if (call.Outcome == CallOutcome.FakeClosure)
            StartCoroutine(Callback(call));
        if (call.Promises.Any(p => !p.Supported && !p.Checked))
            StartCoroutine(Review(call));
    }

    void SkillFailed(string key, string what)
    {
        _skillFailures.TryGetValue(key, out int n);
        _skillFailures[key] = ++n;
        if (n >= 2)
            supervisor.Suspicion.Raise("skill_" + key, what, 24f);
    }

    /// The supervisor reads the notes later: a written promise is checked against the record.
    IEnumerator Review(CallCase call)
    {
        yield return new WaitForSeconds(ReviewDelay);
        if (Current == Phase.Payout || Current == Phase.Exposed)
            yield break;
        foreach (var p in call.Promises) p.Checked = true;
        supervisor.Suspicion.Raise($"promise_{call.Id}", $"wrote down a promise the system doesn't support (call {call.Id}: \"{call.Def.PromiseIntent.ToLowerInvariant()}\")", 14f);
        session.Hud.ShowSubtitle("Supervisor", $"I'm reading call {call.Id}'s notes. We can't promise that. Who wrote this?", 3.4f);
        _notes.Add($"Call {call.Id}: an unsupported promise was found in the notes.");
    }

    /// A call marked resolved that wasn't comes back, angrier, and everyone sees it.
    IEnumerator Callback(CallCase call)
    {
        yield return new WaitForSeconds(CallbackDelay);
        if (Current != Phase.Calls)
            yield break;
        var again = Router.Ring(call.Def, call.Language, callback: true);
        again.Patience = 0.6f;
        again.Note($"Callback: call {call.Id} was marked resolved");
        supervisor.Suspicion.Raise($"callback_{call.Id}", $"a call marked resolved came straight back (call {call.Id})", 20f);
        session.Hud.ShowSubtitle("Supervisor", "Why is a 'resolved' customer calling back?", 2.6f);
        Changed?.Invoke(again);
    }

    /// The supervisor has seen enough.
    public void Expose()
    {
        if (_paid)
            return;
        _paid = true;
        Current = Phase.Exposed;
        StopAllCoroutines();
        GadgetKit.Instance?.ShiftOver();
        var lines = new List<string> { "<size=28><b><color=#FF6B5B>LET GO</color></b></size>", "The supervisor has heard enough.\n", "<b>What she noticed:</b>" };
        foreach (var i in supervisor.Suspicion.Witnessed)
            lines.Add($"·  {i.What}");
        lines.Add("\n<size=13><color=#FFFFFF99>No pay for this shift. The CV still stands; you can try the shift again.</color></size>");
        EndShiftUi(string.Join("\n", lines));
    }

    /// Ends the shift and pays (once; later calls do nothing).
    public void Payout()
    {
        if (_paid)
            return;
        _paid = true;
        Current = Phase.Payout;
        session.Hud.HideBanner();
        var crew = CrewState.Current;
        var me = Router.Worker(PlayerId);
        var mine = _finished.Where(c => c.Owner == PlayerId).ToList();
        var bonuses = new List<(string, int)>();
        var deductions = new List<(string, int)>();
        int resolved = mine.Count(c => c.Outcome == CallOutcome.Resolved);
        bonuses.Add(($"Calls fixed ({resolved})", resolved * 6));
        foreach (var language in me.Languages.Where(l => l != CallLanguage.English))
        {
            int n = mine.Count(c => c.Language == language && c.Outcome == CallOutcome.Resolved);
            bonuses.Add(($"{CallLanguage.Name(language)} calls fixed ({n})", n * 12));
        }
        if (me.Technical)
        {
            int n = mine.Count(c => c.Def.Technical && c.Outcome == CallOutcome.Resolved);
            bonuses.Add(($"Technical calls fixed ({n})", n * 15));
        }
        if (crew.HasClaim(Claims.Service))
        {
            int n = mine.Count(c => CallConversation.Quality(c) >= 0.8f);
            bonuses.Add(($"Delighted customers ({n})", n * 8));
        }
        if (crew.HasClaim(Claims.Pressure))
        {
            int n = mine.Count(c => _rush.Contains(c.Id) && c.Outcome == CallOutcome.Resolved);
            bonuses.Add(($"Rush calls cleared ({n})", n >= 2 ? 30 : 0));
        }
        int hung = mine.Count(c => c.Outcome == CallOutcome.HungUp);
        if (hung > 0) deductions.Add(($"{hung} customer(s) hung up on you", hung * 10));
        int fake = mine.Count(c => c.Outcome == CallOutcome.FakeClosure);
        if (fake > 0) deductions.Add(($"{fake} call(s) marked resolved that weren't", fake * 15));
        int promises = mine.Sum(c => c.Promises.Count(p => !p.Supported));
        if (promises > 0) deductions.Add(($"{promises} promise(s) the company has to walk back", promises * 5));
        int abandoned = Router.Calls.Count(c => c.Outcome == CallOutcome.HungUp && c.Owner == "");
        if (supervisor.Suspicion.Value >= Suspicion.WarningAt) deductions.Add(("Formal warning from the supervisor", 15));

        int deducted = Mathf.Min(MaxDeductions, deductions.Sum(d => d.Item2));
        int earned = bonuses.Sum(b => b.Item2);
        int pay = Mathf.Max(0, Pay - deducted + earned);
        crew.savings += pay;
        crew.shiftsCompleted++;
        if (WorkshopLedger.GiveStarterKit(crew))
            _notes.Add("A text from your cousin Dev: \"Left a box of salvaged parts at your door. Build something with it.\"");
        GadgetKit.Instance?.ShiftOver();
        crew.Save();

        var lines = new List<string>
        {
            "<size=28><b>SHIFT OVER</b></size>",
            $"<color=#FFFFFFAA>Brightline Home Support, day {crew.day}</color>\n",
            $"Base pay ({CvLies.Tier(Lies).ToLowerInvariant()} CV, {Lies} {(Lies == 1 ? "lie" : "lies")})          <b>${Pay}</b>" + (Lies > 0 ? $"   <color=#FFFFFF88>(honest pay ${CrewState.CallCenterShiftPay})</color>" : ""),
        };
        foreach (var (what, amount) in bonuses)
            lines.Add($"<color=#8CE073>+ ${amount}</color>   {what}");
        foreach (var (what, amount) in deductions)
            lines.Add($"<color=#FF8A6B>- ${amount}</color>   {what}");
        if (deductions.Sum(d => d.Item2) > MaxDeductions)
            lines.Add($"<color=#FFFFFF88>(deductions capped at ${MaxDeductions})</color>");
        lines.Add($"\n<size=22><b>Paid: ${pay}</b></size>     savings ${crew.savings} of ${crew.rentDue} rent");
        lines.Add($"<size=13><color=#FFFFFF99>You took {mine.Count} call(s). {abandoned} caller(s) gave up waiting in the queue.</color></size>");
        foreach (var note in _notes)
            lines.Add($"<size=13><color=#FFFFFF99>{note}</color></size>");
        EndShiftUi(string.Join("\n", lines));
    }

    void EndShiftUi(string text)
    {
        foreach (var desk in desks)
            if (desk.Occupied)
                desk.StandUp();
        session.Hud.ShowPayout(text, GoHome, Retry, Current == Phase.Payout ? "Work tomorrow's shift too (skip home)" : "Try this shift again");
        session.Player.WantsCursor = true;
        session.RefreshCursor();
    }

    void GoHome()
    {
        var crew = CrewState.Current;
        crew.day++;
        crew.daysLeft = Mathf.Max(0, crew.daysLeft - 1);
        crew.Save();
        session.Player.WantsCursor = false;
        session.TravelTo(GameSession.ApartmentScene, 0.8f);
    }

    /// Work again: after a paid shift that is tomorrow's shift (a new day, a day
    /// closer to the rent), straight home instead if the rent is due. After being
    /// exposed it is the same evening again, unpaid.
    void Retry()
    {
        session.Player.WantsCursor = false;
        if (Current == Phase.Payout)
        {
            var crew = CrewState.Current;
            crew.day++;
            crew.daysLeft = Mathf.Max(0, crew.daysLeft - 1);
            crew.Save();
            if (WorkshopLedger.RentOverdue(crew))
            {
                session.TravelTo(GameSession.ApartmentScene, 0.8f);
                return;
            }
        }
        session.TravelTo(GameSession.CallCenterScene, 0.6f);
    }

    /// Lets the first call wait for the player to sit down (solo play starts when you are ready).
    public void PlayerSatDown()
    {
        if (Current == Phase.Briefing)
        {
            StopAllCoroutines();
            session.Hud.ClearSubtitle();
            supervisor.ResumePatrol();
            Begin();
        }
    }

    /// The supervisor decided the crew is a fraud (wired from her suspicion).
    public void OnSuspicion(Suspicion.Level level)
    {
        if (level == Suspicion.Level.Exposed)
            Expose();
        else if (level == Suspicion.Level.Warning && !_warned)
        {
            _warned = true;
            session.Hud.ShowSubtitle("Supervisor", "This is a formal warning. One more thing and you're done.", 3.2f);
        }
    }
}
