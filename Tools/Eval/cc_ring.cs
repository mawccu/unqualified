// Rings a call now: Tools/Eval/.ring = "<caseId> <language> [solo] [unknown]". "solo" sends the coworkers on break first.
var a = System.IO.File.ReadAllText("Tools/Eval/.ring").Trim().Split(' ');
var sh = CallCenterShift.Instance;
bool solo = System.Array.IndexOf(a, "solo") >= 0;
if (solo) foreach (var w in sh.Router.Workers) if (w.Simulated) sh.Router.SetBreak(w.Id, true);
var call = sh.Router.Ring(CaseLibrary.Get(a[0]), a[1], languageKnown: System.Array.IndexOf(a, "unknown") < 0);
return $"rang {call} aid {call.PhraseAid}";
