// Sets the saved CV from Tools/Eval/.cv: claim ids separated by spaces ("none" for a truthful temp).
var ids = System.IO.File.ReadAllText("Tools/Eval/.cv").Trim().Split(' ');
var crew = CrewState.Current;
crew.claims = new System.Collections.Generic.List<string>();
foreach (var id in ids) if (id != "none") crew.claims.Add(id);
crew.hired = true; crew.billReceived = true; crew.introSeen = true;
crew.Save();
return "claims: " + string.Join(",", crew.claims);
