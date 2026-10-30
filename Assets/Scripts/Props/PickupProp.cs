using System;
using UnityEngine;

/// <summary>
/// A carryable object. Only InteractionService changes its ownership state.
/// While held it is kinematic and follows the holder's hold point every rendered
/// frame (not every physics step, so it never stutters against the camera);
/// when released it is a normal rigid body again with its category's weight,
/// damping and sound. The model's origin is at its base, so a socket's snap
/// point is simply where its bottom goes.
///
/// Ownership changes are instant (the rules need that) but the look never
/// jumps: a prop taken into the hands travels there from where it was, and a
/// placed prop eases down onto its spot.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PickupProp : MonoBehaviour
{
    public enum Condition { Fine, Smeared, Ruined }

    const float FallLimit = -10f;
    const float HeldYawSmoothing = 14f;
    const float HeldMaxLag = 23f; // degrees
    const float TiltStiffness = 110f;
    const float TiltDamping = 7f;
    /// How long a prop takes to come into the hands, and to settle onto a spot.
    public static float GrabSeconds = 0.4f;
    public static float PlaceSeconds = 0.22f;
    const float MaxDropSpeed = 2.5f;

    [SerializeField] string entityId = "prop";
    [SerializeField] string kind = "cake_tier";
    [SerializeField] string displayName = "prop";
    [SerializeField] PropCategory category = PropCategory.Small;

    public string EntityId => entityId;
    public string Kind => kind;
    public string DisplayName => displayName;
    public PropCategory Category => category;

    /// Bumped on every ownership change so stale requests can be rejected.
    public int Revision { get; private set; }
    public string HolderId { get; private set; } = "";
    public string SocketId { get; private set; } = "";
    /// The prop's own box (local space), for grips and for keeping it out of walls.
    public Bounds LocalBounds { get; private set; }
    public bool IsArriving => _blendT < 1f;

    /// How it looks: a hard knock smears a cake, a fall ruins it. Only the authority changes it.
    public Condition State { get; private set; } = Condition.Fine;
    /// 0 calm to 1 slipping out of the hands (wide things only, while carried).
    public float Wobble { get; private set; }
    /// How well it was lined up when placed on a spot (1 dead centre, 0 at the edge of the tolerance).
    public float PlacementQuality { get; set; } = 1f;
    /// Raised once per change of State, with the new state.
    public event Action<PickupProp, Condition> Damaged;
    /// Raised on any hard hit (for things that listen: suspicion, sounds).
    public event Action<PickupProp, float> HitHard;
    Vector3 _baseScale;
    Vector3 _smoothedHoldVelocity;
    float _lastYawSeen;
    /// The middle of the prop (its base is its origin), for reach checks.
    public Vector3 Centre => transform.TransformPoint(LocalBounds.center);

    Rigidbody _body;
    Collider[] _colliders;
    AudioSource _audio;
    Vector3 _startPosition;
    Quaternion _startRotation;
    Transform _holdTarget;
    Func<Pose> _npcPose;
    float _heldYaw;
    Vector3 _lastHoldPosition;
    Vector2 _tilt, _tiltVelocity;
    Pose _blendFrom, _placeTarget;
    float _blendT = 1f;
    bool _placing;
    float _lastImpact;
    float _flutterPhase;

    public void Configure(string id, string propKind, string display, PropCategory propCategory = PropCategory.Small)
    {
        entityId = id;
        kind = propKind;
        displayName = display;
        category = propCategory;
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>(true);
        _startPosition = transform.position;
        _startRotation = transform.rotation;
        _body.interpolation = RigidbodyInterpolation.Interpolate;
        _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        PropPhysics.Apply(_body, _colliders, category);
        if (category == PropCategory.Furniture)
            _body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        var bounds = new Bounds();
        bool first = true;
        foreach (var c in _colliders)
            if (c is BoxCollider box)
            {
                var b = new Bounds(box.center, box.size);
                if (first) { bounds = b; first = false; }
                else bounds.Encapsulate(b);
            }
        LocalBounds = bounds;
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 1f;
        _audio.minDistance = 0.8f;
        _audio.maxDistance = 14f;
        _audio.playOnAwake = false;
        _flutterPhase = UnityEngine.Random.value * 10f;
        _baseScale = transform.localScale;
    }

    /// A jolt (landing from a jump, a bump): wide things wobble more.
    public void AddWobble(float amount)
    {
        if (category == PropCategory.Wide && _holdTarget != null)
            Wobble = Mathf.Clamp01(Wobble + amount);
    }

    /// Changes how it looks (authority). Never improves past Fine; repairs go Smeared -> Fine.
    public void SetCondition(Condition state)
    {
        if (state == State)
            return;
        State = state;
        transform.localScale = state switch
        {
            Condition.Smeared => Vector3.Scale(_baseScale, new Vector3(1.03f, 0.93f, 1.03f)),
            Condition.Ruined => Vector3.Scale(_baseScale, new Vector3(1.14f, 0.7f, 1.14f)),
            _ => _baseScale,
        };
        Damaged?.Invoke(this, state);
    }

    void LateUpdate()
    {
        if (_holdTarget != null)
            FollowHold(Time.deltaTime);
        else if (_npcPose != null)
        {
            var pose = _npcPose();
            Present(pose, Time.deltaTime);
        }
        else if (_placing)
        {
            Present(_placeTarget, Time.deltaTime);
            if (_blendT >= 1f)
                _placing = false;
        }
    }

    void FixedUpdate()
    {
        if (_holdTarget != null || _npcPose != null || !string.IsNullOrEmpty(SocketId))
            return;
        if (transform.position.y < FallLimit)
        {
            ResetToStart();
            return;
        }
        if (category == PropCategory.Paper && !_body.isKinematic && _body.linearVelocity.y < -0.2f)
            Flutter();
    }

    /// A falling sheet drifts side to side and rocks instead of dropping like a brick.
    void Flutter()
    {
        _flutterPhase += Time.fixedDeltaTime * 7f;
        Vector3 side = Vector3.Cross(transform.up, Vector3.up);
        if (side.sqrMagnitude < 0.01f)
            side = transform.right;
        _body.AddForce(side.normalized * (Mathf.Sin(_flutterPhase) * 0.06f), ForceMode.Force);
        _body.AddTorque(transform.forward * (Mathf.Cos(_flutterPhase * 0.8f) * 0.0006f), ForceMode.Force);
    }

    void OnCollisionEnter(Collision collision)
    {
        float speed = collision.relativeVelocity.magnitude;
        if (speed > 1.2f)
            HitHard?.Invoke(this, speed);
        // Cake takes damage from hard landings.
        if (category == PropCategory.Wide && kind == "cake_tier")
        {
            if (speed > 2.2f && State != Condition.Ruined)
                SetCondition(Condition.Ruined);
            else if (speed > 0.9f && State == Condition.Fine)
                SetCondition(Condition.Smeared);
        }
        if (speed < 0.5f || Time.time - _lastImpact < 0.15f)
            return;
        _lastImpact = Time.time;
        var p = PropPhysics.For(category);
        _audio.pitch = (p.ImpactPitch > 0f ? p.ImpactPitch : 1f) * UnityEngine.Random.Range(0.92f, 1.08f);
        _audio.PlayOneShot(PropPhysics.ImpactClip(category), Mathf.Clamp01(speed / 4f) * (category == PropCategory.Paper ? 0.5f : 0.9f));
    }

    /// Where it should be while held: from the hold point, with a lagging yaw and a
    /// tilt from the holder's motion. Paper is held up to read (follows the look);
    /// everything else stays level.
    void FollowHold(float dt)
    {
        dt = Mathf.Max(dt, 0.0001f);
        Vector3 target = _holdTarget.position;
        float yaw = _holdTarget.eulerAngles.y;
        float lag = Mathf.DeltaAngle(_heldYaw, yaw) * (1f - Mathf.Exp(-HeldYawSmoothing * dt));
        _heldYaw = yaw - Mathf.Clamp(Mathf.DeltaAngle(_heldYaw + lag, yaw), -HeldMaxLag, HeldMaxLag);

        Vector3 seen = (target - _lastHoldPosition) / dt;
        _lastHoldPosition = target;
        Vector3 local = Quaternion.Inverse(Quaternion.Euler(0f, yaw, 0f)) * seen;
        float sway = category == PropCategory.Wide ? 1.4f : 1f;
        if (category == PropCategory.Wide)
            UpdateWobble(seen, yaw, dt);
        // Moving forward leans the top back; moving sideways leans it the other way.
        var wanted = new Vector2(Mathf.Clamp(-local.z * 0.03f * sway, -0.2f, 0.2f), Mathf.Clamp(local.x * 0.04f * sway, -0.2f, 0.2f));
        _tiltVelocity += (TiltStiffness * (wanted - _tilt) - TiltDamping * _tiltVelocity) * dt;
        _tilt += _tiltVelocity * dt;

        Quaternion rotation;
        if (category == PropCategory.Furniture)
        {
            // Dragged upright along the floor, turning with the holder.
            target.y = FloorBelow(target);
            Present(new Pose(target, Quaternion.Euler(0f, _heldYaw, 0f)), dt);
            return;
        }
        if (category == PropCategory.Paper)
        {
            // Held up facing you, leaning back a little, with the same small lag.
            float pitch = _holdTarget.eulerAngles.x;
            rotation = Quaternion.Euler(pitch + 4f + _tilt.x * Mathf.Rad2Deg * 0.5f, _heldYaw, _tilt.y * Mathf.Rad2Deg * 0.5f);
        }
        else
            rotation = Quaternion.Euler(_tilt.x * Mathf.Rad2Deg, _heldYaw, _tilt.y * Mathf.Rad2Deg);
        if (Wobble > 0.05f)
        {
            // A visible rocking that grows with the wobble.
            float t = Time.time;
            rotation *= Quaternion.Euler(Mathf.Sin(t * 9f) * Wobble * 8f, 0f, Mathf.Cos(t * 7.3f) * Wobble * 8f);
        }
        Present(new Pose(target, rotation), dt);
    }

    /// Wobble builds from sudden starts and stops, sharp turns and running, and
    /// settles when the carrier calms down.
    void UpdateWobble(Vector3 seenVelocity, float yaw, float dt)
    {
        Vector3 previous = _smoothedHoldVelocity;
        _smoothedHoldVelocity = Vector3.Lerp(_smoothedHoldVelocity, seenVelocity, 1f - Mathf.Exp(-10f * dt));
        Vector3 accel = (_smoothedHoldVelocity - previous) / dt;
        accel.y = 0f;
        float yawRate = Mathf.Abs(Mathf.DeltaAngle(_lastYawSeen, yaw)) / dt;
        _lastYawSeen = yaw;
        float speed = new Vector2(_smoothedHoldVelocity.x, _smoothedHoldVelocity.z).magnitude;
        // Walking pace (4 m/s) is safe; running with it is not.
        float target = Mathf.Clamp01(accel.magnitude / 16f + Mathf.Max(0f, yawRate - 90f) / 500f + Mathf.Max(0f, speed - 4.1f) / 0.7f);
        Wobble = target > Wobble ? Mathf.MoveTowards(Wobble, target, dt * 0.9f) : Mathf.MoveTowards(Wobble, target, dt * 0.5f);
    }

    /// The floor height under a point (anything solid below it in the world layer).
    static float FloorBelow(Vector3 point)
    {
        return Physics.Raycast(point + Vector3.up * 0.5f, Vector3.down, out var hit, 3f, 1 << GameLayers.World, QueryTriggerInteraction.Ignore)
            ? hit.point.y
            : point.y;
    }

    /// Applies a pose, eased in from where the prop was when its owner changed.
    void Present(Pose pose, float dt)
    {
        if (_blendT < 1f)
        {
            float duration = _placing ? PlaceSeconds : GrabSeconds;
            _blendT = duration <= 0f ? 1f : Mathf.Min(1f, _blendT + dt / duration);
            float w = _blendT * _blendT * (3f - 2f * _blendT);
            pose = new Pose(Vector3.Lerp(_blendFrom.position, pose.position, w), Quaternion.Slerp(_blendFrom.rotation, pose.rotation, w));
        }
        transform.SetPositionAndRotation(pose.position, pose.rotation);
    }

    void StartBlend(float duration)
    {
        _blendFrom = new Pose(transform.position, transform.rotation);
        _blendT = duration <= 0f ? 1f : 0f;
    }

    /// Held by someone who is not a player (the landlord): it follows the pose
    /// they give every frame and cannot be picked up until they hand it over.
    public void HoldByNpc(string holder, Func<Pose> pose)
    {
        _holdTarget = null;
        _placing = false;
        _npcPose = pose;
        HolderId = holder;
        SocketId = "";
        SetPhysical(false, false);
        StartBlend(0f);
        Revision++;
    }

    public void BeginHold(Transform target, string holder)
    {
        _npcPose = null;
        _placing = false;
        _holdTarget = target;
        Wobble = 0f;
        _smoothedHoldVelocity = Vector3.zero;
        _lastYawSeen = target.eulerAngles.y;
        _heldYaw = target.eulerAngles.y;
        _lastHoldPosition = target.position;
        _tilt = _tiltVelocity = Vector2.zero;
        HolderId = holder;
        SocketId = "";
        SetPhysical(false, false);
        StartBlend(GrabSeconds);
        Revision++;
        _audio.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        _audio.PlayOneShot(ProceduralAudio.Pickup, 0.6f);
    }

    public void PlaceAt(Vector3 position, Quaternion rotation, string socket)
    {
        _audio.pitch = (Category == PropCategory.Paper ? 1.6f : Category == PropCategory.Wide ? 0.8f : 1f) * UnityEngine.Random.Range(0.94f, 1.06f);
        _audio.PlayOneShot(ProceduralAudio.SetDown, Category == PropCategory.Paper ? 0.25f : 0.7f);
        _holdTarget = null;
        _npcPose = null;
        HolderId = "";
        SocketId = socket;
        SetPhysical(false, true);
        _placeTarget = new Pose(position, rotation);
        StartBlend(PlaceSeconds);
        _placing = true;
        if (_blendT >= 1f)
        {
            transform.SetPositionAndRotation(position, rotation);
            _placing = false;
        }
        Revision++;
    }

    public void Release(Vector3 velocity)
    {
        Wobble = 0f;
        _holdTarget = null;
        _npcPose = null;
        _placing = false;
        _blendT = 1f;
        HolderId = "";
        SetPhysical(true, true);
        // A drop, not a throw: keep some of the hand's motion, never fling it.
        _body.linearVelocity = Vector3.ClampMagnitude(velocity, MaxDropSpeed);
        _body.angularVelocity = category == PropCategory.Small ? UnityEngine.Random.insideUnitSphere * 1.5f : Vector3.zero;
        Revision++;
    }

    public void ResetToStart()
    {
        Wobble = 0f;
        PlacementQuality = 1f;
        _holdTarget = null;
        _npcPose = null;
        _placing = false;
        _blendT = 1f;
        HolderId = "";
        SocketId = "";
        SetPhysical(false, true);
        transform.SetPositionAndRotation(_startPosition, _startRotation);
        _body.position = _startPosition;
        _body.rotation = _startRotation;
        SetPhysical(true, true);
        Revision++;
    }

    /// The visible meshes, for a socket's ghost preview.
    public MeshFilter[] VisualMeshes() => GetComponentsInChildren<MeshFilter>();

    void SetPhysical(bool simulate, bool collide)
    {
        if (simulate)
        {
            _body.isKinematic = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.WakeUp();
        }
        else
        {
            if (!_body.isKinematic)
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
            }
            _body.isKinematic = true;
            // Interpolation on a body moved by hand would drag it a frame behind.
            _body.interpolation = RigidbodyInterpolation.None;
        }
        foreach (var c in _colliders)
            c.enabled = collide;
    }
}
