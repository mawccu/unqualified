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

    WorkerAvatar _avatar;
    Transform _handL, _handR;
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
        _lastPosition = transform.position;
    }

    void Start()
    {
        var bill = OpeningDirector.Instance != null ? OpeningDirector.Instance.Bill : null;
        if (bill != null && OpeningDirector.Instance.Stage != OpeningStage.FindJob && OpeningDirector.Instance.Stage != OpeningStage.Hired)
            bill.HoldByNpc("landlord", BillPose);
        _avatar.Perform(WorkerAvatar.Impatient, 0f);
    }

    /// Where the bill sits: held out flat toward whoever he faces while offering,
    /// otherwise hanging from his left hand.
    Pose BillPose()
    {
        if (_offering && _handR != null)
        {
            var pos = _handR.position + transform.forward * 0.06f - Vector3.up * 0.2f;
            return new Pose(pos, transform.rotation * Quaternion.Euler(-8f, 180f, 0f));
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
        return string.IsNullOrEmpty(actor.HeldPropId) ? InteractionResult.Ok : InteractionResult.HandsFull;
    }

    public override void Use(IInteractionActor actor)
    {
        if (OpeningDirector.Instance.TakeBill(actor) == InteractionResult.Ok)
            _offering = false;
    }
}
