var sb = new System.Text.StringBuilder();
var root = UnityEngine.GameObject.Find("ComputerDesk");
foreach (var r in root.GetComponentsInChildren<UnityEngine.Renderer>(true))
  sb.AppendLine($"{r.name} active {r.gameObject.activeInHierarchy} enabled {r.enabled} bounds {r.bounds.min} .. {r.bounds.max} mats {string.Join(",", System.Array.ConvertAll(r.sharedMaterials, m => m ? m.name + ":" + m.GetColor("_BaseColor") : "null"))} lightmap {r.lightmapIndex}");
return sb.ToString();
