using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A pedal bin. Open the lid and drop clutter in: it is gone. Anything the job
/// needs (cake, signs, cables, tools, the bill) is never lost: it is fished back
/// out and put back where it started, with a note saying so.
/// </summary>
public class PedalBin : Interactable
{
    static readonly HashSet<string> Disposable = new() { "clutter", "popper_used", "mug" };

    [SerializeField] Transform lid;
    bool _open;
    float _angle;
    AudioSource _audio;

    public void Configure(Transform lidTransform) => lid = lidTransform;

    void Awake()
    {
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 1f;
        _audio.playOnAwake = false;
        // The opening, as a trigger that catches what is dropped in.
        var mouth = gameObject.AddComponent<BoxCollider>();
        mouth.isTrigger = true;
        mouth.center = new Vector3(0f, 0.36f, 0f);
        mouth.size = new Vector3(0.28f, 0.14f, 0.28f);
    }

    public override string Prompt(IInteractionActor actor) => _open ? "Close the bin" : "Open the bin";
    public override InteractionResult CanUse(IInteractionActor actor) => InteractionResult.Ok;

    public override void Use(IInteractionActor actor)
    {
        _open = !_open;
        _audio.PlayOneShot(PropPhysics.ImpactClip(PropCategory.Small), 0.5f);
    }

    void Update()
    {
        _angle = Mathf.MoveTowards(_angle, _open ? -75f : 0f, Time.deltaTime * 300f);
        if (lid != null)
            lid.localRotation = Quaternion.Euler(_angle, 0f, 0f);
    }

    void OnTriggerStay(Collider other)
    {
        if (!_open)
            return;
        var prop = other.GetComponentInParent<PickupProp>();
        if (prop == null || !string.IsNullOrEmpty(prop.HolderId) || !string.IsNullOrEmpty(prop.SocketId))
            return;
        var hud = GameSession.Instance != null ? GameSession.Instance.Hud : null;
        // A ruined cake layer is rubbish too (the storage room has spares).
        if (Disposable.Contains(prop.Kind) || prop.State == PickupProp.Condition.Ruined)
        {
            prop.gameObject.SetActive(false);
            hud?.ShowToast($"Binned {prop.DisplayName}.");
        }
        else
        {
            prop.ResetToStart();
            hud?.ShowToast($"Someone fished {prop.DisplayName} out of the bin and put it back.");
        }
    }
}
