UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var models = UnityEditor.AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art" }).Length;
var textures = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Textures" }).Length;
var script = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>("Assets/Editor/ArtImportSetup.cs");
return models + " models, " + textures + " textures, import script asset: " + (script != null);
