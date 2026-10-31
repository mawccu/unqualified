/// <summary>The blueprints pinned over the workbench: what you can build and the build in progress.</summary>
public class BlueprintBoard : Interactable
{
    public override string Prompt(IInteractionActor actor) =>
        AssemblyBench.Instance != null && AssemblyBench.Instance.HasBuild ? "Check the blueprint" : "Look at the blueprints";

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        if (WorkshopPanel.Instance == null || WorkshopPanel.Instance.IsOpen)
            return InteractionResult.NotNow;
        var crew = CrewState.Current;
        if (crew.build.Active && !WorkshopLedger.CanEdit(crew, actor.ActorId))
            return InteractionResult.Busy;
        return InteractionResult.Ok;
    }

    public override void Use(IInteractionActor actor)
    {
        WorkshopLedger.ClaimBuild(CrewState.Current, actor.ActorId);
        if (actor is PlayerActor player && player == GameSession.Instance.Player)
            WorkshopPanel.Instance.OpenBench(player);
    }

    public override void OnActorLeft(string actorId) => AssemblyBench.Instance?.OnActorLeft(actorId);
}
