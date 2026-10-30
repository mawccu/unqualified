using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Walking for any NPC: a navigation mesh agent that routes round tables,
/// counters and moved chairs (chairs carve the mesh), steers round the player,
/// and has a solid body the player cannot walk through. When someone stands in
/// the way it slows, says "Excuse me" (now and then, not every frame), and after
/// a while reports that it is stuck so its owner can choose another plan
/// instead of pushing anyone through a wall.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NpcWalker : MonoBehaviour
{
    const float BlockedSpeed = 0.15f;
    const float ExcuseAfter = 0.7f;
    const float ExcuseCooldown = 6f;
    const float GiveUpAfter = 3.5f;

    public NavMeshAgent Agent { get; private set; }
    public WorkerAvatar Avatar { get; set; }
    /// While true the owner is posing the body; the walker does not drive locomotion.
    public bool Performing { get; set; }
    public bool HasDestination { get; private set; }
    public event Action<string> Said;
    public event Action Stuck;

    float _blocked, _lastExcuse = -99f;

    public static NpcWalker Setup(GameObject go, float speed)
    {
        var agent = Get<NavMeshAgent>(go);
        agent.speed = speed;
        agent.angularSpeed = 300f;
        agent.acceleration = 6f;
        agent.radius = 0.3f;
        agent.height = 1.75f;
        agent.stoppingDistance = 0.08f;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        // A solid body: the player's controller stops against it.
        var capsule = Get<CapsuleCollider>(go);
        capsule.height = 1.75f;
        capsule.radius = 0.28f;
        capsule.center = new Vector3(0f, 0.875f, 0f);
        var body = Get<Rigidbody>(go);
        body.isKinematic = true;
        var walker = Get<NpcWalker>(go);
        walker.Agent = agent;
        return walker;
    }

    // Unity objects do not work with ??, so look up explicitly.
    static T Get<T>(GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }

    void Awake() => Agent = GetComponent<NavMeshAgent>();

    public void Warp(Vector3 position, Quaternion rotation)
    {
        if (Agent.isOnNavMesh || NavMesh.SamplePosition(position, out _, 1f, NavMesh.AllAreas))
            Agent.Warp(position);
        else
            transform.position = position;
        transform.rotation = rotation;
    }

    public void GoTo(Vector3 destination)
    {
        if (!Agent.isOnNavMesh)
            return;
        Agent.isStopped = false;
        Agent.updateRotation = true;
        Agent.SetDestination(destination);
        HasDestination = true;
        _blocked = 0f;
    }

    public void Stop()
    {
        HasDestination = false;
        if (Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.ResetPath();
        }
    }

    public bool Arrived => HasDestination && Agent.isOnNavMesh && !Agent.pathPending
        && Agent.remainingDistance <= Agent.stoppingDistance + 0.06f;

    /// Turn on the spot toward a point (while standing).
    public void Face(Vector3 point, float turnSpeed = 4f)
    {
        Agent.updateRotation = false;
        Vector3 flat = point - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0025f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), Mathf.Min(1f, turnSpeed * Time.deltaTime));
    }

    AudioSource _feet;
    float _stride;

    /// Footsteps in time with the walk (about one step every 0.65 m).
    void Steps(float dt)
    {
        float speed = Agent.enabled ? Agent.velocity.magnitude : 0f;
        if (speed < 0.2f || Performing)
            return;
        _stride += speed * dt;
        if (_stride < 0.65f)
            return;
        _stride = 0f;
        if (_feet == null)
        {
            _feet = gameObject.AddComponent<AudioSource>();
            _feet.spatialBlend = 1f;
            _feet.minDistance = 1f;
            _feet.maxDistance = 12f;
            _feet.playOnAwake = false;
        }
        _feet.pitch = UnityEngine.Random.Range(0.85f, 1.05f);
        _feet.PlayOneShot(ProceduralAudio.Footstep, 0.45f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (Avatar != null && !Performing)
            Avatar.UpdateMotion(Agent.velocity, false, false, dt);
        Steps(dt);
        if (!HasDestination || Arrived)
        {
            _blocked = 0f;
            return;
        }
        bool wantsToMove = Agent.desiredVelocity.magnitude > 0.3f;
        _blocked = wantsToMove && Agent.velocity.magnitude < BlockedSpeed ? _blocked + dt : 0f;
        if (_blocked > ExcuseAfter && Time.time - _lastExcuse > ExcuseCooldown && PlayerNearby())
        {
            _lastExcuse = Time.time;
            Said?.Invoke("Excuse me.");
        }
        if (_blocked > GiveUpAfter)
        {
            _blocked = 0f;
            Stuck?.Invoke();
        }
    }

    bool PlayerNearby()
    {
        var player = GameSession.Instance != null ? GameSession.Instance.Player : null;
        return player != null && Vector3.Distance(player.transform.position, transform.position) < 1.6f;
    }
}
