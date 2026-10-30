using UnityEngine;

/// <summary>Handing the coordinator something she asked for (the rush-job coffee).</summary>
public class CoordinatorHandoff : Interactable
{
    static WeddingShift Shift => Object.FindFirstObjectByType<WeddingShift>();

    static PickupProp FullMug(IInteractionActor actor)
    {
        var held = GameSession.Instance.Service.GetProp(actor.HeldPropId);
        var mug = held != null ? held.GetComponent<Mug>() : null;
        return mug != null && mug.Full ? held : null;
    }

    public override string Prompt(IInteractionActor actor) => "Hand over the coffee";

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        var shift = Shift;
        return shift != null && shift.CoffeeWanted && FullMug(actor) != null ? InteractionResult.Ok : InteractionResult.NotNow;
    }

    public override void Use(IInteractionActor actor)
    {
        var mug = FullMug(actor);
        if (mug == null)
            return;
        // The mug leaves the player's hands for hers (it is simply put away).
        actor.HeldPropId = "";
        mug.Release(Vector3.zero);
        mug.gameObject.SetActive(false);
        Shift.DeliverCoffee(mug);
    }
}
