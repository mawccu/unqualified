using System.Collections;
using UnityEngine;

/// <summary>
/// The casing's lid, standing open on its hinge behind the casing while a build
/// is on the mat. Closing it (a hand pushes it over) needs every part in and
/// both screws done; it seals what is inside. Opening it again lets you fix a
/// fault the test found.
/// </summary>
public class CaseLid : Interactable
{
    const float OpenAngle = -110f;

    [SerializeField] AssemblyBench bench;
    [SerializeField] Transform hinge;
    [SerializeField] Transform edge;

    public bool Closed { get; private set; }
    float _angle = OpenAngle;

    public void Configure(AssemblyBench owner, Transform hingePivot, Transform frontEdge)
    {
        bench = owner;
        hinge = hingePivot;
        edge = frontEdge;
    }

    public void Show(bool on)
    {
        gameObject.SetActive(on);
        if (!on)
            SetClosed(false, true);
    }

    public void SetClosed(bool closed, bool instant)
    {
        Closed = closed;
        if (instant)
        {
            _angle = closed ? 0f : OpenAngle;
            hinge.localRotation = Quaternion.Euler(_angle, 0f, 0f);
        }
    }

    void Update()
    {
        float target = Closed ? 0f : OpenAngle;
        _angle = Mathf.MoveTowards(_angle, target, Time.deltaTime * 320f);
        hinge.localRotation = Quaternion.Euler(_angle, 0f, 0f);
    }

    public override string Prompt(IInteractionActor actor) => Closed ? "Open the lid" : "Close the lid";

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        if (bench == null || !bench.HasBuild)
            return InteractionResult.NotNow;
        if (!WorkshopLedger.CanEdit(CrewState.Current, actor.ActorId))
            return InteractionResult.Busy;
        if (bench.Testing)
            return InteractionResult.Busy;
        return Closed || string.IsNullOrEmpty(bench.WhatStopsClosing()) ? InteractionResult.Ok : InteractionResult.NotNow;
    }

    public override string BlockedReason(IInteractionActor actor) => Closed ? "" : bench != null ? bench.WhatStopsClosing() : "";

    public override void Use(IInteractionActor actor)
    {
        bool closing = !Closed;
        if (actor is PlayerActor player && player == GameSession.Instance.Player)
            StartCoroutine(Push(player));
        Closed = closing;
        var source = bench.Audio;
        if (source != null)
            source.PlayOneShot(closing ? ProceduralAudio.Latch : ProceduralAudio.Pickup, closing ? 0.5f : 0.6f);
        bench.OnLidChanged(actor);
    }

    /// The right hand goes to the lid's edge and follows it over.
    IEnumerator Push(PlayerActor player)
    {
        player.Arms.SetOverride(FirstPersonArms.Side.Right, () => new Pose(edge.position + Vector3.up * 0.02f,
            Quaternion.LookRotation(Vector3.ProjectOnPlane(edge.position - player.EyePosition, Vector3.up).normalized + Vector3.down * 0.6f, Vector3.up)), Grip.Open, 0.2f);
        yield return new WaitForSeconds(0.55f);
        player.Arms.ClearOverride(FirstPersonArms.Side.Right, 0.3f);
    }
}
