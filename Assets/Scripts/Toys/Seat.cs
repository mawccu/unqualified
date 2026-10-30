using UnityEngine;

/// <summary>
/// Somewhere to sit for a moment (the break-room couch, a spare chair). Sitting
/// lowers you and stops you walking; E or moving gets you up again. It advances
/// nothing: it is just a place to be.
/// </summary>
public class Seat : Interactable
{
    [SerializeField] Transform sitPoint;
    PlayerActor _sitter;
    Collider[] _bodies;

    public void Configure(Transform point) => sitPoint = point;

    void Awake() => _bodies = GetComponentsInChildren<Collider>();

    public override string Prompt(IInteractionActor actor) => _sitter != null ? "Get up" : "Sit down";

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        if (_sitter != null && (object)_sitter != actor)
            return InteractionResult.Busy;
        return string.IsNullOrEmpty(actor.HeldPropId) || _sitter != null ? InteractionResult.Ok : InteractionResult.HandsFull;
    }

    public override void Use(IInteractionActor actor)
    {
        if (actor is not PlayerActor player)
            return;
        if (_sitter == player)
        {
            Stand();
            return;
        }
        _sitter = player;
        foreach (var c in _bodies)
            if (!c.isTrigger)
                c.enabled = false;
        player.Teleport(sitPoint.position);
        player.SetLook(sitPoint.eulerAngles.y, 0f);
        player.SetCameraOverride(new Pose(sitPoint.position + Vector3.up * 1.05f, sitPoint.rotation), 0.45f);
        player.Avatar.SetIdlePose(WorkerAvatar.SitType);
        player.SeatedOn = this;
    }

    /// Called by the player when they move or press E again.
    public void Stand()
    {
        var player = _sitter;
        if (player == null)
            return;
        _sitter = null;
        player.SeatedOn = null;
        player.ClearCameraOverride(0.35f);
        player.Avatar.SetIdlePose(null);
        player.Teleport(sitPoint.position + sitPoint.forward * 0.55f);
        foreach (var c in _bodies)
            c.enabled = true;
    }

    public override void OnActorLeft(string actorId)
    {
        if (_sitter != null && _sitter.ActorId == actorId)
            Stand();
    }
}
