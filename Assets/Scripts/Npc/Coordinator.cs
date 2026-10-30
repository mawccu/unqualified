using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The wedding coordinator: the client's eyes in the ballroom. She greets the
/// crew, then patrols a loop of stations, walking on the navigation mesh (round
/// tables and moved chairs, past the player) and pausing to look at each one.
/// Suspicion builds on top of this body and route.
///
/// Only the authority walks the route; a client copy will follow the pose it is
/// sent. Route numbers are the Godot ones (north is -Z there), converted on load.
/// </summary>
public class Coordinator : MonoBehaviour
{
    const float Speed = 1.25f;

    public struct Stop
    {
        public Vector3 Position, Look;
        public float Pause;
        public string State;
    }

    static Vector3 G(float x, float y, float z) => new(x, y, -z);

    public static readonly Stop[] Route =
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
    public NpcWalker Walker { get; private set; }
    public WorkerAvatar Avatar { get; private set; }
    /// Something else (an inspection, a question) is directing her; the patrol waits.
    public bool Busy { get; set; }

    int _stop;
    float _pauseLeft;
    bool _arrived;
    bool _greeting;
    Transform _greetTarget;

    public void Configure(WorkerLibrary library) => workers = library;

    void Awake()
    {
        Walker = NpcWalker.Setup(gameObject, Speed);
        if (workers != null)
            Avatar = WorkerAvatar.Create(workers, 0, WorkerCostumes.Coordinator, transform);
        Walker.Avatar = Avatar;
        Walker.Warp(Route[0].Position, Quaternion.identity);
        Walker.Said += line => GameSession.Instance?.Hud.ShowSubtitle("Coordinator", line, 1.6f);
        Walker.Stuck += () =>
        {
            // Someone is planted in her way: go on to the next stop rather than shove.
            if (!_greeting && !Busy)
                NextStop();
        };
    }

    /// Meet the crew at the staff entrance before the patrol: face the target,
    /// say each line with its pose, then go about the route.
    public void Greet(Transform target, Transform spot, IList<(string pose, string line, float seconds)> lines, Action<string, float> say)
    {
        Walker.Warp(spot.position, spot.rotation);
        Walker.Stop();
        _greetTarget = target;
        _greeting = true;
        State = "welcoming the crew";
        StartCoroutine(GreetRoutine(lines, say));
    }

    IEnumerator GreetRoutine(IList<(string pose, string line, float seconds)> lines, Action<string, float> say)
    {
        Walker.Performing = true;
        yield return new WaitForSeconds(1.2f);
        foreach (var (pose, line, seconds) in lines)
        {
            Avatar?.Perform(pose);
            say?.Invoke(line, seconds);
            yield return new WaitForSeconds(seconds);
        }
        Avatar?.Perform(WorkerAvatar.Idle);
        Walker.Performing = false;
        _greeting = false;
        _stop = 0;
        _arrived = false;
        Walker.GoTo(Route[0].Position);
    }

    void Update()
    {
        if (_greeting)
        {
            if (_greetTarget != null)
                Walker.Face(_greetTarget.position);
            return;
        }
        if (!Authoritative || Busy)
            return;
        var stop = Route[_stop];
        if (!_arrived)
        {
            if (!Walker.HasDestination)
                Walker.GoTo(stop.Position);
            if (Walker.Arrived)
            {
                _arrived = true;
                _pauseLeft = stop.Pause;
                State = stop.State;
                Walker.Stop();
            }
            else
                State = "walking";
        }
        else
        {
            Walker.Face(stop.Look);
            _pauseLeft -= Time.deltaTime;
            if (_pauseLeft <= 0f)
                NextStop();
        }
    }

    void NextStop()
    {
        _stop = (_stop + 1) % Route.Length;
        _arrived = false;
        Walker.GoTo(Route[_stop].Position);
    }

    /// Walk to a point and stop there (an inspection, a question). The patrol resumes when Busy clears.
    public void GoAndStand(Vector3 point)
    {
        _arrived = false;
        Walker.GoTo(point);
    }

    /// Back to the patrol from wherever she is.
    public void ResumePatrol()
    {
        Busy = false;
        _arrived = false;
        Walker.GoTo(Route[_stop].Position);
    }

    /// Pose as the network should carry it.
    public (Vector3 position, float yaw, string state) SyncPose() => (transform.position, transform.eulerAngles.y, State);
}
