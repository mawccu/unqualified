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
    static readonly string[] ModelFolders = { "Assets/Art/Models", "Assets/Art/Characters", "Assets/Art/Apartment", "Assets/Art/Hands", "Assets/Art/Props", "Assets/Art/Workshop" };

    /// Material name in the pack -> texture set, tiling (models have 1 UV unit = 1 metre),
    /// metallic, and how much of the set's own smoothness to keep (a waxed floor at
    /// full gloss mirrors every lamp into bright speckles).
    static readonly Dictionary<string, (string set, float tiling, float metallic, float gloss)> Textured = new()
    {
        { "Oak floor", ("wood_planks", 0.5f, 0f, 0.45f) },
        { "Plaster", ("wallpaper", 1f, 0f, 0.6f) },
        { "Warm linen", ("fabric", 4f, 0f, 0.6f) },
        { "Brushed brass", ("brushed_brass", 2f, 1f, 1f) },
        // The beds: woven fabric, tinted per piece (Tints), never glossy.
        { "Mattress ticking", ("fabric", 5f, 0f, 0.25f) },
        { "Bed sheet", ("fabric", 5f, 0f, 0.25f) },
        { "Pillowcase", ("fabric", 5f, 0f, 0.25f) },
        { "Duvet mustard", ("fabric", 3f, 0f, 0.2f) },
        { "Duvet teal", ("fabric", 3f, 0f, 0.2f) },
        { "Pallet pine", ("wood_planks", 1.5f, 0f, 0.2f) },
        { "Pallet block", ("wood_planks", 1.5f, 0f, 0.2f) },
    };

    /// A colour multiplied over a texture set (otherwise textured materials are white-tinted).
    static readonly Dictionary<string, Color> Tints = new()
    {
        { "Mattress ticking", new Color(0.74f, 0.8f, 0.88f) },
        { "Bed sheet", new Color(0.98f, 0.96f, 0.9f) },
        { "Pillowcase", new Color(1f, 0.98f, 0.94f) },
        { "Duvet mustard", new Color(1f, 0.72f, 0.3f) },
        { "Duvet teal", new Color(0.35f, 0.66f, 0.68f) },
        { "Pallet pine", new Color(1.15f, 1.05f, 0.9f) },
        { "Pallet block", new Color(0.85f, 0.75f, 0.62f) },
    };

    /// One model's slot that must not share the library material of the same name:
    /// the floor tile's plank gaps are modelled with "Walnut", which is right for
    /// furniture but makes every seam a bright line on the floor.
    static readonly Dictionary<(string model, string material), (string name, Color colour, float smoothness)> PerModel = new()
    {
        { ("floor_tile_2m", "Walnut"), ("Plank gap", new Color(0.075f, 0.05f, 0.035f), 0.1f) },
    };

    /// Points the overridden slots at their own materials (also run at the end of Build).
    public static string ApplyOverrides()
    {
        var report = new StringBuilder();
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        foreach (var pair in PerModel)
        {
            string path = AssetDatabase.FindAssets($"t:Model {pair.Key.model}", ModelFolders)
                .Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == pair.Key.model);
            if (path == null)
                continue;
            string matPath = $"{MaterialsFolder}/{Safe(pair.Value.name)}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(lit) { name = pair.Value.name };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetColor("_BaseColor", pair.Value.colour);
            mat.SetFloat("_Smoothness", pair.Value.smoothness);
            EditorUtility.SetDirty(mat);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key.material), mat);
            importer.SaveAndReimport();
            report.AppendLine($"{pair.Key.model}: {pair.Key.material} -> {pair.Value.name}");
        }
        AssetDatabase.SaveAssets();
        return report.ToString();
    }

    public static string Build()
    {
        var all = new List<string>();
        foreach (string folder in ModelFolders)
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
                all.Add(AssetDatabase.GUIDToAssetPath(guid));
        return BuildModels(all.ToArray());
    }

    /// Re-applies the texture sets and tints to existing shared materials by name
    /// (after changing the tables above; models already remapped keep pointing at them).
    public static string RefreshMaterials(params string[] names)
    {
        var done = new List<string>();
        foreach (var name in names)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/{Safe(name)}.mat");
            if (mat == null)
                continue;
            if (Textured.TryGetValue(name, out var tex))
                ApplyTextureSet(mat, tex.set, tex.tiling, tex.metallic, tex.gloss);
            if (Tints.TryGetValue(name, out var tint))
                mat.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(mat);
            done.Add(name);
        }
        AssetDatabase.SaveAssets();
        return "refreshed: " + string.Join(", ", done);
    }

    /// The same, for just these models (a new prop) without touching the rest.
    public static string BuildModels(params string[] modelPaths)
    {
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder("Assets/Art", "Materials");
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        var report = new StringBuilder();

        // Gather every embedded material and its colour.
        var colours = new Dictionary<string, Color>();
        foreach (string path in modelPaths)
        {
            foreach (var embedded in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
            {
                if (colours.ContainsKey(embedded.name))
                    continue;
                Color c = embedded.HasProperty("_BaseColor") ? embedded.GetColor("_BaseColor")
                    : embedded.HasProperty("_Color") ? embedded.GetColor("_Color") : Color.white;
                colours[embedded.name] = c;
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
                // New: take the model's colour. Existing shared materials keep theirs.
                mat = new Material(lit) { name = pair.Key };
                AssetDatabase.CreateAsset(mat, assetPath);
                mat.SetColor("_BaseColor", pair.Value);
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Smoothness", 0.3f);
            }
            // Only switch shaders that need it: reassigning Lit resets a material's
            // surface setup (a transparent one comes back opaque).
            if (mat.shader != lit)
                mat.shader = lit;
            if (Textured.TryGetValue(pair.Key, out var tex))
                ApplyTextureSet(mat, tex.set, tex.tiling, tex.metallic, tex.gloss);
            if (Tints.TryGetValue(pair.Key, out var tint))
                mat.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(mat);
            library[pair.Key] = mat;
        }
        AssetDatabase.SaveAssets();
        report.AppendLine($"materials: {library.Count} ({string.Join(", ", library.Keys.OrderBy(k => k))})");

        // Point every model at the shared materials.
        int remapped = 0;
        foreach (string path in modelPaths)
        {
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
        report.AppendLine($"models remapped to shared materials: {remapped}");
        report.Append(ApplyOverrides());
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
