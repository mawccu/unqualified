var cam = GameSession.Instance.Player.Camera;
var list = new System.Collections.Generic.List<string>();
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None))
{
    var n = t.name.ToLower();
    if (!(n.Contains("hand") || n.Contains("palm") || n.Contains("finger"))) continue;
    var v = cam.WorldToViewportPoint(t.position);
    if (v.z > 0 && v.x > 0.4f && v.x < 0.6f && v.y > 0.75f && v.y < 0.95f)
        list.Add($"{t.root.name}/{t.parent?.name}/{t.name} world {t.position.ToString("F2")} vp {v.ToString("F2")}");
}
return list.Count == 0 ? "none" : string.Join(" | ", list.GetRange(0, System.Math.Min(8, list.Count)));
