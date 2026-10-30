using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// What the coordinator notices and what she does about it. She sees through a
/// cone in front of her (about 13 m, 65 degrees either side) and only where
/// nothing solid is in the way. Noises bring her over to look. She announces
/// inspections before walking to a station. When suspicion is high enough she
/// comes to ask about it, and the player answers with one of three short
/// replies; a bluff buys time, it does not fix anything.
/// </summary>
[RequireComponent(typeof(Coordinator))]
public class CoordinatorMind : MonoBehaviour
{
    const float SightRange = 13f;
    const float SightHalfAngle = 65f;
    const float EyeHeight = 1.6f;
    const float QuestionCooldown = 45f;
    const float NoiseMemory = 60f;

    public Suspicion Suspicion { get; private set; }
    public bool Questioning { get; private set; }
    public string Doing { get; private set; } = "";

    Coordinator _body;
    readonly List<float> _recentNoises = new();
    float _lastQuestion = -999f;
    float _lastInvestigate = -999f;
    Coroutine _errand;
    readonly HashSet<string> _bluffsUsed = new();
    int _distractions;

    /// The shift fills these in: what to inspect, and how to bluff about each claim.
    public Func<string> BluffLine;
    public Func<IEnumerable<Incident>> OpenProblems;

    void Awake()
    {
        _body = GetComponent<Coordinator>();
        Suspicion = gameObject.AddComponent<Suspicion>();
        Suspicion.Bind(CanSee, Investigate);
        Suspicion.Saw += incident =>
        {
            Hud?.ShowToast($"The coordinator saw {incident.What}.");
            if (!Questioning && _errand == null)
                StartErrand(LookAt(incident.Position, 1.2f, "staring at " + incident.What));
        };
        Suspicion.Changed += level =>
        {
            if (level == Suspicion.Level.Warning)
                Say("That's a formal warning. One more thing and you're done here.", 3.4f);
        };
    }

    static Hud Hud => GameSession.Instance != null ? GameSession.Instance.Hud : null;

    void Update()
    {
        if (Suspicion.State == Suspicion.Level.Questioning || Suspicion.State == Suspicion.Level.Warning)
            if (!Questioning && Time.time - _lastQuestion > QuestionCooldown && _errand == null && !_body.Greeting)
                StartErrand(Question());
    }

    // Senses -------------------------------------------------------------------------

    public Vector3 Eye => transform.position + Vector3.up * EyeHeight;

    /// Sight: range, facing, and nothing solid between her eyes and the point.
    public bool CanSee(Vector3 point)
    {
        Vector3 to = point + Vector3.up * 0.15f - Eye;
        float distance = to.magnitude;
        if (distance > SightRange)
            return false;
        Vector3 flat = new Vector3(to.x, 0f, to.z);
        if (flat.sqrMagnitude > 0.04f && Vector3.Angle(transform.forward, flat) > SightHalfAngle)
            return false;
        return !Physics.Raycast(Eye, to / distance, distance - 0.2f, 1 << GameLayers.World, QueryTriggerInteraction.Ignore);
    }

    /// A noise: she comes over to look. The more often it happens, the less she bothers.
    public void Investigate(Vector3 point)
    {
        if (Questioning || _body.Greeting || Time.time - _lastInvestigate < 4f)
            return;
        _recentNoises.RemoveAll(t => Time.time - t > NoiseMemory);
        _recentNoises.Add(Time.time);
        if (_recentNoises.Count > 3)
        {
            Say("Whatever that is, stop it.", 2f);
            return;
        }
        _lastInvestigate = Time.time;
        StartErrand(LookAt(point, 2.5f, "looking into a noise"));
    }

    // Errands: things that pause the patrol ----------------------------------------------

    void StartErrand(IEnumerator routine)
    {
        if (_errand != null)
            StopCoroutine(_errand);
        _errand = StartCoroutine(Run(routine));
    }

    IEnumerator Run(IEnumerator routine)
    {
        _body.Busy = true;
        yield return routine;
        _body.ResumePatrol();
        Doing = "";
        _errand = null;
    }

    /// Walk to within a couple of metres of a point and look at it.
    IEnumerator LookAt(Vector3 point, float seconds, string doing)
    {
        Doing = doing;
        Vector3 from = transform.position - point;
        from.y = 0f;
        Vector3 stand = point + (from.sqrMagnitude > 0.01f ? from.normalized : Vector3.back) * 1.8f;
        yield return WalkTo(stand, 8f);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            _body.Walker.Face(point, 6f);
            yield return null;
        }
    }

    IEnumerator WalkTo(Vector3 point, float timeout)
    {
        _body.GoAndStand(point);
        for (float t = 0f; t < timeout && !_body.Walker.Arrived; t += Time.deltaTime)
            yield return null;
        _body.Walker.Stop();
    }

    /// Announce, wait, walk over, look. What she sees there counts.
    public void Inspect(string station, Vector3 point)
    {
        if (Questioning || _errand != null)
            return;
        Say($"I'll check the {station} in a moment.", 2.6f);
        StartErrand(InspectRoutine(station, point));
    }

    IEnumerator InspectRoutine(string station, Vector3 point)
    {
        Doing = $"about to check the {station}";
        yield return new WaitForSeconds(6f);
        yield return LookAt(point, 3.5f, $"inspecting the {station}");
    }

    // Questions ---------------------------------------------------------------------------

    IEnumerator Question()
    {
        Questioning = true;
        _lastQuestion = Time.time;
        var player = GameSession.Instance.Player;
        Doing = "coming to have a word";
        yield return WalkTo(player.transform.position + (transform.position - player.transform.position).normalized * 1.3f, 10f);
        Doing = "asking questions";
        string about = "";
        foreach (var i in Suspicion.Witnessed)
            if (!i.Resolved)
            {
                about = i.What;
                break;
            }
        var avatar = _body.Avatar;
        avatar?.Perform(WorkerAvatar.Point);
        _body.Walker.Performing = true;
        Say(string.IsNullOrEmpty(about) ? "A word. What exactly is going on over here?" : $"A word. What happened with {about}?", 3f);
        int choice = -1;
        string bluff = BluffLine != null ? BluffLine() : "First day nerves, sorry!";
        Hud?.ShowChoices(new[] { $"\"{bluff}\"", "\"I'm fixing it right now.\"", "\"Is that the photographer? He looks lost.\"" }, 9f, i => choice = i);
        float t = 0f;
        while (choice == -1 && t < 9.5f)
        {
            _body.Walker.Face(player.transform.position, 6f);
            t += Time.deltaTime;
            yield return null;
        }
        avatar?.Perform(WorkerAvatar.Talk);
        switch (choice)
        {
            case 0:
                if (_bluffsUsed.Add(bluff))
                {
                    Say("...Right. I suppose you'd know.", 2.4f);
                    Suspicion.Ease(12f);
                }
                else
                {
                    Say("You said that already.", 2f);
                    Suspicion.Ease(3f);
                }
                break;
            case 1:
                Say("You have twenty-five seconds.", 2.2f);
                StartCoroutine(Deadline(25f));
                break;
            case 2:
                _distractions++;
                if (_distractions <= 2)
                {
                    Say("The photographer? Where?", 1.8f);
                    yield return new WaitForSeconds(1.2f);
                    _body.Walker.Performing = false;
                    avatar?.Perform(WorkerAvatar.Idle);
                    Questioning = false;
                    yield return LookAt(transform.position + transform.right * 6f, 4f, "looking for the photographer");
                    yield break;
                }
                Say("Nice try.", 1.6f);
                Suspicion.Raise($"distraction_{_distractions}", "an obvious distraction", 5f);
                break;
            default:
                Say("Nothing to say? Noted.", 2f);
                Suspicion.Raise($"silence_{Time.frameCount}", "saying nothing", 6f);
                break;
        }
        yield return new WaitForSeconds(2.2f);
        _body.Walker.Performing = false;
        avatar?.Perform(WorkerAvatar.Idle);
        Questioning = false;
    }

    /// A promised fix: if what she saw is still there when time is up, it counts double.
    IEnumerator Deadline(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        foreach (var i in Suspicion.Witnessed)
            if (!i.Resolved && i.Major)
            {
                Say("Still not fixed. I'm writing that down.", 2.4f);
                Suspicion.Raise($"promise_{i.Id}", $"{i.What}, still there after you promised", 12f);
                break;
            }
    }

    void Say(string line, float seconds) => Hud?.ShowSubtitle("Coordinator", line, seconds);
}
