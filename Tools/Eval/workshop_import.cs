// Import settings for the workshop models (lightmap UVs, no animation), then the shared materials.
var sb = new System.Text.StringBuilder();
int n = 0;
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Workshop" }))
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var importer = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.importAnimation = false;
    importer.animationType = UnityEditor.ModelImporterAnimationType.None;
    importer.generateSecondaryUV = true;
    importer.importCameras = false;
    importer.importLights = false;
    importer.globalScale = 1f;
    importer.SaveAndReimport();
    n++;
}
sb.AppendLine("workshop models configured: " + n);
sb.AppendLine(MaterialLibrary.Build());
return sb.ToString();
