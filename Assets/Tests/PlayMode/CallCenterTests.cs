using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// The call center shift in its real scene: language claims route calls, a
/// coworker hands a caller to the player who claimed their language with the
/// case intact, the phrase aid covers languages nobody claimed, and pay is once.
public class CallCenterTests
{
    [UnitySetUp]
    public IEnumerator LoadOffice()
    {
        CrewState.StartOver();
        var crew = CrewState.Current;
        crew.job = Jobs.CallCenter;
        crew.hired = true;
        // These tests are about routing and transfers; the training call has its own.
        crew.callTrainingDone = true;
        crew.SetLanguages(GameSession.OfflineActor, new[] { CallLanguage.Spanish });
        SceneManager.LoadScene(GameSession.CallCenterScene);
        yield return null;
        yield return null;
        CallCenterShift.Instance.PlayerSatDown();   // skip the briefing
    }

    [UnityTearDown]
    public IEnumerator Restore()
    {
        Time.timeScale = 1f;
        // Leave an empty world behind: tests that build their own rooms must not land in the office.
        var office = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("AfterCallCenterTest_" + Time.frameCount));
        yield return SceneManager.UnloadSceneAsync(office);
    }

    [UnityTest]
    public IEnumerator ACoworkerTransfersASpanishCallerToThePlayerWithTheCaseIntact()
    {
        var shift = CallCenterShift.Instance;
        var router = shift.Router;
        string me = shift.PlayerId;
        // Keep the player busy, so the caller (who skipped the language menu) reaches a coworker.
        var mine = router.Ring(CaseLibrary.Get("refund_toaster"), CallLanguage.English);
        Assert.AreEqual(me, mine.Owner, "first in the rotation");
        var call = router.Ring(CaseLibrary.Get("setup_plug"), CallLanguage.Spanish, languageKnown: false);
        string coworker = call.Owner;
        Assert.AreNotEqual(me, coworker, "a coworker picks it up");
        Assert.IsTrue(router.Worker(coworker).Simulated);

        Time.timeScale = 4f;
        float waited = 0f;
        while (call.Transfers.Count == 0 && waited < 15f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = 1f;
        Assert.AreEqual(1, call.Transfers.Count, "they heard Spanish and passed it on");
        Assert.AreEqual(coworker, call.Transfers[0].From);
        Assert.AreEqual(me, call.Transfers[0].To, "to the one who claimed Spanish");
        Assert.AreEqual(me, call.ReservedFor, "waiting for the player, who is busy");
        Assert.IsTrue(call.Facts.Contains("greeted") && call.Facts.Contains("order_heard"), "what the coworker learned travels with the call");
        int lines = call.Transcript.Count;

        router.Answer(mine.Id, me);
        router.End(mine, CallOutcome.Resolved);
        yield return null;
        Assert.AreEqual(me, call.Owner, "it rings at the player's desk now");
        Assert.AreEqual(CallState.Ringing, call.State);
        shift.Do(me, "answer");
        Assert.AreEqual(CallState.Active, call.State);
        Assert.Greater(call.Transcript.Count, lines, "the customer speaks up on reaching the player");
        Assert.AreEqual("transferred", call.Transcript.Last().Key);
        Assert.AreEqual(CallLanguage.Spanish, call.Transcript.Last().Language, "in Spanish");
        Assert.IsFalse(call.PhraseAid, "the player claimed Spanish: no phrase aid");
    }

    [UnityTest]
    public IEnumerator ALanguageNobodyClaimedComesWithThePhraseAid()
    {
        var shift = CallCenterShift.Instance;
        foreach (var w in shift.Router.Workers.Where(w => w.Simulated))
            shift.Router.SetBreak(w.Id, true);
        var call = shift.Router.Ring(CaseLibrary.Get("delivery_kettle"), CallLanguage.Arabic);
        Assert.AreEqual(shift.PlayerId, call.Owner, "solo: the call is assigned to the player");
        Assert.IsTrue(call.PhraseAid);
        shift.Do(shift.PlayerId, "answer");
        shift.Do(shift.PlayerId, "greet");
        var line = call.Transcript.Last();
        Assert.AreEqual(CallLanguage.Arabic, line.Language, "the customer speaks Arabic");
        Assert.AreEqual(CaseLibrary.Get("delivery_kettle").Line("greet", CallLanguage.English), call.English(line), "and the aid has the English");
        Assert.AreEqual(0f, shift.Boss.Suspicion.Value, "a foreign-language call is not evidence of anything");
        yield return null;
    }

    [UnityTest]
    public IEnumerator PayoutHappensOnce()
    {
        var shift = CallCenterShift.Instance;
        yield return null;
        int before = CrewState.Current.savings;
        shift.Payout();
        int after = CrewState.Current.savings;
        shift.Payout();
        shift.Payout();
        Assert.Greater(after, before, "the base pay at least");
        Assert.AreEqual(after, CrewState.Current.savings, "a second payout adds nothing");
        Assert.IsTrue(shift.Paid);
        Assert.AreEqual(1, CrewState.Current.shiftsCompleted);
    }
}
