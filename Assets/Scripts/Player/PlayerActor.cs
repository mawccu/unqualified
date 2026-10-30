using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One player in the room. The local player reads input, shows what the
/// crosshair can do, and sends every world change through Submit. Moving and
/// looking happen every rendered frame (CharacterController, no physics step in
/// between), so the view is as smooth as the screen allows.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(-50)]
public class PlayerActor : MonoBehaviour, IInteractionActor
{
    const float WalkSpeed = 4f;
    const float SprintSpeed = 6f;
    const float CrouchSpeed = 2f;
    const float StandHeight = 1.75f;
    const float CrouchHeight = 1.15f;
    const float CrouchEye = 1.0f;
    const float StepLength = 0.72f;
    const float Acceleration = 12f;
    const float Gravity = 20f;
    public const float EyeHeight = 1.55f;
    /// The crosshair looks this far; the hands only reach InteractionService.MaxReach.
    const float AimReach = 2.6f;
    const float BaseLookDegreesPerPixel = 0.126f;
    const float MaxPitch = 83f;

    public string ActorId { get; private set; } = "peer_1";
    public int Slot { get; private set; }
    public string HeldPropId { get; set; } = "";
    public Transform HoldPoint { get; private set; }
    public Vector3 EyePosition => _head.position;
    public Vector3 DropVelocity => _velocity * 0.5f;
    /// Off while the pause menu is open (the session sets it).
    public bool ControlsEnabled { get; set; } = true;
    /// Off while something else drives the player (waking up, sitting at the desk).
    public bool CanControl => ControlsEnabled && _locks == 0;
    /// True while a screen interface wants the mouse (the computer).
    public bool WantsCursor { get; set; }
    /// Esc goes here first while set (closing the computer instead of opening the menu).
    public Func<bool> EscapeHandler;
    public PickupProp HeldProp => _service?.GetProp(HeldPropId);
    public Camera Camera { get; private set; }
    public WorkerAvatar Avatar { get; private set; }
    public FirstPersonArms Arms { get; private set; }
    public Vector3 Velocity => _body != null ? _body.velocity : Vector3.zero;
    public bool Crouching => _crouch > 0.5f;

    /// Set by the session: sends a request onward (applied here, or sent to the host).
    public Action<InteractionRequest> Submit;
    public event Action<string> PromptChanged;
    public event Action<string> Feedback;
    public event Action<bool> AimChanged;
    public event Action<PickupProp> HeldChanged;

    InteractionService _service;
    CharacterController _body;
    Transform _head;
    Vector3 _velocity;
    float _pitch;
    Vector3 _spawnPosition;
    Quaternion _spawnRotation;
    int _requestCounter;
    string _lastPrompt;
    bool _lastAim;
    PlacementSocket _previewedSocket;
    PickupProp _previewedProp;
    RaycastHit _aimHit;
    bool _aimHasHit;
    int _locks;
    string _lastHeld = "";
    Pose _camPose;
    float _camWeight, _camTarget, _camSpeed = 1f;
    float _crouch;
    bool _wantCrouch;
    float _stepPhase;
    int _lastStep;
    bool _wasGrounded = true;
    float _landDip, _landDipVelocity;
    AudioSource _feet;
    Vector3 _approachTarget;
    float _approachLeft;

    InputAction _move, _look, _sprint, _interact, _use, _drop, _crouchKey;

    public void Setup(string actorId, int slot, InteractionService service, WorkerLibrary workers)
    {
        ActorId = actorId;
        Slot = slot;
        _service = service;
        gameObject.layer = GameLayers.Player;

        _body = GetComponent<CharacterController>();
        _body.height = 1.75f;
        _body.radius = 0.35f;
        _body.center = new Vector3(0f, 0.875f, 0f);
        _body.stepOffset = 0.32f;
        _body.slopeLimit = 50f;
        _body.skinWidth = 0.04f;

        _head = new GameObject("Head").transform;
        _head.SetParent(transform, false);
        _head.localPosition = new Vector3(0f, EyeHeight, 0f);

        var camGo = new GameObject("Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(_head, false);
        Camera = camGo.AddComponent<Camera>();
        Camera.nearClipPlane = 0.05f;
        Camera.farClipPlane = 80f;
        var data = camGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.High;
        camGo.AddComponent<AudioListener>();

        HoldPoint = new GameObject("HoldPoint").transform;
        HoldPoint.SetParent(Camera.transform, false);
        HoldPoint.localPosition = HoldOffsetFor(PropCategory.Small);

        // Everyone has a worker body. The local player does not see theirs (first
        // person) but it still throws a shadow on the floor.
        Avatar = WorkerAvatar.Create(workers, slot, WorkerCostumes.ForSlot(slot), transform);
        Avatar.SetShadowOnly(true);

        // The arms you see: a separate first-person rig, in the same sleeves.
        var arms = new GameObject("FirstPersonArms");
        arms.transform.SetParent(transform, false);
        Arms = arms.AddComponent<FirstPersonArms>();
        Arms.Build(this, workers, WorkerCostumes.ForSlot(slot));

        _feet = gameObject.AddComponent<AudioSource>();
        _feet.spatialBlend = 0.6f;
        _feet.playOnAwake = false;

        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;

        CreateInput();
        GameSettings.Changed += ApplySettings;
        ApplySettings();
        service.RegisterActor(this);
    }

    void CreateInput()
    {
        _move = new InputAction("move", InputActionType.Value);
        _move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        _move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        _move.AddBinding("<Gamepad>/leftStick");
        _look = new InputAction("look", InputActionType.Value, "<Mouse>/delta");
        _sprint = new InputAction("sprint", InputActionType.Button, "<Keyboard>/leftShift");
        _sprint.AddBinding("<Gamepad>/leftStickPress");
        _interact = new InputAction("interact", InputActionType.Button, "<Keyboard>/e");
        _interact.AddBinding("<Gamepad>/buttonSouth");
        _use = new InputAction("use", InputActionType.Button, "<Mouse>/leftButton");
        _use.AddBinding("<Gamepad>/rightTrigger");
        _drop = new InputAction("drop", InputActionType.Button, "<Keyboard>/q");
        _drop.AddBinding("<Gamepad>/buttonEast");
        _crouchKey = new InputAction("crouch", InputActionType.Button, "<Keyboard>/c");
        _crouchKey.AddBinding("<Keyboard>/leftCtrl");
        _crouchKey.AddBinding("<Gamepad>/rightStickPress");
        foreach (var a in new[] { _move, _look, _sprint, _interact, _use, _drop, _crouchKey })
            a.Enable();
    }

    void OnDestroy()
    {
        GameSettings.Changed -= ApplySettings;
        foreach (var a in new[] { _move, _look, _sprint, _interact, _use, _drop, _crouchKey })
            a?.Dispose();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        bool sprinting = false;
        if (CanControl)
        {
            Look();
            sprinting = _sprint.IsPressed();
            if (_interact.WasPressedThisFrame())
                TryInteract();
            else if (_use.WasPressedThisFrame())
                TryPlace();
            else if (_drop.WasPressedThisFrame())
                TryDrop();
        }

        Vector2 input = CanControl ? Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f) : Vector2.zero;
        _wantCrouch = CanControl && _crouchKey.IsPressed();
        UpdateCrouch(dt);
        var carried = HeldProp;
        bool heavy = carried != null && carried.Category == PropCategory.Wide;
        float speed = Crouching ? CrouchSpeed : sprinting ? SprintSpeed * (heavy ? 0.8f : 1f) : WalkSpeed;
        if (Crouching)
            sprinting = false;
        Vector3 wish = transform.TransformDirection(new Vector3(input.x, 0f, input.y)) * speed;
        float blend = 1f - Mathf.Exp(-Acceleration * dt);
        _velocity.x = Mathf.Lerp(_velocity.x, wish.x, blend);
        _velocity.z = Mathf.Lerp(_velocity.z, wish.z, blend);
        _velocity.y = _body.isGrounded ? -2f : _velocity.y - Gravity * dt;
        if (_approachLeft > 0f)
        {
            // A scripted step in: walk (through the controller, so walls still count)
            // toward the spot, arriving when the time is up.
            Vector3 to = _approachTarget - transform.position;
            to.y = 0f;
            Vector3 step = to * Mathf.Min(1f, dt / _approachLeft);
            _approachLeft -= dt;
            _velocity.x = step.x / Mathf.Max(dt, 1e-4f);
            _velocity.z = step.z / Mathf.Max(dt, 1e-4f);
        }
        _body.Move(_velocity * dt);
        UpdateSteps(dt);

        UpdateAim();
        UpdateHold(dt);
        if (HeldPropId != _lastHeld)
        {
            _lastHeld = HeldPropId;
            var held = HeldProp;
            Avatar.SetIdlePose(held != null && held.Category == PropCategory.Paper ? WorkerAvatar.HoldPaper : null);
            HeldChanged?.Invoke(held);
        }
        Avatar.UpdateMotion(_body.velocity, sprinting, !string.IsNullOrEmpty(HeldPropId), dt);
    }

    // Body: crouch, steps, camera bob --------------------------------------------

    /// Crouch while held. Standing up waits until there is headroom.
    void UpdateCrouch(float dt)
    {
        float target = _wantCrouch ? 1f : 0f;
        if (target < _crouch && !HeadroomToStand())
            target = _crouch;
        _crouch = Mathf.MoveTowards(_crouch, target, dt * 4.5f);
        float height = Mathf.Lerp(StandHeight, CrouchHeight, _crouch);
        _body.height = height;
        _body.center = new Vector3(0f, height * 0.5f, 0f);
    }

    bool HeadroomToStand()
    {
        float r = _body.radius * 0.95f;
        Vector3 bottom = transform.position + Vector3.up * (r + 0.05f);
        Vector3 top = transform.position + Vector3.up * (StandHeight - r);
        return !Physics.CheckCapsule(bottom, top, r, (1 << GameLayers.World) | (1 << GameLayers.Prop), QueryTriggerInteraction.Ignore);
    }

    /// Footsteps from distance actually covered (a blocked player makes none),
    /// a small bob in step with them, and a dip when landing from a drop.
    void UpdateSteps(float dt)
    {
        var flat = new Vector3(_body.velocity.x, 0f, _body.velocity.z);
        float speed = flat.magnitude;
        bool grounded = _body.isGrounded;
        if (grounded && speed > 0.4f)
            _stepPhase += speed * dt * (Mathf.PI / StepLength);
        int step = Mathf.FloorToInt(_stepPhase / Mathf.PI);
        if (step != _lastStep)
        {
            _lastStep = step;
            float volume = Crouching ? 0.1f : speed > 4.6f ? 0.42f : 0.26f;
            _feet.pitch = UnityEngine.Random.Range(0.86f, 1.05f);
            _feet.PlayOneShot(ProceduralAudio.Footstep, volume);
        }
        if (grounded && !_wasGrounded && _velocity.y < -3f)
        {
            _landDipVelocity -= 0.6f;
            _feet.PlayOneShot(ProceduralAudio.Footstep, 0.4f);
        }
        _wasGrounded = grounded;
        // A critically damped return from the landing dip.
        _landDipVelocity += (-120f * _landDip - 22f * _landDipVelocity) * dt;
        _landDip += _landDipVelocity * dt;

        float walk = Mathf.Clamp01(speed / WalkSpeed);
        float bob = Mathf.Sin(_stepPhase * 2f) * 0.009f * walk * GameSettings.CameraBob;
        float eye = Mathf.Lerp(EyeHeight, CrouchEye, _crouch);
        _head.localPosition = new Vector3(0f, eye + bob + _landDip * GameSettings.CameraBob, 0f);
    }

    // Being driven ---------------------------------------------------------------

    /// Step in so a point ends up a comfortable reach in front of the chest
    /// (horizontally `reach` from the eye), taking `seconds`. Never steps back, never
    /// more than `maxStep`. Returns the distance it will move.
    public float ApproachForReach(Vector3 point, float reach = 0.42f, float seconds = 0.3f, float maxStep = 0.7f)
    {
        Vector3 flat = point - transform.position;
        flat.y = 0f;
        float distance = flat.magnitude;
        float move = Mathf.Clamp(distance - reach, 0f, maxStep);
        if (move < 0.02f)
            return 0f;
        _approachTarget = transform.position + flat / distance * move;
        _approachLeft = Mathf.Max(seconds, 0.05f);
        return move;
    }

    public void Lock() => _locks++;
    public void Unlock() => _locks = Mathf.Max(0, _locks - 1);

    /// Moves the camera toward a world pose (lying on the couch, looking at a
    /// screen). blendSeconds 0 snaps.
    public void SetCameraOverride(Pose pose, float blendSeconds)
    {
        _camPose = pose;
        _camTarget = 1f;
        _camSpeed = blendSeconds <= 0f ? float.MaxValue : 1f / blendSeconds;
        if (blendSeconds <= 0f)
            _camWeight = 1f;
    }

    public void ClearCameraOverride(float blendSeconds)
    {
        _camTarget = 0f;
        _camSpeed = blendSeconds <= 0f ? float.MaxValue : 1f / blendSeconds;
        if (blendSeconds <= 0f)
            _camWeight = 0f;
    }

    public bool CameraSettled => Mathf.Approximately(_camWeight, _camTarget);

    void Look()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;
        Vector2 delta = _look.ReadValue<Vector2>() * (BaseLookDegreesPerPixel * GameSettings.MouseSensitivity);
        transform.Rotate(0f, delta.x, 0f, Space.Self);
        _pitch = Mathf.Clamp(_pitch - delta.y, -MaxPitch, MaxPitch);
        _head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    /// Turns the player. Used by tests and by reset; mouse look sets the same values.
    public void SetLook(float yaw, float pitch)
    {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        _pitch = Mathf.Clamp(pitch, -MaxPitch, MaxPitch);
        _head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    public void Teleport(Vector3 position)
    {
        _body.enabled = false;
        transform.position = position;
        _body.enabled = true;
        _velocity = Vector3.zero;
    }

    public void ResetToSpawn()
    {
        Teleport(_spawnPosition);
        transform.rotation = _spawnRotation;
        HeldPropId = "";
        SetLook(_spawnRotation.eulerAngles.y, 0f);
        SetPreview(null);
    }

    /// Shows the outcome of a request that did not succeed.
    public void ReportResult(InteractionAction action, InteractionResult result)
    {
        if (result == InteractionResult.Ok)
        {
            Avatar.Trigger(WorkerAvatar.Interact);
            return;
        }
        Feedback?.Invoke(result switch
        {
            InteractionResult.HandsFull => "Your hands are full.",
            InteractionResult.NothingHeld => "You are not holding anything.",
            InteractionResult.AlreadyHeld => "Someone else has that.",
            InteractionResult.OutOfRange => "Too far away.",
            InteractionResult.StaleRevision => "It just changed. Try again.",
            InteractionResult.SocketTaken => "Something is already there.",
            InteractionResult.SocketRejects => "That does not go there.",
            InteractionResult.SocketLocked => "Put the layer below it first.",
            InteractionResult.Busy => "Someone is using that.",
            InteractionResult.NotNow => "Not now.",
            _ => "Nothing to use there.",
        });
    }

    // Aim and prompts -----------------------------------------------------------

    void UpdateAim()
    {
        var ray = new Ray(Camera.transform.position, Camera.transform.forward);
        _aimHasHit = Physics.Raycast(ray, out _aimHit, AimReach, GameLayers.AimMask, QueryTriggerInteraction.Collide);
        if (!CanControl)
        {
            SetPrompt("");
            SetPreview(null);
            SetAim(false);
            return;
        }
        var socket = _aimHasHit ? _aimHit.collider.GetComponentInParent<PlacementSocket>() : null;
        var prop = _aimHasHit ? _aimHit.collider.GetComponentInParent<PickupProp>() : null;
        var usable = _aimHasHit ? _aimHit.collider.GetComponentInParent<Interactable>() : null;
        string prompt = "";
        PlacementSocket preview = null;
        bool actionable = false;
        if (usable != null)
        {
            var result = _service.Validate(UseRequest(usable));
            if (result == InteractionResult.Ok)
            {
                prompt = $"<b>E</b>  {usable.Prompt(this)}";
                actionable = true;
            }
            else if (result == InteractionResult.NotNow || result == InteractionResult.Busy)
            {
                string why = result == InteractionResult.Busy ? "Someone is using that." : usable.BlockedReason(this);
                if (!string.IsNullOrEmpty(why))
                    prompt = $"<color=#FFFFFF99>{why}</color>";
            }
            else if (result == InteractionResult.OutOfRange)
                prompt = "<color=#FFFFFF99>Get closer</color>";
            if (!string.IsNullOrEmpty(HeldPropId))
                prompt += (prompt.Length > 0 ? "\n" : "") + "<b>Q</b>  Drop";
        }
        else if (!string.IsNullOrEmpty(HeldPropId))
        {
            if (socket != null && _service.Validate(PlaceRequest(socket)) == InteractionResult.Ok)
            {
                prompt = $"<b>Click</b>  Place on {socket.DisplayName}\n";
                preview = socket;
                actionable = true;
            }
            prompt += "<b>Q</b>  Drop";
        }
        else if (prop != null)
        {
            var result = _service.Validate(PickupRequest(prop));
            if (result == InteractionResult.Ok)
            {
                prompt = $"<b>E</b>  Pick up {prop.DisplayName}";
                actionable = true;
            }
            else if (result == InteractionResult.OutOfRange)
                prompt = "<color=#FFFFFF99>Get closer</color>";
        }
        SetPreview(preview);
        SetPrompt(prompt);
        SetAim(actionable);
    }

    /// Where a carried thing sits in front of you (its base; camera space). Paper is
    /// held up to read, a small thing to the right, a wide thing low in both hands.
    public static Vector3 HoldOffsetFor(PropCategory category) => category switch
    {
        PropCategory.Paper => new Vector3(0f, -0.2f, 0.34f),
        PropCategory.Wide => new Vector3(0f, -0.42f, 0.5f),
        _ => new Vector3(0.17f, -0.36f, 0.44f),
    };

    /// Keeps the carried thing on this side of walls: sweep its box from the eye to
    /// where it wants to be and pull it in (and lower) if something is in the way.
    void UpdateHold(float dt)
    {
        var held = HeldProp;
        Vector3 wanted = HoldOffsetFor(held != null ? held.Category : PropCategory.Small);
        if (held != null)
        {
            var b = held.LocalBounds;
            Vector3 origin = Camera.transform.position;
            Vector3 centre = Camera.transform.TransformPoint(wanted + b.center);
            Vector3 dir = centre - origin;
            float dist = dir.magnitude;
            if (dist > 1e-3f && Physics.BoxCast(origin, b.extents * 0.9f, dir / dist, out var hit, Camera.transform.rotation, dist,
                    (1 << GameLayers.World) | (1 << GameLayers.Prop), QueryTriggerInteraction.Ignore))
            {
                float ratio = Mathf.Clamp(hit.distance / dist, 0.3f, 1f);
                wanted = new Vector3(wanted.x * ratio, wanted.y - (1f - ratio) * 0.12f, wanted.z * ratio);
            }
        }
        HoldPoint.localPosition = Vector3.Lerp(HoldPoint.localPosition, wanted, 1f - Mathf.Exp(-18f * dt));
    }

    void TryInteract()
    {
        if (!_aimHasHit)
            return;
        var usable = _aimHit.collider.GetComponentInParent<Interactable>();
        if (usable != null)
        {
            var req = UseRequest(usable);
            var result = _service.Validate(req);
            if (result == InteractionResult.Ok)
                Send(req);
            else if (result == InteractionResult.NotNow)
            {
                string why = usable.BlockedReason(this);
                if (!string.IsNullOrEmpty(why))
                    Feedback?.Invoke(why);
            }
            else
                ReportResult(InteractionAction.Use, result);
            return;
        }
        if (!string.IsNullOrEmpty(HeldPropId))
            return;
        var prop = _aimHit.collider.GetComponentInParent<PickupProp>();
        if (prop != null)
            Send(PickupRequest(prop));
    }

    void TryPlace()
    {
        if (string.IsNullOrEmpty(HeldPropId) || !_aimHasHit)
            return;
        var socket = _aimHit.collider.GetComponentInParent<PlacementSocket>();
        if (socket != null)
            Send(PlaceRequest(socket));
    }

    void TryDrop()
    {
        if (!string.IsNullOrEmpty(HeldPropId))
            Send(MakeRequest(InteractionAction.Drop, "", -1));
    }

    InteractionRequest UseRequest(Interactable thing) => MakeRequest(InteractionAction.Use, thing.EntityId, -1);
    InteractionRequest PickupRequest(PickupProp prop) => MakeRequest(InteractionAction.Pickup, prop.EntityId, prop.Revision);
    InteractionRequest PlaceRequest(PlacementSocket socket) => MakeRequest(InteractionAction.Place, socket.EntityId, -1);

    InteractionRequest MakeRequest(InteractionAction action, string target, int revision) => new()
    {
        RequestId = ++_requestCounter,
        ActorId = ActorId,
        Action = action,
        TargetId = target,
        ExpectedRevision = revision,
    };

    void Send(InteractionRequest req) => Submit?.Invoke(req);

    void SetPrompt(string text)
    {
        if (text == _lastPrompt)
            return;
        _lastPrompt = text;
        PromptChanged?.Invoke(text);
    }

    void SetAim(bool actionable)
    {
        if (actionable == _lastAim)
            return;
        _lastAim = actionable;
        AimChanged?.Invoke(actionable);
    }

    void SetPreview(PlacementSocket socket)
    {
        var held = _service?.GetProp(HeldPropId);
        if (socket == _previewedSocket && (socket == null || _previewedProp == held))
            return;
        if (_previewedSocket != null)
            _previewedSocket.SetPreview(false);
        _previewedSocket = socket;
        _previewedProp = held;
        if (socket != null)
            socket.SetPreview(true, held);
    }

    void ApplySettings()
    {
        if (Camera != null)
            Camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(GameSettings.HorizontalFov, Mathf.Max(Camera.aspect, 0.1f));
    }

    void LateUpdate()
    {
        // The window may have been resized: keep the horizontal field of view.
        ApplySettings();
        _camWeight = Mathf.MoveTowards(_camWeight, _camTarget, _camSpeed * Time.unscaledDeltaTime);
        if (_camWeight <= 0f)
        {
            Camera.transform.localPosition = Vector3.zero;
            Camera.transform.localRotation = Quaternion.identity;
            return;
        }
        float w = _camWeight * _camWeight * (3f - 2f * _camWeight);
        Camera.transform.SetPositionAndRotation(
            Vector3.Lerp(_head.position, _camPose.position, w),
            Quaternion.Slerp(_head.rotation, _camPose.rotation, w));
    }
}
