using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time (repeatable) import setup for the art converted from the Godot
/// project: static props get lightmap UVs for baked lighting, the rigged workers
/// become Generic Mecanim characters with looping clips, and the PBR texture sets
/// are imported with the right colour space and packed for URP's Lit shader.
/// Run from the CLI: unity command script run --file Assets/Editor/ArtImportSetup.cs --entry ArtImportSetup.RunAll
/// </summary>
public static class ArtImportSetup
{
    const string ModelsFolder = "Assets/Art/Models";
    const string CharactersFolder = "Assets/Art/Characters";
    const string TexturesFolder = "Assets/Art/Textures";

    const string ApartmentFolder = "Assets/Art/Apartment";
    static readonly string[] LoopingClips = { "idle", "walk", "sprint", "carry_idle", "carry_walk", "clean",
        "knock", "talk", "point", "offer", "impatient", "hold_paper", "sit_type" };
    static readonly string[] TextureSets = { "wood_planks", "wallpaper", "fabric", "brushed_brass" };

    public static string RunAll()
    {
        var report = new StringBuilder();
        report.AppendLine(ConfigureProps());
        report.AppendLine(ConfigureCharacters());
        report.AppendLine(ConfigureTextures());
        report.AppendLine(PackMetallicSmoothness());
        AssetDatabase.Refresh();
        return report.ToString();
    }

    public static string ConfigureProps()
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ModelsFolder, ApartmentFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.generateSecondaryUV = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.globalScale = 1f;
            importer.SaveAndReimport();
            count++;
        }
        return $"props configured: {count}";
    }

    public static string ConfigureCharacters()
    {
        var lines = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { CharactersFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();

            // Name each clip after its action (Blender prefixes "Armature|") and set looping.
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                string clean = clip.name.Contains("|") ? clip.name.Substring(clip.name.LastIndexOf('|') + 1) : clip.name;
                clip.name = clean;
                clip.loopTime = LoopingClips.Contains(clean);
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            lines.Add($"{Path.GetFileNameWithoutExtension(path)}: {string.Join(", ", clips.Select(c => c.name + (c.loopTime ? "(loop)" : "")))}");
        }
        return "characters configured:\n  " + string.Join("\n  ", lines);
    }

    public static string ConfigureTextures()
    {
        int count = 0;
        foreach (string set in TextureSets)
        {
            foreach (string map in new[] { "albedo", "normal", "roughness", "height", "metallic" })
            {
                string path = $"{TexturesFolder}/{set}_{map}.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 4;
                if (map == "normal")
                {
                    importer.textureType = TextureImporterType.NormalMap;
                }
                else
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = map == "albedo";
                }
                // The two maps packed below must be readable from script.
                importer.isReadable = map == "roughness" || map == "metallic";
                importer.SaveAndReimport();
                count++;
            }
        }
        return $"textures configured: {count}";
    }

    /// URP Lit reads metallic from R and smoothness from A of one texture.
    public static string PackMetallicSmoothness()
    {
        var made = new List<string>();
        foreach (string set in TextureSets)
        {
            var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/{set}_metallic.png");
            var roughness = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/{set}_roughness.png");
            if (metallic == null || roughness == null)
                continue;
            int w = roughness.width, h = roughness.height;
            Color[] m = metallic.GetPixels();
            Color[] r = roughness.GetPixels();
            var packed = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                float metal = m.Length == pixels.Length ? m[i].r : m[0].r;
                pixels[i] = new Color(metal, 0f, 0f, 1f - r[i].r);
            }
            packed.SetPixels(pixels);
            packed.Apply();
            string outPath = $"{TexturesFolder}/{set}_metallic_smoothness.png";
            File.WriteAllBytes(outPath, packed.EncodeToPNG());
            Object.DestroyImmediate(packed);
            AssetDatabase.ImportAsset(outPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(outPath);
            importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.SaveAndReimport();
            made.Add(Path.GetFileName(outPath));
        }
        return "packed: " + string.Join(", ", made);
    }

    /// Lists every material each converted model uses, to decide where the PBR sets go.
    public static string ListModelMaterials()
    {
        var sb = new StringBuilder();
        foreach (string folder in new[] { ModelsFolder, CharactersFolder, ApartmentFolder })
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var names = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Select(mat => mat.name).Distinct();
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var bounds = new Bounds();
                bool first = true;
                foreach (var renderer in go.GetComponentsInChildren<Renderer>())
                {
                    if (first) { bounds = renderer.bounds; first = false; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                sb.AppendLine($"{Path.GetFileNameWithoutExtension(path)}  size {bounds.size.x:F2}x{bounds.size.y:F2}x{bounds.size.z:F2}  min y {bounds.min.y:F2}  materials: {string.Join(", ", names)}");
            }
        }
        return sb.ToString();
    }
}
