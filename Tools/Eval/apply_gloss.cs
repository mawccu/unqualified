// Applies the gloss scale to the four textured materials without a full library rebuild.
var gloss = new System.Collections.Generic.Dictionary<string, float> { { "Oak floor", 0.45f }, { "Plaster", 0.6f }, { "Warm linen", 0.6f }, { "Brushed brass", 1f } };
foreach (var pair in gloss) {
  var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>($"Assets/Art/Materials/{pair.Key}.mat");
  m.SetFloat("_Smoothness", pair.Value); UnityEditor.EditorUtility.SetDirty(m);
}
UnityEditor.AssetDatabase.SaveAssets();
return "gloss applied";
