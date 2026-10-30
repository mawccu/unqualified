using UnityEngine;

/// <summary>A wall switch for a room's (real-time) light, with a click.</summary>
public class LightSwitch : Interactable
{
    [SerializeField] Light target;
    [SerializeField] Renderer fitting;
    AudioSource _audio;

    public void Configure(Light light, Renderer glow)
    {
        target = light;
        fitting = glow;
    }

    void Awake()
    {
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 1f;
        _audio.playOnAwake = false;
    }

    public bool IsOn => target != null && target.enabled;

    public override string Prompt(IInteractionActor actor) => IsOn ? "Switch the light off" : "Switch the light on";
    public override InteractionResult CanUse(IInteractionActor actor) => target != null ? InteractionResult.Ok : InteractionResult.NoTarget;

    public override void Use(IInteractionActor actor)
    {
        target.enabled = !target.enabled;
        if (fitting != null)
            fitting.enabled = target.enabled;
        _audio.pitch = target.enabled ? 1.3f : 1.1f;
        _audio.PlayOneShot(PropPhysics.ImpactClip(PropCategory.Small), 0.4f);
    }
}
