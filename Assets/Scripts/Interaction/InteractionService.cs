using System.Collections.Generic;
using UnityEngine;

public enum InteractionAction { Pickup, Place, Drop, Use, Repair }

public enum InteractionResult
{
    Ok, UnknownActor, NoTarget, HandsFull, NothingHeld, AlreadyHeld,
    OutOfRange, StaleRevision, SocketTaken, SocketRejects, SocketLocked,
    /// Not lined up over the spot (for spots that need care).
    Misaligned,
    /// Something is stacked on top of it.
    Buried,
    /// Someone else is using it.
    Busy,
    /// Not at this point of the story; the Interactable says why.
    NotNow,
}

/// <summary>A player's intent. Offline play and remote peers send the same thing.</summary>
public struct InteractionRequest
{
    public int RequestId;
    public string ActorId;
    public InteractionAction Action;
    public string TargetId;
    public int ExpectedRevision;
}

/// <summary>What the service needs to know about a player.</summary>
public interface IInteractionActor
{
    string ActorId { get; }
    string HeldPropId { get; set; }
    Vector3 EyePosition { get; }
    Transform HoldPoint { get; }
    Vector3 DropVelocity { get; }
}

/// <summary>
/// The single path every world mutation takes, so the host can validate intents
/// instead of trusting clients. Once online, the actor id must come from the
/// connection, never from the request body.
/// </summary>
public sealed class InteractionService
{
    /// Arm's length plus a lean: nothing is picked up, placed or used from further away.
    public const float MaxReach = 1.8f;

    /// False on a client: the host owns the truth, so this copy never drops or
    /// releases props by itself. It only validates, for prompts.
    public bool Authoritative = true;

    readonly Dictionary<string, IInteractionActor> _actors = new();
    readonly Dictionary<string, PickupProp> _props = new();
    readonly Dictionary<string, PlacementSocket> _sockets = new();
    readonly Dictionary<string, Interactable> _interactables = new();

    public void RegisterActor(IInteractionActor actor) => _actors[actor.ActorId] = actor;
    public void RegisterProp(PickupProp prop) => _props[prop.EntityId] = prop;
    public void RegisterSocket(PlacementSocket socket) => _sockets[socket.EntityId] = socket;
    public void RegisterInteractable(Interactable thing) => _interactables[thing.EntityId] = thing;

    /// A player left. On the authority, whatever they held is dropped where it is.
    public void UnregisterActor(string actorId)
    {
        if (!_actors.TryGetValue(actorId, out var actor))
            return;
        if (Authoritative && actor.HeldPropId != null && _props.TryGetValue(actor.HeldPropId, out var held))
            held.Release(Vector3.zero);
        actor.HeldPropId = "";
        _actors.Remove(actorId);
        foreach (var thing in _interactables.Values)
            thing.OnActorLeft(actorId);
    }

    public IEnumerable<PickupProp> Props => _props.Values;
    public PickupProp GetProp(string id) => id != null && _props.TryGetValue(id, out var p) ? p : null;
    public PlacementSocket GetSocket(string id) => id != null && _sockets.TryGetValue(id, out var s) ? s : null;
    public Interactable GetInteractable(string id) => id != null && _interactables.TryGetValue(id, out var i) ? i : null;
    public IInteractionActor GetActor(string id) => id != null && _actors.TryGetValue(id, out var a) ? a : null;

    /// Socket id to the id of the prop sitting in it. Empty sockets are left out.
    public Dictionary<string, string> Occupancy()
    {
        var map = new Dictionary<string, string>();
        foreach (var socket in _sockets.Values)
            if (!string.IsNullOrEmpty(socket.OccupantId))
                map[socket.EntityId] = socket.OccupantId;
        return map;
    }

    /// Checks a request without changing anything.
    public InteractionResult Validate(in InteractionRequest req)
    {
        if (req.ActorId == null || !_actors.TryGetValue(req.ActorId, out var actor))
            return InteractionResult.UnknownActor;
        return req.Action switch
        {
            InteractionAction.Pickup => ValidatePickup(actor, req),
            InteractionAction.Place => ValidatePlace(actor, req),
            InteractionAction.Use => ValidateUse(actor, req),
            InteractionAction.Repair => ValidateRepair(actor, req),
            _ => ValidateDrop(actor),
        };
    }

    /// Validates, then applies.
    public InteractionResult Request(in InteractionRequest req)
    {
        var result = Validate(req);
        if (result == InteractionResult.Ok)
            Apply(req);
        return result;
    }

    /// Every prop and socket back to its starting state, every actor's hands empty.
    /// It does not move players: each peer moves its own player.
    public void ResetAll()
    {
        foreach (var actor in _actors.Values)
            actor.HeldPropId = "";
        foreach (var socket in _sockets.Values)
        {
            socket.OccupantId = "";
            socket.SetPreview(false);
        }
        foreach (var prop in _props.Values)
            prop.ResetToStart();
    }

    InteractionResult ValidatePickup(IInteractionActor actor, in InteractionRequest req)
    {
        var prop = GetProp(req.TargetId);
        if (prop == null)
            return InteractionResult.NoTarget;
        if (!string.IsNullOrEmpty(actor.HeldPropId))
            return InteractionResult.HandsFull;
        if (!string.IsNullOrEmpty(prop.HolderId))
            return InteractionResult.AlreadyHeld;
        if (!InReach(actor, prop.Centre))
            return InteractionResult.OutOfRange;
        if (req.ExpectedRevision != prop.Revision)
            return InteractionResult.StaleRevision;
        // A tier with another on top of it stays put until the top one is lifted off.
        var under = GetSocket(prop.SocketId);
        if (under != null)
            foreach (var other in _sockets.Values)
                if (other.StackOn == under && !string.IsNullOrEmpty(other.OccupantId))
                    return InteractionResult.Buried;
        return InteractionResult.Ok;
    }

    InteractionResult ValidatePlace(IInteractionActor actor, in InteractionRequest req)
    {
        var socket = GetSocket(req.TargetId);
        if (socket == null)
            return InteractionResult.NoTarget;
        var held = GetProp(actor.HeldPropId);
        if (held == null)
            return InteractionResult.NothingHeld;
        if (!string.IsNullOrEmpty(socket.OccupantId))
            return InteractionResult.SocketTaken;
        if (!socket.Accepts(held))
            return InteractionResult.SocketRejects;
        if (!string.IsNullOrEmpty(socket.RequiresSocket))
        {
            var below = GetSocket(socket.RequiresSocket);
            if (below != null && string.IsNullOrEmpty(below.OccupantId))
                return InteractionResult.SocketLocked;
        }
        if (!InReach(actor, socket.transform.position))
            return InteractionResult.OutOfRange;
        if (!socket.IsLinedUp(held))
            return InteractionResult.Misaligned;
        return InteractionResult.Ok;
    }

    /// Fixing a smeared cake with the spatula (the hold-to-smooth finished).
    InteractionResult ValidateRepair(IInteractionActor actor, in InteractionRequest req)
    {
        var tool = GetProp(actor.HeldPropId);
        var target = GetProp(req.TargetId);
        if (target == null)
            return InteractionResult.NoTarget;
        if (tool == null || tool.Kind != "spatula")
            return InteractionResult.NothingHeld;
        if (target.State != PickupProp.Condition.Smeared)
            return InteractionResult.NotNow;
        if (!InReach(actor, target.Centre))
            return InteractionResult.OutOfRange;
        return InteractionResult.Ok;
    }

    InteractionResult ValidateUse(IInteractionActor actor, in InteractionRequest req)
    {
        var thing = GetInteractable(req.TargetId);
        if (thing == null)
            return InteractionResult.NoTarget;
        if (thing.DistanceFrom(actor.EyePosition) > MaxReach)
            return InteractionResult.OutOfRange;
        return thing.CanUse(actor);
    }

    /// A hand-over from someone who is not a player (the landlord's bill): the
    /// authority gives the prop straight to the actor. Until this call the prop
    /// is the giver's; after it, the actor's. Never both.
    public InteractionResult GiveProp(PickupProp prop, IInteractionActor actor)
    {
        if (!string.IsNullOrEmpty(actor.HeldPropId))
            return InteractionResult.HandsFull;
        prop.BeginHold(actor.HoldPoint, actor.ActorId);
        actor.HeldPropId = prop.EntityId;
        return InteractionResult.Ok;
    }

    /// Puts what the actor holds onto a socket without the reach check (a seat
    /// at the desk puts the bill down beside the keyboard).
    public InteractionResult PlaceHeldOn(IInteractionActor actor, PlacementSocket socket)
    {
        var held = GetProp(actor.HeldPropId);
        if (held == null)
            return InteractionResult.NothingHeld;
        if (!string.IsNullOrEmpty(socket.OccupantId))
            return InteractionResult.SocketTaken;
        if (!socket.Accepts(held))
            return InteractionResult.SocketRejects;
        socket.SetOccupant(held);
        held.PlaceAt(socket.SnapPosition, socket.transform.rotation, socket.EntityId);
        actor.HeldPropId = "";
        return InteractionResult.Ok;
    }

    InteractionResult ValidateDrop(IInteractionActor actor) =>
        GetProp(actor.HeldPropId) == null ? InteractionResult.NothingHeld : InteractionResult.Ok;

    void Apply(in InteractionRequest req)
    {
        var actor = _actors[req.ActorId];
        switch (req.Action)
        {
            case InteractionAction.Pickup:
            {
                var prop = _props[req.TargetId];
                var from = GetSocket(prop.SocketId);
                if (from != null)
                    from.OccupantId = "";
                prop.BeginHold(actor.HoldPoint, actor.ActorId);
                actor.HeldPropId = prop.EntityId;
                break;
            }
            case InteractionAction.Place:
            {
                var held = _props[actor.HeldPropId];
                var socket = _sockets[req.TargetId];
                // Lined-up spots keep where it actually is; a poor line-up leans.
                Vector3 landing = socket.LandingFor(held, out float quality);
                var rotation = Quaternion.Euler(0f, held.transform.eulerAngles.y, 0f);
                if (socket.AlignTolerance > 0f && quality < 0.4f)
                {
                    Vector3 lean = landing - socket.SnapPosition;
                    lean.y = 0f;
                    if (lean.sqrMagnitude > 1e-6f)
                        rotation = Quaternion.AngleAxis(5f, Vector3.Cross(Vector3.up, lean.normalized)) * rotation;
                }
                else if (socket.AlignTolerance <= 0f)
                    rotation = socket.transform.rotation;
                held.PlacementQuality = quality;
                socket.SetOccupant(held);
                held.PlaceAt(landing, rotation, socket.EntityId);
                actor.HeldPropId = "";
                break;
            }
            case InteractionAction.Drop:
            {
                _props[actor.HeldPropId].Release(actor.DropVelocity);
                actor.HeldPropId = "";
                break;
            }
            case InteractionAction.Use:
                _interactables[req.TargetId].Use(actor);
                break;
            case InteractionAction.Repair:
                _props[req.TargetId].SetCondition(PickupProp.Condition.Fine);
                break;
        }
    }

    static bool InReach(IInteractionActor actor, Vector3 point) => Vector3.Distance(actor.EyePosition, point) <= MaxReach;
}
