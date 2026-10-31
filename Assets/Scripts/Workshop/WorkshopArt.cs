using System;
using UnityEngine;

/// <summary>
/// The workshop's models and a couple of materials, kept in Resources so any
/// scene can make a part, a parcel or a gadget at runtime (the apartment spawns
/// what was bought; a shift shows the gadget under your tray). The apartment
/// builder fills it in.
/// </summary>
public class WorkshopArt : ScriptableObject
{
    [Serializable]
    public struct Model
    {
        public string id;
        public GameObject model;
    }

    public Model[] parts = Array.Empty<Model>();
    public Model[] gadgets = Array.Empty<Model>();
    public GameObject parcel;
    public GameObject parcelOpen;
    public GameObject lid;
    /// An unlit colour for indicator lights (tinted per renderer).
    public Material led;
    public Font labelFont;
    /// Depth-tested text (Unqualified/World Text); its font texture is filled in at runtime.
    public Material text;

    static WorkshopArt _loaded;

    public static WorkshopArt Load()
    {
        if (_loaded == null)
            _loaded = Resources.Load<WorkshopArt>("WorkshopArt");
        return _loaded;
    }

    public GameObject PartModel(string id) => Array.Find(parts, m => m.id == id).model;
    public GameObject GadgetModel(string recipe) => Array.Find(gadgets, m => m.id == recipe).model;

    /// A copy of a model with no colliders, for show (a gadget on the rack, under a tray).
    public static GameObject Show(GameObject model, Transform parent, Vector3 localPosition, Quaternion localRotation)
    {
        if (model == null)
            return null;
        var go = Instantiate(model, parent);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Destroy(c);
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    /// A small glowing cube (a charge light), tinted with a property block.
    public static Renderer Light(Transform parent, Vector3 localPosition, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.name = "Light";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = size;
        var r = go.GetComponent<MeshRenderer>();
        var art = Load();
        if (art != null && art.led != null)
            r.sharedMaterial = art.led;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return r;
    }

    static MaterialPropertyBlock _block;

    public static void Tint(Renderer r, Color colour)
    {
        if (r == null)
            return;
        _block ??= new MaterialPropertyBlock();
        _block.SetColor("_BaseColor", colour);
        r.SetPropertyBlock(_block);
    }

    /// World text for labels and the jig's screen (legacy TextMesh: no extra packages).
    public static TextMesh Label(Transform parent, Vector3 localPosition, Quaternion localRotation, float height, Color colour, string text = "")
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        var mesh = go.AddComponent<TextMesh>();
        var art = Load();
        var font = art != null && art.labelFont != null ? art.labelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mesh.font = font;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = TextMaterial(font);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mesh.fontSize = 64;
        mesh.characterSize = height / 6.4f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = colour;
        mesh.text = text;
        return mesh;
    }

    static Material _textMaterial;

    /// One shared depth-tested material for the label font. A dynamic font rebuilds its
    /// texture as new characters are needed, so it follows the rebuilds.
    static Material TextMaterial(Font font)
    {
        if (_textMaterial != null)
            return _textMaterial;
        var art = Load();
        if (art == null || art.text == null)
            return font.material;
        _textMaterial = new Material(art.text) { mainTexture = font.material.mainTexture };
        Font.textureRebuilt += rebuilt =>
        {
            if (rebuilt == font && _textMaterial != null)
                _textMaterial.mainTexture = font.material.mainTexture;
        };
        return _textMaterial;
    }
}
