using System.Linq;
using NUnit.Framework;

/// The WhisperBox only rephrases what the call has already shown: it suggests a
/// question or a help page, never states an order detail you have not been
/// told, and never names the fix.
public class WhisperHintTests
{
    static CallCase Call(string caseId, string language = CallLanguage.English)
    {
        var call = new CallCase(1, CaseLibrary.Get(caseId), language, 0f) { State = CallState.Active, Owner = "peer_1" };
        return call;
    }

    static string[] FixLabels(CaseDefinition def) => CaseLibrary.Actions(def.Family).Select(a => a.label).ToArray();

    [Test]
    public void EachStepSuggestsTheNextThingToFindOut()
    {
        var call = Call("delivery_kettle");
        StringAssert.Contains("Greet", WhisperHints.For(call).Text);
        call.Facts.Add("greeted");
        StringAssert.Contains("order number", WhisperHints.For(call).Text);
        call.Facts.Add("order_heard");
        StringAssert.Contains("account search", WhisperHints.For(call).Text);
        call.Facts.Add("order_found");
        var hint = WhisperHints.For(call);
        StringAssert.Contains("NOT collected by the courier", hint.Text, "the record you looked up");
        StringAssert.Contains("Ask what the tracking page says", hint.Text);
        call.Facts.Add("tracking");
        hint = WhisperHints.For(call);
        Assert.AreEqual("Deliveries", hint.Page);
        StringAssert.Contains("your call", hint.Text);
    }

    [Test]
    public void ItNeverRevealsWhatYouHaveNotLearnedOrPicksTheFix()
    {
        foreach (var def in CaseLibrary.All)
        {
            var call = Call(def.Id);
            // Walk the call forward one fact at a time, checking every hint on the way.
            foreach (var fact in new[] { "greeted", "order_heard", "order_found", def.DetailFact })
            {
                string text = WhisperHints.For(call).Text;
                if (!call.Facts.Contains("order_found"))
                    foreach (var line in def.Record.Split('\n').Skip(1))
                        StringAssert.DoesNotContain(line.Trim(), text, $"{def.Id}: the record before it was looked up");
                if (!call.Facts.Contains("order_heard"))
                    StringAssert.DoesNotContain(def.OrderId, text, $"{def.Id}: the order number before the customer said it");
                foreach (var fix in FixLabels(def))
                    StringAssert.DoesNotContain(fix, text, $"{def.Id}: it named a fix");
                call.Facts.Add(fact);
            }
        }
    }

    [Test]
    public void SetupCallsSkipTheOrderAndPointAtTheRightLightsPage()
    {
        var call = Call("setup_vacuum");
        call.Facts.Add("greeted");
        StringAssert.Contains("Ask what the lights are doing", WhisperHints.For(call).Text);
        call.Facts.Add("lights");
        Assert.AreEqual("DustBot lights", WhisperHints.For(call).Page);
    }

    [Test]
    public void OnAForeignCallItGlossesTheKeyWordsOnceTheCustomerSaidThem()
    {
        foreach (var pair in WhisperHints.Glossary)
        {
            var def = CaseLibrary.Get(pair.Key.caseId);
            StringAssert.Contains(pair.Value.words, def.Line("detail", pair.Key.language), $"{pair.Key}: the words are in what the customer says");
        }
        var call = Call("delivery_kettle", CallLanguage.Spanish);
        call.Facts.UnionWith(new[] { "greeted", "order_heard", "order_found" });
        Assert.IsNull(WhisperHints.For(call).Words, "not before they said it");
        call.Facts.Add("tracking");
        var hint = WhisperHints.For(call);
        Assert.AreEqual("etiqueta creada", hint.Words);
        Assert.AreEqual("label created", hint.WordsEnglish);
    }
}
