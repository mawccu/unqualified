using UnityEngine;

/// <summary>The office printer: prints the help pages as a sheet you can carry about
/// (the sheets wait in the paper tray until printed).</summary>
public class OfficePrinter : Interactable
{
    [SerializeField] PickupProp[] sheets;
    [SerializeField] Transform tray;
    AudioSource _audio;
    int _next;

    public void Configure(PickupProp[] paper, Transform output)
    {
        sheets = paper;
        tray = output;
    }

    void Awake()
    {
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 1f;
        _audio.playOnAwake = false;
    }

    public override string Prompt(IInteractionActor actor) => "Print the help pages";

    public override InteractionResult CanUse(IInteractionActor actor) => _next < sheets.Length ? InteractionResult.Ok : InteractionResult.NotNow;

    public override string BlockedReason(IInteractionActor actor) => "Out of paper. Of course.";

    public override void Use(IInteractionActor actor)
    {
        var sheet = sheets[_next++];
        // From the paper stack to the output tray.
        var body = sheet.GetComponent<Rigidbody>();
        if (body != null)
            body.position = tray.position + Vector3.up * 0.02f * _next;
        sheet.transform.SetPositionAndRotation(tray.position + Vector3.up * 0.02f * _next, tray.rotation);
        _audio.pitch = 0.5f;
        _audio.PlayOneShot(Tones.Murmur, 0.5f);
        GameSession.Instance.Hud.ShowToast("The printer wheezes out a page.");
    }
}
