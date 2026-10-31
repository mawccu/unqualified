// Every pickup prop in the open (playing) scene: where it rests, the gap under its collider to whatever is below, and its body state.
var sb = new System.Text.StringBuilder();
foreach (var p in UnityEngine.Object.FindObjectsByType<PickupProp>(UnityEngine.FindObjectsSortMode.None)) {
    var col = p.GetComponent<UnityEngine.Collider>();
    var rb = p.GetComponent<UnityEngine.Rigidbody>();
    var b = col.bounds;
    var visual = new UnityEngine.Bounds(p.transform.position, UnityEngine.Vector3.zero); bool first = true;
    foreach (var r in p.GetComponentsInChildren<UnityEngine.Renderer>()) { if (first) { visual = r.bounds; first = false; } else visual.Encapsulate(r.bounds); }
    string below = "nothing";
    var hits = UnityEngine.Physics.RaycastAll(new UnityEngine.Vector3(b.center.x, b.min.y + 0.05f, b.center.z), UnityEngine.Vector3.down, 3f, ~0, UnityEngine.QueryTriggerInteraction.Ignore);
    float best = 99f; string name = "";
    foreach (var h in hits) { if (h.collider == col || h.collider.transform.IsChildOf(p.transform)) continue; if (h.distance < best) { best = h.distance; name = h.collider.name; } }
    if (name != "") below = $"{name} gap {(best - 0.05f) * 100f:0.0}cm";
    float visGap = visual.min.y - b.min.y;
    sb.AppendLine($"{p.EntityId} [{p.Kind}] at {p.transform.position.ToString("F2")} socket '{p.SocketId}' kin {rb.isKinematic} sleep {rb.IsSleeping()} | below: {below} | mesh bottom vs collider {visGap * 100f:0.0}cm");
}
return sb.ToString();
