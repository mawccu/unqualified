// What renderer is seen at a viewport point (Tools/Eval/.vp = "x y").
var a = System.IO.File.ReadAllText("Tools/Eval/.vp").Trim().Split(' ');
var cam = GameSession.Instance.Player.Camera;
var ray = cam.ViewportPointToRay(new UnityEngine.Vector3(float.Parse(a[0]), float.Parse(a[1]), 0));
var best = ""; float bestD = 99f;
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None))
{
    if (!r.enabled || !r.isVisible) continue;
    if (r.bounds.IntersectRay(ray, out float d) && d < bestD && d > 0.05f) { bestD = d; var p = r.transform; best = $"{p.root.name}/{p.parent?.name}/{p.name} at {d:0.00} m, bounds {r.bounds.center.ToString("F2")}"; }
}
return best;
