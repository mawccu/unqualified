using System.Collections;
using UnityEngine;

/// <summary>
/// The flat's sliding window. The front sash slides along its track over the
/// fixed one: a hand takes the pull handle, the sash runs with it, the hand
/// lets go. Open, the alley's noise comes in louder. It cannot close on a thing
/// left on the sill, and a press while it is moving does nothing. The opening
/// is half the window and the grille outside keeps everyone in.
/// </summary>
public class ApartmentWindow : Interactable
{
    const float Seconds = 0.65f;
    const float ClosedVolume = 0.04f, OpenVolume = 0.4f;

    [SerializeField] Transform sash;
    [SerializeField] Transform handle;
    [SerializeField] Vector3 openOffset = new(0.56f, 0f, 0f);
    [SerializeField] AudioSource street;
    [SerializeField] AudioSource sounds;
    [SerializeField] BoxCollider opening;

    public bool IsOpen { get; private set; }
    public bool Moving => !Mathf.Approximately(_t, IsOpen ? 1f : 0f);
    /// 0 shut to 1 fully open.
    public float Openness => _t;

    Vector3 _closed;
    float _t;

    public void Configure(Transform slidingSash, Transform pullHandle, Vector3 offset, AudioSource outside, AudioSource effects, BoxCollider gap)
    {
        sash = slidingSash;
        handle = pullHandle;
        openOffset = offset;
        street = outside;
        sounds = effects;
        opening = gap;
    }

    void Awake()
    {
        _closed = sash.localPosition;
        if (street != null)
        {
            street.clip = ProceduralAudio.Street;
            street.loop = true;
            street.volume = ClosedVolume;
            street.Play();
        }
    }

    void Update()
    {
        float target = IsOpen ? 1f : 0f;
        _t = Mathf.MoveTowards(_t, target, Time.deltaTime / Seconds);
        float eased = _t * _t * (3f - 2f * _t);
        sash.localPosition = _closed + openOffset * eased;
        if (street != null)
            street.volume = Mathf.Lerp(ClosedVolume, OpenVolume, eased);
    }

    /// Something sitting where the sash closes (a mug on the sill).
    bool Obstructed()
    {
        if (opening == null)
            return false;
        var hits = Physics.OverlapBox(opening.bounds.center, opening.bounds.extents, opening.transform.rotation, 1 << GameLayers.Prop, QueryTriggerInteraction.Ignore);
        return hits.Length > 0;
    }

    public override string Prompt(IInteractionActor actor) => IsOpen ? "Close the window" : "Open the window";

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        if (Moving)
            return InteractionResult.NotNow;   // quietly: it is only sliding
        if (IsOpen && Obstructed())
            return InteractionResult.NotNow;
        return InteractionResult.Ok;
    }

    public override string BlockedReason(IInteractionActor actor) => IsOpen && Obstructed() ? "Something on the sill is in the way." : "";

    public override void Use(IInteractionActor actor)
    {
        bool opening = !IsOpen;
        IsOpen = opening;
        if (sounds != null)
            sounds.PlayOneShot(ProceduralAudio.Slide, 0.7f);
        if (actor is PlayerActor player && GameSession.Instance != null && player == GameSession.Instance.Player)
            StartCoroutine(Pull(player, opening));
        else
            StartCoroutine(Thunk(opening));
    }

    /// The hand takes the handle, rides with the sash, and lets go.
    IEnumerator Pull(PlayerActor player, bool opening)
    {
        player.Arms.SetOverride(FirstPersonArms.Side.Right, HandOnHandle(player), Grip.Wrap, 0.18f);
        yield return Thunk(opening);
        player.Arms.SetOverrideGrip(FirstPersonArms.Side.Right, Grip.Open);
        player.Arms.ClearOverride(FirstPersonArms.Side.Right, 0.3f);
    }

    System.Func<Pose> HandOnHandle(PlayerActor player) => () =>
    {
        Vector3 at = handle.position;
        Vector3 toWindow = Vector3.ProjectOnPlane(at - player.EyePosition, Vector3.up).normalized;
        return new Pose(at - toWindow * 0.06f, Quaternion.LookRotation(toWindow, Vector3.up));
    };

    IEnumerator Thunk(bool opening)
    {
        yield return new WaitForSeconds(Seconds);
        if (sounds != null)
            sounds.PlayOneShot(ProceduralAudio.Latch, opening ? 0.35f : 0.6f);
    }
}
