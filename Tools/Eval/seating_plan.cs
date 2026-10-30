var tables = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(System.Linq.Enumerable.OrderBy(UnityEngine.Object.FindObjectsByType<GuestTable>(UnityEngine.FindObjectsSortMode.None), t => t.Index), t => t.Info));
var roster = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(Guest.All, g => g.Profile));
var plan = SeatingChallenge.Solve(roster, tables);
string o = "";
foreach (var g in Guest.All) o += g.EntityId + "=" + (plan != null ? plan[g.Profile.Name].ToString() : "?") + "(" + g.Profile.Need + (g.Profile.Other != null ? " " + g.Profile.Other : "") + "," + g.State + ") ";
o += " | tables: " + string.Join(" ", System.Linq.Enumerable.Select(tables, t => $"{t.Index}:{(t.NearSpeaker ? "spk" : "")}{(t.NearStage ? "stg" : "")}/{t.Seats}"));
return o;
