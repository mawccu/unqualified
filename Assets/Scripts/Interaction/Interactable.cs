using UnityEngine;

/// <summary>
/// Something in the world a player uses rather than carries: a door, a desk, a
/// person offering you something. Uses go through InteractionService like every
/// other world change, so the authority decides and clients only ask.
/// </summary>
public abstract class Interactable : MonoBehaviour
{
    [SerializeField] string entityId = "interactable";

    public string EntityId => entityId;
    public void SetEntityId(string id) => entityId = id;

    /// What the prompt says when this can be used now, e.g. "Answer the door".
    public abstract string Prompt(IInteractionActor actor);

    /// Ok, or why not (NotNow with a BlockedReason, Busy, HandsFull...).
    public abstract InteractionResult CanUse(IInteractionActor actor);

    /// Applied on the authority after CanUse said Ok.
    public abstract void Use(IInteractionActor actor);

    /// Shown when CanUse says NotNow. Empty means stay quiet.
    public virtual string BlockedReason(IInteractionActor actor) => "";

    /// A player left: let go of anything they had locked.
    public virtual void OnActorLeft(string actorId) { }

    Collider[] _colliders;

    /// Distance from a point to the nearest part of this thing (not its pivot).
    public float DistanceFrom(Vector3 point)
    {
        _colliders ??= GetComponentsInChildren<Collider>();
        float best = Vector3.Distance(point, transform.position);
        foreach (var c in _colliders)
            if (c != null && c.enabled)
                best = Mathf.Min(best, Vector3.Distance(point, c.ClosestPoint(point)));
        return best;
    }
}
