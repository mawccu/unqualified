// One step of the opening, from Tools/Eval/.step:
//   status | goto x z yaw pitch | use <id> | click <button text> | type <text> | skip
var s = GameSession.Instance;
var p = s.Player;
var d = OpeningDirector.Instance;
var line = System.IO.File.ReadAllText("Tools/Eval/.step").Trim();
var cmd = line.Split(' ')[0];
var arg = line.Length > cmd.Length ? line.Substring(cmd.Length + 1) : "";
string outcome = "";
UnityEngine.UIElements.VisualElement root = s.Hud.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
switch (cmd) {
  case "goto": {
    var a = arg.Split(' ');
    p.Teleport(new UnityEngine.Vector3(float.Parse(a[0]), 0.05f, float.Parse(a[1])));
    p.SetLook(float.Parse(a[2]), float.Parse(a[3]));
    outcome = "moved"; break; }
  case "use": {
    var req = new InteractionRequest { RequestId = 990, ActorId = p.ActorId, Action = InteractionAction.Use, TargetId = arg, ExpectedRevision = -1 };
    var v = s.Service.Validate(req);
    outcome = v.ToString();
    if (v == InteractionResult.Ok) s.Submit(req);
    else if (v == InteractionResult.NotNow) outcome += " (" + s.Service.GetInteractable(arg).BlockedReason(p) + ")";
    break; }
  default: {
    if (arg == "") { outcome = "unknown command"; break; }
    var buttons = UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Button>(root).ToList();
    var match = buttons.Find(b => (b.text ?? "").Contains(arg) || UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Label>(b).ToList().Exists(l => (l.text ?? "").Contains(arg)));
    if (match == null) { outcome = "no button '" + arg + "' among: " + string.Join(" | ", buttons.ConvertAll(b => b.text)); break; }
    using (var ev = UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()) { ev.target = match; match.SendEvent(ev); }
    outcome = "clicked"; break; }
  case "pickup": {
    var prop = s.Service.GetProp(arg);
    var req = new InteractionRequest { RequestId = 992, ActorId = p.ActorId, Action = InteractionAction.Pickup, TargetId = arg, ExpectedRevision = prop.Revision };
    outcome = s.Service.Validate(req).ToString(); s.Submit(req); break; }
  case "place": {
    var req = new InteractionRequest { RequestId = 993, ActorId = p.ActorId, Action = InteractionAction.Place, TargetId = arg, ExpectedRevision = -1 };
    outcome = s.Service.Validate(req).ToString(); s.Submit(req); break; }
  case "drop": {
    var req = new InteractionRequest { RequestId = 994, ActorId = p.ActorId, Action = InteractionAction.Drop, TargetId = "", ExpectedRevision = -1 };
    outcome = s.Service.Validate(req).ToString(); s.Submit(req); break; }
  case "jump": p.PressJump(); outcome = "jump"; break;
  case "press": p.PressInteract(); outcome = "pressed E"; break;
  case "hold": p.ScriptedInteractHeld = arg != "off"; if (arg != "off") p.PressInteract(); outcome = "hold " + arg; break;
  case "click": if (arg == "") { p.PressUse(); outcome = "clicked"; break; } goto default;
  case "reply": s.Hud.PickChoice(int.Parse(arg)); outcome = "replied " + arg; break;
  case "aim": {
    // Look at a world point: "aim x y z"
    var a = arg.Split(' ');
    var target = new UnityEngine.Vector3(float.Parse(a[0]), float.Parse(a[1]), float.Parse(a[2]));
    var eye = p.EyePosition; var dir = target - eye;
    float yaw = UnityEngine.Mathf.Atan2(dir.x, dir.z) * UnityEngine.Mathf.Rad2Deg;
    float pitch = -UnityEngine.Mathf.Atan2(dir.y, new UnityEngine.Vector2(dir.x, dir.z).magnitude) * UnityEngine.Mathf.Rad2Deg;
    p.SetLook(yaw, pitch); outcome = $"aimed yaw {yaw:0} pitch {pitch:0}"; break; }
  case "aimat": {
    // Look at a prop, socket or usable thing by id.
    UnityEngine.Vector3 target; var prop = s.Service.GetProp(arg); var sock = s.Service.GetSocket(arg); var thing = s.Service.GetInteractable(arg);
    if (prop != null) target = prop.Centre; else if (sock != null) target = sock.transform.position + UnityEngine.Vector3.up * 0.1f; else if (thing != null) { var col = thing.GetComponentInChildren<UnityEngine.Collider>(); target = col != null ? col.bounds.center : thing.transform.position; } else { outcome = "no such id"; break; }
    var eye = p.EyePosition; var dir = target - eye;
    p.SetLook(UnityEngine.Mathf.Atan2(dir.x, dir.z) * UnityEngine.Mathf.Rad2Deg, -UnityEngine.Mathf.Atan2(dir.y, new UnityEngine.Vector2(dir.x, dir.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
    outcome = "aimed at " + arg; break; }
  case "move": {
    var a = arg.Split(' ');
    if (a[0] == "stop") { p.ScriptedMove = null; p.ScriptedSprint = false; outcome = "stopped"; break; }
    p.ScriptedMove = new UnityEngine.Vector2(float.Parse(a[0]), float.Parse(a[1]));
    p.ScriptedSprint = a.Length > 2 && a[2] == "sprint";
    outcome = "moving"; break; }
  case "near": {
    // Stand about a metre from a prop, socket or usable thing and look at it: "near <id> [distance]"
    var a = arg.Split(' ');
    var prop = s.Service.GetProp(a[0]); var sock = s.Service.GetSocket(a[0]); var thing = s.Service.GetInteractable(a[0]);
    UnityEngine.Vector3 target;
    if (prop != null) target = prop.Centre; else if (sock != null) target = sock.transform.position; else if (thing != null) { var col = thing.GetComponentInChildren<UnityEngine.Collider>(); target = col != null ? col.bounds.center : thing.transform.position; } else { outcome = "no such id"; break; }
    float dist = a.Length > 1 ? float.Parse(a[1]) : 1.0f;
    var from = p.transform.position - target; from.y = 0f;
    if (from.sqrMagnitude < 0.01f) from = UnityEngine.Vector3.back;
    var stand = target + from.normalized * dist; stand.y = 0.05f;
    // Do not stand inside a wall: step back toward the player's side if blocked.
    p.Teleport(stand);
    var eye = p.EyePosition; var dir = target - eye;
    p.SetLook(UnityEngine.Mathf.Atan2(dir.x, dir.z) * UnityEngine.Mathf.Rad2Deg, -UnityEngine.Mathf.Atan2(dir.y, new UnityEngine.Vector2(dir.x, dir.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
    outcome = $"near {a[0]} at {stand}"; break; }
  case "timescale": UnityEngine.Time.timeScale = float.Parse(arg); outcome = "timescale " + arg; break;
  case "body": p.Avatar.SetShadowOnly(arg == "hide"); outcome = "body " + arg; break;
  case "type": {
    var field = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.TextField>(root);
    if (field == null) { outcome = "no text field"; break; }
    field.value = arg; outcome = "typed"; break; }
}
var crew = CrewState.Current;
var held = p.HeldProp != null ? p.HeldProp.EntityId : "-";
var landlord = UnityEngine.Object.FindFirstObjectByType<Landlord>(UnityEngine.FindObjectsInactive.Include);
return $"{line} -> {outcome} | stage {(d != null ? d.Stage.ToString() : "n/a")} | held {held} | canControl {p.CanControl} | landlord {(landlord != null && landlord.gameObject.activeInHierarchy ? landlord.transform.position.ToString() : "gone")} | crew bill={crew.billReceived} hired={crew.hired} claims=[{string.Join(",", crew.claims)}] savings={crew.savings} | scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}";
