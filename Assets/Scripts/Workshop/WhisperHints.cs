using System.Collections.Generic;
using System.Linq;

/// <summary>
/// What the WhisperBox says in your ear: a short, structured nudge built only
/// from what the call has already shown you (the facts heard, the record you
/// looked up). It suggests the next question or the help page that covers the
/// case; it never states an order detail you have not been told, and never
/// picks the fix. Every hint is certain: it does not guess, and it does not lie.
/// On a call in another language it also glosses the key words of the clue the
/// customer gave, once they have given it.
/// </summary>
public static class WhisperHints
{
    /// The help page that covers a case.
    public static string PageFor(CaseDefinition def) => def.Family switch
    {
        CaseFamily.Delivery => "Deliveries",
        CaseFamily.Refund => "Refunds",
        _ => def.Id == "setup_vacuum" ? "DustBot lights" : "Smart plug lights",
    };

    /// The key words of each case's clue in each language (they are in the customer's line).
    public static readonly Dictionary<(string caseId, string language), (string words, string english)> Glossary = new()
    {
        [("delivery_kettle", CallLanguage.Spanish)] = ("etiqueta creada", "label created"),
        [("delivery_kettle", CallLanguage.Arabic)] = ("تم إنشاء الملصق", "label created"),
        [("delivery_speaker", CallLanguage.Spanish)] = ("piso 3B", "flat 3B"),
        [("delivery_speaker", CallLanguage.Arabic)] = ("الشقة 3B", "flat 3B"),
        [("setup_plug", CallLanguage.Spanish)] = ("parpadea en azul, muy rápido", "blinking blue, very fast"),
        [("setup_plug", CallLanguage.Arabic)] = ("يومض باللون الأزرق بسرعة كبيرة", "blinking blue, very fast"),
        [("setup_vacuum", CallLanguage.Spanish)] = ("rojo, rojo, luego verde", "red, red, then green"),
        [("setup_vacuum", CallLanguage.Arabic)] = ("أحمر، أحمر، ثم أخضر", "red, red, then green"),
        [("refund_toaster", CallLanguage.Spanish)] = ("del 18 de octubre", "from 18 October"),
        [("refund_toaster", CallLanguage.Arabic)] = ("بتاريخ 18 أكتوبر", "dated 18 October"),
        [("refund_headphones", CallLanguage.Spanish)] = ("el 15 de septiembre", "on 15 September"),
        [("refund_headphones", CallLanguage.Arabic)] = ("في 15 سبتمبر", "on 15 September"),
    };

    public sealed class Hint
    {
        public string Text;
        /// The help page to open and highlight, or null.
        public string Page;
        /// The glossed key words in the customer's language (shown in their script), or null.
        public string Words;
        public string WordsLanguage;
        public string WordsEnglish;
    }

    public static Hint For(CallCase call)
    {
        if (call == null || !call.Live)
            return new Hint { Text = "No call. Nothing to whisper about." };
        if (call.State == CallState.Ringing)
            return new Hint { Text = "It's ringing. Answer it." };
        var facts = call.Facts;
        var def = call.Def;
        if (!facts.Contains("greeted"))
            return new Hint { Text = "Greet them and let them explain the problem." };
        if (facts.Contains("resolved"))
            return new Hint { Text = "That's fixed. Check there's nothing else, then close the call." };
        bool needsOrder = def.Requires.Contains("order_found");
        if (needsOrder && !facts.Contains("order_heard"))
            return new Hint { Text = "Problem heard. Ask for the order number." };
        if (needsOrder && !facts.Contains("order_found"))
            return new Hint { Text = "You have the order number. Type it into the account search." };
        string found = needsOrder ? $"Order found: {RecordFlag(def)}. " : "A setup problem, nothing wrong with the order. ";
        if (!facts.Contains(def.DetailFact))
        {
            string ask = CallConversation.Questions.First(q => q.fact == def.DetailFact).label;
            return new Hint { Text = found + $"Next: \"{ask}\"." };
        }
        var hint = new Hint
        {
            Text = $"{CallConversation.FactLabel(def.DetailFact)[..1].ToUpperInvariant()}{CallConversation.FactLabel(def.DetailFact)[1..]}. " +
                   $"You have what you need: the '{PageFor(def)}' help page covers it. The fix is your call.",
            Page = PageFor(def),
        };
        if (call.Language != CallLanguage.English && Glossary.TryGetValue((def.Id, call.Language), out var gloss))
        {
            hint.Words = gloss.words;
            hint.WordsLanguage = call.Language;
            hint.WordsEnglish = gloss.english;
        }
        return hint;
    }

    /// The line of the order record that stands out (already on your screen once you looked it up).
    static string RecordFlag(CaseDefinition def)
    {
        var lines = def.Record.Split('\n');
        // The system shouts what matters ("NOT collected", "HELD at"): a word in capitals.
        var flagged = lines.FirstOrDefault(l => l.Split(' ').Any(w => w.Count(char.IsLetter) > 2 && w.Where(char.IsLetter).All(char.IsUpper)));
        return (flagged ?? lines[^1]).Trim().TrimEnd('.');
    }
}
