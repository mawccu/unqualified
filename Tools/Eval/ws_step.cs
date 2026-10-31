// One workshop playtest step, from Tools/Eval/.wsstep (play mode):
//   crew <job> <savings> [shifts] | reload | travel <scene> | lookat x y z | aim <interactable id> | hold | release
//   begin <recipe> | unpack <prop id> | kit | give <prop kind> | pack <recipe> | charge <0..1> | state
var s = GameSession.Instance;
var p = s.Player;
var crew = CrewState.Current;
var line = System.IO.File.ReadAllText("Tools/Eval/.wsstep").Trim();
var a = line.Split(' ');
string outcome = "";
switch (a[0]) {
  case "crew":
    crew.billReceived = true; crew.hired = true; crew.job = a[1]; crew.savings = int.Parse(a[2]);
    if (a.Length > 3) crew.shiftsCompleted = int.Parse(a[3]);
    crew.Save(); outcome = "crew set"; break;
  case "reload":
    UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); outcome = "reloading"; break;
  case "travel":
    UnityEngine.SceneManagement.SceneManager.LoadScene(a[1]); outcome = "travelling"; break;
  case "lookat": {
    var target = new UnityEngine.Vector3(float.Parse(a[1]), float.Parse(a[2]), float.Parse(a[3]));
    var d = target - p.Camera.transform.position;
    float yaw = UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg;
    float pitch = -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg;
    p.SetLook(yaw, pitch); outcome = $"look {yaw:0} {pitch:0}"; break; }
  case "aim": {
    var thing = s.Service.GetInteractable(a[1]);
    var t = thing.GetComponentInChildren<UnityEngine.Collider>().bounds.center;
    var d = t - p.Camera.transform.position;
    p.SetLook(UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
    outcome = "aimed at " + a[1]; break; }
  case "hold": p.PressInteract(); p.ScriptedInteractHeld = true; outcome = "holding E"; break;
  case "release": p.ScriptedInteractHeld = false; outcome = "released"; break;
  case "begin": outcome = AssemblyBench.Instance.Begin(a[1], p).ToString(); break;
  case "unpack": { var prop = s.Service.GetProp(a[1]); prop.GetComponent<Parcel>().UseHeld(p); outcome = "unpacked"; break; }
  case "kit": GadgetKit.Instance.Press(); outcome = "kit pressed: " + GadgetKit.Instance.LastHint; break;
  case "give": {
    var prop = System.Linq.Enumerable.First(s.Service.Props, x => x.Kind == a[1] && string.IsNullOrEmpty(x.SocketId) && string.IsNullOrEmpty(x.HolderId));
    outcome = s.Service.GiveProp(prop, p).ToString(); break; }
  case "pack": {
    var g = crew.gadgets.Find(x => x.recipe == a[1]);
    outcome = WorkshopLedger.Equip(crew, g.id, p.ActorId).ToString(); crew.Save(); GadgetRack.Instance?.Refresh(); break; }
  case "charge": foreach (var g in crew.gadgets) g.charge = float.Parse(a[1]); outcome = "charged"; break;
  default: outcome = "unknown"; break;
}
var b = AssemblyBench.Instance;
string bench = b != null && b.HasBuild ? $" bench[{b.Recipe.Name} casing {b.CasingSeated} board {b.BoardSeated} screws {b.ScrewsFastened} batt {b.BatterySeated} lid {b.LidClosed} faults {string.Join(",", b.Faults)}]" : "";
string kit = GadgetKit.Instance != null ? $" kit[{GadgetKit.Instance.Recipe.Name} on {GadgetKit.Instance.On} active {GadgetKit.Instance.Active} charge {GadgetKit.Instance.Gadget.charge:0.00} watched {GadgetKit.Instance.Watched:0.0} seen {GadgetKit.Instance.Sightings}]" : "";
return $"{outcome} | ${crew.savings} parts {crew.parts.Count} orders {string.Join(",", crew.orders.ConvertAll(o => o.id + ":" + o.state))} gadgets {crew.gadgets.Count} held '{p.HeldPropId}'{bench}{kit}";
