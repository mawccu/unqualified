var g = UnityEditor.AssetDatabase.FindAssets("t:WorkerLibrary"); var lib = UnityEditor.AssetDatabase.LoadAssetAtPath<WorkerLibrary>(UnityEditor.AssetDatabase.GUIDToAssetPath(g[0]));
var sb = new System.Text.StringBuilder();
for (int m = 0; m < lib.models.Length; m++)
foreach (var name in new[] { "idle", "sit_type" })
{
    var go = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(lib.models[m]);
    var clip = System.Array.Find(lib.controller.animationClips, c => c.name == name);
    clip.SampleAnimation(go, clip.length * 0.5f);
    UnityEngine.Transform F(string n) { foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>()) if (t.name == n) return t; return null; }
    float minY = 9f; foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>()) minY = UnityEngine.Mathf.Min(minY, r.bounds.min.y);
    var hips = F("Hips"); 
    // lowest point of the body near the hips = what touches the seat
    sb.Append($"m{m} {name}: hips {hips.position.ToString("F2")} lowest {minY:F2}; ");
    UnityEngine.Object.DestroyImmediate(go);
}
string Seat(string p) { var a = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p); float best = 0f; foreach (var mf in a.GetComponentsInChildren<UnityEngine.MeshFilter>()) { var w = mf.transform.localToWorldMatrix; foreach (var v in mf.sharedMesh.vertices) { var p2 = w.MultiplyPoint3x4(v); if (UnityEngine.Mathf.Abs(p2.x) < 0.12f && UnityEngine.Mathf.Abs(p2.z) < 0.12f && p2.y < 0.8f && p2.y > best) best = p2.y; } } return best.ToString("F3"); }
return sb + $"| guest seat {Seat("Assets/Art/Models/guest_chair.fbx")} | desk seat {Seat("Assets/Art/Apartment/desk_chair.fbx")}";
