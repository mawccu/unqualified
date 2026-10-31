UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Wedding.unity");
var groups = new System.Collections.Generic.Dictionary<string, int>();
foreach (var l in UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None))
{
    var key = $"{l.type}/{l.lightmapBakeType}/shadows {l.shadows}";
    groups[key] = groups.TryGetValue(key, out var n) ? n + 1 : 1;
}
var sb = new System.Text.StringBuilder();
foreach (var kv in groups) sb.Append($"{kv.Key}: {kv.Value}; ");
foreach (var l in UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None))
    if (l.lightmapBakeType != UnityEngine.LightmapBakeType.Baked) sb.Append($" | {l.name} {l.type} {l.lightmapBakeType} {l.shadows}");
return sb.ToString();
