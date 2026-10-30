using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A support agent's desk: chair, screen, keyboard and phone. The player sits at
/// their own desk to work the console; a coworker's desk is theirs. Hands show
/// the work: both on the keyboard while typing, one lifts the handset to the ear
/// when a call is answered (and puts it back), a finger presses hold, mute and
/// transfer on the phone. Standing up keeps the call; the phone keeps ringing.
/// </summary>
public class SupportDesk : Interactable
{
    [SerializeField] Transform seat;
    [SerializeField] Transform screenView;
    [SerializeField] Transform keyboard;
    [SerializeField] Collider chair;
    [SerializeField] DeskPhone phone;
    [SerializeField] SupportConsole console;
    [SerializeField] TextMesh nameplate;

    public DeskPhone Phone => phone;
    public string WorkerId { get; private set; }
    public string WorkerName { get; private set; }
    public bool ForPlayer { get; private set; }
    public bool Occupied => _local != null;
    public Transform Seat => seat;
    public Transform Keyboard => keyboard;

    PlayerActor _local;
    readonly float[] _tap = new float[2];
    int _nextHand;
    bool _onPhone;

    public void Configure(Transform seatSpot, Transform view, Transform keys, Collider chairBody, DeskPhone deskPhone, SupportConsole screen, TextMesh plate)
    {
        seat = seatSpot;
        screenView = view;
        keyboard = keys;
        chair = chairBody;
        phone = deskPhone;
        console = screen;
        nameplate = plate;
    }

    public void Assign(string workerId, string name, bool player)
    {
        WorkerId = workerId;
        WorkerName = name;
        ForPlayer = player;
        if (nameplate != null)
            nameplate.text = player ? $"{name.ToUpperInvariant()}  (YOU)" : name.ToUpperInvariant();
    }

    static CallCenterShift Shift => CallCenterShift.Instance;

    public override string Prompt(IInteractionActor actor)
    {
        var call = Shift != null ? Shift.CallOf(WorkerId) : null;
        return call != null && call.State == CallState.Ringing ? "Sit down and answer the phone" : "Sit at your desk";
    }

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        if (!ForPlayer)
            return InteractionResult.NotNow;
        if (_local != null)
            return InteractionResult.Busy;
        if (!string.IsNullOrEmpty(actor.HeldPropId))
            return InteractionResult.HandsFull;
        return InteractionResult.Ok;
    }

    public override string BlockedReason(IInteractionActor actor) => ForPlayer ? "" : $"{WorkerName}'s desk.";

    public override void Use(IInteractionActor actor)
    {
        if (actor is PlayerActor player && player == GameSession.Instance.Player)
            StartCoroutine(SitDown(player));
    }

    // Hands ------------------------------------------------------------------------------

    Pose KeyboardHand(float side, int index)
    {
        Vector3 toScreen = -keyboard.forward;
        Vector3 right = Vector3.Cross(Vector3.up, toScreen).normalized;
        Vector3 wrist = keyboard.position + right * (side * 0.09f) + keyboard.forward * 0.06f
            + Vector3.up * (0.05f - _tap[index] * 0.012f);
        return new Pose(wrist, Quaternion.LookRotation(toScreen - Vector3.up * 0.35f, Vector3.up));
    }

    /// The handset at the left ear of whoever sits here.
    Pose EarPose()
    {
        var cam = _local != null ? _local.Camera.transform : null;
        Vector3 head = cam != null ? cam.position : seat.position + Vector3.up * 1.15f;
        Vector3 forward = cam != null ? cam.forward : seat.forward;
        Vector3 right = cam != null ? cam.right : seat.right;
        // Close enough to the ear to read as "on the phone", just in view at the left edge.
        Vector3 at = head - right * 0.24f + forward * 0.2f - Vector3.up * 0.16f;
        // Earpiece (-X of the handset) up by the ear, mouthpiece down toward the chin.
        return new Pose(at, Quaternion.LookRotation(forward, right) * Quaternion.Euler(0f, 0f, -70f));
    }

    Pose HandOnHandset() => new(phone.Handset.position - phone.Handset.up * 0.02f, Quaternion.LookRotation(phone.Handset.right, phone.Handset.up));

    Pose ButtonPose(int index, float press)
    {
        var b = phone.Button(index);
        var p = b.position + Vector3.up * (0.05f - press * 0.035f);
        return new Pose(p, Quaternion.LookRotation(Vector3.down + seat.forward * 0.6f, seat.forward));
    }

    void Update()
    {
        if (_local == null)
            return;
        for (int i = 0; i < 2; i++)
            _tap[i] = Mathf.MoveTowards(_tap[i], 0f, Time.deltaTime * 9f);
        var keys = UnityEngine.InputSystem.Keyboard.current;
        bool typed = keys != null && keys.anyKey.wasPressedThisFrame && !keys.escapeKey.wasPressedThisFrame;
        bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (typed || clicked)
            Tap(_onPhone ? 0 : _nextHand++ % 2);
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
        if (side == FirstPersonArms.Side.Left && _onPhone)
            yield break;
        player.Arms.SetOverrideGrip(side, Grip.Push);
        yield return new WaitForSeconds(0.12f);
        if (_local == player && !(side == FirstPersonArms.Side.Left && _onPhone))
            player.Arms.SetOverrideGrip(side, Grip.Keyboard);
    }

    // Sitting -----------------------------------------------------------------------------

    IEnumerator SitDown(PlayerActor player)
    {
        _local = player;
        player.Lock();
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
            if (console.HandleEscape())
                return true;
            StandUp();
            return true;
        };
        var hud = GameSession.Instance.Hud;
        hud.SetCrosshairVisible(false);
        hud.SetPrompt("");
        GameSession.Instance.RefreshCursor();
        Shift?.PlayerSatDown();
        yield return new WaitForSeconds(0.7f);
        if (_local != player)
            yield break;
        console.Open(this);
        var call = Shift?.CallOf(WorkerId);
        if (call != null && call.State != CallState.Ringing)
            LiftHandset(0.3f);
    }

    public void StandUp()
    {
        var player = _local;
        if (player == null)
            return;
        _local = null;
        console.Close();
        _onPhone = false;
        player.ClearCameraOverride(0.5f);
        player.Arms.ClearAll(0.4f);
        player.Teleport(seat.position - seat.forward * 0.55f);
        if (chair != null)
            chair.enabled = true;
        player.WantsCursor = false;
        player.EscapeHandler = null;
        player.Avatar.SetIdlePose(null);
        player.Avatar.SitOn(0f);
        player.Unlock();
        GameSession.Instance.Hud.SetCrosshairVisible(true);
        GameSession.Instance.RefreshCursor();
        // A call in progress stays on the line (on the desk, not in your hand).
        var call = Shift?.CallOf(WorkerId);
        if (call == null || call.State == CallState.Ringing)
            phone.HangUp();
        else
            phone.PickUp(DeskRest);
    }

    Pose DeskRest() => new(keyboard.position + keyboard.right * 0.32f + Vector3.up * 0.03f, keyboard.rotation * Quaternion.Euler(0f, 90f, 0f));

    // The phone -----------------------------------------------------------------------------

    /// The call was answered at this desk: lift the handset.
    public void Answered(CallCase call)
    {
        if (_local != null)
            LiftHandset(0f);
    }

    void LiftHandset(float delay) => StartCoroutine(LiftRoutine(delay));

    IEnumerator LiftRoutine(float delay)
    {
        var player = _local;
        if (player == null)
            yield break;
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        _onPhone = true;
        // Reach for the handset, then bring it up to the ear.
        player.Arms.SetOverride(FirstPersonArms.Side.Left, HandOnHandset, Grip.Wrap, 0.25f);
        yield return new WaitForSeconds(0.28f);
        if (_local != player || !_onPhone)
            yield break;
        phone.PickUp(EarPose);
        player.Arms.SetOverride(FirstPersonArms.Side.Left, HandOnHandset, Grip.Wrap, 0.05f);
    }

    /// The call ended or moved on: the handset goes back in the cradle.
    public void HungUp()
    {
        if (!_onPhone && !phone.Lifted)
            return;
        StartCoroutine(HangUpRoutine());
    }

    IEnumerator HangUpRoutine()
    {
        _onPhone = false;
        phone.HangUp();
        yield return new WaitForSeconds(0.32f);
        if (_local != null && !_onPhone)
            _local.Arms.SetOverride(FirstPersonArms.Side.Left, () => KeyboardHand(-1f, 1), Grip.Keyboard, 0.35f);
    }

    /// A finger on hold (0), mute (1) or transfer (2).
    public void PressButton(int index)
    {
        phone.Press(index);
        if (_local != null)
            StartCoroutine(PressRoutine(_local, index));
    }

    IEnumerator PressRoutine(PlayerActor player, int index)
    {
        float t = 0f;
        player.Arms.SetOverride(FirstPersonArms.Side.Right, () => ButtonPose(index, Mathf.Clamp01(Mathf.Sin(t * Mathf.PI))), Grip.Point, 0.15f);
        for (; t < 1f; t += Time.deltaTime / 0.35f)
            yield return null;
        if (_local == player)
            player.Arms.SetOverride(FirstPersonArms.Side.Right, () => KeyboardHand(1f, 0), Grip.Keyboard, 0.2f);
    }

    public override void OnActorLeft(string actorId)
    {
        if (_local != null && _local.ActorId == actorId)
            StandUp();
    }
}
