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
public class PlayerActor : MonoBehaviour, IInteractionActor
{
    const float WalkSpeed = 4f;
    const float SprintSpeed = 6f;
    const float Acceleration = 12f;
    const float Gravity = 20f;
    public const float EyeHeight = 1.55f;
    const float AimReach = 3.2f;
    const float HoldDistance = 1.2f;
    const float HoldMinDistance = 0.8f;
    static readonly Vector2 HoldOffset = new(0.38f, -0.4f);
    const float BaseLookDegreesPerPixel = 0.126f;
    const float MaxPitch = 83f;

    public string ActorId { get; private set; } = "peer_1";
    public int Slot { get; private set; }
    public string HeldPropId { get; set; } = "";
    public Transform HoldPoint { get; private set; }
    public Vector3 EyePosition => _head.position;
    public Vector3 DropVelocity => _velocity * 0.5f;
    public bool ControlsEnabled { get; set; } = true;
    public Camera Camera { get; private set; }
    public WorkerAvatar Avatar { get; private set; }

    /// Set by the session: sends a request onward (applied here, or sent to the host).
    public Action<InteractionRequest> Submit;
    public event Action<string> PromptChanged;
    public event Action<string> Feedback;
    public event Action<bool> AimChanged;

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

    InputAction _move, _look, _sprint, _interact, _use, _drop;

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
        HoldPoint.localPosition = new Vector3(HoldOffset.x, HoldOffset.y, HoldDistance);

        // Everyone has a worker body. The local player does not see theirs (first
        // person) but it still throws a shadow on the floor.
        Avatar = WorkerAvatar.Create(workers, slot, WorkerCostumes.ForSlot(slot), transform);
        Avatar.SetShadowOnly(true);

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
        foreach (var a in new[] { _move, _look, _sprint, _interact, _use, _drop })
            a.Enable();
    }

    void OnDestroy()
    {
        GameSettings.Changed -= ApplySettings;
        foreach (var a in new[] { _move, _look, _sprint, _interact, _use, _drop })
            a?.Dispose();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        bool sprinting = false;
        if (ControlsEnabled)
        {
            Look();
            sprinting = _sprint.IsPressed();
            if (_interact.WasPressedThisFrame())
                TryPickup();
            else if (_use.WasPressedThisFrame())
                TryPlace();
            else if (_drop.WasPressedThisFrame())
                TryDrop();
        }

        Vector2 input = ControlsEnabled ? Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f) : Vector2.zero;
        float speed = sprinting ? SprintSpeed : WalkSpeed;
        Vector3 wish = transform.TransformDirection(new Vector3(input.x, 0f, input.y)) * speed;
        float blend = 1f - Mathf.Exp(-Acceleration * dt);
        _velocity.x = Mathf.Lerp(_velocity.x, wish.x, blend);
        _velocity.z = Mathf.Lerp(_velocity.z, wish.z, blend);
        _velocity.y = _body.isGrounded ? -2f : _velocity.y - Gravity * dt;
        _body.Move(_velocity * dt);

        UpdateAim();
        UpdateHoldDistance();
        Avatar.UpdateMotion(_body.velocity, sprinting, !string.IsNullOrEmpty(HeldPropId), dt);
    }

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
            _ => "Nothing to use there.",
        });
    }

    // Aim and prompts -----------------------------------------------------------

    void UpdateAim()
    {
        var ray = new Ray(Camera.transform.position, Camera.transform.forward);
        _aimHasHit = Physics.Raycast(ray, out _aimHit, AimReach, GameLayers.AimMask, QueryTriggerInteraction.Collide);
        if (!ControlsEnabled)
        {
            SetPrompt("");
            SetPreview(null);
            SetAim(false);
            return;
        }
        var socket = _aimHasHit ? _aimHit.collider.GetComponentInParent<PlacementSocket>() : null;
        var prop = _aimHasHit ? _aimHit.collider.GetComponentInParent<PickupProp>() : null;
        string prompt = "";
        PlacementSocket preview = null;
        bool actionable = false;
        if (!string.IsNullOrEmpty(HeldPropId))
        {
            if (socket != null && _service.Validate(PlaceRequest(socket)) == InteractionResult.Ok)
            {
                prompt = $"<b>Click</b>  Place on {socket.DisplayName}\n";
                preview = socket;
                actionable = true;
            }
            prompt += "<b>Q</b>  Drop";
        }
        else if (prop != null && _service.Validate(PickupRequest(prop)) == InteractionResult.Ok)
        {
            prompt = $"<b>E</b>  Pick up {prop.DisplayName}";
            actionable = true;
        }
        SetPreview(preview);
        SetPrompt(prompt);
        SetAim(actionable);
    }

    /// Pull a held prop in when something is close in front, so it never pokes through.
    void UpdateHoldDistance()
    {
        float distance = HoldDistance;
        if (_aimHasHit && !_aimHit.collider.isTrigger)
            distance = Mathf.Clamp(_aimHit.distance - 0.45f, HoldMinDistance, HoldDistance);
        HoldPoint.localPosition = new Vector3(HoldOffset.x, HoldOffset.y, distance);
    }

    void TryPickup()
    {
        if (!string.IsNullOrEmpty(HeldPropId) || !_aimHasHit)
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
    }
}
