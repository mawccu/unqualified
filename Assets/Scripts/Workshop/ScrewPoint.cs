using UnityEngine;

/// <summary>
/// One of the two screws that hold the board in the casing. Hold E with the
/// screwdriver in hand: its tip goes to the screw head and twists, the head
/// turns and sinks home with a ratchet. It only exists while there is a board
/// to fasten, and only one person works on a build at a time.
/// </summary>
public class ScrewPoint : Interactable
{
    const float Sink = 0.004f;

    [SerializeField] AssemblyBench bench;
    [SerializeField] Transform head;

    public bool Fastened { get; private set; }
    float _turn;
    Vector3 _raised;

    public void Configure(AssemblyBench owner, Transform screwHead)
    {
        bench = owner;
        head = screwHead;
    }

    void Awake() => _raised = head.localPosition;

    public void SetFastened(bool on)
    {
        Fastened = on;
        head.localPosition = _raised - (on ? Vector3.up * Sink : Vector3.zero);
        head.localRotation = Quaternion.Euler(0f, on ? 270f : 0f, 0f);
    }

    public void Show(bool on)
    {
        gameObject.SetActive(on);
        if (!on)
            SetFastened(false);
    }

    public override float HoldSeconds => 1.0f;

    public override Vector3? WorkPoint => head.position + Vector3.up * 0.003f;

    public override string Prompt(IInteractionActor actor) => "Fasten the screw";

    static bool HoldsScrewdriver(IInteractionActor actor)
    {
        var held = GameSession.Instance != null ? GameSession.Instance.Service.GetProp(actor.HeldPropId) : null;
        return held != null && held.Kind == "screwdriver";
    }

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        if (Fastened || bench == null || !bench.ScrewsWanted)
            return InteractionResult.NotNow;
        if (!WorkshopLedger.CanEdit(CrewState.Current, actor.ActorId))
            return InteractionResult.Busy;
        return HoldsScrewdriver(actor) ? InteractionResult.Ok : InteractionResult.NotNow;
    }

    public override string BlockedReason(IInteractionActor actor)
    {
        if (Fastened)
            return "";
        if (bench != null && bench.LidClosed)
            return "Open the lid first.";
        return HoldsScrewdriver(actor) ? "" : "You need the screwdriver (on the bench).";
    }

    void Update()
    {
        // While someone is driving it, the head turns.
        var player = GameSession.Instance != null ? GameSession.Instance.Player : null;
        if (!Fastened && player != null && player.HoldProgress > 0f && player.HoldProgress < 1f && WorkPointBeingUsed(player))
        {
            _turn += Time.deltaTime * 540f;
            head.localRotation = Quaternion.Euler(0f, _turn, 0f);
            head.localPosition = _raised - Vector3.up * (Sink * player.HoldProgress);
        }
    }

    bool WorkPointBeingUsed(PlayerActor player) => player.HoldTarget == this;

    public override void Use(IInteractionActor actor)
    {
        SetFastened(true);
        var source = bench.Audio;
        if (source != null)
            source.PlayOneShot(ProceduralAudio.Ratchet, 0.7f);
        bench.OnScrewFastened(actor);
    }
}
