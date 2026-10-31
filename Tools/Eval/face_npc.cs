// Stand in front of a named character and look at their face: Tools/Eval/.face holds
// "npc_dana" / "npc_omar" / "npc_kim" / "supervisor" / "coordinator" / "landlord" and a distance.
var a = System.IO.File.ReadAllText("Tools/Eval/.face").Trim().Split(' ');
float dist = a.Length > 1 ? float.Parse(a[1]) : 1.6f;
UnityEngine.Transform t = null;
if (a[0] == "supervisor") t = UnityEngine.Object.FindFirstObjectByType<Supervisor>()?.transform;
else if (a[0] == "coordinator") t = UnityEngine.Object.FindFirstObjectByType<Coordinator>()?.transform;
else foreach (var c in UnityEngine.Object.FindObjectsByType<Coworker>(UnityEngine.FindObjectsSortMode.None)) if (c.WorkerId == a[0]) t = c.GetComponentInChildren<WorkerAvatar>()?.transform ?? c.transform;
if (t == null) return "no " + a[0];
var avatar = t.GetComponentInChildren<WorkerAvatar>();
var body = avatar != null ? avatar.transform : t;
var head = body.position + UnityEngine.Vector3.up * 1.45f;
var fwd = body.forward; fwd.y = 0f; fwd.Normalize();
var p = GameSession.Instance.Player;
var spot = body.position + fwd * dist; spot.y = 0.05f;
p.Teleport(spot);
var d = head - p.EyePosition;
p.SetLook(UnityEngine.Mathf.Atan2(d.x, d.z) * UnityEngine.Mathf.Rad2Deg, -UnityEngine.Mathf.Atan2(d.y, new UnityEngine.Vector2(d.x, d.z).magnitude) * UnityEngine.Mathf.Rad2Deg);
return a[0] + " (" + (avatar != null ? avatar.name : "?") + ") at " + body.position.ToString("F1");
