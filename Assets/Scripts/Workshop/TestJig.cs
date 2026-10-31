using UnityEngine;

/// <summary>
/// The test jig beside the mat: a little screen (charge, connection, output) and
/// a big red button. Pressing it with the lid closed runs a short test; a fault
/// names the port and lights it red, a pass turns the build into a gadget.
/// </summary>
public class TestJig : Interactable
{
    [SerializeField] AssemblyBench bench;
    [SerializeField] Transform button;
    [SerializeField] Transform screenSpot;
    TextMesh screen;

    public void Configure(AssemblyBench owner, Transform redButton, Transform display)
    {
        bench = owner;
        button = redButton;
        screenSpot = display;
    }

    void Awake() => screen = WorkshopArt.Label(screenSpot, Vector3.zero, Quaternion.identity, 0.013f, Color.white);

    public void Show(string text, Color colour)
    {
        if (screen == null)
            return;
        screen.text = text;
        screen.color = colour;
    }

    public override string Prompt(IInteractionActor actor) => "Run the test";

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        if (bench == null || !bench.HasBuild || bench.Testing)
            return InteractionResult.NotNow;
        if (!WorkshopLedger.CanEdit(CrewState.Current, actor.ActorId))
            return InteractionResult.Busy;
        return bench.LidClosed ? InteractionResult.Ok : InteractionResult.NotNow;
    }

    public override string BlockedReason(IInteractionActor actor) =>
        bench == null || !bench.HasBuild ? "Nothing on the mat to test." : bench.Testing ? "" : "Close the lid before testing.";

    public override void Use(IInteractionActor actor)
    {
        if (actor is PlayerActor player && player == GameSession.Instance.Player)
            StartCoroutine(Press(player));
        bench.RunTest(actor);
    }

    System.Collections.IEnumerator Press(PlayerActor player)
    {
        Vector3 top = button.position + Vector3.up * 0.03f;
        player.Arms.SetOverride(FirstPersonArms.Side.Right, () => new Pose(top,
            Quaternion.LookRotation(Vector3.ProjectOnPlane(top - player.EyePosition, Vector3.up).normalized + Vector3.down * 1.2f, Vector3.up)), Grip.Push, 0.18f);
        yield return new WaitForSeconds(0.18f);
        var rest = button.localPosition;
        button.localPosition = rest - Vector3.up * 0.006f;
        yield return new WaitForSeconds(0.2f);
        button.localPosition = rest;
        player.Arms.ClearOverride(FirstPersonArms.Side.Right, 0.3f);
    }
}
