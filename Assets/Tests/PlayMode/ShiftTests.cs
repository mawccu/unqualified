using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// The shift in the real venue: line of sight through walls, and paying once.
public class ShiftTests
{
    [UnitySetUp]
    public IEnumerator LoadWedding()
    {
        CrewState.StartOver();
        SceneManager.LoadScene(GameSession.WeddingScene);
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator WallsBlockHerSight()
    {
        var mind = Object.FindFirstObjectByType<CoordinatorMind>();
        var body = mind.GetComponent<Coordinator>();
        yield return new WaitForSeconds(0.2f);
        // Stand her in the ballroom just north of the dividing wall, facing it (south, -Z).
        body.Busy = true;
        body.Walker.Stop();
        body.Walker.Warp(new Vector3(0f, 0f, 1.2f), Quaternion.Euler(0f, 180f, 0f));
        yield return null;
        body.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        Assert.IsTrue(mind.CanSee(new Vector3(0.5f, 0.2f, -1.2f + 2f)), "in front of her in the same room: seen");
        Assert.IsFalse(mind.CanSee(new Vector3(0f, 0.2f, -5f)), "behind the dividing wall: not seen");
        Assert.IsFalse(mind.CanSee(new Vector3(0f, 0.2f, 8f)), "behind her back: not seen");
        Assert.IsFalse(mind.CanSee(new Vector3(-11f, 0.2f, -11f)), "in the kitchen, two walls away: not seen");
    }

    [UnityTest]
    public IEnumerator PayoutHappensOnce()
    {
        var shift = Object.FindFirstObjectByType<WeddingShift>();
        yield return null;
        int before = CrewState.Current.savings;
        shift.Payout();
        int after = CrewState.Current.savings;
        shift.Payout();
        shift.Payout();
        Assert.GreaterOrEqual(after, before, "paid (possibly nothing after deductions)");
        Assert.AreEqual(after, CrewState.Current.savings, "a second payout adds nothing");
        Assert.IsTrue(shift.Paid);
        Assert.AreEqual(1, CrewState.Current.shiftsCompleted, "one shift counted");
    }
}
