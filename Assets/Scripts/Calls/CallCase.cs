using System.Collections.Generic;

public enum CallState { Ringing, Queued, Active, OnHold, Ended }

public enum CallOutcome { Open, Resolved, FakeClosure, HungUp, Escalated }

/// <summary>One line of a call as it happened. Customer lines keep their language.</summary>
public sealed class TranscriptLine
{
    public string Speaker;   // "customer", "agent" (and who), "system"
    public string Agent;     // the worker who was on the call when it was said
    public string Language;
    public string Text;
    public string Key;       // the line key, for subtitles and tests
}

public sealed class TransferRecord
{
    public string From, To, Reason;
    public float At;
}

/// <summary>Something an agent told the customer that the record can check later.</summary>
public sealed class Promise
{
    public string Text;
    public bool Supported;
    public string By;
    public bool Checked;
}

/// <summary>
/// A live call: who it is, what language they speak, what has been learned and
/// said, who has it, and how patient the customer still is. Everything here
/// travels with the call when it is transferred; nothing is reset.
/// </summary>
public sealed class CallCase
{
    public int Id;
    public CaseDefinition Def;
    public string Language;
    /// False when the caller skipped the language menu: they are routed as English
    /// until someone answers and hears them.
    public bool LanguageKnown = true;
    public CallState State = CallState.Ringing;
    public CallOutcome Outcome = CallOutcome.Open;
    /// The worker who has the call ("" while ringing or queued for anyone).
    public string Owner = "";
    /// While queued: only this worker may take it (a transfer to someone busy).
    public string ReservedFor = "";
    /// 1 = calm, 0 = hangs up.
    public float Patience = 1f;
    /// Learned so far: "greeted", "order_heard", "order_found", plus the case's detail fact.
    public readonly HashSet<string> Facts = new();
    /// The agents' notes, in order (written by the console as facts come in).
    public readonly List<string> Notes = new();
    public readonly List<TranscriptLine> Transcript = new();
    public readonly List<TransferRecord> Transfers = new();
    public readonly List<Promise> Promises = new();
    /// Wrong actions tried on the account.
    public int Attempts;
    public string ActionTaken = "";
    public bool Apologised;
    public bool Muted;
    /// Nobody on shift claimed this language: the console offers the phrase aid.
    public bool PhraseAid;
    /// A repeat call after a closure that did not fix anything.
    public bool Callback;
    public float RangAt, AnsweredAt, EndedAt;
    public float HoldSince;
    public bool HoldComplained;

    public bool Resolved => ActionTaken == Def.CorrectAction;
    public bool Live => State != CallState.Ended;
    public int TransferCount => Transfers.Count;

    public CallCase(int id, CaseDefinition def, string language, float now)
    {
        Id = id;
        Def = def;
        Language = language;
        RangAt = now;
    }

    public string CustomerName => Def.Name(Language);

    public void Say(string speaker, string agent, string key, string text) =>
        Transcript.Add(new TranscriptLine { Speaker = speaker, Agent = agent, Language = speaker == "customer" ? Language : CallLanguage.English, Text = text, Key = key });

    public void CustomerSays(string key, string agent) => Say("customer", agent, key, Def.Line(key, Language));

    public void Note(string text)
    {
        if (!Notes.Contains(text))
            Notes.Add(text);
    }

    /// The language the router sends this call by.
    public string RoutedLanguage => LanguageKnown ? Language : CallLanguage.English;

    /// A transcript line in English (for translated subtitles and the phrase aid).
    public string English(TranscriptLine line)
    {
        if (line.Speaker != "customer" || line.Language == CallLanguage.English)
            return "";
        if (Def.Lines.ContainsKey(line.Key))
            return Def.Line(line.Key, CallLanguage.English);
        return CommonLines.Line(line.Key, CallLanguage.English);
    }

    public override string ToString() => $"call {Id} ({Def.Id}, {Language}, {State}, owner '{Owner}', patience {Patience:0.00})";
}
