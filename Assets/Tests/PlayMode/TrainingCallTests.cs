using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// The call center's first shift: the guided training call (English whatever the
/// CV says, at your desk, patient, unwatched, unscored), what it holds back until
/// it is done or skipped, and replaying it. Plus where in-call help may come
/// from: a WhisperBox only helps when equipped and charged.
/// </summary>
public class TrainingCallTests
{
    CallCenterShift _shift;

    IEnumerator Office(bool trainingDone, System.Action<CrewState> extra = null)
    {
        CrewState.StartOver();
        var crew = CrewState.Current;
        crew.job = Jobs.CallCenter;
        crew.hired = true;
        crew.callTrainingDone = trainingDone;
        crew.SetLanguages(GameSession.OfflineActor, new[] { CallLanguage.Spanish });
        extra?.Invoke(crew);
        SceneManager.LoadScene(GameSession.CallCenterScene);
        yield return null;
        yield return null;
        _shift = CallCenterShift.Instance;
        _shift.PlayerSatDown();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Restore()
    {
        Time.timeScale = 1f;
        var office = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("AfterTrainingTest_" + Time.frameCount));
        yield return SceneManager.UnloadSceneAsync(office);
    }

    string Me => _shift.PlayerId;

    void Do(string intent, string arg = "") => _shift.Do(Me, intent, arg);

    [UnityTest]
    public IEnumerator TheFirstShiftStartsWithAnEnglishTrainingCallThatHoldsBackTheCvLanguage()
    {
        yield return Office(false);
        var training = _shift.CallOf(Me);
        Assert.IsNotNull(training, "the training call rings at once");
        Assert.IsTrue(training.Training);
        Assert.AreEqual(CallLanguage.English, training.Language, "English, though the CV claims Spanish");

        // A Spanish caller meanwhile goes to someone else, not into the trainee's queue.
        var spanish = _shift.Router.Ring(CaseLibrary.Get("refund_toaster"), CallLanguage.Spanish);
        Assert.AreNotEqual(Me, spanish.Owner);
        Assert.AreNotEqual(Me, spanish.ReservedFor);

        // The supervisor lets the trainee learn: she does not come over to listen.
        Do("answer");
        Time.timeScale = 8f;
        for (float t = 0f; t < 8f; t += Time.unscaledDeltaTime)
        {
            Assert.AreNotEqual(Me, _shift.Boss.ListeningTo, "listening in on the training call");
            yield return null;
        }
        Time.timeScale = 1f;
        Assert.Greater(training.Patience, 0.85f, "a patient training customer");

        // The whole loop, as the console's steps lead it.
        Do("greet");
        Do("ask_order");
        Do("lookup", training.Def.OrderId);
        Do("ask", training.Def.DetailFact);
        Do("act", training.Def.CorrectAction);
        Do("close");
        Assert.AreEqual(CallOutcome.Resolved, training.Outcome);
        Assert.IsTrue(CrewState.Current.callTrainingDone, "done, and saved");
        Assert.IsFalse(_shift.Router.Worker(Me).TrainingPending);
        Assert.AreEqual(0f, _shift.Boss.Suspicion.Value, 0.001f, "nothing on the training call counted against them");

        // Now the CV's language comes to them (at once, or as soon as they are free:
        // an English caller who queued during training may have reached them first).
        _shift.Router.End(spanish, CallOutcome.Resolved);
        var next = _shift.Router.Ring(CaseLibrary.Get("refund_toaster"), CallLanguage.Spanish);
        var me = _shift.Router.Worker(Me);
        Assert.IsTrue(next.Owner == Me || (next.Owner == "" && _shift.Router.Eligible(next, out _).Contains(me)),
            $"the Spanish caller is routed to the player (owner '{next.Owner}')");
    }

    [UnityTest]
    public IEnumerator TrainingCanBeSkippedAndReplayed()
    {
        yield return Office(false);
        var training = _shift.TrainingCall;
        Assert.IsNotNull(training);
        _shift.SkipTraining();
        Assert.IsFalse(training.Live, "skipping ends it");
        Assert.IsNull(_shift.TrainingCall);
        Assert.IsTrue(CrewState.Current.callTrainingDone);
        Assert.IsNull(_shift.CallOf(Me), "nothing left at the desk");
        // Replay, from "How does this work?", once the desk is free.
        Assert.IsTrue(_shift.StartTraining());
        Assert.IsNotNull(_shift.TrainingCall);
        Assert.AreEqual(CallLanguage.English, _shift.TrainingCall.Language);
        Assert.IsFalse(_shift.StartTraining(), "one at a time");
    }

    [UnityTest]
    public IEnumerator AfterTrainingTheShiftStartsWithoutIt()
    {
        yield return Office(true);
        Assert.IsNull(_shift.TrainingCall, "a returning agent goes straight to the phones");
        Assert.IsFalse(_shift.Router.Worker(Me).TrainingPending);
    }

    [UnityTest]
    public IEnumerator AWhisperBoxLeftAtHomeOrFlatGivesNoHelp()
    {
        // Owned, not equipped: nothing comes to work.
        yield return Office(true, crew => crew.gadgets.Add(new GadgetInstance { id = "G-HOME", recipe = PartCatalog.WhisperBox, charge = 1f }));
        Assert.IsNull(GadgetKit.Instance, "a WhisperBox left at home does nothing at work");
        yield return Restore();

        // Equipped but flat: pressing it gives no hint and says why.
        yield return Office(true, crew =>
        {
            crew.gadgets.Add(new GadgetInstance { id = "G-FLAT", recipe = PartCatalog.WhisperBox, charge = 0f });
            WorkshopLedger.Equip(crew, "G-FLAT", GameSession.OfflineActor);
        });
        Assert.IsNotNull(GadgetKit.Instance);
        var call = _shift.Router.Ring(CaseLibrary.Get("delivery_kettle"), CallLanguage.Spanish);
        Do("answer");
        Do("greet");
        GadgetKit.Instance.Press();
        Assert.IsTrue(string.IsNullOrEmpty(GadgetKit.Instance.LastHint), "a flat WhisperBox whispers nothing");
        Assert.AreEqual(CallTranslation.Source.None, CallTranslation.For(call.Transcript[^1], false), "and nothing translates the call");
    }
}
