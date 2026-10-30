using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The wedding coordinator: the client's eyes in the ballroom. For now she only
/// patrols a fixed loop and pauses to look at each station. Attention, line of
/// sight and suspicion come next; this is the body and the route they will use.
///
/// Only the authority (offline player or host) walks the route; a client copy
/// will follow the pose it is sent. Route numbers are the Godot ones (north is
/// -Z there), converted on load.
/// </summary>
public class Coordinator : MonoBehaviour
{
    const float Speed = 1.25f;
    const float TurnSpeed = 4f;

    struct Stop
    {
        public Vector3 Position, Look;
        public float Pause;
        public string State;
    }

    static Vector3 G(float x, float y, float z) => new(x, y, -z);

    static readonly Stop[] Route =
    {
        new() { Position = G(0f, 0f, -1f), Pause = 1.5f, Look = G(0f, 1.5f, -14f), State = "checking the stage" },
        new() { Position = G(0f, 0f, -9f), Pause = 2.5f, Look = G(7.5f, 1f, -11.2f), State = "checking the sound desk" },
        new() { Position = G(6f, 0f, -6.5f), Pause = 0.1f, Look = G(10.8f, 1f, -6.6f), State = "walking" },
        new() { Position = G(10.8f, 0f, -6.6f), Pause = 3.5f, Look = G(12.7f, 1.1f, -5f), State = "inspecting the desserts" },
        new() { Position = G(0f, 0f, -6f), Pause = 1.2f, Look = G(-9f, 1f, -9f), State = "watching the floor" },
        new() { Position = G(-6f, 0f, -6.5f), Pause = 3f, Look = G(-9f, 1f, -6.5f), State = "checking the seating" },
        new() { Position = G(-11f, 0f, -7f), Pause = 2f, Look = G(-12.5f, 1.4f, -7f), State = "admiring the photo corner" },
        new() { Position = G(-6.5f, 0f, -6f), Pause = 0.1f, Look = G(-5f, 1f, -1f), State = "walking" },
        new() { Position = G(-5f, 0f, -1f), Pause = 0.5f, Look = G(0f, 1f, -1f), State = "walking" },
    };

    [SerializeField] WorkerLibrary workers;

    public bool Authoritative = true;
    public string State { get; private set; } = "patrolling";

    WorkerAvatar _avatar;
    int _stop;
    float _pauseLeft;
    bool _arrived;
    Vector3 _lastPosition;
    bool _greeting;
    Transform _greetTarget;

    public void Configure(WorkerLibrary library) => workers = library;

    void Awake()
    {
        transform.position = Route[0].Position;
        _lastPosition = transform.position;
        if (workers != null)
            _avatar = WorkerAvatar.Create(workers, 0, WorkerCostumes.Coordinator, transform);
    }

    /// Meet the crew at the staff entrance before the patrol: face the target,
    /// say each line with its pose, then go about the route.
    public void Greet(Transform target, Transform spot, IList<(string pose, string line, float seconds)> lines, Action<string, float> say)
    {
        transform.SetPositionAndRotation(spot.position, spot.rotation);
        _lastPosition = transform.position;
        _greetTarget = target;
        _greeting = true;
        State = "welcoming the crew";
        StartCoroutine(GreetRoutine(lines, say));
    }

    IEnumerator GreetRoutine(IList<(string pose, string line, float seconds)> lines, Action<string, float> say)
    {
        yield return new WaitForSeconds(1.2f);
        foreach (var (pose, line, seconds) in lines)
        {
            _avatar?.Perform(pose);
            say?.Invoke(line, seconds);
            yield return new WaitForSeconds(seconds);
        }
        _avatar?.Perform(WorkerAvatar.Idle);
        _greeting = false;
        _stop = 0;
        _arrived = false;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (_greeting)
        {
            if (_greetTarget != null)
                Face(_greetTarget.position, dt);
            _lastPosition = transform.position;
            return;
        }
        if (Authoritative)
            Walk(dt);
        var velocity = (transform.position - _lastPosition) / Mathf.Max(dt, 0.0001f);
        _lastPosition = transform.position;
        if (_avatar != null)
            _avatar.UpdateMotion(velocity, false, false, dt);
    }

    void Walk(float dt)
    {
        var stop = Route[_stop];
        if (!_arrived)
        {
            Vector3 toTarget = stop.Position - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude < 0.08f)
            {
                _arrived = true;
                _pauseLeft = stop.Pause;
                State = stop.State;
            }
            else
            {
                transform.position += toTarget.normalized * Mathf.Min(Speed * dt, toTarget.magnitude);
                Face(transform.position + toTarget, dt);
                State = "walking";
            }
        }
        else
        {
            Face(stop.Look, dt);
            _pauseLeft -= dt;
            if (_pauseLeft <= 0f)
            {
                _stop = (_stop + 1) % Route.Length;
                _arrived = false;
            }
        }
    }

    void Face(Vector3 point, float dt)
    {
        Vector3 flat = point - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0025f)
            return;
        var wanted = Quaternion.LookRotation(flat, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, wanted, Mathf.Min(1f, TurnSpeed * dt));
    }

    /// Pose as the network should carry it.
    public (Vector3 position, float yaw, string state) SyncPose() => (transform.position, transform.eulerAngles.y, State);
}
