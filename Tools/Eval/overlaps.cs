// Pairs of separate placed models (scene roots under a group) whose rendered bounds interpenetrate by more than 3 cm on every axis.
// Skips pairs where one sits on top of the other (contact) and things deliberately nested (a child of the other).
var items = new System.Collections.Generic.List<(UnityEngine.GameObject go, UnityEngine.Bounds b)>();
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>(UnityEngine.FindObjectsSortMode.None)) {
    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
    var root = r.transform; // the placed model: the topmost object that is a prefab instance root, else the renderer itself
    var pr = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
    var go = pr != null ? pr : r.gameObject;
    var name = go.name.ToLowerInvariant();
    if (name.Contains("floor") || name.Contains("wall") || name.Contains("ceiling") || name.Contains("skirting") || name.Contains("pad") || name.Contains("ghost")) continue;
    int idx = items.FindIndex(i => i.go == go);
    if (idx < 0) items.Add((go, r.bounds)); else { var b = items[idx].b; b.Encapsulate(r.bounds); items[idx] = (go, b); }
}
var sb = new System.Text.StringBuilder(); int n = 0;
for (int i = 0; i < items.Count; i++) for (int j = i + 1; j < items.Count; j++) {
    var a = items[i]; var c = items[j];
    if (a.go.transform.IsChildOf(c.go.transform) || c.go.transform.IsChildOf(a.go.transform)) continue;
    var min = UnityEngine.Vector3.Max(a.b.min, c.b.min); var max = UnityEngine.Vector3.Min(a.b.max, c.b.max); var o = max - min;
    if (o.x < 0.03f || o.y < 0.03f || o.z < 0.03f) continue;
    // Resting on top: the upper one's bottom is near the lower one's top.
    if (UnityEngine.Mathf.Abs(a.b.min.y - c.b.max.y) < 0.04f || UnityEngine.Mathf.Abs(c.b.min.y - a.b.max.y) < 0.04f) continue;
    float vol = o.x * o.y * o.z; float small = UnityEngine.Mathf.Min(a.b.size.x * a.b.size.y * a.b.size.z, c.b.size.x * c.b.size.y * c.b.size.z);
    if (vol < 0.15f * small) continue;
    sb.AppendLine($"{a.go.name} x {c.go.name}: overlap {o.ToString("F2")} ({vol / small * 100f:0}% of the smaller) at {((min + max) * 0.5f).ToString("F2")}"); n++;
}
return n + " suspicious overlaps\n" + sb;
