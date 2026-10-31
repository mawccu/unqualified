using System.Linq;

/// <summary>
/// Where a call stands, in plain words, built only from what the call has shown
/// so far. The console's "next" cue says what kind of thing to do next (identify
/// the order, find out what's wrong, choose a remedy) and never which remedy. The
/// training call walks the same loop one step at a time, and there it does point
/// at the control to use.
/// </summary>
public static class CaseFlow
{
    /// The short cue at the top of the case workspace.
    public static string NextGoal(CallCase call)
    {
        if (call == null)
            return "Wait for a call. Your phone rings on the right.";
        if (call.State == CallState.Ringing)
            return "Answer the call.";
        if (call.State == CallState.OnHold)
            return "They're on hold. Research, then resume the call.";
        var facts = call.Facts;
        var def = call.Def;
        if (call.Resolved)
            return "Fixed. Check there's nothing else, then close the call.";
        if (!facts.Contains("greeted"))
            return "Find out why they're calling: greet them.";
        bool needsOrder = def.Requires.Contains("order_found");
        if (needsOrder && !facts.Contains("order_heard"))
            return "Identify the order: ask for the order number.";
        if (needsOrder && !facts.Contains("order_found"))
            return "Look the order up in the company system.";
        if (!facts.Contains(def.DetailFact))
            return "Find out exactly what's wrong (Ask about).";
        return call.Attempts > 0
            ? "That didn't fix it. Check the help pages again and choose another remedy."
            : "Choose a remedy. The help pages say what fits.";
    }

    /// The training call's six steps (answer, ask for the missing detail, find the
    /// record, check the policy, choose the remedy, close), one at a time.
    public const int TrainingSteps = 6;

    public readonly struct Step
    {
        public readonly int Number;
        public readonly string Text;
        /// The name of the console control to point at briefly ("" for none).
        public readonly string Control;

        public Step(int number, string text, string control)
        {
            Number = number;
            Text = text;
            Control = control;
        }
    }

    public static Step Training(CallCase call, bool helpRead)
    {
        if (call == null || call.State == CallState.Ringing)
            return new Step(1, "Answer the phone: the green Answer button on the right.", "phone-answer");
        if (call.State == CallState.OnHold)
            return new Step(1, "You put them on hold. Press Resume (right) to talk again.", "phone-hold");
        var facts = call.Facts;
        var def = call.Def;
        if (!facts.Contains("greeted"))
            return new Step(2, "Greet them and let them explain the problem (Open, \"Greet\").", "say-greet");
        if (!facts.Contains("order_heard"))
            return new Step(2, "It's about an order. Ask for the missing detail: the order number.", "say-ask_order");
        if (!facts.Contains("order_found"))
            return new Step(3, "Find their record: press \"Use in search\" beside the order number in the case summary.", "use-order");
        if (!facts.Contains(def.DetailFact))
        {
            string ask = CallConversation.Questions.First(q => q.fact == def.DetailFact).label;
            return new Step(3, $"The record shows what the company knows. Now ask what the customer sees: \"{ask}\".", "say-ask-" + def.DetailFact);
        }
        if (!call.Resolved && !helpRead && call.ActionTaken == "")
            return new Step(4, "Check the policy: open Help pages (top of the company system) and read the page for this problem.", "tab-help");
        if (!call.Resolved)
            return new Step(5, "Choose the remedy the policy gives, under Account actions. A wrong one costs patience; you can try again.", "tab-account");
        return new Step(6, "Fixed. Confirm and close: \"Is there anything else?\" The notes record the outcome.", "say-close");
    }
}
