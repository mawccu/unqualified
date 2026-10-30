using System.Linq;
using UnityEngine;

/// <summary>
/// The shared reference shelf: the same help pages as the console, in binders,
/// for anyone who wants to read them away from a screen. Walk away to put them back.
/// </summary>
public class ReferenceShelf : Interactable
{
    PlayerActor _reader;

    public override string Prompt(IInteractionActor actor) => _reader != null ? "Put the binders back" : "Read the reference binders";
    public override InteractionResult CanUse(IInteractionActor actor) => InteractionResult.Ok;

    public override void Use(IInteractionActor actor)
    {
        var hud = GameSession.Instance.Hud;
        if (_reader != null)
        {
            _reader = null;
            hud.ShowDocument(null);
            return;
        }
        _reader = actor as PlayerActor;
        hud.ShowDocument("<size=13><color=#888888>BRIGHTLINE · AGENT REFERENCE</color></size>\n" +
            string.Join("\n\n", CaseLibrary.HelpPages.Select(p => $"<b>{p.title}</b>\n<size=13>{p.body}</size>")));
    }

    void Update()
    {
        if (_reader != null && DistanceFrom(_reader.transform.position) > 2.2f)
        {
            _reader = null;
            GameSession.Instance.Hud.ShowDocument(null);
        }
    }
}

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
