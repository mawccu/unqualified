using UnityEngine;

/// <summary>
/// The flat's front door. Answering it lets the landlord in (as far as the
/// threshold); once you are hired it is the way to work. In between it tells
/// you, politely, that you have nowhere to be.
/// </summary>
public class FrontDoor : Interactable
{
    // Swings into the flat (the leaf extends toward -X from the hinge).
    const float OpenAngle = 100f;
    const float Speed = 4f;

    [SerializeField] Transform leaf;

    float _target, _angle;
    Quaternion _closed;

    public void Configure(Transform doorLeaf) => leaf = doorLeaf;

    void Awake() => _closed = leaf.localRotation;

    void Update()
    {
        _angle = Mathf.Lerp(_angle, _target, 1f - Mathf.Exp(-Speed * Time.deltaTime));
        leaf.localRotation = _closed * Quaternion.Euler(0f, _angle, 0f);
    }

    public void Open()
    {
        if (_target != OpenAngle)
            Sound(ProceduralAudio.Creak, 0.7f);
        _target = OpenAngle;
    }

    public void Close()
    {
        if (_target != 0f)
        {
            Sound(ProceduralAudio.Creak, 0.5f);
            Invoke(nameof(Shut), 0.45f);
        }
        _target = 0f;
    }

    void Shut() => Sound(ProceduralAudio.Latch, 0.9f);

    void Sound(AudioClip clip, float volume)
    {
        var source = GetComponent<AudioSource>();
        if (source != null)
            source.PlayOneShot(clip, volume);
    }

    /// Where a right hand goes to work the handle (the room side of the leaf):
    /// palm toward the door, fingers round the knob.
    public Pose HandleGrip()
    {
        var leafPart = leaf.childCount > 0 ? leaf.GetChild(0) : leaf;
        // The knob, in the leaf model's space (modelled at +X 0.8 in Blender, mirrored on import).
        Vector3 knob = leafPart.TransformPoint(new Vector3(-0.8f, 1.0f, 0.06f));
        Vector3 outward = leafPart.forward;           // the room side
        Vector3 towardHinge = leafPart.right;         // -X is the free edge, so +X runs back to the hinge
        Vector3 wrist = knob + outward * 0.05f - towardHinge * 0.075f - Vector3.up * 0.02f;
        return new Pose(wrist, Quaternion.LookRotation(towardHinge, outward));
    }

    static OpeningStage Stage => OpeningDirector.Instance != null ? OpeningDirector.Instance.Stage : OpeningStage.Waking;

    public override string Prompt(IInteractionActor actor) =>
        Stage == OpeningStage.Hired ? $"Leave for your shift ({Jobs.Title(CrewState.Current.Job)})" : "Answer the door";

    public override InteractionResult CanUse(IInteractionActor actor) =>
        Stage == OpeningStage.Knocking || Stage == OpeningStage.Hired ? InteractionResult.Ok : InteractionResult.NotNow;

    public override string BlockedReason(IInteractionActor actor) => Stage switch
    {
        OpeningStage.FindJob => "No job, no reason to go out. Try the computer.",
        OpeningStage.LandlordLeaving => "Give him a second to leave.",
        _ => "",
    };

    public override void Use(IInteractionActor actor)
    {
        var director = OpeningDirector.Instance;
        if (Stage == OpeningStage.Knocking)
            director.OnDoorAnswered(actor);
        else if (Stage == OpeningStage.Hired)
            director.LeaveForWork(actor);
    }
}
