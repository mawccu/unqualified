using UnityEngine;

/// <summary>The kitchen coffee machine: hold a mug under it and it fills, with a gurgle.</summary>
public class CoffeeMachine : Interactable
{
    AudioSource _audio;

    void Awake()
    {
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 1f;
        _audio.playOnAwake = false;
    }

    static Mug HeldMug(IInteractionActor actor)
    {
        var held = GameSession.Instance.Service.GetProp(actor.HeldPropId);
        return held != null ? held.GetComponent<Mug>() : null;
    }

    public override string Prompt(IInteractionActor actor) => "Pour a coffee";

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        var mug = HeldMug(actor);
        return mug != null && !mug.Full ? InteractionResult.Ok : InteractionResult.NotNow;
    }

    public override string BlockedReason(IInteractionActor actor)
    {
        var mug = HeldMug(actor);
        return mug == null ? "Coffee machine. Bring a mug." : "That mug is already full.";
    }

    public override void Use(IInteractionActor actor)
    {
        HeldMug(actor).Fill();
        _audio.pitch = 0.6f;
        _audio.PlayOneShot(Tones.Splash, 0.6f);
    }
}
