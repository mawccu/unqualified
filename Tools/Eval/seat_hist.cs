var a = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/Models/guest_chair.fbx");
var counts = new System.Collections.Generic.SortedDictionary<float, int>();
foreach (var mf in a.GetComponentsInChildren<UnityEngine.MeshFilter>()) { var w = mf.transform.localToWorldMatrix; foreach (var v in mf.sharedMesh.vertices) { var p = w.MultiplyPoint3x4(v); if (p.y > 0.25f && p.y < 0.75f) { float k = UnityEngine.Mathf.Round(p.y * 50f) / 50f; counts[k] = counts.TryGetValue(k, out var n) ? n + 1 : 1; } } }
var sb = new System.Text.StringBuilder(); foreach (var kv in counts) sb.Append($"{kv.Key:F2}:{kv.Value} ");
var chair = UnityEngine.GameObject.Find("Chair_t0_0") ;
return sb.ToString();
