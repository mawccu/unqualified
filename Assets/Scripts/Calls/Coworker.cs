using System.Collections;
using UnityEngine;

/// <summary>
/// A simulated colleague at a support desk: a worker in the same router a
/// teammate would use (nothing here is networked). They pick up, work English
/// calls through the same conversation rules as the player (and sometimes try
/// the wrong fix), and when a caller speaks a language they don't, they hand
/// the call to whoever claimed it, saying so across the office. With nobody to
/// hand it to, they muddle through with the phrase aid, slowly.
/// One coworker can start on a break in the break corner and come back later.
/// </summary>
public class Coworker : MonoBehaviour
{
    [SerializeField] string workerId = "npc";
    [SerializeField] string displayName = "Coworker";
    [SerializeField] bool technical;
    [SerializeField] bool startsOnBreak;
    [SerializeField] float breakUntil = 150f;
    [SerializeField] int costumeSlot = 1;
    [SerializeField] WorkerLibrary workers;
    [SerializeField] Transform breakSpot;

    public string WorkerId => workerId;
    public string DisplayName => displayName;
    public bool Technical => technical;
    public bool StartsOnBreak => startsOnBreak;
    public WorkerAvatar Avatar { get; private set; }

    SupportDesk _desk;
    Transform _body;
    bool _onBreak;

    public void Configure(string id, string name, bool tech, int slot, WorkerLibrary library, bool onBreak = false, Transform breakArea = null, float backAt = 150f)
    {
        workerId = id;
        displayName = name;
        technical = tech;
        costumeSlot = slot;
        workers = library;
        startsOnBreak = onBreak;
        breakSpot = breakArea;
        breakUntil = backAt;
    }

    static CallCenterShift Shift => CallCenterShift.Instance;

    void Awake()
    {
        _desk = GetComponent<SupportDesk>();
        _body = new GameObject($"{displayName}_Body").transform;
        _body.SetParent(transform, false);
        if (workers != null)
            Avatar = WorkerAvatar.Create(workers, costumeSlot, WorkerCostumes.ForSlot(costumeSlot), _body);
    }

    void Start()
    {
        _onBreak = startsOnBreak;
        if (_onBreak && breakSpot != null)
        {
            _body.SetPositionAndRotation(breakSpot.position, breakSpot.rotation);
            Avatar?.SetIdlePose(WorkerAvatar.HoldPaper);
            Avatar?.SitOn(0f);
        }
        else
            SitAtDesk();
        StartCoroutine(Work());
    }

    void SitAtDesk()
    {
        _body.SetPositionAndRotation(_desk.Seat.position - Vector3.up * 0.05f, _desk.Seat.rotation);
        Avatar?.SetIdlePose(WorkerAvatar.SitType);
        Avatar?.SitOn(WorkerAvatar.DeskChairSeat);
    }

    void Update()
    {
        // Seated workers do not walk; keep the animator in its idle (sitting) pose.
        Avatar?.UpdateMotion(Vector3.zero, false, false, Time.deltaTime);
    }

    Pose Ear() => new(_body.position + Vector3.up * 1.12f - _body.right * 0.14f + _body.forward * 0.05f,
        _body.rotation * Quaternion.Euler(0f, 0f, 70f));

    IEnumerator Work()
    {
        while (Shift == null)
            yield return null;
        while (true)
        {
            if (_onBreak)
            {
                if (Shift.Clock >= breakUntil)
                {
                    _onBreak = false;
                    SitAtDesk();
                    Shift.Router.SetBreak(workerId, false);
                }
                yield return new WaitForSeconds(1f);
                continue;
            }
            var call = Shift.CallOf(workerId);
            if (call != null && call.State == CallState.Ringing)
            {
                yield return new WaitForSeconds(Random.Range(1.5f, 3f));
                call = Shift.CallOf(workerId);
                if (call == null || call.State != CallState.Ringing)
                    continue;
                Shift.Do(workerId, "answer");
                _desk.Phone.PickUp(Ear);
                yield return Handle(call);
                _desk.Phone.HangUp();
                // Wrap-up: notes, a sip of coffee.
                Shift.Router.SetBreak(workerId, true);
                yield return new WaitForSeconds(Random.Range(6f, 10f));
                Shift.Router.SetBreak(workerId, false);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    bool StillMine(CallCase call) => call.Live && call.Owner == workerId && call.State != CallState.Ringing;

    IEnumerator Step(CallCase call, string intent, string arg = "", string label = "", float pause = 3f)
    {
        if (!StillMine(call))
            yield break;
        Shift.Do(workerId, intent, arg, label);
        yield return new WaitForSeconds(pause * (call.PhraseAid ? 1.8f : 1f));
    }

    IEnumerator Handle(CallCase call)
    {
        yield return new WaitForSeconds(1.5f);
        yield return Step(call, "greet", pause: 3.5f);
        if (call.Language != CallLanguage.English)
        {
            var best = Shift.Router.BestFor(call, workerId);
            if (best != null && StillMine(call))
            {
                // "You speak Spanish, right?" across the office, then the transfer.
                yield return Step(call, "ask_order", pause: 2.5f);
                string to = best.Id == Shift.PlayerId ? "you" : best.Name;
                GameSession.Instance.Hud.ShowSubtitle(displayName, best.Id == Shift.PlayerId
                    ? $"Hey, you speak {CallLanguage.Name(call.Language)}, right? Sending this one over. Order number's in the notes."
                    : $"{best.Name}, a {CallLanguage.Name(call.Language)} caller for you.", 3.6f);
                yield return new WaitForSeconds(1.2f);
                if (StillMine(call))
                {
                    var result = Shift.Do(workerId, "transfer", best.Id);
                    if (call.Owner != workerId && call.Transfers.Count > 0)
                        yield break;
                    Debug.Log($"{displayName} could not transfer to {to}: {result}");
                }
            }
        }
        // Work it through (slowly with the phrase aid).
        yield return Step(call, "ask_order");
        yield return Step(call, "lookup", call.Def.OrderId, pause: 2f);
        yield return Step(call, "ask", call.Def.DetailFact);
        bool unsure = call.PhraseAid || (call.Def.Technical && !technical);
        if (unsure && Random.value < 0.6f)
        {
            var wrong = System.Array.Find(CaseLibrary.Actions(call.Def.Family), a => a.id != call.Def.CorrectAction);
            yield return Step(call, "act", wrong.id, wrong.label);
            yield return Step(call, "apologise", pause: 2f);
        }
        var right = System.Array.Find(CaseLibrary.Actions(call.Def.Family), a => a.id == call.Def.CorrectAction);
        yield return Step(call, "act", right.id, right.label);
        yield return Step(call, "close", pause: 1f);
    }
}
