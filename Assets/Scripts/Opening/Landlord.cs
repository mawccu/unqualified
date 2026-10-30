using System.Collections;
using UnityEngine;

/// <summary>
/// The landlord. Knocks, waits, steps into the doorway (never past it), talks,
/// points into the flat and holds the bill out until someone takes it, then
/// leaves down the corridor. The bill is in his hand the whole time and stays
/// his until the hand-over; after that it is the player's.
/// </summary>
[RequireComponent(typeof(CapsuleCollider))]
public class Landlord : Interactable
{
    const float WalkSpeed = 1.3f;
    const float TurnSpeed = 6f;

    [SerializeField] WorkerLibrary workers;
    [SerializeField] Transform knockSpot;
    [SerializeField] Transform doorwaySpot;
    [SerializeField] Transform exitSpot;
    [SerializeField] AudioSource doorAudio;

    /// How far a player's eye may be from the bill's free edge to take it.
    public const float HandoverReach = 1.3f;

    WorkerAvatar _avatar;
    Transform _handL, _handR, _head;
    bool _lookAtBill;
    float _headDip;
    Transform _faceTarget;
    bool _walking, _offering, _knocking;
    Vector3 _lastPosition;

    public void Configure(WorkerLibrary library, Transform knock, Transform doorway, Transform exit, AudioSource audio)
    {
        workers = library;
        knockSpot = knock;
        doorwaySpot = doorway;
        exitSpot = exit;
        doorAudio = audio;
    }

    void Awake()
    {
        var capsule = GetComponent<CapsuleCollider>();
        capsule.height = 1.75f;
        capsule.radius = 0.3f;
        capsule.center = new Vector3(0f, 0.875f, 0f);
        transform.SetPositionAndRotation(knockSpot.position, knockSpot.rotation);
        _avatar = WorkerAvatar.Create(workers, 0, WorkerCostumes.Landlord, transform);
        _handL = _avatar.FindBone("HandL");
        _handR = _avatar.FindBone("HandR");
        _head = _avatar.FindBone("Head");
        _lastPosition = transform.position;
    }

    void Start()
    {
        var bill = OpeningDirector.Instance != null ? OpeningDirector.Instance.Bill : null;
        if (bill != null && OpeningDirector.Instance.Stage != OpeningStage.FindJob && OpeningDirector.Instance.Stage != OpeningStage.Hired)
            bill.HoldByNpc("landlord", BillPose);
        _avatar.Perform(WorkerAvatar.Impatient, 0f);
    }

    /// Where the bill sits: held out by one edge toward whoever he faces while
    /// offering (print toward them, its other edge free to take), otherwise hanging
    /// from his left hand.
    Pose BillPose()
    {
        if (_offering && _handR != null)
        {
            var rotation = transform.rotation * Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(-6f, 0f, 0f);
            // His fingers are at the sheet's -X edge (his right), a little above its middle.
            var pos = _handR.position + transform.forward * 0.03f - rotation * new Vector3(-0.1f, 0.17f, 0f);
            return new Pose(pos, rotation);
        }
        var hand = _handL != null ? _handL : transform;
        return new Pose(hand.position - Vector3.up * 0.26f + transform.forward * 0.03f, transform.rotation * Quaternion.Euler(0f, 90f, 0f));
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (_walking)
        {
            var velocity = (transform.position - _lastPosition) / Mathf.Max(dt, 0.0001f);
            _avatar.UpdateMotion(velocity, false, false, dt);
        }
        else if (_faceTarget != null)
        {
            Vector3 flat = _faceTarget.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), Mathf.Min(1f, TurnSpeed * dt));
        }
        _lastPosition = transform.position;
    }

    void LateUpdate()
    {
        // His eyes go from your face to the paper while you take it.
        _headDip = Mathf.MoveTowards(_headDip, _lookAtBill ? 20f : 0f, Time.deltaTime * 60f);
        if (_head != null && _headDip > 0.01f)
            _head.rotation = Quaternion.AngleAxis(_headDip, transform.right) * _head.rotation;
    }

    public void LookAtBill(bool on) => _lookAtBill = on;

    public void Perform(string state)
    {
        _offering = state == WorkerAvatar.Offer;
        _avatar.Perform(state);
    }

    /// One round of knocking, heard through the door.
    public void Knock()
    {
        _avatar.Perform(WorkerAvatar.Knock);
        if (doorAudio != null)
            doorAudio.PlayOneShot(ProceduralAudio.Knock);
    }

    /// Knock, wait, complain, repeat, until the door opens.
    public void BeginKnocking()
    {
        _knocking = true;
        StartCoroutine(KnockLoop());
    }

    IEnumerator KnockLoop()
    {
        int round = 0;
        while (_knocking)
        {
            Knock();
            yield return new WaitForSeconds(1.2f);
            if (!_knocking)
                break;
            Knock();
            yield return new WaitForSeconds(1.2f);
            if (!_knocking)
                break;
            _avatar.Perform(WorkerAvatar.Impatient);
            if (round % 2 == 0)
                GameSession.Instance.Hud.ShowSubtitle("Landlord, behind the door",
                    round == 0 ? "I know you're in there. I can hear the microwave." : "I can hear you breathing. Open up.", 3f);
            round++;
            yield return new WaitForSeconds(4f);
        }
    }

    /// Steps up to the threshold (staying outside) and turns to whoever opened.
    public IEnumerator StepIntoDoorway(Transform face)
    {
        _knocking = false;
        yield return WalkTo(doorwaySpot.position, 0.9f);
        _faceTarget = face;
        yield return new WaitForSeconds(0.3f);
    }

    public IEnumerator Leave()
    {
        _faceTarget = null;
        yield return WalkTo(exitSpot.position, 0f);
        gameObject.SetActive(false);
    }

    IEnumerator WalkTo(Vector3 target, float speedScale)
    {
        _walking = true;
        float speed = speedScale > 0f ? WalkSpeed * speedScale : WalkSpeed;
        while (true)
        {
            Vector3 to = target - transform.position;
            to.y = 0f;
            if (to.magnitude < 0.04f)
                break;
            transform.position += to.normalized * Mathf.Min(speed * Time.deltaTime, to.magnitude);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Mathf.Min(1f, TurnSpeed * Time.deltaTime));
            yield return null;
        }
        _walking = false;
        _avatar.Perform(WorkerAvatar.Idle);
    }

    // The hand-over -------------------------------------------------------------------

    public override string Prompt(IInteractionActor actor) => "Take the bill";

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        var director = OpeningDirector.Instance;
        if (director == null || director.Stage != OpeningStage.BillOffered)
            return InteractionResult.NotNow;
        if (director.HandoverInProgress)
            return InteractionResult.Busy;
        if (!string.IsNullOrEmpty(actor.HeldPropId))
            return InteractionResult.HandsFull;
        return BillReachable(actor) ? InteractionResult.Ok : InteractionResult.NotNow;
    }

    public override string BlockedReason(IInteractionActor actor)
    {
        var director = OpeningDirector.Instance;
        return director != null && director.Stage == OpeningStage.BillOffered && !BillReachable(actor) ? "Step closer to take it." : "";
    }

    bool BillReachable(IInteractionActor actor)
    {
        var bill = OpeningDirector.Instance.Bill;
        var edge = Grips.PaperEdge(bill.transform, bill.LocalBounds, 1f).Wrist.position;
        return Vector3.Distance(actor.EyePosition, edge) <= HandoverReach;
    }

    public override void Use(IInteractionActor actor) => OpeningDirector.Instance.BeginHandover(actor);

    public override void OnActorLeft(string actorId) => OpeningDirector.Instance?.OnActorLeft(actorId);
}
