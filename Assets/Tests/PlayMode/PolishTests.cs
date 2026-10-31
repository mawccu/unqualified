using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// Regressions from the polish pass: nothing rests in mid-air on an invisible
/// collider (the takeout box on the kitchen counter, the cloth and order slips
/// in the wedding kitchen), and the flat's window slides open and shut, ignores
/// presses while moving, and will not close on something left on the sill.
public class PolishTests
{
    [UnityTearDown]
    public IEnumerator Restore()
    {
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("AfterPolishTest_" + Time.frameCount));
        yield return SceneManager.UnloadSceneAsync(scene);
    }

    static IEnumerator Load(string scene)
    {
        CrewState.StartOver();
        var crew = CrewState.Current;
        crew.billReceived = true;
        crew.hired = true;
        PlayerPrefs.SetInt("intro_seen", 1);
        SceneManager.LoadScene(scene);
        yield return null;
        yield return null;
    }

    /// Every loose prop, once settled, sits on something solid right under it.
    static void AssertSupported()
    {
        Physics.SyncTransforms();
        var floating = new System.Collections.Generic.List<string>();
        foreach (var prop in Object.FindObjectsByType<PickupProp>(FindObjectsSortMode.None))
        {
            if (!string.IsNullOrEmpty(prop.SocketId) || !string.IsNullOrEmpty(prop.HolderId))
                continue;
            var col = prop.GetComponent<Collider>();
            var b = col.bounds;
            // Below the box, anywhere across its footprint (paper can lie across a gap).
            bool supported = false;
            foreach (var dx in new[] { -0.4f, 0f, 0.4f })
                foreach (var dz in new[] { -0.4f, 0f, 0.4f })
                {
                    var from = new Vector3(b.center.x + b.extents.x * dx, b.min.y + 0.02f, b.center.z + b.extents.z * dz);
                    foreach (var hit in Physics.RaycastAll(from, Vector3.down, 0.06f, ~0, QueryTriggerInteraction.Ignore))
                        if (hit.collider != col)
                            supported = true;
                }
            if (!supported)
                floating.Add($"{prop.EntityId} at {prop.transform.position}");
        }
        Assert.IsEmpty(floating, "props resting on nothing: " + string.Join(", ", floating));
    }

    [UnityTest]
    public IEnumerator NothingInTheFlatFloats()
    {
        yield return Load(GameSession.ApartmentScene);
        yield return new WaitForSeconds(4f);
        AssertSupported();
        var takeout = GameSession.Instance.Service.GetProp("takeout_3");
        Assert.Less(takeout.transform.position.y, 0.95f, "the takeout box sits on the worktop (0.91 m), not on a lid at the tap's height");
    }

    [UnityTest]
    public IEnumerator NothingInTheWeddingFloats()
    {
        yield return Load(GameSession.WeddingScene);
        yield return new WaitForSeconds(4f);
        AssertSupported();
        Assert.Less(GameSession.Instance.Service.GetProp("cloth").transform.position.y, 0.98f, "the cloth lies on the counter");
    }

    [UnityTest]
    public IEnumerator TheWindowSlidesOpenAndShutAndWillNotCloseOnThings()
    {
        yield return Load(GameSession.ApartmentScene);
        var session = GameSession.Instance;
        var window = Object.FindFirstObjectByType<ApartmentWindow>();
        Assert.IsNotNull(window, "the flat has a window that opens");
        var player = session.Player;
        player.Teleport(new Vector3(-3.3f, 0.05f, 0.5f));
        yield return null;
        var use = new InteractionRequest { ActorId = player.ActorId, Action = InteractionAction.Use, TargetId = "window", ExpectedRevision = -1 };
        Vector3 shut = window.transform.position;
        Assert.AreEqual(InteractionResult.Ok, session.Service.Request(use));
        yield return null;
        Assert.AreEqual(InteractionResult.NotNow, session.Service.Validate(use), "a press while it slides does nothing");
        Assert.AreEqual("", window.BlockedReason(player), "and says nothing about it");
        yield return new WaitForSeconds(0.8f);
        Assert.IsTrue(window.IsOpen);
        Assert.AreEqual(1f, window.Openness, 1e-3f);
        Assert.Greater(Vector3.Distance(window.transform.position, shut), 0.5f, "the sash slid along its track");

        // A mug on the sill, in the sash's way.
        var thing = session.Service.Props.First(p => p.Kind == "clutter");
        thing.transform.position = shut + Vector3.up * 0.47f;
        thing.GetComponent<Rigidbody>().isKinematic = true;
        Physics.SyncTransforms();
        Assert.AreEqual(InteractionResult.NotNow, session.Service.Validate(use), "it will not close on something in the way");
        thing.GetComponent<Rigidbody>().isKinematic = false;
        thing.ResetToStart();
        Physics.SyncTransforms();

        Assert.AreEqual(InteractionResult.Ok, session.Service.Request(use));
        yield return new WaitForSeconds(0.8f);
        Assert.IsFalse(window.IsOpen);
        Assert.Less(Vector3.Distance(window.transform.position, shut), 0.001f, "back where it started");
        Assert.IsTrue(player.CanControl, "no stuck hands or locks");
    }
}
