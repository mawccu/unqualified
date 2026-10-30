using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A wedding guest. Arrives at the entrance, waits to be shown to a seat (or
/// finds one alone when nobody on the crew claimed to do seating), follows
/// whoever escorts them, sits down, and says plainly if the seat is wrong, then
/// gets up to be seated again.
/// </summary>
public class Guest : Interactable
{
    public enum Mode { Arriving, Waiting, Following, ToSeat, Seated, Upset }

    public static readonly List<Guest> All = new();

    public GuestProfile Profile { get; private set; }
    public Mode State { get; private set; } = Mode.Arriving;
    public PlayerActor Leader { get; private set; }
    public GuestTable Table { get; private set; }
    public PickupProp Chair { get; private set; }
    public bool Satisfied { get; private set; }
    public int Complaints { get; private set; }
    public event Action<Guest, string> Complained;

    NpcWalker _walker;
    WorkerAvatar _avatar;
    Vector3 _waitSpot;
    float _repath;
    CapsuleCollider _body;

    public static Guest Spawn(GuestProfile profile, WorkerLibrary library, int look, Vector3 at, Vector3 waitSpot)
    {
        var go = new GameObject($"Guest_{profile.Name}");
        go.transform.position = at;
        var walker = NpcWalker.Setup(go, 1.1f);
        var guest = go.AddComponent<Guest>();
        guest.Profile = profile;
        guest.SetEntityId("guest_" + profile.Name.Replace(" ", "_").ToLowerInvariant());
        guest._walker = walker;
        guest._waitSpot = waitSpot;
        guest._body = go.GetComponent<CapsuleCollider>();
        guest._avatar = WorkerAvatar.Create(library, look, GuestCostume(look), go.transform);
        walker.Avatar = guest._avatar;
        walker.Warp(at, Quaternion.identity);
        walker.GoTo(waitSpot);
        GameSession.Instance?.Service.RegisterInteractable(guest);
        All.Add(guest);
        return guest;
    }

    void OnDestroy() => All.Remove(this);

    /// Party clothes, varied by guest.
    static WorkerCostumes.Costume GuestCostume(int look)
    {
        Color[] suits = { new(0.2f, 0.25f, 0.45f), new(0.45f, 0.2f, 0.3f), new(0.25f, 0.4f, 0.3f), new(0.5f, 0.45f, 0.3f), new(0.3f, 0.3f, 0.32f), new(0.55f, 0.35f, 0.55f) };
        Color[] skins = { new(0.85f, 0.69f, 0.57f), new(0.6f, 0.43f, 0.32f), new(0.9f, 0.74f, 0.62f), new(0.45f, 0.32f, 0.24f) };
        string[] hair = { "short", "pompadour", "ponytail", "beanie" };
        return new WorkerCostumes.Costume
        {
            Name = "Guest",
            Shirt = new Color(0.95f, 0.94f, 0.9f),
            Vest = suits[look % suits.Length],
            Trousers = suits[look % suits.Length] * 0.7f,
            Shoes = new Color(0.1f, 0.09f, 0.09f),
            Skin = skins[look % skins.Length],
            Hair = look % 3 == 0 ? new Color(0.7f, 0.7f, 0.72f) : new Color(0.2f, 0.13f, 0.1f),
            Accent = new Color(0.9f, 0.8f, 0.4f),
            HairStyle = hair[look % hair.Length],
            Accessories = look % 2 == 0 ? new[] { "bow_tie" } : Array.Empty<string>(),
        };
    }

    public static HashSet<PickupProp> TakenChairs()
    {
        var taken = new HashSet<PickupProp>();
        foreach (var g in All)
            if (g.Chair != null && (g.State == Mode.Seated || g.State == Mode.ToSeat))
                taken.Add(g.Chair);
        return taken;
    }

    // Being shown to a seat ---------------------------------------------------------------

    public override string Prompt(IInteractionActor actor) => State switch
    {
        Mode.Following => $"Ask {Profile.Name} to wait here",
        _ => $"Show {Profile.Name} to a seat  <color=#FFFFFF99>({Profile.Clue})</color>",
    };

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        if (State == Mode.Following)
            return Leader != null && Leader.ActorId == actor.ActorId ? InteractionResult.Ok : InteractionResult.Busy;
        if (State != Mode.Waiting && State != Mode.Upset && State != Mode.Arriving)
            return InteractionResult.NotNow;
        foreach (var g in All)
            if (g != this && g.Leader != null && g.Leader.ActorId == actor.ActorId)
                return InteractionResult.Busy; // one guest at a time
        return InteractionResult.Ok;
    }

    public override string BlockedReason(IInteractionActor actor) => State == Mode.Seated ? $"{Profile.Name} is seated." : "";

    public override void Use(IInteractionActor actor)
    {
        if (State == Mode.Following)
        {
            Leader = null;
            State = Mode.Waiting;
            _walker.Stop();
            return;
        }
        Leader = actor as PlayerActor;
        State = Mode.Following;
        Say("Lead the way.");
    }

    /// Escorted guest: walk to a free chair at this table and sit.
    public void SeatAt(GuestTable table)
    {
        var chairs = table.FreeChairs(TakenChairs());
        if (chairs.Count == 0)
            return;
        // The nearest free chair to where the guest is.
        chairs.Sort((a, b) => Vector3.Distance(a.transform.position, transform.position).CompareTo(Vector3.Distance(b.transform.position, transform.position)));
        Chair = chairs[0];
        Table = table;
        Leader = null;
        State = Mode.ToSeat;
        _walker.GoTo(Chair.transform.position - Chair.transform.forward * 0.45f);
    }

    /// Without a seating coordinator, guests find their own (valid) table.
    public void SeatYourself(GuestTable table) => SeatAt(table);

    void Update()
    {
        switch (State)
        {
            case Mode.Arriving:
                if (_walker.Arrived)
                {
                    State = Mode.Waiting;
                    _walker.Stop();
                }
                break;
            case Mode.Following:
                if (Leader == null)
                {
                    State = Mode.Waiting;
                    break;
                }
                _repath -= Time.deltaTime;
                if (_repath <= 0f)
                {
                    _repath = 0.4f;
                    Vector3 behind = Leader.transform.position - Leader.transform.forward * 1.0f;
                    if (Vector3.Distance(transform.position, behind) > 0.6f)
                        _walker.GoTo(behind);
                }
                break;
            case Mode.ToSeat:
                if (Chair == null || !string.IsNullOrEmpty(Chair.HolderId))
                {
                    State = Mode.Waiting;
                    break;
                }
                if (_walker.Arrived)
                    SitDown();
                break;
            case Mode.Seated:
                // Someone took the chair away: up they get.
                if (Chair == null || !string.IsNullOrEmpty(Chair.HolderId) || Vector3.Distance(Chair.transform.position, transform.position) > 0.6f)
                {
                    StandUp();
                    Complain("Excuse me! I was sitting on that.");
                }
                break;
        }
    }

    void SitDown()
    {
        _walker.Stop();
        _walker.Performing = true;
        _body.enabled = false;
        Vector3 seat = Chair.transform.position + Chair.transform.forward * 0.02f;
        _walker.Agent.enabled = false;
        transform.SetPositionAndRotation(seat, Quaternion.LookRotation(Table.transform.position - seat - Vector3.up * (Table.transform.position.y - seat.y)));
        _avatar.Perform(WorkerAvatar.SitType);
        State = Mode.Seated;
        string problem = Seating.Judge(this);
        if (problem == null)
        {
            Satisfied = true;
            Say(UnityEngine.Random.value < 0.5f ? "Lovely, thank you." : "Perfect.");
        }
        else
        {
            Satisfied = false;
            StartCoroutine(UpsetSoon(problem));
        }
    }

    System.Collections.IEnumerator UpsetSoon(string problem)
    {
        yield return new WaitForSeconds(1.2f);
        StandUp();
        Complain(problem);
    }

    void StandUp()
    {
        _walker.Agent.enabled = true;
        _walker.Warp(transform.position - transform.forward * 0.6f, transform.rotation);
        _walker.Performing = false;
        _body.enabled = true;
        _avatar.Perform(WorkerAvatar.Impatient);
        Satisfied = false;
        Chair = null;
        Table = null;
        State = Mode.Upset;
    }

    void Complain(string line)
    {
        Complaints++;
        Say(line);
        Complained?.Invoke(this, line);
    }

    void Say(string line) => GameSession.Instance?.Hud.ShowSubtitle(Profile.Name, line, 2.6f);

    /// Hands judging to the shift's seating state (which knows everyone's tables).
    public static ISeatingJudge Seating;
}

public interface ISeatingJudge
{
    string Judge(Guest guest);
}
