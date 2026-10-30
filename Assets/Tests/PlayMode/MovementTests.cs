using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// Jumping, solid furniture and NPC movement, in the real wedding scene.
public class MovementTests
{
    GameSession _session;
    PlayerActor _player;

    [UnitySetUp]
    public IEnumerator LoadWedding()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSession.WeddingScene);
        yield return null;
        yield return null;
        _session = GameSession.Instance;
        _player = _session.Player;
        Assert.IsNotNull(_player);
        yield return new WaitForSeconds(0.3f);
    }

    [UnityTearDown]
    public IEnumerator Restore()
    {
        Time.timeScale = 1f;
        if (_player != null)
        {
            _player.ScriptedMove = null;
            _player.ScriptedSprint = false;
        }
        yield return null;
    }

    IEnumerator Settle()
    {
        for (int i = 0; i < 40 && !_player.Grounded; i++)
            yield return null;
        yield return new WaitForSeconds(0.1f);
    }

    [UnityTest]
    public IEnumerator JumpReachesItsHeightOnceAndLands()
    {
        // Open floor in the ballroom.
        _player.Teleport(new Vector3(-3f, 0.05f, 6.5f));
        yield return Settle();
        float floor = _player.transform.position.y;
        _player.PressJump();
        float apex = floor;
        bool left = false;
        for (float t = 0f; t < 1.5f; t += Time.deltaTime)
        {
            // Mashing jump on the way up must not add height (no double jump).
            if (t < 0.25f)
                _player.PressJump();
            apex = Mathf.Max(apex, _player.transform.position.y);
            left |= !_player.Grounded;
            yield return null;
        }
        Assert.IsTrue(left, "the player left the ground");
        Assert.AreEqual(PlayerActor.JumpHeight, apex - floor, 0.12f, "about the configured height, no double jump");
        Assert.IsTrue(_player.Grounded, "landed again");
        Assert.Less(Mathf.Abs(_player.transform.position.y - floor), 0.05f, "back on the same floor");
    }

    [UnityTest]
    public IEnumerator ACeilingStopsTheRise()
    {
        _player.Teleport(new Vector3(-3f, 0.05f, 6.5f));
        yield return Settle();
        float floor = _player.transform.position.y;
        // A low beam 25 cm above the head.
        var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.transform.position = new Vector3(-3f, floor + 1.75f + 0.25f + 0.1f, 6.5f);
        beam.transform.localScale = new Vector3(2f, 0.2f, 2f);
        yield return new WaitForFixedUpdate();
        _player.PressJump();
        float apex = floor;
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            apex = Mathf.Max(apex, _player.transform.position.y);
            yield return null;
        }
        Object.Destroy(beam);
        Assert.Less(apex - floor, 0.32f, "the head stops at the beam");
        Assert.IsTrue(_player.Grounded);
    }

    [UnityTest]
    public IEnumerator WalkingIntoAChairNeverPassesThroughIt()
    {
        // A chair at the first guest table; walk at it from 1.5 m for two seconds.
        PickupProp chair = null;
        foreach (var p in _session.Service.Props)
            if (p.Kind == "chair") { chair = p; break; }
        Assert.IsNotNull(chair);
        Vector3 c = chair.transform.position;
        Vector3 away = (c - new Vector3(-9f, 0f, 9f)).normalized; // outward from its table
        _player.Teleport(c + away * 1.5f + Vector3.up * 0.05f);
        Vector3 toward = -away;
        _player.SetLook(Quaternion.LookRotation(toward).eulerAngles.y, 0f);
        yield return Settle();
        var controller = _player.GetComponent<CharacterController>();
        var chairColliders = chair.GetComponentsInChildren<Collider>();
        float deepest = 0f;
        _player.ScriptedMove = Vector2.up;
        for (float t = 0f; t < 2f; t += Time.deltaTime)
        {
            yield return null;
            foreach (var col in chairColliders)
                if (Physics.ComputePenetration(controller, controller.transform.position, controller.transform.rotation,
                        col, col.transform.position, col.transform.rotation, out _, out float depth))
                    deepest = Mathf.Max(deepest, depth);
        }
        _player.ScriptedMove = null;
        Assert.Less(deepest, 0.06f, "the body never sinks into the chair");
        Assert.Less(Vector3.Distance(chair.transform.position, c), 1.2f, "a walked-into chair is nudged, not launched");
    }

    [UnityTest]
    public IEnumerator TheCoordinatorWalksRoundFurnitureAndPeople()
    {
        var coordinator = Object.FindFirstObjectByType<Coordinator>();
        Assert.IsNotNull(coordinator);
        // Stand in the ballroom where her route passes.
        _player.Teleport(new Vector3(0f, 0.05f, 5.0f));
        Time.timeScale = 3f;
        var self = coordinator.GetComponent<CapsuleCollider>();
        int mask = (1 << GameLayers.World) | (1 << GameLayers.Prop) | (1 << GameLayers.Player);
        var hits = new Collider[16];
        float worst = 0f;
        string worstName = "";
        Vector3 start = coordinator.transform.position;
        float travelled = 0f;
        Vector3 last = start;
        for (float t = 0f; t < 45f; t += Time.deltaTime)
        {
            yield return null;
            travelled += Vector3.Distance(coordinator.transform.position, last);
            last = coordinator.transform.position;
            Vector3 p = coordinator.transform.position;
            int n = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * 0.4f, p + Vector3.up * 1.5f, 0.24f, hits, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (hits[i] == self || hits[i].transform.IsChildOf(coordinator.transform))
                    continue;
                if (hits[i].name.Contains("Floor"))
                    continue;
                if (Physics.ComputePenetration(self, self.transform.position, self.transform.rotation, hits[i], hits[i].transform.position, hits[i].transform.rotation, out _, out float depth) && depth > worst)
                {
                    worst = depth;
                    worstName = hits[i].name;
                }
            }
        }
        Time.timeScale = 1f;
        Assert.Greater(travelled, 20f, "she actually patrols");
        Assert.Less(worst, 0.1f, $"she never walks into furniture or people (deepest: {worstName})");
    }
}
