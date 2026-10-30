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

    public void Open() => _target = OpenAngle;
    public void Close() => _target = 0f;

    static OpeningStage Stage => OpeningDirector.Instance != null ? OpeningDirector.Instance.Stage : OpeningStage.Waking;

    public override string Prompt(IInteractionActor actor) =>
        Stage == OpeningStage.Hired ? "Leave for the wedding shift" : "Answer the door";

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
            director.LeaveForWork();
    }
}
