var lib = UnityEditor.AssetDatabase.LoadAssetAtPath<WorkerLibrary>("Assets/Art/Characters/WorkerLibrary.asset");
if (lib == null) { var g = UnityEditor.AssetDatabase.FindAssets("t:WorkerLibrary"); lib = UnityEditor.AssetDatabase.LoadAssetAtPath<WorkerLibrary>(UnityEditor.AssetDatabase.GUIDToAssetPath(g[0])); }
var go = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(lib.models[0]);
go.transform.position = UnityEngine.Vector3.zero;
var clip = System.Array.Find(lib.controller.animationClips, c => c.name == "sit_type");
clip.SampleAnimation(go, clip.length * 0.5f);
var sb = new System.Text.StringBuilder();
foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>())
{
    var n = t.name.ToLower();
    if (n.Contains("hip") || n.Contains("pelvis") || n.Contains("knee") || n.Contains("leg") || n.Contains("foot") || n.Contains("head") || n.Contains("thigh") || n.Contains("spine") || n.Contains("root"))
        sb.Append($"{t.name} {t.position.ToString("F2")}; ");
}
UnityEngine.Object.DestroyImmediate(go);
string Bounds(string p) { var a = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p); var b = new UnityEngine.Bounds(); bool f = true; foreach (var m in a.GetComponentsInChildren<UnityEngine.MeshFilter>()) { var mb = m.sharedMesh.bounds; var w = m.transform.localToWorldMatrix; var c = w.MultiplyPoint3x4(mb.center); if (f) { b = new UnityEngine.Bounds(c, UnityEngine.Vector3.zero); f = false; } b.Encapsulate(w.MultiplyPoint3x4(mb.min)); b.Encapsulate(w.MultiplyPoint3x4(mb.max)); } return b.ToString("F2"); }
return $"clip len {clip.length} | {sb} | guest_chair {Bounds("Assets/Art/Models/guest_chair.fbx")} | desk_chair {Bounds("Assets/Art/Apartment/desk_chair.fbx")}";
