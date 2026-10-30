using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds one shared URP Lit material per material name found in the converted
/// models (Assets/Art/Materials/&lt;name&gt;.mat), then remaps every model to use
/// them. Four surfaces get Astra's tileable PBR sets; the rest keep the pack's
/// colours with a matte finish. Shared materials keep the look consistent,
/// editable in one place, and let Unity batch draws.
/// Run: unity command script eval_file (return MaterialLibrary.Build();)
/// </summary>
public static class MaterialLibrary
{
    const string MaterialsFolder = "Assets/Art/Materials";
    const string TexturesFolder = "Assets/Art/Textures";
    static readonly string[] ModelFolders = { "Assets/Art/Models", "Assets/Art/Characters", "Assets/Art/Apartment" };

    /// Material name in the pack -> texture set, tiling (models have 1 UV unit = 1 metre),
    /// metallic, and how much of the set's own smoothness to keep (a waxed floor at
    /// full gloss mirrors every lamp into bright speckles).
    static readonly Dictionary<string, (string set, float tiling, float metallic, float gloss)> Textured = new()
    {
        { "Oak floor", ("wood_planks", 0.5f, 0f, 0.45f) },
        { "Plaster", ("wallpaper", 1f, 0f, 0.6f) },
        { "Warm linen", ("fabric", 4f, 0f, 0.6f) },
        { "Brushed brass", ("brushed_brass", 2f, 1f, 1f) },
    };

    public static string Build()
    {
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder("Assets/Art", "Materials");
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        var report = new StringBuilder();

        // Gather every embedded material and its colour.
        var colours = new Dictionary<string, Color>();
        foreach (string folder in ModelFolders)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var embedded in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                {
                    if (colours.ContainsKey(embedded.name))
                        continue;
                    Color c = embedded.HasProperty("_BaseColor") ? embedded.GetColor("_BaseColor")
                        : embedded.HasProperty("_Color") ? embedded.GetColor("_Color") : Color.white;
                    colours[embedded.name] = c;
                }
            }
        }

        // Create or update one shared material per name.
        var library = new Dictionary<string, Material>();
        foreach (var pair in colours)
        {
            string assetPath = $"{MaterialsFolder}/{Safe(pair.Key)}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(lit) { name = pair.Key };
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            mat.shader = lit;
            mat.SetColor("_BaseColor", pair.Value);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.3f);
            if (Textured.TryGetValue(pair.Key, out var tex))
                ApplyTextureSet(mat, tex.set, tex.tiling, tex.metallic, tex.gloss);
            EditorUtility.SetDirty(mat);
            library[pair.Key] = mat;
        }
        AssetDatabase.SaveAssets();
        report.AppendLine($"materials: {library.Count} ({string.Join(", ", library.Keys.OrderBy(k => k))})");

        // Point every model at the shared materials.
        int remapped = 0;
        foreach (string folder in ModelFolders)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                foreach (var embedded in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Select(m => m.name).Distinct().ToList())
                {
                    if (library.TryGetValue(embedded, out var shared))
                        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded), shared);
                }
                importer.SaveAndReimport();
                remapped++;
            }
        }
        report.AppendLine($"models remapped to shared materials: {remapped}");
        return report.ToString();
    }

    static void ApplyTextureSet(Material mat, string set, float tiling, float metallic, float gloss)
    {
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/{set}_albedo.png");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/{set}_normal.png");
        var packed = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/{set}_metallic_smoothness.png");
        var scale = new Vector2(tiling, tiling);
        mat.SetColor("_BaseColor", Color.white);
        if (albedo != null)
        {
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTextureScale("_BaseMap", scale);
        }
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", set == "fabric" || set == "brushed_brass" ? 0.4f : 0.8f);
            mat.EnableKeyword("_NORMALMAP");
        }
        if (packed != null)
        {
            mat.SetTexture("_MetallicGlossMap", packed);
            mat.SetFloat("_Smoothness", gloss);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        mat.SetFloat("_Metallic", metallic);
    }

    static string Safe(string name)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
