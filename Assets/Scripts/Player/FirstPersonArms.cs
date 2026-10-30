using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The local player's own forearms and hands, seen in first person. Built from
/// segment models (sleeves, palm, two-part fingers and thumb), placed every frame
/// by a two-bone arm solve from a shoulder just outside the view, and posed by
/// grip shapes. Arms are physically limited: a target out of reach leaves the
/// hand at the end of a straight arm, never a stretched one.
///
/// What each hand does comes from, in order: an override (a scripted reach: the
/// bill, a door handle, the keyboard), the prop being carried or still settling
/// onto its spot, or a relaxed pose low at the edge of the view that swings with
/// walking. Changes of target blend in camera space, so turning never drags them.
/// </summary>
[DefaultExecutionOrder(100)]
public class FirstPersonArms : MonoBehaviour
{
    public enum Side { Right = 0, Left = 1 }

    const float UpperLength = 0.32f;
    const float ForeLength = 0.3f;
    public const float Reach = UpperLength + ForeLength - 0.005f;
    const float FingerBendSpeed = 14f;

    sealed class Hand
    {
        public float SideSign;          // +1 right, -1 left
        public Transform Upper, Fore, Wrist;
        public Transform[] FingerBase = new Transform[4], FingerTip = new Transform[4];
        public Transform ThumbBase, ThumbTip;
        public Grips.Shape Shape = Grips.Of(Grip.Relaxed);
        // Target blending, in camera space.
        public Pose Current;            // last resolved, camera space
        public Pose BlendFrom;
        public float BlendT = 1f, BlendSeconds = 0.25f;
        public int Source = -1;         // which source was used last frame (for detecting changes)
        // Override.
        public Func<Pose> Override;
        public Grip OverrideGrip;
        public bool HasOverride;
    }

    readonly Hand[] _hands = { new() { SideSign = 1f }, new() { SideSign = -1f } };
    PlayerActor _player;
    Transform _cam;
    PickupProp _lastProp;
    float _swingPhase, _time;
    bool _visible = true;
    Renderer[] _renderers;

    public bool Visible
    {
        get => _visible;
        set
        {
            _visible = value;
            if (_renderers != null)
                foreach (var r in _renderers)
                    r.enabled = value;
        }
    }

    /// The world pose of a wrist (for tests and captures).
    public Pose WristPose(Side side) => new(_hands[(int)side].Wrist.position, _hands[(int)side].Wrist.rotation);

    public void Build(PlayerActor player, WorkerLibrary library, WorkerCostumes.Costume costume)
    {
        _player = player;
        _cam = player.Camera.transform;
        foreach (var hand in _hands)
        {
            string n = hand.SideSign > 0 ? "R" : "L";
            hand.Upper = Part(library.armUpper, $"UpperArm{n}", transform);
            hand.Fore = Part(library.armForearm, $"Forearm{n}", transform);
            hand.Wrist = Part(library.handPalm, $"Hand{n}", transform);
            float thumbSide = -hand.SideSign;
            float[] scale = { 0.95f, 1f, 0.94f, 0.8f };
            for (int i = 0; i < 4; i++)
            {
                var knuckle = Part(library.fingerBase, $"Finger{i}{n}", hand.Wrist);
                knuckle.localPosition = new Vector3(thumbSide * (0.027f - i * 0.018f), 0.002f, 0.08f);
                knuckle.localScale = Vector3.one * scale[i];
                hand.FingerBase[i] = knuckle;
                var tip = Part(library.fingerTip, $"Finger{i}Tip{n}", knuckle);
                tip.localPosition = new Vector3(0f, 0f, 0.04f);
                hand.FingerTip[i] = tip;
            }
            hand.ThumbBase = Part(library.thumbBase, $"Thumb{n}", hand.Wrist);
            hand.ThumbBase.localPosition = new Vector3(thumbSide * 0.034f, -0.008f, 0.02f);
            hand.ThumbTip = Part(library.thumbTip, $"ThumbTip{n}", hand.ThumbBase);
            hand.ThumbTip.localPosition = new Vector3(0f, 0f, 0.034f);
            hand.Current = IdlePose(hand, 0f);
        }
        _renderers = GetComponentsInChildren<Renderer>(true);
        var block = new MaterialPropertyBlock();
        foreach (var r in _renderers)
        {
            r.shadowCastingMode = ShadowCastingMode.Off; // the body's shadow already includes the arms
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && costume.TryGetRole(mats[i].name, out var colour))
                {
                    r.GetPropertyBlock(block, i);
                    block.SetColor("_BaseColor", colour);
                    r.SetPropertyBlock(block, i);
                }
        }
    }

    static Transform Part(GameObject prefab, string name, Transform parent)
    {
        var go = Instantiate(prefab, parent, false);
        go.name = name;
        return go.transform;
    }

    // Scripted hands ------------------------------------------------------------------

    /// One hand follows a world wrist pose (a reach for the bill, a door handle,
    /// the keyboard) until cleared. The change blends over blendSeconds.
    public void SetOverride(Side side, Func<Pose> wrist, Grip grip, float blendSeconds = 0.3f)
    {
        var hand = _hands[(int)side];
        hand.Override = wrist;
        hand.OverrideGrip = grip;
        hand.HasOverride = true;
        Retarget(hand, blendSeconds);
    }

    /// Changes the grip shape of an overridden hand without moving it (closing on the paper).
    public void SetOverrideGrip(Side side, Grip grip) => _hands[(int)side].OverrideGrip = grip;

    public void ClearOverride(Side side, float blendSeconds = 0.3f)
    {
        var hand = _hands[(int)side];
        if (!hand.HasOverride)
            return;
        hand.HasOverride = false;
        hand.Override = null;
        Retarget(hand, blendSeconds);
    }

    public void ClearAll(float blendSeconds = 0.3f)
    {
        ClearOverride(Side.Right, blendSeconds);
        ClearOverride(Side.Left, blendSeconds);
    }

    void Retarget(Hand hand, float seconds)
    {
        hand.BlendFrom = hand.Current;
        hand.BlendSeconds = seconds;
        hand.BlendT = seconds <= 0f ? 1f : 0f;
    }

    // Every frame -----------------------------------------------------------------------

    void LateUpdate()
    {
        if (_player == null)
            return;
        float dt = Time.deltaTime;
        _time += dt;
        float speed = new Vector2(_player.Velocity.x, _player.Velocity.z).magnitude;
        _swingPhase += speed * dt * 2.1f;

        var prop = _player.HeldProp;
        if (prop != null)
            _lastProp = prop;
        else if (_lastProp != null && !_lastProp.IsArriving)
            _lastProp = null; // keep the hands on a placed prop until it has settled
        Grips.Target right = default, left = default;
        if (_lastProp != null && (prop != null || !string.IsNullOrEmpty(_lastProp.SocketId)))
            Grips.ForProp(_lastProp, out right, out left);

        Solve(_hands[0], right, speed, dt);
        Solve(_hands[1], left, speed, dt);
    }

    void Solve(Hand hand, Grips.Target propTarget, float speed, float dt)
    {
        // Choose the source: override, prop, idle.
        int source;
        Pose desired;
        Grip grip;
        if (hand.HasOverride && hand.Override != null)
        {
            source = 0;
            desired = ToCamera(hand.Override());
            grip = hand.OverrideGrip;
        }
        else if (propTarget.Active)
        {
            source = 1;
            desired = ToCamera(propTarget.Wrist);
            grip = propTarget.Grip;
        }
        else
        {
            source = 2;
            desired = IdlePose(hand, speed);
            grip = Grip.Relaxed;
        }
        if (source != hand.Source && hand.Source != -1)
            Retarget(hand, source == 2 ? 0.35f : 0.25f);
        hand.Source = source;

        // Blend toward it in camera space.
        if (hand.BlendT < 1f)
        {
            hand.BlendT = Mathf.Min(1f, hand.BlendT + dt / Mathf.Max(hand.BlendSeconds, 0.0001f));
            float w = hand.BlendT * hand.BlendT * (3f - 2f * hand.BlendT);
            desired = new Pose(Vector3.Lerp(hand.BlendFrom.position, desired.position, w), Quaternion.Slerp(hand.BlendFrom.rotation, desired.rotation, w));
        }
        hand.Current = desired;

        // Fingers ease toward the grip shape.
        hand.Shape = Grips.Lerp(hand.Shape, Grips.Of(grip), 1f - Mathf.Exp(-FingerBendSpeed * dt));
        PlaceArm(hand, FromCamera(desired));
        PoseFingers(hand);
    }

    /// Relaxed, low at the edge of the view, swinging gently with each step.
    Pose IdlePose(Hand hand, float speed)
    {
        float s = hand.SideSign;
        float walk = Mathf.Clamp01(speed / 4f);
        float sprint = Mathf.Clamp01((speed - 4.2f) / 1.6f);
        float amp = Mathf.Lerp(0.0f, 0.035f, walk) + 0.04f * sprint;
        float swing = Mathf.Sin(_swingPhase + (s > 0f ? 0f : Mathf.PI)) * amp;
        float breathe = Mathf.Sin(_time * 1.7f + s) * 0.004f;
        var pos = new Vector3(s * 0.2f, -0.24f + breathe + Mathf.Abs(swing) * 0.25f - sprint * 0.02f, 0.3f + swing);
        var forward = new Vector3(-s * 0.12f, -0.55f, 1f);
        var up = new Vector3(s * 1f, 0.35f, 0f);
        return new Pose(pos, Quaternion.LookRotation(forward, up));
    }

    static Vector3 ShoulderOffset(float side) => new(side * 0.17f, -0.22f, -0.05f);

    /// How far a wrist target is beyond what the arm can reach (0 when reachable).
    public float ShortBy(Side side, Vector3 wristWorld)
    {
        Vector3 shoulder = _cam.TransformPoint(ShoulderOffset(side == Side.Right ? 1f : -1f));
        return Mathf.Max(0f, Vector3.Distance(shoulder, wristWorld) - Reach);
    }

    Pose ToCamera(Pose world) => new(_cam.InverseTransformPoint(world.position), Quaternion.Inverse(_cam.rotation) * world.rotation);
    Pose FromCamera(Pose local) => new(_cam.TransformPoint(local.position), _cam.rotation * local.rotation);

    /// Two-bone solve from a shoulder just behind and below the view.
    void PlaceArm(Hand hand, Pose wrist)
    {
        float s = hand.SideSign;
        Vector3 shoulder = _cam.TransformPoint(ShoulderOffset(s));
        Vector3 toWrist = wrist.position - shoulder;
        float d = toWrist.magnitude;
        Vector3 dir = d > 1e-4f ? toWrist / d : _cam.forward;
        float reach = Mathf.Clamp(d, 0.08f, Reach);
        Vector3 wristPos = shoulder + dir * reach; // out of reach: the hand stops at a straight arm
        Vector3 pole = _cam.TransformDirection(new Vector3(s * 0.8f, -1f, -0.3f));
        Vector3 bend = (pole - dir * Vector3.Dot(pole, dir)).normalized;
        float cos = Mathf.Clamp((UpperLength * UpperLength + reach * reach - ForeLength * ForeLength) / (2f * UpperLength * reach), -1f, 1f);
        float angle = Mathf.Acos(cos);
        Vector3 elbow = shoulder + dir * (Mathf.Cos(angle) * UpperLength) + bend * (Mathf.Sin(angle) * UpperLength);

        hand.Upper.SetPositionAndRotation(shoulder, Quaternion.LookRotation(elbow - shoulder, -bend));
        hand.Fore.SetPositionAndRotation(elbow, Quaternion.LookRotation(wristPos - elbow, -bend));
        hand.Wrist.SetPositionAndRotation(wristPos, wrist.rotation);
    }

    void PoseFingers(Hand hand)
    {
        var shape = hand.Shape;
        float thumbSide = -hand.SideSign;
        for (int i = 0; i < 4; i++)
        {
            float spread = (i - 1.5f) * 4f * shape.Spread * -thumbSide;
            hand.FingerBase[i].localRotation = Quaternion.Euler(shape.Base[i], spread, 0f);
            hand.FingerTip[i].localRotation = Quaternion.Euler(shape.Tip[i], 0f, 0f);
        }
        // The thumb points forward and out at rest and swings under the palm to oppose the fingers.
        float yaw = thumbSide * Mathf.Lerp(40f, -8f, shape.ThumbAcross);
        float pitch = Mathf.Lerp(18f, 42f, shape.ThumbAcross);
        float roll = -thumbSide * Mathf.Lerp(10f, 55f, shape.ThumbAcross);
        hand.ThumbBase.localRotation = Quaternion.Euler(pitch, yaw, roll);
        hand.ThumbTip.localRotation = Quaternion.Euler(shape.ThumbBend, 0f, 0f);
    }
}
