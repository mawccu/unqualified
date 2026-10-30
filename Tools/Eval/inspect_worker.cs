var sb = new System.Text.StringBuilder();
var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/Characters/worker_host.fbx");
void Walk(UnityEngine.Transform t, string ind) {
  var r = t.GetComponent<UnityEngine.Renderer>();
  string mats = r != null ? " [" + string.Join(",", System.Array.ConvertAll(r.sharedMaterials, m => m != null ? m.name : "null")) + "]" : "";
  if (ind.Length < 14 || r != null) sb.AppendLine(ind + t.name + mats + (r is UnityEngine.SkinnedMeshRenderer ? " skinned" : ""));
  foreach (UnityEngine.Transform c in t) Walk(c, ind + " ");
}
Walk(go.transform, "");
var clips = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/worker_host.fbx");
foreach (var o in clips) if (o is UnityEngine.AnimationClip c && !c.name.StartsWith("__")) sb.AppendLine("clip " + c.name + " " + c.length.ToString("F2") + "s loop=" + c.isLooping);
return sb.ToString();
