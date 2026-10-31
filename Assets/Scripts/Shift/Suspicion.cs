using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Something that went wrong where someone might notice it.</summary>
public sealed class Incident
{
    public int Id;
    /// Same key = same incident (a cake that fell is one incident however often it is reported).
    public string Key;
    public string What;          // plain words: "a cake layer on the floor"
    public Vector3 Position;
    public float Severity;       // suspicion added when witnessed
    public bool Major;           // keeps a suspicion floor while unresolved
    public Func<bool> StillThere; // false once fixed or cleaned
    public bool Witnessed, Resolved;
}

/// <summary>
/// The coordinator's suspicion of the crew. Task quality and suspicion are
/// separate: a mistake only costs suspicion when it is seen (range, facing and
/// a clear line of sight: walls and closed doors block it). A noise does not
/// tell her who or what; it makes her come and look, and what she then sees
/// counts. Minor attention fades; witnessed major evidence holds a floor until
/// it is cleaned or fixed. Every incident is counted once.
/// </summary>
public class Suspicion : MonoBehaviour
{
    public enum Level { Calm, Watching, Questioning, Warning, Exposed }

    public const float WatchingAt = 25f, QuestioningAt = 50f, WarningAt = 75f, ExposedAt = 100f;
    /// Attention fades slowly, and only after a quiet spell with nothing new.
    public const float FadePerSecond = 0.4f;
    public const float QuietBeforeFade = 10f;

    /// How strict this watcher is (the CV's lies set it): suspicion gained is
    /// multiplied by it, fading is divided by it.
    public float Strictness { get; set; } = 1f;

    public float Value { get; private set; }
    public Level State { get; private set; } = Level.Calm;
    public IReadOnlyList<Incident> Incidents => _incidents;
    public IEnumerable<Incident> Witnessed { get { foreach (var i in _incidents) if (i.Witnessed) yield return i; } }

    /// (incident, line) when she sees something; the HUD says what caught her eye.
    public event Action<Incident> Saw;
    public event Action<Level> Changed;

    readonly List<Incident> _incidents = new();
    int _nextId;
    Func<Vector3, bool> _canSee;
    Action<Vector3> _investigate;
    float _floor, _quiet;

    /// Wires her senses: canSee(point) and investigate(point).
    public void Bind(Func<Vector3, bool> canSee, Action<Vector3> investigate)
    {
        _canSee = canSee;
        _investigate = investigate;
    }

    /// Records a visible mistake. Reporting the same key again returns the open
    /// incident (deduplicated); a resolved one may be reported anew.
    public Incident Report(string key, string what, Vector3 position, float severity, bool major, Func<bool> stillThere)
    {
        foreach (var open in _incidents)
            if (open.Key == key && !open.Resolved)
            {
                open.Position = position;
                return open;
            }
        var incident = new Incident { Id = ++_nextId, Key = key, What = what, Position = position, Severity = severity, Major = major, StillThere = stillThere };
        _incidents.Add(incident);
        return incident;
    }

    /// A sound she can hear: she comes to look, but learns nothing by itself.
    public void Noise(Vector3 position, float radius)
    {
        if (_investigate != null && Vector3.Distance(position, transform.position) <= radius)
            _investigate(position);
    }

    /// Adds suspicion directly (a lie that did not land, a missed deadline). Once per key.
    public void Raise(string key, string what, float amount)
    {
        foreach (var i in _incidents)
            if (i.Key == key)
                return;
        var incident = new Incident { Id = ++_nextId, Key = key, What = what, Position = transform.position, Severity = amount, Witnessed = true, Resolved = true, StillThere = () => false };
        _incidents.Add(incident);
        Add(amount);
    }

    /// Lowers suspicion (a believable excuse, good work noticed).
    public void Ease(float amount) => Add(-amount);

    void Update() => Tick(Time.deltaTime);

    /// One step of noticing and fading (Update calls it; tests call it directly).
    public void Tick(float dt)
    {
        float floor = 0f;
        foreach (var incident in _incidents)
        {
            if (incident.Resolved)
                continue;
            if (incident.StillThere != null && !incident.StillThere())
            {
                incident.Resolved = true;
                continue;
            }
            if (!incident.Witnessed && _canSee != null && _canSee(incident.Position))
            {
                incident.Witnessed = true;
                Add(incident.Severity);
                Saw?.Invoke(incident);
            }
            if (incident.Witnessed && incident.Major)
                floor += incident.Severity * Strictness * 0.6f;
        }
        _floor = Mathf.Min(floor, WarningAt - 1f);
        _quiet += dt;
        if (Value > _floor && State != Level.Exposed && _quiet >= QuietBeforeFade)
            Value = Mathf.Max(_floor, Value - FadePerSecond / Strictness * dt);
        UpdateState();
    }

    void Add(float amount)
    {
        if (State == Level.Exposed)
            return;
        if (amount > 0f)
        {
            amount *= Strictness;
            _quiet = 0f;
        }
        Value = Mathf.Clamp(Value + amount, 0f, ExposedAt);
        UpdateState();
    }

    void UpdateState()
    {
        var next = Value >= ExposedAt ? Level.Exposed : Value >= WarningAt ? Level.Warning : Value >= QuestioningAt ? Level.Questioning
            : Value >= WatchingAt ? Level.Watching : Level.Calm;
        // Exposure is final for the shift; the others go up and down.
        if (State == Level.Exposed || next == State)
            return;
        State = next;
        Changed?.Invoke(next);
    }
}
