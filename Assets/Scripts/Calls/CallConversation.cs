using System.Linq;

/// <summary>
/// What happens when an agent picks a response. Agents choose intents (there is
/// no microphone); the customer answers in their own language. Mistakes are
/// recoverable: a wrong fix costs patience and an attempt, and the call goes on.
/// Returns a line for the console's system strip, or "" when there is nothing to say.
/// </summary>
public static class CallConversation
{
    public const float WrongActionCost = 0.15f, IrrelevantCost = 0.05f, RepeatCost = 0.05f, OverheardCost = 0.08f;
    public const float ApologyGain = 0.1f, PromiseGain = 0.1f;

    /// The questions an agent can ask on any call, by the fact they reveal.
    public static readonly (string fact, string label)[] Questions =
    {
        ("tracking", "Ask what the tracking page says"),
        ("address", "Ask them to confirm the full address"),
        ("lights", "Ask what the lights are doing"),
        ("receipt", "Ask for the receipt number"),
        ("date", "Ask when they bought it"),
    };

    static readonly string[] Plain = { "greeted", "order_heard", "order_found" };

    public static string FactLabel(string fact) => fact switch
    {
        "greeted" => "problem heard",
        "order_heard" => "order number heard",
        "order_found" => "order looked up",
        "tracking" => "tracking status heard",
        "address" => "full address confirmed",
        "lights" => "light pattern heard",
        "receipt" => "receipt number heard",
        "date" => "purchase date heard",
        "resolved" => "fixed",
        _ => fact,
    };

    /// The agent greets and asks how to help: the customer explains the problem.
    public static string Greet(CallCase call, string agent)
    {
        call.Say("agent", agent, "greet", "Thanks for calling. How can I help?");
        if (call.Facts.Contains("greeted"))
        {
            call.CustomerSays("already_said", agent);
            call.Patience -= RepeatCost;
        }
        call.CustomerSays("greet", agent);
        call.Facts.Add("greeted");
        call.Note($"{call.Def.Product}: {Summary(call.Def.Family)}");
        return "";
    }

    public static string AskOrder(CallCase call, string agent)
    {
        call.Say("agent", agent, "ask_order", "Can I have your order number, please?");
        if (call.Facts.Contains("order_heard"))
        {
            call.CustomerSays("already_said", agent);
            call.Patience -= RepeatCost;
        }
        call.CustomerSays("order", agent);
        call.Facts.Add("order_heard");
        call.Note($"Order number given: {call.Def.OrderId}");
        return "";
    }

    public static string Ask(CallCase call, string agent, string fact)
    {
        var label = Questions.FirstOrDefault(q => q.fact == fact).label ?? fact;
        call.Say("agent", agent, "ask_" + fact, label + ".");
        if (fact != call.Def.DetailFact)
        {
            call.CustomerSays("irrelevant", agent);
            call.Patience -= IrrelevantCost;
            return "";
        }
        if (call.Facts.Contains(fact))
        {
            call.CustomerSays("already_said", agent);
            call.Patience -= RepeatCost;
        }
        call.CustomerSays("detail", agent);
        call.Facts.Add(fact);
        call.Note($"Customer: {FactLabel(fact)}");
        return "";
    }

    /// Typing an order number into the company system.
    public static string LookUp(CallCase call, string agent, string typed, out string record)
    {
        typed = (typed ?? "").Trim().TrimStart('#');
        record = "";
        if (typed == "")
            return call.Facts.Contains("order_heard")
                ? "The search box is empty. Type the order number they gave you, or press \"Use in search\" beside it."
                : "The search box is empty. Ask the customer for their order number first.";
        if (typed != call.Def.OrderId)
            return call.Facts.Contains("order_heard")
                ? $"Searched for order {typed}: no such order. Check it against the number the customer gave (in the case summary)."
                : $"Searched for order {typed}: no such order. You haven't asked the customer for their order number yet.";
        call.Facts.Add("order_found");
        call.Note($"Looked up order {call.Def.OrderId}");
        record = call.Def.Record;
        return "";
    }

    public static string Apologise(CallCase call, string agent)
    {
        call.Say("agent", agent, "apologise", "I'm really sorry about this.");
        if (call.Apologised)
        {
            call.CustomerSays("apology_again", agent);
            return "";
        }
        call.Apologised = true;
        call.Patience = System.Math.Min(1f, call.Patience + ApologyGain);
        call.CustomerSays("apology", agent);
        return "";
    }

    /// A promise the system may not back up. It is written down, and can be checked.
    public static string MakePromise(CallCase call, string agent)
    {
        string text = call.Def.PromiseIntent.Replace("Promise ", "I promise ");
        call.Say("agent", agent, "promise", text + ".");
        if (call.Promises.Count == 0)
            call.Patience = System.Math.Min(1f, call.Patience + PromiseGain);
        call.Promises.Add(new Promise { Text = call.Def.PromiseIntent, Supported = call.Def.PromiseSupported, By = agent });
        call.CustomerSays("promise", agent);
        call.Note($"Promised the customer: {call.Def.PromiseIntent.Replace("Promise ", "").Replace("promise ", "")}");
        return "";
    }

    /// Asking the person at the next desk. The customer hears it unless muted or on hold.
    public static string AskColleague(CallCase call, string agent, out string advice)
    {
        advice = Advice(call);
        if (call.State == CallState.Active && !call.Muted)
        {
            call.CustomerSays("who_is_that", agent);
            call.Patience -= OverheardCost;
            return "The customer heard that. (Mute or hold first.)";
        }
        return "";
    }

    static string Advice(CallCase call)
    {
        var page = CaseLibrary.HelpPages.FirstOrDefault(p => p.title == call.Def.Family switch
        {
            CaseFamily.Delivery => "Deliveries",
            CaseFamily.Refund => "Refunds",
            _ => call.Def.Id == "setup_vacuum" ? "DustBot lights" : "Smart plug lights",
        });
        return call.Def.DetailFact switch
        {
            "tracking" => "\"Ask what the tracking says. If it never left, rebook the courier.\"",
            "address" => "\"Held at depot? That's nearly always the address. Confirm it, then release it.\"",
            "lights" => $"\"What are the lights doing? It's all in the '{page.title}' page.\"",
            "receipt" => "\"Got the receipt? Under 30 days is a straight refund.\"",
            "date" => "\"When did they buy it? Over 30 days, it's warranty, not refund.\"",
            _ => "\"No idea, mate.\"",
        };
    }

    /// One of the company system's actions on the account.
    public static string Act(CallCase call, string agent, string action, string label)
    {
        if (call.Resolved)
            return "Already done. Ask if there is anything else, then close.";
        if (call.Def.Family != CaseFamily.Setup && !call.Facts.Contains("order_found"))
            return "The system needs the order first. Look it up.";
        if (action == call.Def.CorrectAction)
        {
            var missing = call.Def.Requires.Where(r => !call.Facts.Contains(r)).ToList();
            if (missing.Count > 0)
                return "Not yet: " + string.Join(", ", missing.Select(FactLabel)) + " first.";
        }
        call.Say("agent", agent, "action_" + action, label + ".");
        if (action != call.Def.CorrectAction)
        {
            call.Attempts++;
            call.Patience -= WrongActionCost;
            call.CustomerSays("bad", agent);
            call.Note($"Tried: {label} (did not help)");
            return "";
        }
        call.ActionTaken = action;
        call.Facts.Add("resolved");
        call.CustomerSays("good", agent);
        call.Note($"Done: {label}");
        return "";
    }

    /// "Is there anything else?" Ends the call only when the problem is fixed.
    public static string Close(CallRouter router, CallCase call, string agent)
    {
        call.Say("agent", agent, "close", "Is there anything else I can help with?");
        if (!call.Resolved)
        {
            call.CustomerSays("confirm_unresolved", agent);
            call.Patience -= RepeatCost;
            return "Not fixed yet. You can still mark it resolved (it isn't), or escalate.";
        }
        call.CustomerSays("confirm_happy", agent);
        router.End(call, CallOutcome.Resolved);
        return "";
    }

    /// The phone's End button. Ending is not resolving: a fixed call closes as
    /// resolved (the customer is happy to go); an unfixed one is dropped, which
    /// counts against you like a customer hanging up.
    public static string EndCall(CallRouter router, CallCase call, string agent)
    {
        if (call.Resolved)
        {
            call.Say("agent", agent, "close", "Is there anything else I can help with?");
            call.CustomerSays("confirm_happy", agent);
            router.End(call, CallOutcome.Resolved);
            return "";
        }
        call.Say("agent", agent, "end", "I'm going to have to end the call here. Goodbye.");
        call.Note("Ended the call with the problem unfixed");
        router.End(call, CallOutcome.Dropped);
        return "";
    }

    /// What a muted agent cannot do: anything the customer would have to hear.
    public static bool NeedsVoice(string intent) => intent is "greet" or "ask_order" or "ask" or "apologise" or "promise" or "close" or "close_anyway" or "escalate";

    /// Writes "resolved" on a call that is not. The record will not agree for long.
    public static string CloseAnyway(CallRouter router, CallCase call, string agent)
    {
        call.Say("agent", agent, "close_anyway", "Great, that's all sorted then. Goodbye!");
        call.Note("Marked as resolved");
        router.End(call, call.Resolved ? CallOutcome.Resolved : CallOutcome.FakeClosure);
        return "";
    }

    /// The honest way out: hand it to second line and tell the customer so.
    public static string Escalate(CallRouter router, CallCase call, string agent)
    {
        call.Say("agent", agent, "escalate", "I'm passing this to our specialist team. They'll call you back.");
        call.CustomerSays("escalated", agent);
        call.Note("Escalated to second line");
        router.End(call, call.Resolved ? CallOutcome.Resolved : CallOutcome.Escalated);
        return "";
    }

    /// What the next agent hears when a transferred call reaches them.
    public static void Arrived(CallCase call, string agent)
    {
        if (call.Transfers.Count(t => t.Reason != "disconnected") >= 2)
            call.CustomerSays("bounce", agent);
        else if (call.Transfers.Count > 0)
            call.CustomerSays("transferred", agent);
        else
            call.CustomerSays("ring_greeting", agent);
    }

    static string Summary(CaseFamily family) => family switch
    {
        CaseFamily.Delivery => "delivery problem",
        CaseFamily.Setup => "won't work",
        _ => "wants money back",
    };

    /// 0..1, how well the call went (for pay and the supervisor).
    public static float Quality(CallCase call)
    {
        if (call.Outcome != CallOutcome.Resolved)
            return call.Outcome == CallOutcome.Escalated ? 0.25f : 0f;
        float q = 0.55f + 0.35f * call.Patience - 0.12f * call.Attempts;
        if (call.Promises.Any(p => !p.Supported)) q -= 0.2f;
        return System.Math.Clamp(q, 0.1f, 1f);
    }
}
