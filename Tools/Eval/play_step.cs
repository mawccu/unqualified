// Reads one command from Tools/Eval/.step: "goto x z yaw pitch" (Unity coords), "pickup <prop>", "place <socket>", "drop", "status".
var s = UnityEngine.Object.FindFirstObjectByType<GameSession>();
var p = s.Player;
var parts = System.IO.File.ReadAllText("Tools/Eval/.step").Trim().Split(' ');
string outcome = "";
switch (parts[0]) {
  case "goto":
    p.Teleport(new UnityEngine.Vector3(float.Parse(parts[1]), 0.05f, float.Parse(parts[2])));
    p.SetLook(float.Parse(parts[3]), float.Parse(parts[4]));
    outcome = "moved"; break;
  case "pickup": {
    var prop = s.Service.GetProp(parts[1]);
    var req = new InteractionRequest { RequestId = 900, ActorId = p.ActorId, Action = InteractionAction.Pickup, TargetId = parts[1], ExpectedRevision = prop.Revision };
    outcome = s.Service.Validate(req).ToString(); s.Submit(req); break; }
  case "place": {
    var req = new InteractionRequest { RequestId = 901, ActorId = p.ActorId, Action = InteractionAction.Place, TargetId = parts[1], ExpectedRevision = -1 };
    outcome = s.Service.Validate(req).ToString(); s.Submit(req); break; }
  case "drop": {
    var req = new InteractionRequest { RequestId = 902, ActorId = p.ActorId, Action = InteractionAction.Drop, TargetId = "", ExpectedRevision = -1 };
    outcome = s.Service.Validate(req).ToString(); s.Submit(req); break; }
}
var tasks = string.Join(" | ", System.Linq.Enumerable.Select(s.TaskResults, r => r.Name + ": " + r.Text));
return $"{string.Join(" ", parts)} -> {outcome}; held '{p.HeldPropId}'; {tasks}";
