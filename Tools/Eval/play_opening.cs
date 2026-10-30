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
  case "click": {
    var buttons = UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Button>(root).ToList();
    var match = buttons.Find(b => (b.text ?? "").Contains(arg) || UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Label>(b).ToList().Exists(l => (l.text ?? "").Contains(arg)));
    if (match == null) { outcome = "no button '" + arg + "' among: " + string.Join(" | ", buttons.ConvertAll(b => b.text)); break; }
    using (var ev = UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()) { ev.target = match; match.SendEvent(ev); }
    outcome = "clicked"; break; }
  case "type": {
    var field = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.TextField>(root);
    if (field == null) { outcome = "no text field"; break; }
    field.value = arg; outcome = "typed"; break; }
}
var crew = CrewState.Current;
var held = p.HeldProp != null ? p.HeldProp.EntityId : "-";
var landlord = UnityEngine.Object.FindFirstObjectByType<Landlord>(UnityEngine.FindObjectsInactive.Include);
return $"{line} -> {outcome} | stage {(d != null ? d.Stage.ToString() : "n/a")} | held {held} | canControl {p.CanControl} | landlord {(landlord != null && landlord.gameObject.activeInHierarchy ? landlord.transform.position.ToString() : "gone")} | crew bill={crew.billReceived} hired={crew.hired} claims=[{string.Join(",", crew.claims)}] savings={crew.savings} | scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}";
