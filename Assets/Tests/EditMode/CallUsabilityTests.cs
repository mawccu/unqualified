using System.Linq;
using NUnit.Framework;

/// <summary>
/// The call-center usability rules, without a scene: where English translations
/// may come from, what the phone's End button counts as, what mute and hold do,
/// the training call's routing and steps, and the cues that must never give the
/// answer away.
/// </summary>
public class CallUsabilityTests
{
    static CaseDefinition Kettle => CaseLibrary.Get("delivery_kettle");

    static CallRouter Office(out CallWorker you, out CallWorker dana)
    {
        var router = new CallRouter();
        you = router.AddWorker("peer_1", "You", new[] { CallLanguage.Spanish }, false, false);
        dana = router.AddWorker("npc_dana", "Dana", null, true, true);
        return router;
    }

    static TranscriptLine Customer(string language) => new() { Speaker = "customer", Language = language, Text = "…", Key = "greet" };

    // Translation has one explicit source --------------------------------------------------

    [Test]
    public void CustomersStayInTheirLanguageWithoutCaptions()
    {
        Assert.AreEqual(CallTranslation.Source.None, CallTranslation.For(Customer(CallLanguage.Spanish), false));
        Assert.AreEqual(CallTranslation.Source.None, CallTranslation.For(Customer(CallLanguage.Arabic), false));
    }

    [Test]
    public void NobodyClaimingTheLanguageNoLongerTranslatesTheCall()
    {
        var router = Office(out _, out _);
        var call = router.Ring(Kettle, CallLanguage.Arabic);
        Assert.IsTrue(call.PhraseAid, "nobody on shift claimed Arabic");
        call.CustomerSays("greet", call.Owner);
        Assert.AreEqual(CallTranslation.Source.None, CallTranslation.For(call.Transcript[^1], false),
            "the phrase aid used to print the English of every line; now there is only a phrasebook");
    }

    [Test]
    public void CaptionsAreTheOnlySourceAndAreLabelled()
    {
        Assert.AreEqual(CallTranslation.Source.Captions, CallTranslation.For(Customer(CallLanguage.Spanish), true));
        Assert.AreEqual("Caption: ", CallTranslation.Label(CallTranslation.Source.Captions));
        Assert.AreEqual(CallTranslation.Source.None, CallTranslation.For(Customer(CallLanguage.English), true), "nothing to translate");
        Assert.AreEqual(CallTranslation.Source.None, CallTranslation.For(new TranscriptLine { Speaker = "agent", Language = CallLanguage.English }, true));
    }

    [Test]
    public void ThePhrasebookIsGeneralWordsNotTheCall()
    {
        foreach (var language in CallLanguage.Claimable)
        {
            var terms = Phrasebook.For(language);
            Assert.That(terms.Count, Is.GreaterThan(8), language);
            // No entry is a whole customer line from any case.
            foreach (var def in CaseLibrary.All)
                foreach (var key in def.Lines.Keys)
                {
                    string line = def.Line(key, language);
                    Assert.IsFalse(terms.Any(t => t.term == line), $"{language} phrasebook holds the {def.Id} line '{key}'");
                }
            // Nor does it hold the WhisperBox's glossed clue (that stays a gadget's job).
            foreach (var gloss in WhisperHints.Glossary.Where(g => g.Key.language == language))
                Assert.IsFalse(terms.Any(t => t.term == gloss.Value.words), $"{language} phrasebook holds the clue '{gloss.Value.words}'");
        }
        Assert.AreEqual(0, Phrasebook.For(CallLanguage.English).Count);
    }

    // The phone's buttons -----------------------------------------------------------------------

    [Test]
    public void EndingAnUnfixedCallDropsItAndIsNotSuccess()
    {
        var router = Office(out var you, out _);
        var call = router.Ring(Kettle, CallLanguage.English);
        router.Answer(call.Id, call.Owner);
        CallConversation.EndCall(router, call, call.Owner);
        Assert.AreEqual(CallOutcome.Dropped, call.Outcome);
        Assert.IsFalse(call.Live);
    }

    [Test]
    public void EndingAFixedCallClosesItResolved()
    {
        var router = Office(out _, out _);
        var call = router.Ring(Kettle, CallLanguage.English);
        router.Answer(call.Id, call.Owner);
        call.ActionTaken = call.Def.CorrectAction;
        CallConversation.EndCall(router, call, call.Owner);
        Assert.AreEqual(CallOutcome.Resolved, call.Outcome, "the fix made it a success, not the button");
    }

    [Test]
    public void MutedAgentsCannotTalkButCanStillUseTheSystem()
    {
        foreach (var intent in new[] { "greet", "ask_order", "ask", "apologise", "promise", "close", "close_anyway", "escalate" })
            Assert.IsTrue(CallConversation.NeedsVoice(intent), intent);
        foreach (var intent in new[] { "lookup", "act", "colleague", "hold", "resume", "mute", "end", "transfer" })
            Assert.IsFalse(CallConversation.NeedsVoice(intent), intent);
    }

    [Test]
    public void HoldHasAPatientAllowanceThenGetsWorse()
    {
        var router = Office(out _, out _);
        var held = router.Ring(Kettle, CallLanguage.English);
        router.Answer(held.Id, held.Owner);
        var talking = router.Ring(Kettle, CallLanguage.English);
        router.Answer(talking.Id, talking.Owner);
        router.SetHold(held.Id, held.Owner, true);
        float startHeld = held.Patience, startTalking = talking.Patience;
        router.Tick(20f);
        Assert.Greater(router.HoldLeft(held), 0f, "still within the allowance");
        Assert.Less(startHeld - held.Patience, startTalking - talking.Patience, "within the allowance hold drains slower than talking");
        router.Tick(CallRouter.HoldAllowance);
        Assert.AreEqual(0f, router.HoldLeft(held), "allowance used up");
        float heldBefore = held.Patience, talkingBefore = talking.Patience;
        router.Tick(5f);
        Assert.Greater(heldBefore - held.Patience, talkingBefore - talking.Patience, "past the allowance it drains faster than talking");
    }

    // The training call ----------------------------------------------------------------------

    [Test]
    public void TheTrainingCallIsEnglishAndStaysWithTheTrainee()
    {
        var router = Office(out var you, out var dana);
        var call = router.RingTraining(Kettle, you.Id);
        Assert.AreEqual(you.Id, call.Owner);
        Assert.AreEqual(CallLanguage.English, call.Language, "whatever languages the CV claimed");
        Assert.IsTrue(call.Training);
        router.Tick(60f);
        Assert.AreEqual(you.Id, call.Owner, "an unanswered training call does not move to a free colleague");
        Assert.Greater(call.Patience, 0.9f, "and the customer is very patient");
    }

    [Test]
    public void ClaimedLanguagesWaitUntilTrainingIsDone()
    {
        var router = Office(out var you, out var dana);
        you.TrainingPending = true;
        var spanish = router.Ring(Kettle, CallLanguage.Spanish);
        Assert.AreNotEqual(you.Id, spanish.Owner, "no Spanish for the trainee yet");
        router.End(spanish, CallOutcome.Resolved);
        you.TrainingPending = false;
        var next = router.Ring(Kettle, CallLanguage.Spanish);
        Assert.AreEqual(you.Id, next.Owner, "after training the CV's language routes to them");
    }

    [Test]
    public void TrainingWalksTheSixStepsInOrder()
    {
        var router = Office(out var you, out _);
        var call = router.RingTraining(Kettle, you.Id);
        var seen = new System.Collections.Generic.List<int> { CaseFlow.Training(call, false).Number };
        router.Answer(call.Id, you.Id);
        seen.Add(CaseFlow.Training(call, false).Number);
        CallConversation.Greet(call, you.Id);
        CallConversation.AskOrder(call, you.Id);
        seen.Add(CaseFlow.Training(call, false).Number);
        CallConversation.LookUp(call, you.Id, call.Def.OrderId, out _);
        CallConversation.Ask(call, you.Id, call.Def.DetailFact);
        seen.Add(CaseFlow.Training(call, false).Number);
        seen.Add(CaseFlow.Training(call, true).Number);
        call.ActionTaken = call.Def.CorrectAction;
        seen.Add(CaseFlow.Training(call, true).Number);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, seen);
    }

    [Test]
    public void CuesNeverNameTheRemedy()
    {
        var router = Office(out var you, out _);
        foreach (var def in CaseLibrary.All)
        {
            var call = router.Ring(def, CallLanguage.English);
            router.Answer(call.Id, call.Owner);
            CallConversation.Greet(call, call.Owner);
            CallConversation.AskOrder(call, call.Owner);
            CallConversation.LookUp(call, call.Owner, def.OrderId, out _);
            CallConversation.Ask(call, call.Owner, def.DetailFact);
            string correct = CaseLibrary.Actions(def.Family).First(a => a.id == def.CorrectAction).label;
            StringAssert.DoesNotContain(correct.ToLowerInvariant(), CaseFlow.NextGoal(call).ToLowerInvariant(), def.Id);
            StringAssert.DoesNotContain(correct.ToLowerInvariant(), CaseFlow.Training(call, true).Text.ToLowerInvariant(), def.Id);
            router.End(call, CallOutcome.Resolved);
        }
    }

    [Test]
    public void SearchFailuresSayWhatWasSearchedAndWhatToDo()
    {
        var router = Office(out var you, out _);
        var call = router.Ring(Kettle, CallLanguage.English);
        router.Answer(call.Id, call.Owner);
        string early = CallConversation.LookUp(call, call.Owner, "99999", out _);
        StringAssert.Contains("99999", early);
        StringAssert.Contains("haven't asked", early);
        CallConversation.AskOrder(call, call.Owner);
        string wrong = CallConversation.LookUp(call, call.Owner, "12", out _);
        StringAssert.Contains("12", wrong);
        StringAssert.Contains("Check it against the number", wrong);
        StringAssert.Contains("Use in search", CallConversation.LookUp(call, call.Owner, "", out _));
    }
}
