using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum OpeningStage { Waking, Knocking, LandlordAtDoor, BillOffered, LandlordLeaving, FindJob, Hired }

/// <summary>
/// The playable opening in the apartment: wake up, answer the landlord, take the
/// bill, find a job on the computer, lie on the CV, leave for the shift.
///
/// One explicit stage at a time, each transition safe to call twice. Saved
/// progress (bill received, hired) starts a repeat visit further along instead
/// of replaying the landlord. Once online, only the host runs these transitions
/// and peers receive the stage; players keep camera control throughout.
/// </summary>
public class OpeningDirector : MonoBehaviour
{
    const string IntroSeenKey = "intro_seen";

    [SerializeField] GameSession session;
    [SerializeField] Landlord landlord;
    [SerializeField] FrontDoor door;
    [SerializeField] PickupProp bill;
    [SerializeField] PlacementSocket billSpot;
    [SerializeField] Transform wakeEye;

    public static OpeningDirector Instance { get; private set; }
    public OpeningStage Stage { get; private set; } = OpeningStage.Waking;
    public PickupProp Bill => bill;
    public PlacementSocket BillSpot => billSpot;
    public GameSession Session => session;

    InputAction _skip;
    bool _skipPressed;
    float _cardTimer;
    bool _replay;

    public void Configure(GameSession owner, Landlord npc, FrontDoor frontDoor, PickupProp rentBill, PlacementSocket spot, Transform eye)
    {
        session = owner;
        landlord = npc;
        door = frontDoor;
        bill = rentBill;
        billSpot = spot;
        wakeEye = eye;
    }

    void Awake()
    {
        Instance = this;
        _skip = new InputAction("skip", InputActionType.Button, "<Keyboard>/space");
        _skip.AddBinding("<Keyboard>/enter");
        _skip.AddBinding("<Gamepad>/buttonNorth");
        _skip.Enable();
    }

    void OnDestroy()
    {
        _skip?.Dispose();
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        var crew = CrewState.Current;
        _replay = PlayerPrefs.GetInt(IntroSeenKey, 0) == 1;
        session.Hud.SetHelp("WASD move    Mouse look    Shift sprint    E use / pick up    Click place    Q drop    Esc menu");
        if (crew.billReceived)
        {
            // A repeat visit: the landlord has been and gone, the bill is on the desk.
            landlord.gameObject.SetActive(false);
            session.Service.PlaceHeldOn(new BillPlacer(bill), billSpot);
            Stage = crew.hired ? OpeningStage.Hired : OpeningStage.FindJob;
            session.Hud.SetBlack(1f);
            session.Hud.Fade(0f, 0.8f);
        }
        else
        {
            StartCoroutine(WakeUp());
        }
        RefreshCard();
    }

    void Update()
    {
        if (_skip.WasPressedThisFrame())
            _skipPressed = true;
        _cardTimer += Time.unscaledDeltaTime;
        if (_cardTimer > 0.25f)
        {
            _cardTimer = 0f;
            RefreshCard();
        }
        var held = session.Player.HeldProp;
        session.Hud.ShowDocument(held != null && held == bill ? BillText(CrewState.Current) : null);
    }

    // The sequence ---------------------------------------------------------------------

    IEnumerator WakeUp()
    {
        var player = session.Player;
        var hud = session.Hud;
        player.Lock();
        hud.SetCrosshairVisible(false);
        hud.SetBlack(1f);
        player.SetCameraOverride(new Pose(wakeEye.position, wakeEye.rotation), 0f);
        yield return new WaitForSeconds(_replay ? 0.3f : 1.2f);
        hud.Fade(0f, _replay ? 0.8f : 2.2f);
        landlord.Knock();
        yield return new WaitForSeconds(_replay ? 0.8f : 2.6f);
        // Sit up and come to your feet: the camera rises from the couch to eye height.
        player.ClearCameraOverride(1.8f);
        yield return new WaitForSeconds(1.8f);
        hud.SetCrosshairVisible(true);
        player.Unlock();
        SetStage(OpeningStage.Knocking);
        landlord.BeginKnocking();
    }

    /// Someone opened the door to the landlord.
    public void OnDoorAnswered(IInteractionActor opener)
    {
        if (Stage != OpeningStage.Knocking)
            return;
        SetStage(OpeningStage.LandlordAtDoor);
        door.Open();
        StartCoroutine(Confrontation(opener));
    }

    IEnumerator Confrontation(IInteractionActor opener)
    {
        var face = (opener as Component)?.transform;
        yield return landlord.StepIntoDoorway(face);
        landlord.Perform(WorkerAvatar.Talk);
        yield return Say("Landlord", "Seven days. Pay the rent, or you're out.", 2.6f);
        landlord.Perform(WorkerAvatar.Point);
        yield return Say("Landlord", "Seven. I counted them for you.", 1.8f);
        landlord.Perform(WorkerAvatar.Offer);
        SetStage(OpeningStage.BillOffered);
        yield return Say("Landlord", "And stop putting \"entrepreneur\" on the payment reference.", 3.4f);
        float wait = 0f;
        while (Stage == OpeningStage.BillOffered)
        {
            wait += Time.deltaTime;
            if (wait > 9f)
            {
                wait = 0f;
                session.Hud.ShowSubtitle("Landlord", "Well? Take it. It's not going to pay itself.", 2.6f);
            }
            yield return null;
        }
    }

    /// The hand-over: the bill goes from the landlord's hand to the player's. Called on the authority.
    public InteractionResult TakeBill(IInteractionActor taker)
    {
        if (Stage != OpeningStage.BillOffered)
            return InteractionResult.NotNow;
        var result = session.Service.GiveProp(bill, taker);
        if (result != InteractionResult.Ok)
            return result;
        var crew = CrewState.Current;
        crew.billReceived = true;
        crew.Save();
        PlayerPrefs.SetInt(IntroSeenKey, 1);
        SetStage(OpeningStage.LandlordLeaving);
        StartCoroutine(Departure());
        return InteractionResult.Ok;
    }

    IEnumerator Departure()
    {
        landlord.Perform(WorkerAvatar.Interact);
        yield return new WaitForSeconds(0.5f);
        landlord.Perform(WorkerAvatar.Talk);
        yield return Say("Landlord", "Try getting a job. Apparently people do that.", 2.8f);
        yield return landlord.Leave();
        door.Close();
        SetStage(OpeningStage.FindJob);
        session.Hud.ShowToast("The computer is on the desk.");
    }

    /// The application went through. Called on the authority.
    public void OnHired()
    {
        if (Stage == OpeningStage.Hired)
            return;
        SetStage(OpeningStage.Hired);
    }

    /// Out the door to the first shift. Solo leaves at once; with a crew this
    /// becomes a ready-up at the door.
    public void LeaveForWork()
    {
        if (Stage != OpeningStage.Hired)
            return;
        door.Open();
        session.TravelTo(GameSession.WeddingScene, 1.2f);
    }

    IEnumerator Say(string speaker, string line, float seconds)
    {
        session.Hud.ShowSubtitle(speaker, line, seconds + 0.3f, _replay);
        _skipPressed = false;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (_replay && _skipPressed)
                break;
            yield return null;
        }
        _skipPressed = false;
    }

    void SetStage(OpeningStage stage)
    {
        Stage = stage;
        RefreshCard();
    }

    // HUD -----------------------------------------------------------------------------

    void RefreshCard()
    {
        var crew = CrewState.Current;
        var hud = session.Hud;
        string eyebrow = $"DAY {crew.day}   ·   FLAT 4B";
        switch (Stage)
        {
            case OpeningStage.Waking:
            case OpeningStage.Knocking:
                hud.SetCard(eyebrow, "Answer the door", "Someone is pounding on it.");
                break;
            case OpeningStage.LandlordAtDoor:
                hud.SetCard(eyebrow, "Deal with the landlord");
                break;
            case OpeningStage.BillOffered:
                hud.SetCard(eyebrow, "Take the bill", "He is not leaving until you do.");
                break;
            case OpeningStage.LandlordLeaving:
            case OpeningStage.FindJob:
                hud.SetCard(eyebrow, "Find something that pays", "The computer is on the desk.");
                break;
            case OpeningStage.Hired:
                hud.SetCard(eyebrow, "Get to your first shift", "Wedding Crew, Grand Hotel Ballroom.", "Leave by the front door.", ClaimSummary(crew));
                break;
        }
        hud.SetStatus(crew.billReceived ? crew.MoneyLine() : "");
    }

    static string ClaimSummary(CrewState crew)
    {
        if (crew.Truthful)
            return "Your CV: no experience. Available immediately.";
        var parts = new System.Collections.Generic.List<string>();
        foreach (var (id, text, _) in Claims.All)
            if (crew.HasClaim(id))
                parts.Add(text.ToLowerInvariant());
        return "Your CV says: " + string.Join(", ", parts) + ".";
    }

    public static string BillText(CrewState crew) =>
        "<size=13><color=#888888>FLAT 4B   ·   FROM YOUR LANDLORD</color></size>\n" +
        "<size=26><b><color=#B3211F>FINAL NOTICE</color></b></size>\n\n" +
        $"Rent owed               <b>${crew.rentDue}</b>\n" +
        $"Due in                     <b>{crew.daysLeft} days</b>\n" +
        $"Paid so far              ${crew.savings}\n\n" +
        "<size=13>The payment reference must not say \"entrepreneur\".\n" +
        "Pay or leave. Your choice. (It is not your choice.)</size>";

    /// Lets a repeat visit put the bill on the desk through the normal placement rules.
    sealed class BillPlacer : IInteractionActor
    {
        readonly PickupProp _bill;
        public BillPlacer(PickupProp bill) => _bill = bill;
        public string ActorId => "setup";
        public string HeldPropId { get => _bill.EntityId; set { } }
        public Vector3 EyePosition => Vector3.zero;
        public Transform HoldPoint => null;
        public Vector3 DropVelocity => Vector3.zero;
    }
}
