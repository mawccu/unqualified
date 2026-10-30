using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A worker body: one of the rigged models, dressed in a costume and animated
/// from what every peer already knows (velocity, whether it carries something).
///
/// The models carry every hairstyle and accessory, so the costume shows only its
/// own. Colours go through property blocks, so all workers share the library
/// materials. The animator has one state per clip and no transitions: this
/// script decides what plays and cross-fades to it, as the Godot presenter did.
/// </summary>
public class WorkerAvatar : MonoBehaviour
{
    public const string Idle = "idle", Walk = "walk", Sprint = "sprint", CarryIdle = "carry_idle", CarryWalk = "carry_walk";
    public const string Interact = "interact", Clean = "clean", Stumble = "stumble", Celebrate = "celebrate";
    public const string Knock = "knock", Talk = "talk", Point = "point", Offer = "offer", Impatient = "impatient";
    public const string HoldPaper = "hold_paper", SitType = "sit_type";

    const float MoveThreshold = 0.25f;
    const float SprintThreshold = 4.8f;
    const float FadeLocomotion = 0.15f;
    const float FadeAction = 0.08f;
    // Speeds at which the walk and sprint clips' feet match the ground.
    const float WalkClipSpeed = 2.4f;
    const float SprintClipSpeed = 5.2f;

    static readonly HashSet<string> Actions = new() { Interact, Clean, Stumble, Celebrate };

    Animator _animator;
    readonly Dictionary<string, float> _clipLengths = new();
    string _current = "";
    string _idlePose;
    float _actionLeft;
    Vector3 _smoothedVelocity;
    readonly List<Renderer> _renderers = new();

    public string CurrentState => _current;

    public static WorkerAvatar Create(WorkerLibrary library, int slot, WorkerCostumes.Costume costume, Transform parent)
    {
        var model = Instantiate(library.ModelForSlot(slot), parent, false);
        model.name = "Avatar";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        var avatar = model.AddComponent<WorkerAvatar>();
        avatar.Setup(library.controller, costume);
        return avatar;
    }

    void Setup(RuntimeAnimatorController controller, WorkerCostumes.Costume costume)
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
            _animator = gameObject.AddComponent<Animator>();
        _animator.runtimeAnimatorController = controller;
        _animator.applyRootMotion = false;
        _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        foreach (var clip in controller.animationClips)
            _clipLengths[clip.name] = clip.length;
        ApplyCostume(costume);
        Play(Idle, 0f);
    }

    void ApplyCostume(WorkerCostumes.Costume costume)
    {
        var block = new MaterialPropertyBlock();
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            string n = renderer.name;
            bool visible = true;
            if (n.StartsWith("Hair_"))
                visible = n.StartsWith($"Hair_{costume.HairStyle}_") || n == $"Hair_{costume.HairStyle}";
            else if (n.StartsWith("Acc_"))
            {
                visible = false;
                foreach (var accessory in costume.Accessories)
                    if (n == $"Acc_{accessory}" || n.StartsWith($"Acc_{accessory}_") || n.StartsWith($"Acc_{accessory}-"))
                        visible = true;
            }
            else if (renderer is not SkinnedMeshRenderer)
                visible = false; // stray helper meshes in the export
            renderer.gameObject.SetActive(visible);
            if (!visible)
                continue;
            _renderers.Add(renderer);
            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || !costume.TryGetRole(mats[i].name, out var colour))
                    continue;
                renderer.GetPropertyBlock(block, i);
                block.SetColor("_BaseColor", colour);
                renderer.SetPropertyBlock(block, i);
            }
            if (renderer is SkinnedMeshRenderer skinned)
                skinned.updateWhenOffscreen = false;
        }
    }

    /// The local player's own body: invisible to them, still throwing a shadow.
    public void SetShadowOnly(bool on)
    {
        foreach (var r in _renderers)
            r.shadowCastingMode = on ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
    }

    /// Drive the locomotion states. Call every frame with the body's velocity.
    public void UpdateMotion(Vector3 worldVelocity, bool sprinting, bool carrying, float dt)
    {
        var flat = new Vector3(worldVelocity.x, 0f, worldVelocity.z);
        _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, flat, 1f - Mathf.Exp(-12f * Mathf.Max(dt, 0.0001f)));
        float speed = _smoothedVelocity.magnitude;
        bool moving = speed > MoveThreshold;

        if (Actions.Contains(_current))
        {
            _actionLeft -= dt;
            bool interruptClean = _current == Clean && moving;
            if (_actionLeft > 0f && !interruptClean)
                return;
        }

        string wanted = Idle;
        if (!moving && _idlePose != null)
            wanted = _idlePose;
        else if (carrying)
            wanted = moving ? CarryWalk : CarryIdle;
        else if (moving)
            wanted = sprinting && speed > SprintThreshold ? Sprint : Walk;
        if (wanted != _current)
            Play(wanted, FadeLocomotion);

        // Feet that keep up with the ground, within a believable range.
        _animator.speed = wanted switch
        {
            Walk or CarryWalk => Mathf.Clamp(speed / WalkClipSpeed, 0.7f, 1.7f),
            Sprint => Mathf.Clamp(speed / SprintClipSpeed, 0.8f, 1.4f),
            _ => 1f,
        };
    }

    /// Play a short action: interact, clean, stumble or celebrate. Stumble may
    /// interrupt anything; the others do not interrupt a stumble.
    public void Trigger(string action)
    {
        if (!Actions.Contains(action))
            return;
        if (_current == Stumble && _actionLeft > 0f && action != Stumble)
            return;
        Play(action, FadeAction);
        _animator.speed = 1f;
        _actionLeft = action == Clean ? float.MaxValue : _clipLengths.GetValueOrDefault(action, 0.5f);
    }

    /// What to do while standing still instead of the usual idle (reading the
    /// bill, typing at the desk). Null goes back to normal.
    public void SetIdlePose(string state) => _idlePose = state;

    /// The sitting clip has the hips 0.24 m above the feet with the thighs level,
    /// as if on a 0.17 m stool. Real seats are higher.
    public const float SitClipSeat = 0.17f;
    /// Seat heights (from the floor) of the chairs people sit on.
    public const float GuestChairSeat = 0.46f, DeskChairSeat = 0.5f, CouchSeat = 0.52f;

    /// Lifts the body so the sitting pose rests on a seat this high above the
    /// character's origin (0 puts it back on its feet).
    public void SitOn(float seatHeight) =>
        transform.localPosition = new Vector3(0f, seatHeight > 0f ? seatHeight - SitClipSeat : 0f, 0f);

    /// Plays a state directly, for performances (the landlord, the coordinator).
    /// Callers that do this should not also call UpdateMotion that frame.
    public void Perform(string state, float fade = FadeLocomotion)
    {
        if (state == _current)
            return;
        _animator.speed = 1f;
        _actionLeft = 0f;
        Play(state, fade);
    }

    /// A bone by name (e.g. "HandR"), for things held in a hand.
    public Transform FindBone(string name)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == name)
                return t;
        return null;
    }

    void Play(string state, float fade)
    {
        _current = state;
        _animator.CrossFadeInFixedTime(state, fade, 0);
    }
}
