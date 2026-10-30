using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A guest table: where an escorted guest can be seated. Its seats are whatever
/// chairs stand around it right now, so a moved or missing chair changes how
/// many people it takes.
/// </summary>
public class GuestTable : Interactable
{
    [SerializeField] int index;
    [SerializeField] bool nearSpeaker;
    [SerializeField] bool nearStage;

    public int Index => index;
    public bool NearSpeaker => nearSpeaker;
    public bool NearStage => nearStage;
    public string Label => $"table {index + 1}";

    public void Configure(int tableIndex, bool speaker, bool stage)
    {
        index = tableIndex;
        nearSpeaker = speaker;
        nearStage = stage;
    }

    public TableInfo Info => new() { Index = index, NearSpeaker = nearSpeaker, NearStage = nearStage, Seats = FreeChairs(null).Count };

    /// Upright chairs within reach of the table that nobody sits on.
    public List<PickupProp> FreeChairs(ICollection<PickupProp> taken)
    {
        var free = new List<PickupProp>();
        var service = GameSession.Instance != null ? GameSession.Instance.Service : null;
        if (service == null)
            return free;
        foreach (var p in service.Props)
        {
            if (p.Kind != "chair" || !p.gameObject.activeInHierarchy || !string.IsNullOrEmpty(p.HolderId))
                continue;
            Vector3 d = p.transform.position - transform.position;
            d.y = 0f;
            if (d.magnitude > 1.7f || Vector3.Dot(p.transform.up, Vector3.up) < 0.9f)
                continue;
            if (taken != null && taken.Contains(p))
                continue;
            free.Add(p);
        }
        return free;
    }

    static Guest Escorted(IInteractionActor actor)
    {
        foreach (var g in Guest.All)
            if (g.Leader != null && g.Leader.ActorId == actor.ActorId)
                return g;
        return null;
    }

    public override string Prompt(IInteractionActor actor) => $"Seat {Escorted(actor)?.Profile.Name} at {Label}";

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        if (Escorted(actor) == null)
            return InteractionResult.NotNow;
        return FreeChairs(Guest.TakenChairs()).Count > 0 ? InteractionResult.Ok : InteractionResult.NotNow;
    }

    public override string BlockedReason(IInteractionActor actor) =>
        Escorted(actor) != null ? $"No free chair at {Label}." : "";

    public override void Use(IInteractionActor actor) => Escorted(actor)?.SeatAt(this);
}
