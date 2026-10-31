// Hand contact on the carried prop, measured against its box (prop.LocalBounds) and
// optionally photographed by a side camera. Tools/Eval/.grip holds "measure" or
// "shot <file> <side|front|below|left>" (file under Docs/Polish/Grips/).
var line = System.IO.File.ReadAllText("Tools/Eval/.grip").Trim().Split(' ');
var p = GameSession.Instance.Player;
var prop = p.HeldProp;
if (prop == null) return "nothing held";
var arms = p.Arms.transform;
var b = prop.LocalBounds;
// A held book is measured in its laid-down frame (cover up), as the grip is built.
var lay = Grips.ProfileOf(prop) == Grips.Profile.Book ? Grips.Lay(prop) : UnityEngine.Quaternion.identity;
{
    var laid = new UnityEngine.Bounds(lay * b.center, UnityEngine.Vector3.zero);
    for (int i = 0; i < 8; i++) laid.Encapsulate(lay * new UnityEngine.Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
    b = laid;
}
UnityEngine.Vector3 Local(UnityEngine.Vector3 world) => lay * prop.transform.InverseTransformPoint(world);
string Describe(UnityEngine.Vector3 world)
{
    var l = Local(world);
    var outside = UnityEngine.Vector3.Max(b.min - l, l - b.max);
    var o = UnityEngine.Vector3.Max(outside, UnityEngine.Vector3.zero);
    if (o.sqrMagnitude > 0f) return "+" + (o.magnitude * 1000f).ToString("0") + "mm";
    float depth = UnityEngine.Mathf.Min(UnityEngine.Mathf.Min(-outside.x, -outside.y), -outside.z);
    return "-" + (depth * 1000f).ToString("0") + "mm";
}
UnityEngine.Vector3 Surface(UnityEngine.Transform t, bool palm)
{
    var mf = t.GetComponentInChildren<UnityEngine.MeshFilter>();
    var mb = mf.sharedMesh.bounds;
    var local = palm ? new UnityEngine.Vector3(mb.center.x, mb.min.y, mb.center.z) : new UnityEngine.Vector3(mb.center.x, mb.min.y * 0.5f, mb.max.z);
    return mf.transform.TransformPoint(local);
}
var sb = new System.Text.StringBuilder();
sb.Append(prop.Kind + " (" + prop.Category + ") size " + b.size.ToString("F3") + " | ");
foreach (var n in new[] { "R", "L" })
{
    var hand = arms.Find("Hand" + n);
    if (hand == null) continue;
    sb.Append(n + ": palm " + Describe(Surface(hand, true)) + " tips");
    for (int i = 0; i < 4; i++) { var tip = hand.Find("Finger" + i + n + "/Finger" + i + "Tip" + n); sb.Append(" " + Describe(Surface(tip, false))); }
    var thumb = hand.Find("Thumb" + n + "/ThumbTip" + n);
    var tl = Local(Surface(thumb, false));
    float edge = n == "R" ? tl.x - b.max.x : b.min.x - tl.x;
    sb.Append(" thumb " + Describe(Surface(thumb, false)) + " (past edge " + (edge * 1000f).ToString("0") + "mm, above top " + ((tl.y - b.max.y) * 1000f).ToString("0") + "mm) | ");
}
// A tool: distances from the handle's surface instead of from the box.
if (Grips.TryHandle(prop, out var handle))
{
    var axis = UnityEngine.Vector3.zero; axis[handle.Axis] = 1f;
    var c0 = Grips.HandleCentre(b, handle);
    string Off(UnityEngine.Vector3 world) { var l = prop.transform.InverseTransformPoint(world) - c0; var radial = l - axis * UnityEngine.Vector3.Dot(l, axis); return ((radial.magnitude - handle.Radius) * 1000f).ToString("+0;-0") + "mm"; }
    var hand = arms.Find("HandR");
    var pm = hand.GetComponentInChildren<UnityEngine.MeshFilter>(); var pb = pm.sharedMesh.bounds;
    sb.Append(" HANDLE palm " + Off(Surface(hand, true)) + " palm-front " + Off(pm.transform.TransformPoint(new UnityEngine.Vector3(pb.center.x, pb.min.y, pb.max.z - 0.012f))) + " tips");
    for (int i = 0; i < 4; i++) sb.Append(" " + Off(Surface(hand.Find("Finger" + i + "R/Finger" + i + "TipR"), false)));
    sb.Append(" thumb " + Off(Surface(hand.Find("ThumbR/ThumbTipR"), false)) + " | ");
}
if (line[0] == "shot")
{
    System.IO.Directory.CreateDirectory("Docs/Polish/Grips");
    var c = prop.Centre; var cam = p.Camera.transform;
    var right = UnityEngine.Vector3.ProjectOnPlane(cam.right, UnityEngine.Vector3.up).normalized;
    var fwd = UnityEngine.Vector3.ProjectOnPlane(cam.forward, UnityEngine.Vector3.up).normalized;
    UnityEngine.Vector3 from = line[2] switch {
        "side" => c + right * 0.75f + UnityEngine.Vector3.up * 0.05f,
        "left" => c - right * 0.75f + UnityEngine.Vector3.up * 0.05f,
        "front" => c + fwd * 0.8f + UnityEngine.Vector3.up * 0.15f,
        "hand" => arms.Find("HandR").position + right * 0.22f + fwd * 0.18f + UnityEngine.Vector3.up * 0.1f,
        "fp" => cam.position,
        _ => c + fwd * 0.35f - UnityEngine.Vector3.up * 0.55f,
    };
    var go = new UnityEngine.GameObject("GripCam"); var shot = go.AddComponent<UnityEngine.Camera>();
    shot.CopyFrom(p.Camera); shot.fieldOfView = 50f; shot.nearClipPlane = 0.02f;
    var look = line[2] == "hand" ? arms.Find("HandR").position : c;
    go.transform.SetPositionAndRotation(from, line[2] == "fp" ? cam.rotation : UnityEngine.Quaternion.LookRotation(look - from, UnityEngine.Vector3.up));
    if (line[2] == "fp") shot.fieldOfView = p.Camera.fieldOfView;
    var rt = new UnityEngine.RenderTexture(960, 720, 24); shot.targetTexture = rt; shot.Render();
    UnityEngine.RenderTexture.active = rt; var tex = new UnityEngine.Texture2D(960, 720, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 960, 720), 0, 0); tex.Apply(); UnityEngine.RenderTexture.active = null;
    shot.targetTexture = null; UnityEngine.Object.Destroy(go); rt.Release();
    System.IO.File.WriteAllBytes("Docs/Polish/Grips/" + line[1] + ".png", tex.EncodeToPNG());
    sb.Append("saved " + line[1]);
}
return sb.ToString();
