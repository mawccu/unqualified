using System.Linq;
using NUnit.Framework;

/// Call routing, transfers, case content and the save format, without a scene.
public class CallRoutingTests
{
    static CaseDefinition Kettle => CaseLibrary.Get("delivery_kettle");
    static CaseDefinition Speaker => CaseLibrary.Get("delivery_speaker");

    static CallRouter Office(out CallWorker you, out CallWorker dana, out CallWorker omar)
    {
        var router = new CallRouter();
        you = router.AddWorker("peer_1", "You", new[] { CallLanguage.Spanish }, false, false);
        dana = router.AddWorker("npc_dana", "Dana", null, true, true);
        omar = router.AddWorker("npc_omar", "Omar", null, false, true);
        return router;
    }

    [Test]
    public void ACallGoesToWhoeverClaimedItsLanguage()
    {
        var router = Office(out var you, out _, out _);
        var call = router.Ring(Kettle, CallLanguage.Spanish);
        Assert.AreEqual(you.Id, call.Owner, "the only Spanish speaker gets the Spanish call");
        Assert.AreEqual(CallState.Ringing, call.State);
        Assert.IsFalse(call.PhraseAid, "someone claimed Spanish: no phrase aid");
    }

    [Test]
    public void EnglishCallsRotateFairly()
    {
        var router = Office(out var you, out var dana, out var omar);
        var owners = new System.Collections.Generic.List<string>();
        for (int i = 0; i < 3; i++)
        {
            router.Tick(1f);
            owners.Add(router.Ring(Kettle, CallLanguage.English).Owner);
        }
        CollectionAssert.AreEquivalent(new[] { you.Id, dana.Id, omar.Id }, owners, "three calls, three different people");
    }

    [Test]
    public void BusySpeakersMeanTheCallQueuesForThem()
    {
        var router = Office(out var you, out _, out _);
        var first = router.Ring(Kettle, CallLanguage.Spanish);
        var second = router.Ring(Kettle, CallLanguage.Spanish);
        Assert.AreEqual(you.Id, first.Owner);
        Assert.AreEqual("", second.Owner, "the only Spanish speaker is busy");
        Assert.AreEqual(CallState.Queued, second.State, "so it waits for them, not for an English speaker");
        router.Answer(first.Id, you.Id);
        router.End(first, CallOutcome.Resolved);
        Assert.AreEqual(you.Id, second.Owner, "and comes through when they are free");
    }

    [Test]
    public void NobodyClaimedArabicSoAnyoneTakesItWithAPhraseAid()
    {
        var router = Office(out _, out _, out _);
        var call = router.Ring(Kettle, CallLanguage.Arabic);
        Assert.AreNotEqual("", call.Owner, "the customer is not left waiting forever");
        Assert.IsTrue(call.PhraseAid);
    }

    [Test]
    public void TechnicalVariantsGoToTheTechnicalExpert()
    {
        var router = Office(out _, out var dana, out _);
        for (int i = 0; i < 4; i++)
        {
            var call = router.Ring(Speaker, CallLanguage.English);
            Assert.AreEqual(dana.Id, call.Owner, "Dana claimed technical support");
            router.Answer(call.Id, dana.Id);
            router.End(call, CallOutcome.Resolved);
        }
    }

    [Test]
    public void ATransferKeepsEverythingAboutTheCall()
    {
        var router = Office(out var you, out var dana, out var omar);
        // Whoever picks up works the call for a while, then hands it on.
        var call = router.Ring(Kettle, CallLanguage.English);
        string first = call.Owner;
        router.Answer(call.Id, first);
        CallConversation.Greet(call, first);
        CallConversation.AskOrder(call, first);
        CallConversation.LookUp(call, first, "48213", out _);
        CallConversation.Act(call, first, "reship", "Send a replacement");
        CallConversation.MakePromise(call, first);
        var facts = call.Facts.ToList();
        var notes = call.Notes.ToList();
        int lines = call.Transcript.Count;
        float patience = call.Patience;

        string to = first == you.Id ? dana.Id : you.Id;
        Assert.AreEqual(TransferResult.Ok, router.Transfer(call.Id, first, to, "speaks the language"));
        Assert.AreEqual(to, call.Owner, "the recipient owns it now");
        Assert.AreEqual(CallState.Ringing, call.State, "ringing at their desk");
        Assert.IsNull(router.CallOf(first), "and the sender is free");
        CollectionAssert.AreEquivalent(facts, call.Facts, "facts kept");
        CollectionAssert.AreEqual(notes, call.Notes, "notes kept");
        Assert.AreEqual(lines, call.Transcript.Count, "transcript kept");
        Assert.AreEqual(1, call.Attempts, "the failed attempt is still on record");
        Assert.AreEqual(1, call.Promises.Count, "and so is the promise");
        Assert.AreEqual(patience - CallRouter.TransferCost, call.Patience, 1e-4, "patience carries over (minus the wait)");
        Assert.AreEqual(1, call.Transfers.Count);
        Assert.AreEqual(first, call.Transfers[0].From);
        Assert.AreEqual(to, call.Transfers[0].To);
        Assert.IsTrue(router.Answer(call.Id, to));
        Assert.IsFalse(router.Answer(call.Id, first), "the old owner cannot answer it");
    }

    [Test]
    public void TransferToSomeoneBusyQueuesItForThem()
    {
        var router = Office(out var you, out var dana, out _);
        var busy = router.Ring(Speaker, CallLanguage.English);
        Assert.AreEqual(dana.Id, busy.Owner);
        var call = router.Ring(Kettle, CallLanguage.Spanish);
        router.Answer(call.Id, you.Id);
        Assert.AreEqual(TransferResult.Queued, router.Transfer(call.Id, you.Id, dana.Id));
        Assert.AreEqual("", call.Owner);
        Assert.AreEqual(dana.Id, call.ReservedFor);
        router.Answer(busy.Id, dana.Id);
        router.End(busy, CallOutcome.Resolved);
        Assert.AreEqual(dana.Id, call.Owner, "she gets it as soon as she is free");
    }

    [Test]
    public void BouncingACustomerBackIsRefusedAndChangesNothing()
    {
        var router = Office(out var you, out var dana, out _);
        var call = router.Ring(Kettle, CallLanguage.Spanish);
        router.Answer(call.Id, you.Id);
        Assert.AreEqual(TransferResult.Ok, router.Transfer(call.Id, you.Id, dana.Id));
        router.Answer(call.Id, dana.Id);
        float patience = call.Patience;
        Assert.AreEqual(TransferResult.Bouncing, router.Transfer(call.Id, dana.Id, you.Id), "straight back within the window");
        Assert.AreEqual(dana.Id, call.Owner, "refused transfers change nothing");
        Assert.AreEqual(1, call.Transfers.Count);
        Assert.AreEqual(patience, call.Patience);
        Assert.AreEqual(TransferResult.NotYours, router.Transfer(call.Id, you.Id, dana.Id), "only the owner can transfer");
        router.Tick(CallRouter.BounceWindow + 1f);
        Assert.AreEqual(TransferResult.Ok, router.Transfer(call.Id, dana.Id, you.Id), "later it is allowed");
    }

    [Test]
    public void TooManyTransfersAreRefused()
    {
        var router = new CallRouter();
        var ids = new[] { "a", "b", "c", "d", "e" };
        foreach (var id in ids)
            router.AddWorker(id, id, null, false, true);
        var call = router.Ring(Kettle, CallLanguage.English);
        for (int i = 0; i < CallRouter.MaxTransfers; i++)
        {
            string from = call.Owner;
            router.Answer(call.Id, from);
            string to = ids.First(x => x != from && router.CallOf(x) == null && call.Transfers.All(t => t.From != x));
            Assert.AreEqual(TransferResult.Ok, router.Transfer(call.Id, from, to));
        }
        router.Answer(call.Id, call.Owner);
        string last = ids.First(x => x != call.Owner && call.Transfers.All(t => t.From != x));
        Assert.AreEqual(TransferResult.TooMany, router.Transfer(call.Id, call.Owner, last));
    }

    [Test]
    public void ADisconnectedWorkersCallGoesBackInTheQueue()
    {
        var router = Office(out var you, out _, out _);
        var call = router.Ring(Kettle, CallLanguage.Spanish);
        router.Answer(call.Id, you.Id);
        CallConversation.Greet(call, you.Id);
        router.Disconnect(you.Id);
        Assert.AreNotEqual(you.Id, call.Owner, "not left with someone who has gone");
        Assert.IsTrue(call.Live);
        Assert.IsTrue(call.PhraseAid, "nobody left speaks Spanish, so the next agent gets the phrase aid");
        Assert.IsTrue(call.Facts.Contains("greeted"), "and what was learned is kept");
        Assert.AreEqual("disconnected", call.Transfers.Last().Reason);
    }

    [Test]
    public void IgnoringTheCustomerLongEnoughLosesThem()
    {
        var router = Office(out var you, out _, out _);
        var call = router.Ring(Kettle, CallLanguage.Spanish);
        for (int i = 0; i < 400 && call.Live; i++)
            router.Tick(1f);
        Assert.AreEqual(CallOutcome.HungUp, call.Outcome);
    }

    [Test]
    public void OneEnglishDeliveryCallFromHelloToGoodbye()
    {
        var router = Office(out var you, out _, out _);
        var call = router.Ring(Kettle, CallLanguage.Spanish);
        Assert.IsTrue(router.Answer(call.Id, you.Id));
        CallConversation.Greet(call, you.Id);
        CallConversation.AskOrder(call, you.Id);
        Assert.AreEqual("The system needs the order first. Look it up.", CallConversation.Act(call, you.Id, "rebook_voucher", "Rebook"));
        string typo = CallConversation.LookUp(call, you.Id, "48231", out _);
        StringAssert.Contains("48231: no such order", typo, "a typo finds nothing, and says what was searched");
        Assert.IsFalse(call.Facts.Contains("order_found"));
        Assert.AreEqual("", CallConversation.LookUp(call, you.Id, "48213", out var record));
        StringAssert.Contains("NOT collected", record);
        CallConversation.Ask(call, you.Id, "tracking");
        CallConversation.Act(call, you.Id, "reship", "Send a replacement");
        Assert.AreEqual(1, call.Attempts, "a wrong fix is recorded...");
        Assert.IsTrue(call.Live, "...and the call goes on");
        CallConversation.Act(call, you.Id, "rebook_voucher", "Rebook courier + $5 voucher");
        Assert.IsTrue(call.Resolved);
        CallConversation.Close(router, call, you.Id);
        Assert.AreEqual(CallOutcome.Resolved, call.Outcome);
        Assert.Greater(CallConversation.Quality(call), 0.5f);
        Assert.AreEqual(Kettle.Line("good", CallLanguage.Spanish), call.Transcript.Last(l => l.Speaker == "customer" && l.Key == "good").Text, "the customer spoke Spanish throughout");
        Assert.IsTrue(call.Transcript.Where(l => l.Speaker == "customer").All(l => l.Language == CallLanguage.Spanish));
    }

    [Test]
    public void ClosingAnUnfixedCallIsAFakeClosure()
    {
        var router = Office(out var you, out _, out _);
        var call = router.Ring(Kettle, CallLanguage.English);
        router.Answer(call.Id, call.Owner);
        CallConversation.Greet(call, call.Owner);
        CallConversation.Close(router, call, call.Owner);
        Assert.IsTrue(call.Live, "'anything else?' does not end an unfixed call");
        CallConversation.CloseAnyway(router, call, call.Owner);
        Assert.AreEqual(CallOutcome.FakeClosure, call.Outcome);
        Assert.AreEqual(0f, CallConversation.Quality(call));
    }

    [Test]
    public void EveryCaseIsCompleteInEveryLanguage()
    {
        Assert.AreEqual(6, CaseLibrary.All.Length);
        foreach (var family in new[] { CaseFamily.Delivery, CaseFamily.Setup, CaseFamily.Refund })
        {
            Assert.AreEqual(1, CaseLibrary.All.Count(c => c.Family == family && !c.Technical), $"{family}: one basic");
            Assert.AreEqual(1, CaseLibrary.All.Count(c => c.Family == family && c.Technical), $"{family}: one technical");
        }
        foreach (var def in CaseLibrary.All)
        {
            foreach (var language in CallLanguage.Supported)
            {
                foreach (var key in new[] { "greet", "order", "detail", "good", "bad", "promise" })
                    Assert.IsTrue(def.Lines[key].ContainsKey(language), $"{def.Id} {key} in {language}");
                Assert.IsTrue(def.Names.ContainsKey(language), $"{def.Id} name in {language}");
                foreach (var common in new[] { "transferred", "bounce", "hangup", "already_said", "irrelevant", "escalated", "confirm_happy" })
                    Assert.IsNotEmpty(CommonLines.Line(common, language));
            }
            Assert.IsTrue(def.Line("order", CallLanguage.Arabic).Contains(def.OrderId), $"{def.Id}: the order number is in the Arabic line as digits");
            Assert.IsTrue(CaseLibrary.Actions(def.Family).Any(a => a.id == def.CorrectAction), $"{def.Id}: its fix is offered");
            Assert.IsTrue(CallConversation.Questions.Any(q => q.fact == def.DetailFact), $"{def.Id}: its question is offered");
        }
    }

    [Test]
    public void ACallerWhoSkippedTheMenuIsHeardThenTransferredToTheSpeaker()
    {
        var router = Office(out var you, out var dana, out var omar);
        router.Ring(Kettle, CallLanguage.English);                      // you are busy with this one
        var call = router.Ring(Speaker, CallLanguage.Spanish, languageKnown: false);
        Assert.AreEqual(dana.Id, call.Owner, "routed as English (to the technical expert)");
        router.Answer(call.Id, dana.Id);
        Assert.IsTrue(call.LanguageKnown, "answering reveals the language");
        Assert.IsFalse(call.PhraseAid, "someone on shift speaks Spanish");
        CallConversation.Greet(call, dana.Id);
        Assert.AreEqual(you, router.BestFor(call, dana.Id), "the Spanish speaker, even though busy");
        Assert.AreEqual(TransferResult.Queued, router.Transfer(call.Id, dana.Id, you.Id, "speaks Spanish"));
        var mine = router.CallOf(you.Id);
        router.Answer(mine.Id, you.Id);
        router.End(mine, CallOutcome.Resolved);
        Assert.AreEqual(you.Id, call.Owner);
        Assert.AreEqual(1, call.Transcript.Count(l => l.Key == "greet" && l.Speaker == "customer"), "what Dana heard is there for you");
    }

    [Test]
    public void OldSavesLoadWithNoLanguages()
    {
        const string old = "{\"day\":3,\"savings\":200,\"rentDue\":840,\"daysLeft\":5,\"introSeen\":true,\"billReceived\":true,\"hired\":true,\"cvName\":\"Sam\",\"claims\":[\"claim_cake\"],\"shiftsCompleted\":2}";
        var crew = CrewState.FromJson(old);
        Assert.AreEqual(3, crew.day);
        Assert.AreEqual(Jobs.Wedding, crew.Job, "hired before there was a choice: the wedding");
        Assert.IsNotNull(crew.languageProfiles);
        Assert.IsEmpty(crew.LanguagesOf("peer_1"), "English only");
        Assert.IsTrue(crew.HasClaim(Claims.Cake));

        crew.SetLanguages("peer_1", new[] { CallLanguage.Spanish, "klingon", CallLanguage.Spanish });
        crew.SetLanguages("peer_2", new[] { CallLanguage.Arabic });
        CollectionAssert.AreEqual(new[] { CallLanguage.Spanish }, crew.LanguagesOf("peer_1"), "only supported ids, once");
        CollectionAssert.AreEqual(new[] { CallLanguage.Arabic }, crew.LanguagesOf("peer_2"), "each player has their own");
        var again = CrewState.FromJson(UnityEngine.JsonUtility.ToJson(crew));
        CollectionAssert.AreEqual(new[] { CallLanguage.Spanish }, again.LanguagesOf("peer_1"), "round-trips");
    }
}
