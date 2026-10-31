using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The flat's sliding window in the west wall (Tools/Blender/make_window.py): a
/// hole through the wall lined by the frame's reveal, a fixed sash on the back
/// track, a sliding one on the front track with a pull handle, a grille outside,
/// and a narrow alley beyond (a neighbour's brick wall, the ground, a strip of
/// sky) with its street noise.
/// </summary>
public static partial class ApartmentSceneBuilder
{
    const float WindowZ = 0.6f, WindowHalf = 0.58f;
    const float WindowBase = 0.9f;
    const float WindowSill = WindowBase + 0.08f, WindowTop = WindowBase + 1.04f;

    /// A point in the window frame's model space (Blender metres) in the world.
    static Vector3 WindowPoint(Transform frame, float x, float y, float z) => frame.TransformPoint(new Vector3(-x, z, -y));

    static void Window(Transform parent)
    {
        string P(string n) => string.Format(Props, n);
        var root = new GameObject("Window").transform;
        root.SetParent(parent, false);
        // The architrave sits on the room face of the wall (x -3.925).
        root.SetPositionAndRotation(new Vector3(-3.945f, WindowBase, WindowZ), Quaternion.Euler(0f, 90f, 0f));
        Place(P("window_frame"), root.position, 90f, root, false);

        GameObject Part(string model, Vector3 local, bool isStatic)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P(model)), root);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.identity;
            if (isStatic)
                MakeStatic(go);
            return go;
        }
        // Model (x, y, z) -> local (-x, z, -y).
        // Both sashes are lit the same way (see below), so the pair matches.
        var fixedSash = Part("window_sash", new Vector3(0.28f, 0.092f, -0.045f), false);
        fixedSash.name = "FixedSash";
        var sliding = Part("window_sash", new Vector3(-0.28f, 0.092f, 0.005f), false);
        sliding.name = "SlidingSash";

        var sashBox = sliding.AddComponent<BoxCollider>();
        sashBox.center = new Vector3(0f, 0.47f, 0f);
        sashBox.size = new Vector3(0.6f, 0.94f, 0.035f);
        var handle = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P("window_handle")), sliding.transform);
        handle.transform.localPosition = new Vector3(-0.25f, 0.4f, 0.02f);
        handle.transform.localRotation = Quaternion.identity;
        // It moves, so it cannot be lightmapped; the probes at the window sit in the
        // window light's beam and would light it (and its handle) white. Light it from
        // a point in the room instead, out of the beam.
        var anchor = new GameObject("SashLightAnchor").transform;
        anchor.SetParent(root, false);
        anchor.position = root.position + root.forward * 1.3f + Vector3.up * 0.4f;
        foreach (var r in sliding.GetComponentsInChildren<Renderer>().Concat(fixedSash.GetComponentsInChildren<Renderer>()))
        {
            r.probeAnchor = anchor;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
        var grip = new GameObject("Grip").transform;
        grip.SetParent(handle.transform, false);
        grip.localPosition = new Vector3(0f, 0.07f, 0.045f);
        var grille = Part("window_grille", new Vector3(0f, 0.08f, -0.2f), true);
        grille.name = "Grille";
        var bars = grille.AddComponent<BoxCollider>();
        bars.center = new Vector3(0f, 0.56f, 0f);
        bars.size = new Vector3(1.2f, 1.0f, 0.03f);

        // The gap the sash closes into: a thing left there stops it closing.
        var gap = new GameObject("Opening").transform;
        gap.SetParent(root, false);
        gap.localPosition = new Vector3(-0.28f, 0.56f, 0.005f);
        var gapBox = gap.gameObject.AddComponent<BoxCollider>();
        gapBox.isTrigger = true;
        gapBox.size = new Vector3(0.56f, 0.9f, 0.05f);
        gap.gameObject.layer = 2; // Ignore Raycast: it is a check, not something to aim at

        var outside = new GameObject("Street").AddComponent<AudioSource>();
        outside.transform.SetParent(root, false);
        outside.transform.localPosition = new Vector3(0f, 0.6f, -0.3f);
        outside.spatialBlend = 1f;
        outside.minDistance = 1.2f;
        outside.maxDistance = 14f;
        outside.playOnAwake = false;
        var sounds = sliding.AddComponent<AudioSource>();
        sounds.spatialBlend = 1f;
        sounds.minDistance = 1f;
        sounds.maxDistance = 10f;
        sounds.playOnAwake = false;

        var window = sliding.AddComponent<ApartmentWindow>();
        window.SetEntityId("window");
        // The sliding sash runs along the frame's model -X, which is its local +X here.
        window.Configure(sliding.transform, grip, new Vector3(0.56f, 0f, 0f), outside, sounds, gapBox);

        Alley(parent);
        Report.AppendLine("window: sliding sash with a handle, fixed sash, grille, a hole through the wall onto an alley");
    }

    /// What is outside: the ground a step down, the neighbour's wall across a narrow
    /// alley, a strip of morning sky. Unlit backdrops: there is nowhere to go.
    static void Alley(Transform parent)
    {
        var alley = new GameObject("Alley").transform;
        alley.SetParent(parent, false);
        Material Backdrop(string name, string texture, Color tint)
        {
            string path = $"Assets/Art/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Apartment/{texture}.png"));
            mat.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(mat);
            return mat;
        }
        void Quad(string name, Vector3 pos, Quaternion rot, Vector2 size, Material mat)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(alley, false);
            q.transform.SetPositionAndRotation(pos, rot);
            q.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        Quad("NeighbourWall", new Vector3(-5.35f, 1.4f, WindowZ), Quaternion.Euler(0f, -90f, 0f), new Vector2(3.4f, 3.8f), Backdrop("Alley wall", "alley_wall", new Color(0.78f, 0.78f, 0.8f)));
        Quad("AlleySky", new Vector3(-4.7f, 3.4f, WindowZ), Quaternion.Euler(-90f, 0f, 0f), new Vector2(1.4f, 3.4f), Backdrop("Alley sky", "alley_sky", Color.white));
        var ground = Paint("Alley ground", new Color(0.24f, 0.23f, 0.22f), 0.3f);
        Block("AlleyGround", new Vector3(-5.4f, -0.4f, WindowZ - 1.7f), new Vector3(-4.075f, -0.3f, WindowZ + 1.7f), ground, alley, false);
    }
}
