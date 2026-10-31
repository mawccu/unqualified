var g = UnityEditor.AssetDatabase.FindAssets("t:WorkerLibrary"); var lib = UnityEditor.AssetDatabase.LoadAssetAtPath<WorkerLibrary>(UnityEditor.AssetDatabase.GUIDToAssetPath(g[0]));
var go = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(lib.models[0]);
var clip = System.Array.Find(lib.controller.animationClips, c => c.name == "sit_type");
var sb = new System.Text.StringBuilder();
foreach (var t in new[] { 0f, 0.5f }) { clip.SampleAnimation(go, clip.length * t); foreach (var tr in go.GetComponentsInChildren<UnityEngine.Transform>()) { var n = tr.name; if (n == "HandL" || n == "HandR" || n == "Hips" || n == "Head" || n.StartsWith("Arm")) sb.Append($"{n} {tr.position.ToString("F2")}; "); } sb.Append(" || "); }
UnityEngine.Object.DestroyImmediate(go);
return sb.ToString();
