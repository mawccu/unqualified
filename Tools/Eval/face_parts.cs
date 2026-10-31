// Bind-pose extents (Unity axes: x, y up, z forward) of the face parts of worker_host's body mesh.
var lib = UnityEditor.AssetDatabase.LoadAssetAtPath<WorkerLibrary>("Assets/Settings/Workers.asset");
var smr = System.Linq.Enumerable.First(lib.models[0].GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true), r => r.name == "Worker");
var m = smr.sharedMesh; var v = m.vertices; var sb = new System.Text.StringBuilder();
for (int s = 0; s < m.subMeshCount; s++) {
  var tris = m.GetTriangles(s); var lo = UnityEngine.Vector3.one * 99; var hi = -lo; int n = 0;
  foreach (var i in tris) { var p = new UnityEngine.Vector3(-v[i].x, v[i].z, -v[i].y); if (p.y < 1.3f || p.z < 0.05f) continue; lo = UnityEngine.Vector3.Min(lo, p); hi = UnityEngine.Vector3.Max(hi, p); n++; }
  sb.Append(s + " " + smr.sharedMaterials[s].name + " n" + n + " " + lo.ToString("F3") + ".." + hi.ToString("F3") + "\n");
}
// Eye region columns: frontmost z of the head at eye height for x across.
for (float x = -0.16f; x <= 0.161f; x += 0.04f) { float best = -9; foreach (var p0 in v) { var p = new UnityEngine.Vector3(-p0.x, p0.z, -p0.y); if (UnityEngine.Mathf.Abs(p.x - x) < 0.012f && UnityEngine.Mathf.Abs(p.y - 1.42f) < 0.02f) best = UnityEngine.Mathf.Max(best, p.z); } sb.Append($"x {x:F2}: front z {best:F3}\n"); }
return sb.ToString();
