using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// Packed gadgets at work, in the real scenes: the SteadyHand damps a carried
/// cake layer and drains, the coordinator spots it after a moment's grace and
/// switching it off ends the incident, a flat battery fades it out without a
/// drop; the WhisperBox gives a hint from what the call has shown and chirps
/// loud enough for a supervisor at your shoulder. A gadget in the wrong job, or
/// with no call to help with, uses nothing. And the call center shift still
/// runs from briefing to payout with nobody using a gadget at all.
public class GadgetTests
{
    GameSession _session;

    static void Pack(string recipe, string job, float charge = 1f)
    {
        CrewState.StartOver();
        var crew = CrewState.Current;
        crew.job = job;
        crew.hired = true;
        crew.billReceived = true;
        crew.gadgets.Add(new GadgetInstance { id = "G-TEST", recipe = recipe, charge = charge });
        WorkshopLedger.Equip(crew, "G-TEST", GameSession.OfflineActor);
    }

    IEnumerator Load(string scene)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
        yield return null;
        yield return null;
        _session = GameSession.Instance;
    }

    [UnityTearDown]
    public IEnumerator Restore()
    {
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("AfterGadgetTest_" + Time.frameCount));
        yield return SceneManager.UnloadSceneAsync(scene);
    }

    static IEnumerator WaitFor(System.Func<bool> condition, float seconds, string what)
    {
        for (float t = 0f; t < seconds && !condition(); t += Time.unscaledDeltaTime)
            yield return null;
        Assert.IsTrue(condition(), "timed out waiting: " + what);
    }

    /// A walkable spot at least this far from a point (on the scene's nav mesh).
    static Vector3 FarFrom(Vector3 from, float atLeast)
    {
        Vector3 best = from;
        float bestDistance = 0f;
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI / 8f;
            foreach (float r in new[] { 8f, 11f, 14f })
            {
                var probe = from + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * r;
                if (!UnityEngine.AI.NavMesh.SamplePosition(probe, out var hit, 1.5f, UnityEngine.AI.NavMesh.AllAreas))
                    continue;
                float d = Vector3.Distance(hit.position, from);
                if (d > bestDistance)
                {
                    best = hit.position;
                    bestDistance = d;
                }
            }
        }
        Assert.Greater(bestDistance, atLeast, "found somewhere far enough away");
        return best;
    }

    PickupProp CakeLayer() => _session.Service.Props.First(p => p.Kind == "cake_tier" && string.IsNullOrEmpty(p.SocketId));

    [UnityTest]
    public IEnumerator SteadyHandDampsTheWobbleDrainsAndGetsSpottedUpClose()
    {
        Pack(PartCatalog.SteadyHand, Jobs.Wedding);
        yield return Load(GameSession.WeddingScene);
        var kit = GadgetKit.Instance;
        Assert.IsNotNull(kit, "the packed gadget came to work");
        Assert.IsTrue(kit.Supported);
        var player = _session.Player;
        var mind = ShiftEvents.Mind;

        // Somewhere she cannot see: switch it on with a cake layer in hand.
        var away = FarFrom(mind.transform.position, GadgetKit.NoticeRange + 1.5f);
        player.Teleport(away + Vector3.up * 0.05f);
        yield return null;
        var layer = CakeLayer();
        Assert.AreEqual(InteractionResult.Ok, _session.Service.GiveProp(layer, player));
        kit.Press();
        Assert.IsTrue(kit.On);
        yield return new WaitForSeconds(0.3f);
        Assert.IsTrue(kit.Active, "working under the layer");
        Assert.AreEqual(1f, layer.Stabilizer);
        float charge = kit.Gadget.charge;
        yield return new WaitForSeconds(1f);
        Assert.Less(kit.Gadget.charge, charge, "it drains while it works");

        // Walk it up to her, in plain view: a moment's grace, then it counts.
        Vector3 front = mind.transform.position + mind.transform.forward * 2.2f;
        player.Teleport(new Vector3(front.x, 0.05f, front.z));
        float before = mind.Suspicion.Value;
        yield return WaitFor(() => kit.Watched > 0f, 3f, "her noticing");
        Assert.AreEqual(0, kit.Sightings, "not straight away: there's time to switch it off");
        yield return WaitFor(() => kit.Sightings == 1, 4f, "the sighting");
        yield return null;
        var incident = mind.Suspicion.Incidents.First(i => i.Key == "gadget_1");
        Assert.IsTrue(incident.Witnessed);
        Assert.Greater(mind.Suspicion.Value, before);
        Assert.AreEqual(1, kit.Gadget.timesSeen);

        // Put it away: the incident ends, nothing more is added.
        kit.Press();
        yield return null;
        yield return null;
        Assert.IsTrue(incident.Resolved, "switching it off ends it");
        Assert.AreEqual(0f, layer.Stabilizer);
        Assert.AreEqual(player.ActorId, layer.HolderId, "still in your hands");
    }

    [UnityTest]
    public IEnumerator AFlatSteadyHandFadesOutWithoutDroppingAnything()
    {
        Pack(PartCatalog.SteadyHand, Jobs.Wedding, 0.004f);
        yield return Load(GameSession.WeddingScene);
        var kit = GadgetKit.Instance;
        var layer = CakeLayer();
        _session.Service.GiveProp(layer, _session.Player);
        kit.Press();
        yield return WaitFor(() => kit.Gadget.charge <= 0f, 5f, "the battery running out");
        yield return null;
        Assert.IsFalse(kit.On, "it switches itself off");
        Assert.AreEqual(0f, layer.Stabilizer);
        Assert.AreEqual(_session.Player.ActorId, layer.HolderId, "no fling, no drop");
        kit.Press();
        Assert.IsFalse(kit.On, "a flat battery won't switch on");
        Assert.AreEqual(0f, kit.Gadget.charge);
    }

    [UnityTest]
    public IEnumerator AGadgetForAnotherJobDoesNothingAndCostsNothing()
    {
        Pack(PartCatalog.WhisperBox, Jobs.Wedding, 0.6f);
        yield return Load(GameSession.WeddingScene);
        var kit = GadgetKit.Instance;
        Assert.IsFalse(kit.Supported);
        kit.Press();
        kit.Press();
        Assert.AreEqual(0.6f, kit.Gadget.charge, 1e-5f);
    }

    [UnityTest]
    public IEnumerator WhisperBoxHintsFromWhatTheCallShowedAndTheSupervisorCanHearIt()
    {
        Pack(PartCatalog.WhisperBox, Jobs.CallCenter);
        yield return Load(GameSession.CallCenterScene);
        var shift = CallCenterShift.Instance;
        shift.PlayerSatDown();
        var kit = GadgetKit.Instance;
        Assert.IsTrue(kit.Supported);
        string me = shift.PlayerId;
        foreach (var w in shift.Router.Workers.Where(w => w.Simulated))
            shift.Router.SetBreak(w.Id, true);

        kit.Press();
        Assert.AreEqual(1f, kit.Gadget.charge, "not at the desk yet: no charge used");
        var desk = shift.DeskOf(me);
        desk.Use(_session.Player);
        yield return WaitFor(() => desk.Occupied, 2f, "sitting down");
        kit.Press();
        Assert.AreEqual(1f, kit.Gadget.charge, "no call: no charge used");

        // Keep her far away for the first hint.
        var boss = shift.Boss;
        boss.Walker.Warp(FarFrom(desk.transform.position, GadgetKit.WhisperHearRange + 1f), Quaternion.identity);
        var call = shift.Router.Ring(CaseLibrary.Get("refund_toaster"), CallLanguage.English);
        Assert.AreEqual(me, call.Owner);
        shift.Do(me, "answer");
        shift.Do(me, "greet");
        kit.Press();
        Assert.AreEqual(0.8f, kit.Gadget.charge, 1e-4f, "a fifth of the battery");
        StringAssert.Contains("order number", kit.LastHint);
        Assert.AreEqual(0f, boss.Suspicion.Value, "nobody heard it");

        // At your shoulder, she hears the chirp.
        boss.Walker.Warp(desk.transform.position + desk.transform.right * 1.2f, Quaternion.identity);
        yield return null;
        kit.Press();
        Assert.AreEqual(1, kit.Sightings);
        Assert.Greater(boss.Suspicion.Value, 0f);
        Assert.AreEqual(CallOutcome.Open, call.Outcome, "it never touches the account");
    }

    [UnityTest]
    public IEnumerator ACallCenterShiftRunsToThePayoutWithNoGadgetAtAll()
    {
        CrewState.StartOver();
        var crew = CrewState.Current;
        crew.job = Jobs.CallCenter;
        crew.hired = true;
        crew.SetLanguages(GameSession.OfflineActor, new[] { CallLanguage.Arabic });
        yield return Load(GameSession.CallCenterScene);
        Assert.IsNull(GadgetKit.Instance, "nothing packed, nothing attached");
        var shift = CallCenterShift.Instance;
        shift.PlayerSatDown();
        int before = crew.savings;
        Time.timeScale = 20f;
        yield return WaitFor(() => shift.Paid, 90f, "the end of the shift");
        Time.timeScale = 1f;
        Assert.AreEqual(CallCenterShift.Phase.Payout, shift.Current, "paid, not exposed");
        Assert.Greater(shift.Router.Calls.Count(), 5, "the evening's callers came through");
        Assert.Greater(crew.savings, before - 1);
        Assert.AreEqual(1, crew.shiftsCompleted);
        Assert.IsTrue(crew.starterKitGiven, "and Dev's box is on its way");
    }
}
