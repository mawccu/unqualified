// Sets the saved CV from Tools/Eval/.cv: claim ids separated by spaces ("none" for a truthful temp),
// plus "lang:es" / "lang:ar" for the local player's languages and "job:call_center" / "job:wedding".
var ids = System.IO.File.ReadAllText("Tools/Eval/.cv").Trim().Split(' ');
var crew = CrewState.Current;
crew.claims = new System.Collections.Generic.List<string>();
var langs = new System.Collections.Generic.List<string>();
foreach (var id in ids)
{
    if (id == "none" || id == "") continue;
    if (id.StartsWith("lang:")) langs.Add(id.Substring(5));
    else if (id.StartsWith("job:")) crew.job = id.Substring(4);
    else crew.claims.Add(id);
}
crew.SetLanguages(GameSession.OfflineActor, langs);
crew.hired = true; crew.billReceived = true; crew.introSeen = true;
crew.Save();
return "job " + crew.Job + ", claims: " + string.Join(",", crew.claims) + ", languages: " + string.Join(",", crew.LanguagesOf(GameSession.OfflineActor));
