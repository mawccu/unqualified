using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// The landlord's bill exchange in the real apartment scene: one owner at a time,
/// one commit, and a cancel before contact puts everything back.
public class BillHandoverTests
{
    GameSession _session;
    OpeningDirector _director;

    [UnitySetUp]
    public IEnumerator LoadApartmentAtTheOffer()
    {
        CrewState.StartOver();
        PlayerPrefs.SetInt("intro_seen", 1); // short wake-up
        SceneManager.LoadScene(GameSession.ApartmentScene);
        yield return null;
        yield return null;
        _session = GameSession.Instance;
        _director = OpeningDirector.Instance;
        Assert.IsNotNull(_session, "the apartment has a session");
        Assert.IsNotNull(_director, "the apartment has a director");
        yield return WaitFor(() => _director.Stage == OpeningStage.Knocking, 15f);

        // Open the door from inside, then wait for him to hold the bill out.
        var player = _session.Player;
        player.Teleport(new Vector3(2.0f, 0.05f, -2.3f));
        player.SetLook(180f, 10f);
        Assert.AreEqual(InteractionResult.Ok, _session.Service.Request(Use("front_door")), "the door can be answered");
        yield return WaitFor(() => _director.Stage == OpeningStage.BillOffered, 20f);
        yield return new WaitForSeconds(0.5f);
    }

    InteractionRequest Use(string id) => new() { ActorId = _session.Player.ActorId, Action = InteractionAction.Use, TargetId = id, ExpectedRevision = -1 };

    static IEnumerator WaitFor(System.Func<bool> condition, float seconds)
    {
        for (float t = 0f; t < seconds && !condition(); t += Time.deltaTime)
            yield return null;
        Assert.IsTrue(condition(), "timed out waiting");
    }

    [UnityTest]
    public IEnumerator TheBillChangesHandsOnceAtContact()
    {
        var player = _session.Player;
        var bill = _director.Bill;
        // Close enough to reach his outstretched hand.
        player.Teleport(new Vector3(2.0f, 0.05f, -2.75f));
        player.SetLook(180f, 18f);
        yield return null;
        Assert.AreEqual("landlord", bill.HolderId, "before: the landlord holds it");
        Assert.AreEqual(InteractionResult.Ok, _session.Service.Request(Use("landlord")), "asking for the bill starts the exchange");
        Assert.AreEqual(InteractionResult.Busy, _director.BeginHandover(player), "a second request while the exchange runs is refused");
        Assert.AreEqual("landlord", bill.HolderId, "during the reach it is still his");
        Assert.IsFalse(player.CanControl, "the player is steadied for the short reach");

        // Through the whole exchange the sheet moves smoothly: no frame-to-frame jump.
        float biggestStep = 0f;
        var last = bill.transform.position;
        for (float t = 0f; t < OpeningDirector.ReachSeconds + OpeningDirector.ContactSeconds + 0.3f; t += Time.deltaTime)
        {
            yield return null;
            biggestStep = Mathf.Max(biggestStep, Vector3.Distance(bill.transform.position, last) / Mathf.Max(Time.deltaTime * 60f, 1f));
            last = bill.transform.position;
        }
        Assert.AreEqual(player.ActorId, bill.HolderId, "after contact it is the player's");
        Assert.AreEqual("rent_bill", player.HeldPropId);
        Assert.Less(biggestStep, 0.1f, "no jump at the moment it changes hands (largest per-frame move at 60 fps; a teleport would be the whole metre)");
        Assert.IsTrue(CrewState.Current.billReceived);
        Assert.AreEqual(OpeningStage.LandlordLeaving, _director.Stage);

        yield return new WaitForSeconds(OpeningDirector.RetractSeconds);
        Assert.IsTrue(player.CanControl, "controls come back after the retract");
        Assert.AreNotEqual(InteractionResult.Ok, _director.BeginHandover(player), "it cannot be taken twice");
    }

    [UnityTest]
    public IEnumerator CancellingBeforeContactPutsEverythingBack()
    {
        var player = _session.Player;
        var bill = _director.Bill;
        player.Teleport(new Vector3(2.0f, 0.05f, -2.75f));
        player.SetLook(180f, 18f);
        yield return null;
        Assert.AreEqual(InteractionResult.Ok, _session.Service.Request(Use("landlord")));
        yield return new WaitForSeconds(0.1f);
        _director.CancelHandover();
        yield return null;
        Assert.AreEqual("landlord", bill.HolderId, "still his");
        Assert.AreEqual("", player.HeldPropId);
        Assert.AreEqual(OpeningStage.BillOffered, _director.Stage, "he is still offering it");
        Assert.IsTrue(player.CanControl, "no control lock is left behind");
        yield return new WaitForSeconds(1f);
        Assert.AreEqual("landlord", bill.HolderId, "nothing commits later by itself");
        Assert.AreEqual(InteractionResult.Ok, _session.Service.Request(Use("landlord")), "it can be taken again");
    }

    [UnityTest]
    public IEnumerator TooFarAwayIsRefusedWithAReason()
    {
        var player = _session.Player;
        player.Teleport(new Vector3(0.0f, 0.05f, 0.5f));
        yield return null;
        var landlord = (Landlord)_session.Service.GetInteractable("landlord");
        Assert.AreNotEqual(InteractionResult.Ok, _session.Service.Validate(Use("landlord")), "no three-metre arms");
        Assert.AreEqual("landlord", _director.Bill.HolderId);
    }
}
