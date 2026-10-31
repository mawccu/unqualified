// Connected pieces of the worker body's "Detail" submesh (bind pose): bounds of each, to find the moustache.
var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/Characters/worker_host.fbx");
UnityEngine.SkinnedMeshRenderer body = null;
foreach (var r in go.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true)) if (r.name == "Worker") body = r;
var mesh = body.sharedMesh; var mats = body.sharedMaterials; int sub = -1;
for (int i = 0; i < mats.Length; i++) if (mats[i].name == "Detail") sub = i;
var verts = mesh.vertices; var tris = mesh.GetTriangles(sub);
var parent = new System.Collections.Generic.Dictionary<int,int>();
System.Func<int,int> find = null; find = x => { if (!parent.ContainsKey(x)) parent[x] = x; return parent[x] == x ? x : (parent[x] = find(parent[x])); };
// Merge by shared position (split normals duplicate vertices).
var byPos = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int,int>();
System.Func<int,int> key = v => { var p = verts[v]; var k = new UnityEngine.Vector3Int(UnityEngine.Mathf.RoundToInt(p.x*2000), UnityEngine.Mathf.RoundToInt(p.y*2000), UnityEngine.Mathf.RoundToInt(p.z*2000)); if (!byPos.TryGetValue(k, out var o)) { byPos[k] = v; return v; } return o; };
for (int t = 0; t < tris.Length; t += 3) { int a = find(key(tris[t])), b = find(key(tris[t+1])), c = find(key(tris[t+2])); parent[a] = b; parent[find(c)] = find(b); }
var groups = new System.Collections.Generic.Dictionary<int, UnityEngine.Bounds>(); var counts = new System.Collections.Generic.Dictionary<int,int>();
for (int t = 0; t < tris.Length; t++) { int root = find(key(tris[t])); var p = verts[tris[t]]; if (!groups.ContainsKey(root)) { groups[root] = new UnityEngine.Bounds(p, UnityEngine.Vector3.zero); counts[root] = 0; } var b = groups[root]; b.Encapsulate(p); groups[root] = b; counts[root]++; }
var sb = new System.Text.StringBuilder(); sb.AppendLine("sub " + sub + " tris " + tris.Length / 3 + " mesh bounds " + mesh.bounds.ToString("F3"));
foreach (var g in groups) sb.AppendLine(g.Key + ": n " + counts[g.Key] / 3 + " center " + g.Value.center.ToString("F3") + " size " + g.Value.size.ToString("F3"));
return sb.ToString();
