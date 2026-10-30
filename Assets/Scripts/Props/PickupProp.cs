using System;
using UnityEngine;

/// <summary>
/// A carryable object. Only InteractionService changes its ownership state.
/// While held it is kinematic and follows the holder's hold point every rendered
/// frame (not every physics step, so it never stutters against the camera);
/// when released it is a normal rigid body again. The model's origin is at its
/// base, so a socket's snap point is simply where its bottom goes.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PickupProp : MonoBehaviour
{
    const float FallLimit = -10f;
    const float HeldYawSmoothing = 14f;
    const float HeldMaxLag = 23f; // degrees
    const float TiltStiffness = 110f;
    const float TiltDamping = 7f;

    [SerializeField] string entityId = "prop";
    [SerializeField] string kind = "cake_tier";
    [SerializeField] string displayName = "prop";

    public string EntityId => entityId;
    public string Kind => kind;
    public string DisplayName => displayName;

    /// Bumped on every ownership change so stale requests can be rejected.
    public int Revision { get; private set; }
    public string HolderId { get; private set; } = "";
    public string SocketId { get; private set; } = "";

    Rigidbody _body;
    Collider[] _colliders;
    Vector3 _startPosition;
    Quaternion _startRotation;
    Transform _holdTarget;
    Func<Pose> _npcPose;
    float _heldYaw;
    Vector3 _lastHoldPosition;
    Vector2 _tilt, _tiltVelocity;

    public void Configure(string id, string propKind, string display)
    {
        entityId = id;
        kind = propKind;
        displayName = display;
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>(true);
        _startPosition = transform.position;
        _startRotation = transform.rotation;
        _body.interpolation = RigidbodyInterpolation.Interpolate;
        _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void LateUpdate()
    {
        if (_holdTarget != null)
            FollowHold(Time.deltaTime);
        else if (_npcPose != null)
        {
            var pose = _npcPose();
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }

    void FixedUpdate()
    {
        if (_holdTarget == null && _npcPose == null && string.IsNullOrEmpty(SocketId) && transform.position.y < FallLimit)
            ResetToStart();
    }

    /// The position follows the holder exactly, so range checks and aiming never
    /// change. Only the look lags: the yaw trails a turn a little and the prop
    /// tilts slightly with the holder's motion.
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
        // Moving forward leans the top back; moving sideways leans it the other way.
        var wanted = new Vector2(Mathf.Clamp(-local.z * 0.03f, -0.2f, 0.2f), Mathf.Clamp(local.x * 0.04f, -0.2f, 0.2f));
        _tiltVelocity += (TiltStiffness * (wanted - _tilt) - TiltDamping * _tiltVelocity) * dt;
        _tilt += _tiltVelocity * dt;

        transform.SetPositionAndRotation(target, Quaternion.Euler(_tilt.x * Mathf.Rad2Deg, _heldYaw, _tilt.y * Mathf.Rad2Deg));
    }

    /// Held by someone who is not a player (the landlord): it follows the pose
    /// they give every frame and cannot be picked up until they hand it over.
    public void HoldByNpc(string holder, Func<Pose> pose)
    {
        _holdTarget = null;
        _npcPose = pose;
        HolderId = holder;
        SocketId = "";
        SetPhysical(false, false);
        Revision++;
    }

    public void BeginHold(Transform target, string holder)
    {
        _npcPose = null;
        _holdTarget = target;
        _heldYaw = target.eulerAngles.y;
        _lastHoldPosition = target.position;
        _tilt = _tiltVelocity = Vector2.zero;
        HolderId = holder;
        SocketId = "";
        SetPhysical(false, false);
        Revision++;
    }

    public void PlaceAt(Vector3 position, Quaternion rotation, string socket)
    {
        _holdTarget = null;
        _npcPose = null;
        HolderId = "";
        SocketId = socket;
        SetPhysical(false, true);
        transform.SetPositionAndRotation(position, rotation);
        Revision++;
    }

    public void Release(Vector3 velocity)
    {
        _holdTarget = null;
        _npcPose = null;
        HolderId = "";
        SetPhysical(true, true);
        _body.linearVelocity = velocity;
        _body.angularVelocity = Vector3.zero;
        Revision++;
    }

    public void ResetToStart()
    {
        _holdTarget = null;
        _npcPose = null;
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
