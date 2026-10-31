using System.Collections;
using UnityEngine;

/// <summary>
/// The floor supervisor. She walks a loop (her desk, the pod, the printer, the
/// break corner) and every so often stands behind an agent and listens in to
/// their call: what she hears then counts. She also reads call notes afterwards,
/// so a written promise can be checked without her being there. Getting a call
/// in a language you claimed is never evidence; failing at it again and again is.
/// Customer service claimants get listened to more often ("to learn from you").
/// </summary>
public class Supervisor : MonoBehaviour
{
    const float Speed = 1.15f;
    const float ListenSeconds = 22f;

    [SerializeField] WorkerLibrary workers;
    [SerializeField] Transform[] route;
    [SerializeField] Transform listenSpot;

    public Suspicion Suspicion { get; private set; }
    public NpcWalker Walker { get; private set; }
    public WorkerAvatar Avatar { get; private set; }
    /// Whose call she is listening to right now ("" when none).
    public string ListeningTo { get; private set; } = "";
    public string Doing { get; private set; } = "walking the floor";

    int _stop;
    float _pause, _nextListen;
    bool _arrived, _busy;
    Transform _greetTarget;

    public void Configure(WorkerLibrary library, Transform[] stops, Transform listen)
    {
        workers = library;
        route = stops;
        listenSpot = listen;
    }

    static CallCenterShift Shift => CallCenterShift.Instance;

    void Awake()
    {
        Walker = NpcWalker.Setup(gameObject, Speed);
        if (workers != null)
            Avatar = WorkerAvatar.Create(workers, 0, WorkerCostumes.Coordinator, transform);
        Walker.Avatar = Avatar;
        Suspicion = gameObject.AddComponent<Suspicion>();
        // She learns from what she hears and reads, not by spotting objects.
        Suspicion.Bind(_ => false, _ => { });
        Walker.Said += line => GameSession.Instance?.Hud.ShowSubtitle("Supervisor", line, 1.6f);
        Walker.Stuck += () =>
        {
            if (!_busy)
                NextStop();
        };
    }

    void Start()
    {
        if (route != null && route.Length > 0)
            Walker.Warp(route[0].position, route[0].rotation);
        Suspicion.Changed += level => Shift?.OnSuspicion(level);
        Suspicion.Strictness = CvLies.Scrutiny(Shift != null ? Shift.Lies : 0);
        _nextListen = 35f / Suspicion.Strictness;
    }

    /// Meet the new agent by the door.
    public void Greet(Transform target)
    {
        _busy = true;
        _greetTarget = target;
        Walker.Stop();
        Doing = "welcoming you";
        Walker.Performing = true;
        Avatar?.Perform(WorkerAvatar.Talk);
    }

    public void ResumePatrol()
    {
        _greetTarget = null;
        _busy = false;
        _arrived = false;
        Walker.Performing = false;
        Avatar?.Perform(WorkerAvatar.Idle);
        if (route != null && route.Length > 0)
            Walker.GoTo(route[_stop].position);
    }

    /// A short comment out loud (she does not need to be next to you to say it).
    public void Remark(string line) => GameSession.Instance?.Hud.ShowSubtitle("Supervisor", line, 2.6f);

    void Update()
    {
        if (_greetTarget != null)
        {
            Walker.Face(_greetTarget.position);
            return;
        }
        if (Shift == null || _busy || route == null || route.Length == 0)
            return;
        if (Shift.Current == CallCenterShift.Phase.Calls && Shift.Clock >= _nextListen)
        {
            var call = Shift.CallOf(Shift.PlayerId);
            if (call != null && (call.State == CallState.Active || call.State == CallState.OnHold))
            {
                StartCoroutine(ListenIn(Shift.PlayerId));
                return;
            }
        }
        var stop = route[_stop];
        if (!_arrived)
        {
            if (!Walker.HasDestination)
                Walker.GoTo(stop.position);
            Doing = "walking the floor";
            if (Walker.Arrived)
            {
                _arrived = true;
                _pause = 3f;
                Walker.Stop();
                Doing = stop.name.Replace("Stop_", "").Replace('_', ' ');
            }
        }
        else
        {
            Walker.Face(stop.position + stop.forward);
            _pause -= Time.deltaTime;
            if (_pause <= 0f)
                NextStop();
        }
    }

    void NextStop()
    {
        _stop = (_stop + 1) % route.Length;
        _arrived = false;
        Walker.GoTo(route[_stop].position);
    }

    IEnumerator ListenIn(string workerId)
    {
        _busy = true;
        Doing = "coming over to listen in";
        Walker.GoTo(listenSpot.position);
        float t = 0f;
        while (!Walker.Arrived && t < 20f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        Walker.Stop();
        var call = Shift.CallOf(workerId);
        if (call != null && call.Live)
        {
            ListeningTo = workerId;
            Doing = "listening in on your call";
            Remark("Don't mind me. Carry on.");
            Walker.Performing = true;
            Avatar?.Perform(WorkerAvatar.HoldPaper);
            for (float k = 0f; k < ListenSeconds && Shift.CallOf(workerId) == call && call.Live; k += Time.deltaTime)
            {
                Walker.Face(Shift.DeskOf(workerId).Keyboard.position);
                yield return null;
            }
            if (!call.Live && call.Outcome == CallOutcome.Resolved && call.Attempts == 0)
                Remark("Nicely done.");
        }
        ListeningTo = "";
        Walker.Performing = false;
        Avatar?.Perform(WorkerAvatar.Idle);
        bool keen = CrewState.Current.HasClaim(Claims.Service);
        // Service claimants, and anyone with a suspiciously good CV, get listened to more.
        _nextListen = Shift.Clock + (keen ? 45f : 85f) / Suspicion.Strictness;
        _busy = false;
        _arrived = false;
        NextStop();
    }
}
