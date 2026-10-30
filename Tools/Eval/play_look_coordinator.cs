var s = UnityEngine.Object.FindFirstObjectByType<GameSession>();
var c = UnityEngine.Object.FindFirstObjectByType<Coordinator>();
var p = s.Player;
var cp = c.transform.position;
var fwd = c.transform.forward;
// Stand 2.6 m in front of her, looking back at her chest.
var at = cp + fwd * 2.6f + c.transform.right * 0.6f;
p.Teleport(new UnityEngine.Vector3(at.x, 0.05f, at.z));
var to = cp - at; to.y = 0;
float yaw = UnityEngine.Quaternion.LookRotation(to).eulerAngles.y;
p.SetLook(yaw, 8f);
var av = c.GetComponentInChildren<WorkerAvatar>();
var visible = System.Linq.Enumerable.Count(c.GetComponentsInChildren<UnityEngine.Renderer>(false));
var bounds = new UnityEngine.Bounds(cp, UnityEngine.Vector3.zero);
foreach (var r in c.GetComponentsInChildren<UnityEngine.Renderer>(false)) bounds.Encapsulate(r.bounds);
return $"coord {cp} state {c.State} anim {av.CurrentState} visible renderers {visible} bounds min {bounds.min.y:F2} max {bounds.max.y:F2}";
