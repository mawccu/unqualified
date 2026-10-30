// The call center shift at a glance: phase, clock, your call, the last lines, suspicion, workers.
var sh = CallCenterShift.Instance;
if (sh == null) return "no call center shift (scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + ")";
var r = sh.Router;
var mine = sh.CallOf(sh.PlayerId);
string Lines(CallCase c) { if (c == null) return "-"; var t = c.Transcript; var tail = t.Skip(System.Math.Max(0, t.Count - 3)).Select(l => l.Speaker + ":" + l.Key); return string.Join(" / ", tail); }
var workers = string.Join("; ", r.Workers.Select(w => { var c = r.CallOf(w.Id); return $"{w.Name}[{string.Join("+", w.Languages)}{(w.Technical ? ",tech" : "")}{(w.OnBreak ? ",break" : "")}]={(c == null ? "free" : c.Id + ":" + c.State)}"; }));
return $"phase {sh.Current} clock {sh.Clock:0} | mine {(mine == null ? "none" : $"#{mine.Id} {mine.Def.Id} {mine.Language} {mine.State} pat {mine.Patience:0.00} facts [{string.Join(",", mine.Facts)}] transfers {mine.Transfers.Count} aid {mine.PhraseAid}")} | lines {Lines(mine)} | queue {r.Waiting.Count()} | finished {string.Join(",", sh.Finished.Select(c => c.Id + ":" + c.Owner + ":" + c.Outcome))} | suspicion {sh.Boss.Suspicion.Value:0} {sh.Boss.Suspicion.State} ({sh.Boss.Doing}, listening '{sh.Boss.ListeningTo}') | {workers} | paid {sh.Paid}";
