using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The Wedding Crew shift, start to finish: the briefing, a timed preparation
/// (with inspections the coordinator announces first), guests arriving to be
/// seated, the ceremony and its music cue, a final inspection, and the payout.
///
/// The CV decides the job. A temp gets the simple version (a standing cake,
/// guests who seat themselves, a sound desk the hotel sets up). Each expert
/// claim swaps in the harder version of one job and opens a visible bonus for
/// doing it well; the base pay is the same for everyone.
///
/// Mistakes are recorded as incidents; the coordinator only holds them against
/// the crew when she sees them (CoordinatorMind / Suspicion). Exposure ends the
/// shift with the evidence and a retry, never the career.
/// </summary>
public class WeddingShift : MonoBehaviour
{
    public enum Phase { Briefing, Prep, Guests, Ceremony, Inspection, Payout, Exposed }

    const float PrepSeconds = 360f;
    const float GuestSeconds = 200f;
    const int BasePay = CrewState.WeddingShiftPay;
    const int MaxDeductions = 80;

    [SerializeField] GameSession session;
    [SerializeField] Coordinator coordinator;
    [SerializeField] Transform greetSpot;
    [SerializeField] AudioDesk desk;
    [SerializeField] GameObject spillPrefab;
    [SerializeField] Transform guestDoor;
    [SerializeField] Transform[] guestWaitSpots;
    [SerializeField] Transform stageSpot;
    [SerializeField] Transform cakeStation, soundStation;

    public Phase Current { get; private set; } = Phase.Briefing;
    public IReadOnlyList<TaskBoard.Result> Results => _results;
    public ICollection<string> Assigned => _assigned;
    public CoordinatorMind Mind { get; private set; }
    public float PhaseLeft => _phaseLeft;
    public bool Paid => _paid;

    List<TaskBoard.Task> _tasks;
    List<TaskBoard.Result> _results = new();
    readonly HashSet<string> _assigned = new();
    readonly List<Guest> _guests = new();
    List<GuestProfile> _roster;
    GuestTable[] _tables;
    float _phaseLeft, _phaseTime, _refresh;
    bool _paid, _earlyCalled;
    bool _expertCake, _expertSeating, _expertAudio, _pressure;
    // Pressure claim: the rush job.
    float _coffeeDue = -1f;
    bool _coffeeDelivered, _coffeeAsked;
    readonly List<string> _notes = new();
    string _cueResult;

    public void Configure(GameSession owner, Coordinator npc, Transform greet)
    {
        session = owner;
        coordinator = npc;
        greetSpot = greet;
    }

    public void ConfigureShift(AudioDesk audio, GameObject spill, Transform door, Transform[] waits, Transform stage, Transform cake, Transform sound)
    {
        desk = audio;
        spillPrefab = spill;
        guestDoor = door;
        guestWaitSpots = waits;
        stageSpot = stage;
        cakeStation = cake;
        soundStation = sound;
    }

    void Awake()
    {
        Mind = coordinator.GetComponent<CoordinatorMind>();
        ShiftEvents.Mind = Mind;
        ShiftEvents.SpillPrefab = spillPrefab;
    }

    void OnDestroy()
    {
        if (ShiftEvents.Mind == Mind)
            ShiftEvents.Mind = null;
    }

    /// The CV's lies: they set the pay grade and the heat (see CvLies).
    public int Lies { get; private set; }
    float PrepTime => PrepSeconds * CvLies.TimeScale(Lies);
    int Pay => CvLies.Pay(BasePay, Lies);

    void Start()
    {
        var crew = CrewState.Current;
        Lies = CvLies.Count(crew, GameSession.OfflineActor);
        _expertCake = crew.HasClaim(Claims.Cake);
        _expertSeating = crew.HasClaim(Claims.Seating);
        _expertAudio = crew.HasClaim(Claims.Audio);
        _pressure = crew.HasClaim(Claims.Pressure);
        foreach (var claim in crew.claims)
        {
            string task = Claims.TaskFor(claim);
            if (task != null)
                _assigned.Add(task);
        }
        BuildTasks();
        _tables = FindObjectsByType<GuestTable>(FindObjectsSortMode.None).OrderBy(t => t.Index).ToArray();
        _roster = SeatingChallenge.Generate(crew.day * 7919 + crew.shiftsCompleted, _tables.Select(t => t.Info).ToList());
        Guest.Seating = new SeatJudge(this);

        // Temps get easier versions: a looser line-up, a pre-wired sound desk.
        foreach (var id in CakeChallenge.Sockets)
            session.Service.GetSocket(id)?.ConfigureAlignment(_expertCake ? 0.16f : 0.3f, session.Service.GetSocket(id)?.StackOn);
        if (!_expertAudio && desk != null)
            desk.SetUpByStaff();

        foreach (var prop in session.Service.Props)
            if (prop.Kind == "cake_tier")
                prop.Damaged += OnCakeDamaged;

        Mind.BluffLine = Bluff;
        Mind.Suspicion.Strictness = CvLies.Scrutiny(Lies);
        Mind.Suspicion.Changed += level =>
        {
            if (level == Suspicion.Level.Exposed)
                Expose();
        };
        session.Hud.SetHelp("WASD move    Mouse look    Space jump    Shift sprint    C crouch    E use / pick up / drag    Click place / use    Q drop    1 2 3 reply    Esc menu");
        if (coordinator != null && greetSpot != null)
            coordinator.Greet(session.Player.transform, greetSpot, GreetingLines(crew, Lies, PrepTime), (line, seconds) => session.Hud.ShowSubtitle("Coordinator", line, seconds));
        Refresh();
    }

    void BuildTasks()
    {
        _tasks = TaskBoard.Catalog();
        var dessert = _tasks.First(t => t.Id == TaskBoard.Dessert);
        dessert.Placements["cake_socket_base"] = "cake_base|cake_base_spare";
        dessert.Placements["cake_socket_middle"] = "cake_middle|cake_middle_spare";
        dessert.Placements["cake_socket_top"] = "cake_top|cake_top_spare";
        if (_expertCake)
        {
            dessert.Placements[CakeChallenge.TopperSocket] = "cake_topper";
            dessert.Goal = "Build the cake: biggest at the bottom, every layer centred, topper on top";
        }
        var audio = _tasks.First(t => t.Id == TaskBoard.Audio);
        audio.NotBuilt = false;
        audio.Placements = new Dictionary<string, string> { { AudioDesk.PortIn, AudioDesk.CableMusic }, { AudioDesk.PortMain, AudioDesk.CableSpeakers } };
        audio.Goal = _expertAudio ? "Wire the desk (labels on the ports), test it, play the processional on the cue" : "The hotel's sound person handles it";
        if (_expertSeating)
            _tasks.First(t => t.Id == TaskBoard.Seating).Goal = "Signs on the tables, then seat every guest where they belong (guest book at the door)";
    }

    /// The welcome, built from the CV. At least one line repeats a claim back.
    public static List<(string pose, string line, float seconds)> GreetingLines(CrewState crew, int lies, float prepSeconds)
    {
        var lines = new List<(string, string, float)>();
        bool cake = crew.HasClaim(Claims.Cake);
        bool seating = crew.HasClaim(Claims.Seating);
        if (cake)
            lines.Add((WorkerAvatar.Point, "You're the cake expert, right? Good. That's yours. Topper and all.", 3.4f));
        if (seating)
            lines.Add((cake ? WorkerAvatar.Talk : WorkerAvatar.Point,
                cake ? "And seating coordinator too, it says. The guests are also yours." : "You're our seating coordinator? Perfect. Every guest, seated properly.", 3.6f));
        if (crew.HasClaim(Claims.Audio))
            lines.Add((WorkerAvatar.Point, "Audio technician. Wonderful. The sound desk, and don't miss my music cue.", 3.4f));
        if (crew.HasClaim(Claims.Pressure))
            lines.Add((WorkerAvatar.Talk, "\"Works well under pressure.\" We'll see about that.", 3f));
        if (lies == 0)
            lines.Add((WorkerAvatar.Point, "You're the temp. No experience, it says. Just... carry things. Carefully.", 3.6f));
        else if (lies >= 5)
            lines.Add((WorkerAvatar.Talk, "With a CV like yours, I'm expecting perfection. We're paying for it.", 3.4f));
        int minutes = Mathf.RoundToInt(prepSeconds / 60f);
        lines.Add((WorkerAvatar.Talk, $"{(minutes == 1 ? "One minute" : minutes + " minutes")} until the doors open. I'll be watching.", 3f));
        return lines;
    }

    string Bluff()
    {
        var crew = CrewState.Current;
        if (crew.HasClaim(Claims.Cake)) return "It's a European technique. Very advanced.";
        if (crew.HasClaim(Claims.Audio)) return "I'm letting the speakers breathe. Standard practice.";
        if (crew.HasClaim(Claims.Seating)) return "It's a deliberate seating dynamic.";
        if (crew.HasClaim(Claims.Pressure)) return "I work best like this. Under pressure.";
        return "First day nerves, sorry!";
    }

    // Every frame ------------------------------------------------------------------------

    AudioSource _crowd;

    /// The room fills up: a murmur that grows as guests are seated, quiet for the vows.
    void UpdateCrowd(float dt)
    {
        _crowd ??= ProceduralAudio.Ambience(gameObject, ProceduralAudio.Crowd, 0f);
        float seated = _guests.Count == 0 ? 0f : _guests.Count(g => g.State == Guest.Mode.Seated) / (float)_guests.Count;
        float target = Current switch
        {
            Phase.Guests => 0.08f + 0.22f * seated,
            Phase.Ceremony => 0.05f,
            Phase.Inspection => 0.3f,
            _ => 0f,
        };
        _crowd.volume = Mathf.MoveTowards(_crowd.volume, target, dt * 0.15f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        UpdateCrowd(dt);
        _phaseTime += dt;
        switch (Current)
        {
            case Phase.Briefing:
                if (!coordinator.Greeting && _phaseTime > 2f)
                    Enter(Phase.Prep, PrepTime);
                break;
            case Phase.Prep:
                _phaseLeft -= dt;
                UpdatePrep();
                if (_phaseLeft <= 0f)
                    StartCoroutine(GuestsArrive());
                break;
            case Phase.Guests:
                _phaseLeft -= dt;
                if (_phaseLeft <= 0f || (_guests.Count == _roster.Count && _guests.All(g => g.State == Guest.Mode.Seated)))
                    StartCoroutine(Ceremony());
                break;
        }
        UpdateCoffee(dt);
        WatchForEvidence();
        UpdateHud();
        _refresh += Time.unscaledDeltaTime;
        if (_refresh > 0.2f)
        {
            _refresh = 0f;
            Refresh();
        }
    }

    void Enter(Phase phase, float seconds)
    {
        Current = phase;
        _phaseLeft = seconds;
        _phaseTime = 0f;
    }

    void UpdatePrep()
    {
        // Inspections she announces first.
        if (Passed(80f) && cakeStation != null)
            Mind.Inspect("cake", cakeStation.position);
        if (Passed(170f))
            Mind.Inspect("tables", _tables.Length > 0 ? _tables[0].transform.position : transform.position);
        if (Passed(250f) && _expertAudio && soundStation != null)
            Mind.Inspect("sound desk", soundStation.position);
        if (_pressure && !_coffeeAsked && _phaseTime > 60f)
        {
            _coffeeAsked = true;
            _coffeeDue = 120f;
            session.Hud.ShowSubtitle("Coordinator", "You. Works well under pressure? Coffee. Black. Two minutes. Kitchen.", 3.6f);
        }
        if (!_earlyCalled && PrepDone())
        {
            _earlyCalled = true;
            _phaseLeft = Mathf.Min(_phaseLeft, 30f);
            session.Hud.ShowSubtitle("Coordinator", "Already done? ...Fine. Doors open in thirty seconds.", 3f);
        }
    }

    bool Passed(float t) => _phaseTime >= t && _phaseTime - Time.deltaTime < t;

    bool PrepDone() => _results.Where(r => r.Id != TaskBoard.Audio || _expertAudio).All(r => r.Status == TaskBoard.Status.Done)
        && (!_expertAudio || desk.Tested);

    // The pressure rush job -----------------------------------------------------------------

    void UpdateCoffee(float dt)
    {
        if (_coffeeDue < 0f || _coffeeDelivered)
            return;
        _coffeeDue -= dt;
        if (_coffeeDue <= 0f)
        {
            _coffeeDue = -1f;
            _notes.Add("Coffee never arrived (no pressure bonus).");
            session.Hud.ShowSubtitle("Coordinator", "Forget the coffee.", 2f);
        }
    }

    public bool CoffeeWanted => _coffeeDue > 0f && !_coffeeDelivered;

    /// Called when the player hands her a full mug in time.
    public void DeliverCoffee(PickupProp mug)
    {
        if (!CoffeeWanted)
            return;
        _coffeeDelivered = true;
        session.Hud.ShowSubtitle("Coordinator", "...That's actually good coffee.", 2.4f);
        Mind.Suspicion.Ease(8f);
    }

    // Guests ---------------------------------------------------------------------------------

    IEnumerator GuestsArrive()
    {
        if (Current != Phase.Prep)
            yield break;
        Enter(Phase.Guests, GuestSeconds);
        session.Hud.ShowSubtitle("Coordinator", _expertSeating ? "Doors are open! Seating coordinator, they're all yours." : "Doors are open! Guests coming through.", 3f);
        if (!PrepDone())
            _notes.Add("Not everything was ready when the doors opened.");
        Dictionary<string, int> plan = _expertSeating ? null : SeatingChallenge.Solve(_roster, _tables.Select(t => t.Info).ToList());
        for (int i = 0; i < _roster.Count; i++)
        {
            var wait = guestWaitSpots[i % guestWaitSpots.Length].position;
            var guest = Guest.Spawn(_roster[i], session.Workers, i, guestDoor.position, wait);
            guest.Complained += OnGuestComplained;
            _guests.Add(guest);
            if (plan != null && plan.TryGetValue(_roster[i].Name, out int table))
                StartCoroutine(SelfSeat(guest, _tables[table]));
            yield return new WaitForSeconds(5f);
        }
    }

    IEnumerator SelfSeat(Guest guest, GuestTable table)
    {
        yield return new WaitForSeconds(3f);
        guest.SeatYourself(table);
    }

    void OnGuestComplained(Guest guest, string line)
    {
        Mind.Suspicion.Report($"guest_{guest.Profile.Name}_{guest.Complaints}", $"{guest.Profile.Name} complaining about their seat", guest.transform.position, 5f, false,
            () => guest != null && guest.State == Guest.Mode.Upset);
    }

    sealed class SeatJudge : ISeatingJudge
    {
        readonly WeddingShift _shift;
        public SeatJudge(WeddingShift shift) => _shift = shift;

        public string Judge(Guest guest)
        {
            var seated = new Dictionary<string, int>();
            foreach (var g in _shift._guests)
                if (g != guest && g.State == Guest.Mode.Seated && g.Table != null)
                    seated[g.Profile.Name] = g.Table.Index;
            return SeatingChallenge.Judge(guest.Profile, guest.Table.Info, seated, _shift._roster, _shift._tables.Select(t => t.Info).ToList());
        }
    }

    // Ceremony and inspection --------------------------------------------------------------------

    IEnumerator Ceremony()
    {
        if (Current != Phase.Guests)
            yield break;
        Enter(Phase.Ceremony, 0f);
        coordinator.Busy = true;
        coordinator.GoAndStand(stageSpot.position);
        session.Hud.ShowSubtitle("Coordinator", "Places, everyone. Processional on my cue.", 3f);
        yield return new WaitForSeconds(7f);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            session.Hud.ShowSubtitle("Coordinator", attempt == 0 ? "And... music. NOW." : "Again. Music. NOW. Please.", 2.4f);
            if (_expertAudio)
            {
                desk.ResetCue();
                desk.CueOpen = true;
                for (float t = 0f; t < 9f && desk.CueOpen; t += Time.deltaTime)
                    yield return null;
                desk.MissCue();
                _cueResult = desk.CueResult;
            }
            else
            {
                desk.SetUpByStaff();
                desk.TogglePlay();
                _cueResult = "perfect";
            }
            if (_cueResult == "perfect")
            {
                session.Hud.ShowSubtitle("Coordinator", "...Beautiful.", 2f);
                break;
            }
            string what = _cueResult switch
            {
                "wrong track" => "the wrong music at the ceremony",
                "feedback" => "a feedback squeal during the vows",
                "too loud" => "deafening music at the ceremony",
                "too quiet" => "music nobody could hear",
                _ => "dead silence at the music cue",
            };
            Mind.Suspicion.Raise($"cue_{attempt}", what, attempt == 0 ? 10f : 15f);
            session.Hud.ShowSubtitle("Coordinator", _cueResult == "wrong track" ? "That is NOT the processional." : "What was that?!", 2.4f);
            yield return new WaitForSeconds(4f);
        }
        yield return new WaitForSeconds(6f);
        StartCoroutine(Inspection());
    }

    IEnumerator Inspection()
    {
        Enter(Phase.Inspection, 0f);
        session.Hud.ShowSubtitle("Coordinator", "Right. Final check.", 2.4f);
        coordinator.Busy = true;
        if (cakeStation != null)
        {
            coordinator.GoAndStand(cakeStation.position - cakeStation.forward * 1.6f);
            for (float t = 0f; t < 12f && !coordinator.Walker.Arrived; t += Time.deltaTime)
                yield return null;
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                coordinator.Walker.Face(cakeStation.position);
                yield return null;
            }
            var cake = CakeChallenge.Evaluate(session.Service, _expertCake);
            session.Hud.ShowSubtitle("Coordinator", cake.Placed < cake.Total ? "Where is the rest of the cake?!" : $"The cake... {CakeChallenge.Words(cake.Presentation)}.", 2.6f);
            yield return new WaitForSeconds(2.8f);
        }
        if (Current == Phase.Exposed)
            yield break;
        Payout();
    }

    // Evidence the coordinator can notice ---------------------------------------------------------

    void OnCakeDamaged(PickupProp tier, PickupProp.Condition state)
    {
        if (state == PickupProp.Condition.Ruined)
        {
            ShiftEvents.Noise(tier.transform.position, 18f, "a crash");
            ShiftEvents.Mess(tier.transform.position, "frosting splattered on the floor", 10f, true);
        }
    }

    void WatchForEvidence()
    {
        var s = Mind.Suspicion;
        foreach (var prop in session.Service.Props)
        {
            if (prop.Kind != "cake_tier" || !prop.gameObject.activeInHierarchy)
                continue;
            var p = prop;
            if (p.State == PickupProp.Condition.Ruined)
                s.Report($"ruined_{p.EntityId}", "a ruined cake layer", p.transform.position, 14f, true,
                    () => p != null && p.gameObject.activeInHierarchy && p.State == PickupProp.Condition.Ruined && string.IsNullOrEmpty(p.HolderId));
            else if (string.IsNullOrEmpty(p.HolderId) && string.IsNullOrEmpty(p.SocketId) && p.transform.position.y < 0.4f)
                s.Report($"floor_{p.EntityId}", "a cake layer on the floor", p.transform.position, 10f, true,
                    () => p != null && string.IsNullOrEmpty(p.HolderId) && string.IsNullOrEmpty(p.SocketId) && p.transform.position.y < 0.4f);
            else if (!string.IsNullOrEmpty(p.SocketId) && p.State == PickupProp.Condition.Smeared)
                s.Report($"smear_{p.EntityId}", "smeared frosting on the cake", p.transform.position, 6f, true,
                    () => p != null && !string.IsNullOrEmpty(p.SocketId) && p.State == PickupProp.Condition.Smeared);
            else if (!string.IsNullOrEmpty(p.SocketId) && p.PlacementQuality < 0.4f)
                s.Report($"lean_{p.EntityId}", "a leaning cake layer", p.transform.position, 7f, true,
                    () => p != null && !string.IsNullOrEmpty(p.SocketId) && p.PlacementQuality < 0.4f);
        }
    }

    // HUD ---------------------------------------------------------------------------------

    void Refresh()
    {
        var crew = CrewState.Current;
        _results = TaskBoard.Evaluate(_tasks, session.Service.Occupancy());
        session.Hud.SetTasks($"WEDDING CREW   ·   DAY {crew.day}", _results, _assigned);
        var cake = CakeChallenge.Evaluate(session.Service, _expertCake);
        session.Hud.AddCardLine($"Cake: {cake.Line}" + (_expertCake ? $"   ({cake.Presentation * 100f:0}%)" : ""));
        if (_expertAudio)
            session.Hud.AddCardLine(desk.Tested ? "Sound: tested" : "Sound: not tested yet (play the test tone)");
        if (_guests.Count > 0)
            session.Hud.AddCardLine($"Guests seated happily: {_guests.Count(g => g.Satisfied)} of {_roster.Count}");
        if (CoffeeWanted)
            session.Hud.AddCardLine($"<color=#F2C88C>RUSH: coffee for the coordinator, {_coffeeDue:0}s</color>");
    }

    void UpdateHud()
    {
        var hud = session.Hud;
        string phase = Current switch
        {
            Phase.Briefing => "BRIEFING",
            Phase.Prep => "PREPARATION",
            Phase.Guests => "GUESTS ARRIVING",
            Phase.Ceremony => "CEREMONY",
            Phase.Inspection => "FINAL CHECK",
            _ => "",
        };
        string clock = Current == Phase.Prep || Current == Phase.Guests ? $"{Mathf.FloorToInt(Mathf.Max(0f, _phaseLeft) / 60f)}:{Mathf.FloorToInt(Mathf.Max(0f, _phaseLeft) % 60f):00}" : "";
        hud.SetPhase(phase, clock);
        hud.SetSuspicion(Mind.Suspicion.Value, Mind.Suspicion.State, Mind.Doing);
        hud.SetStatus("");
        var held = session.Player.HeldProp;
        hud.SetWobble(held != null && held.Category == PropCategory.Wide ? held.Wobble : -1f);
        hud.ShowPage(held != null && held.Kind == "guest_book" ? SeatingChallenge.BookText(_roster) : null);
    }

    // The end ---------------------------------------------------------------------------------

    /// Ends the shift and pays (once; later calls do nothing).
    public void Payout()
    {
        if (_paid)
            return;
        _paid = true;
        Current = Phase.Payout;
        var crew = CrewState.Current;
        var cake = CakeChallenge.Evaluate(session.Service, _expertCake);
        var lines = new List<string>();
        var bonuses = new List<(string, int)>();
        var deductions = new List<(string, int)>();

        if (cake.Placed < cake.Total) deductions.Add(("Cake not finished", 40));
        if (cake.Ruined > 0) deductions.Add(("A ruined layer on the display", 20));
        var signs = _results.FirstOrDefault(r => r.Id == TaskBoard.Seating);
        if (signs.Total > 0 && signs.Correct < signs.Total) deductions.Add(($"{signs.Total - signs.Correct} table sign(s) missing or wrong", 10 * (signs.Total - signs.Correct)));
        int unhappy = _guests.Count(g => !g.Satisfied);
        if (unhappy > 0) deductions.Add(($"{unhappy} guest(s) without a proper seat", 8 * Mathf.Min(unhappy, 4)));
        if (_cueResult != null && _cueResult != "perfect") deductions.Add(($"Music cue: {_cueResult}", 20));
        if (Mind.Suspicion.Value >= Suspicion.WarningAt) deductions.Add(("Formal warning from the coordinator", 15));

        if (_expertCake)
        {
            int bonus = cake.Placed == cake.Total && cake.Topper ? (cake.Presentation >= 0.85f ? 60 : cake.Presentation >= 0.65f ? 30 : 0) : 0;
            bonuses.Add(($"Cake presentation ({cake.Presentation * 100f:0}%)", bonus));
        }
        if (_expertSeating)
        {
            int happy = _guests.Count(g => g.Satisfied);
            bonuses.Add(($"Guest satisfaction ({happy}/{_roster.Count})", happy == _roster.Count ? 50 : happy >= _roster.Count - 2 ? 25 : 0));
        }
        if (_expertAudio)
            bonuses.Add(($"Ceremony music ({_cueResult ?? "not played"})", _cueResult == "perfect" ? 50 : 0));
        if (_pressure)
            bonuses.Add(("Rush job: coffee", _coffeeDelivered ? 40 : 0));

        int deducted = Mathf.Min(MaxDeductions, deductions.Sum(d => d.Item2));
        int earned = bonuses.Sum(b => b.Item2);
        int pay = Mathf.Max(0, Pay - deducted + earned);
        crew.savings += pay;
        crew.shiftsCompleted++;
        crew.Save();

        lines.Add("<size=28><b>SHIFT OVER</b></size>");
        lines.Add($"<color=#FFFFFFAA>Wedding Crew, day {crew.day}</color>\n");
        lines.Add($"Base pay ({CvLies.Tier(Lies).ToLowerInvariant()} CV, {Lies} {(Lies == 1 ? "lie" : "lies")})          <b>${Pay}</b>" + (Lies > 0 ? $"   <color=#FFFFFF88>(honest pay ${BasePay})</color>" : ""));
        foreach (var (what, amount) in bonuses)
            lines.Add($"<color=#8CE073>+ ${amount}</color>   {what}");
        foreach (var (what, amount) in deductions)
            lines.Add($"<color=#FF8A6B>- ${amount}</color>   {what}");
        if (deductions.Sum(d => d.Item2) > MaxDeductions)
            lines.Add($"<color=#FFFFFF88>(deductions capped at ${MaxDeductions})</color>");
        lines.Add($"\n<size=22><b>Paid: ${pay}</b></size>     savings ${crew.savings} of ${crew.rentDue} rent");
        foreach (var note in _notes)
            lines.Add($"<size=13><color=#FFFFFF99>{note}</color></size>");
        session.Hud.ShowPayout(string.Join("\n", lines), GoHome, Retry);
        session.Player.WantsCursor = true;
        session.RefreshCursor();
    }

    void Expose()
    {
        if (_paid)
            return;
        _paid = true;
        Current = Phase.Exposed;
        StopAllCoroutines();
        var lines = new List<string> { "<size=28><b><color=#FF6B5B>EXPOSED</color></b></size>", "The coordinator has seen enough.\n", "<b>What she saw:</b>" };
        foreach (var i in Mind.Suspicion.Witnessed)
            lines.Add($"·  {i.What}");
        lines.Add("\n<size=13><color=#FFFFFF99>No pay for this shift. The CV still stands; you can try the shift again.</color></size>");
        session.Hud.ShowPayout(string.Join("\n", lines), GoHome, Retry);
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

    void Retry()
    {
        session.Player.WantsCursor = false;
        session.TravelTo(GameSession.WeddingScene, 0.6f);
    }
}
