using System.Collections;
using UnityEngine;

/// <summary>
/// The battered desk and its computer. One person at a time: sitting down locks
/// it, getting up (or leaving the game) frees it, and the CV draft survives
/// either way. Sitting with the bill in hand puts it down beside the keyboard.
/// </summary>
public class ComputerDesk : Interactable
{
    [SerializeField] Transform seat;
    [SerializeField] Transform screenView;
    [SerializeField] ComputerScreen screen;

    string _user = "";
    PlayerActor _local;

    public string User => _user;

    public void Configure(Transform seatSpot, Transform view, ComputerScreen computer)
    {
        seat = seatSpot;
        screenView = view;
        screen = computer;
    }

    static OpeningDirector Director => OpeningDirector.Instance;

    public override string Prompt(IInteractionActor actor) => "Use the computer";

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        if (Director == null || Director.Stage < OpeningStage.LandlordLeaving)
            return InteractionResult.NotNow;
        if (!string.IsNullOrEmpty(_user) && _user != actor.ActorId)
            return InteractionResult.Busy;
        var held = GameSession.Instance.Service.GetProp(actor.HeldPropId);
        if (held != null && held != Director.Bill)
            return InteractionResult.HandsFull;
        return InteractionResult.Ok;
    }

    public override string BlockedReason(IInteractionActor actor) =>
        Director != null && Director.Stage < OpeningStage.LandlordLeaving ? "The computer can wait. Someone is at the door." : "";

    public override void Use(IInteractionActor actor)
    {
        _user = actor.ActorId;
        var service = GameSession.Instance.Service;
        if (service.GetProp(actor.HeldPropId) == Director.Bill)
            service.PlaceHeldOn(actor, Director.BillSpot);
        if (actor is PlayerActor player && player == GameSession.Instance.Player)
            StartCoroutine(SitDown(player));
    }

    IEnumerator SitDown(PlayerActor player)
    {
        _local = player;
        player.Lock();
        player.Teleport(seat.position);
        player.SetLook(seat.eulerAngles.y, 12f);
        player.Avatar.SetIdlePose(WorkerAvatar.SitType);
        player.SetCameraOverride(new Pose(screenView.position, screenView.rotation), 0.7f);
        player.WantsCursor = true;
        player.EscapeHandler = () =>
        {
            StandUp();
            return true;
        };
        var hud = GameSession.Instance.Hud;
        hud.SetCrosshairVisible(false);
        hud.SetPrompt("");
        GameSession.Instance.RefreshCursor();
        yield return new WaitForSeconds(0.7f);
        if (_local == player)
            screen.Open(this);
    }

    /// Get up from the desk and give the controls back.
    public void StandUp()
    {
        var player = _local;
        if (player == null)
            return;
        _local = null;
        _user = "";
        screen.Close();
        player.ClearCameraOverride(0.5f);
        player.WantsCursor = false;
        player.EscapeHandler = null;
        player.Avatar.SetIdlePose(null);
        player.Unlock();
        GameSession.Instance.Hud.SetCrosshairVisible(true);
        GameSession.Instance.RefreshCursor();
    }

    public override void OnActorLeft(string actorId)
    {
        if (_user == actorId)
            _user = "";
    }
}
