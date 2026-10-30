using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] Transform keyboard;
    [SerializeField] Collider chair;

    string _user = "";
    PlayerActor _local;
    readonly float[] _tap = new float[2];
    int _nextHand;

    public string User => _user;

    public void Configure(Transform seatSpot, Transform view, ComputerScreen computer, Transform keys, Collider chairBody)
    {
        chair = chairBody;
        seat = seatSpot;
        screenView = view;
        screen = computer;
        keyboard = keys;
    }

    /// A hand resting on the keyboard (side +1 right), dipping when it taps a key.
    Pose KeyboardHand(float side, int index)
    {
        Vector3 toScreen = -keyboard.forward;
        Vector3 right = Vector3.Cross(Vector3.up, toScreen).normalized;
        Vector3 wrist = keyboard.position + right * (side * 0.09f) + keyboard.forward * 0.06f
            + Vector3.up * (0.05f - _tap[index] * 0.012f);
        return new Pose(wrist, Quaternion.LookRotation(toScreen - Vector3.up * 0.35f, Vector3.up));
    }

    void Update()
    {
        if (_local == null)
            return;
        for (int i = 0; i < 2; i++)
            _tap[i] = Mathf.MoveTowards(_tap[i], 0f, Time.deltaTime * 9f);
        // Typing and clicking show on the hands.
        bool pressed = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (pressed)
            Tap(_nextHand++ % 2);
    }

    AudioSource _keys;

    void Tap(int hand)
    {
        _tap[hand] = 1f;
        if (_keys == null)
        {
            _keys = keyboard.gameObject.AddComponent<AudioSource>();
            _keys.spatialBlend = 1f;
            _keys.maxDistance = 6f;
            _keys.playOnAwake = false;
        }
        _keys.pitch = Random.Range(0.85f, 1.2f);
        _keys.PlayOneShot(ProceduralAudio.Key, 0.6f);
        if (_local != null)
            StartCoroutine(TapGrip(_local, hand == 0 ? FirstPersonArms.Side.Right : FirstPersonArms.Side.Left));
    }

    IEnumerator TapGrip(PlayerActor player, FirstPersonArms.Side side)
    {
        player.Arms.SetOverrideGrip(side, Grip.Push);
        yield return new WaitForSeconds(0.12f);
        if (_local == player)
            player.Arms.SetOverrideGrip(side, Grip.Keyboard);
    }

    /// The finger that sends the application.
    public void PressEnter()
    {
        _nextHand = 0;
        Tap(0);
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
        // Sitting in the chair: its body steps aside while you are in it.
        if (chair != null)
            chair.enabled = false;
        player.Teleport(seat.position);
        player.SetLook(seat.eulerAngles.y, 12f);
        player.Avatar.SetIdlePose(WorkerAvatar.SitType);
        player.Avatar.SitOn(WorkerAvatar.DeskChairSeat - seat.position.y);
        player.SetCameraOverride(new Pose(screenView.position, screenView.rotation), 0.7f);
        player.Arms.SetOverride(FirstPersonArms.Side.Right, () => KeyboardHand(1f, 0), Grip.Keyboard, 0.6f);
        player.Arms.SetOverride(FirstPersonArms.Side.Left, () => KeyboardHand(-1f, 1), Grip.Keyboard, 0.7f);
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
        player.Arms.ClearAll(0.4f);
        // Stand up just behind the chair, then it is solid again.
        player.Teleport(seat.position - seat.forward * 0.5f);
        if (chair != null)
            chair.enabled = true;
        player.WantsCursor = false;
        player.EscapeHandler = null;
        player.Avatar.SetIdlePose(null);
        player.Avatar.SitOn(0f);
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
