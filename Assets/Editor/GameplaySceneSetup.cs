using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

/// <summary>
/// Adds the playable layer to the venue scene: physics layers, the worker
/// animator and library, the props and sockets of the Wedding Crew tasks, the
/// coordinator, spawn points, the HUD and the session. Called by
/// VenueSceneBuilder.Build(); numbers are the Godot VenueLayout ones.
/// </summary>
public static partial class GameplaySceneSetup
{
    const string ControllerPath = "Assets/Art/Characters/Worker.controller";
    const string LibraryPath = "Assets/Settings/Workers.asset";
    const string ThemePath = "Assets/Settings/HudTheme.tss";
    const string PanelPath = "Assets/Settings/HudPanel.asset";
    static readonly string[] WorkerModels = { "worker_host", "worker_sound", "worker_catering", "worker_runner" };

    // Godot layout numbers.
    static readonly Vector3 PrepTable = new(0f, 0f, 3.9f);
    static readonly Vector3 SignTable = new(-5.5f, 0f, 3.9f);
    const float PrepTop = 0.9f;
    static readonly Vector3 DessertStand = new(12.7f, 1.225f, -5f);
    static readonly Vector3[] GuestTables = { new(-9f, 0f, -9f), new(-9f, 0f, -4f), new(9f, 0f, -9f) };
    const float TableTop = 0.83f;
    static readonly (string id, string name, float radius, float height)[] CakeLayers =
    {
        ("cake_base", "the bottom cake layer", 0.33f, 0.26f),
        ("cake_middle", "the middle cake layer", 0.25f, 0.23f),
        ("cake_top", "the top cake layer", 0.175f, 0.21f),
    };
    static readonly string[] CakeSockets = { "cake_socket_base", "cake_socket_middle", "cake_socket_top" };

    static Vector3 G(float x, float y, float z) => new(x, y, -z);
    static float Yaw(float godotDegrees) => 180f - godotDegrees;

    public static void Add(Transform root, Camera previewCamera, StringBuilder report)
    {
        EnsureLayers();
        var library = BuildWorkerLibrary(report);
        var pad = TransparentUnlit("Socket pad", new Color(0.25f, 0.85f, 0.72f, 0.5f));
        var ghost = TransparentUnlit("Socket ghost", new Color(0.4f, 1f, 0.7f, 0.38f));

        var gameplay = new GameObject("Gameplay").transform;
        gameplay.SetParent(root, false);

        // Cake layers wait on the prep table; their sockets stack on the dessert stand.
        string previous = "";
        PlacementSocket below = null;
        float[] startX = { -0.55f, 0.12f, 0.62f };
        float stackY = DessertStand.y;
        for (int i = 0; i < CakeLayers.Length; i++)
        {
            var layer = CakeLayers[i];
            MakeProp(layer.id, "cake_tier", layer.name, layer.id, G(PrepTable.x + startX[i], PrepTop + 0.003f, PrepTable.z), Yaw(0f), 2f, gameplay, PropCategory.Wide);
            var socket = MakeSocket(CakeSockets[i], i == 0 ? "the cake stand" : "the cake", "cake_tier", previous, layer.radius,
                G(DessertStand.x, stackY, DessertStand.z), pad, ghost, gameplay);
            // Each layer sits on the actual top of the one below; line-ups are judged (the shift sets how strictly).
            socket.ConfigureAlignment(0.3f, below);
            below = socket;
            previous = CakeSockets[i];
            stackY += layer.height;
        }
        // The topper goes on the top layer.
        var topper = MakeSocket(CakeChallenge.TopperSocket, "the top of the cake", "cake_topper", previous, 0.07f, G(DessertStand.x, stackY, DessertStand.z), pad, ghost, gameplay);
        topper.ConfigureAlignment(0f, below);
        MakeProp("cake_topper", "cake_topper", "the cake topper", "cake_topper", G(SignTable.x + 0.75f, PrepTop + 0.003f, SignTable.z), Yaw(0f), 0.2f, gameplay, PropCategory.Small);

        // Table signs wait on the sign table; each guest table has a sign spot.
        for (int i = 0; i < 3; i++)
        {
            MakeProp($"sign_{i + 1}", "table_sign", $"the sign for table {i + 1}", $"table_sign_{i + 1}",
                G(SignTable.x - 0.7f + 0.45f * i, PrepTop + 0.003f, SignTable.z), Yaw(0f), 0.4f, gameplay);
            var t = GuestTables[i];
            MakeSocket($"table_socket_{i + 1}", $"table {i + 1}", "table_sign", "", 0.16f, G(t.x, TableTop, t.z), pad, ghost, gameplay);
        }

        var coordinatorGo = new GameObject("Coordinator");
        coordinatorGo.transform.SetParent(gameplay, false);
        var coordinator = coordinatorGo.AddComponent<Coordinator>();
        coordinatorGo.AddComponent<CoordinatorMind>();
        coordinatorGo.AddComponent<CoordinatorHandoff>().SetEntityId("coordinator");
        coordinator.Configure(library);

        var spawns = new Transform[4];
        for (int i = 0; i < spawns.Length; i++)
        {
            spawns[i] = new GameObject($"Spawn{i}").transform;
            spawns[i].SetParent(gameplay, false);
            spawns[i].SetPositionAndRotation(G(-1.2f + 0.8f * i, 0.05f, 5.6f), Quaternion.Euler(0f, Yaw(180f), 0f));
        }

        var hudGo = new GameObject("HUD");
        hudGo.transform.SetParent(gameplay, false);
        var document = hudGo.AddComponent<UIDocument>();
        document.panelSettings = BuildPanelSettings();
        var hud = hudGo.AddComponent<Hud>();

        var sessionGo = new GameObject("Session");
        sessionGo.transform.SetParent(gameplay, false);
        var session = sessionGo.AddComponent<GameSession>();
        session.Configure(library, hud, spawns, previewCamera);

        // The coordinator meets the crew by the prep table, then patrols.
        var greet = new GameObject("CoordinatorGreetSpot").transform;
        greet.SetParent(gameplay, false);
        greet.SetPositionAndRotation(new Vector3(2.0f, 0f, -4.6f), Quaternion.Euler(0f, -112f, 0f));
        var shift = sessionGo.AddComponent<WeddingShift>();
        shift.Configure(session, coordinator, greet);
        AddShiftContent(gameplay, shift, pad, ghost, report);
        report.AppendLine("gameplay: 3 cake layers + stand sockets, 3 signs + table sockets, coordinator, 4 spawns, HUD, session");
    }

    // Props and sockets -----------------------------------------------------------

    public static PickupProp MakeProp(string id, string kind, string display, string model, Vector3 position, float yaw, float mass, Transform parent, PropCategory category = PropCategory.Small)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(model.Contains("/") ? model : $"Assets/Art/Models/{model}.fbx");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        go.name = id;
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        GameLayers.SetRecursively(go, GameLayers.Prop);

        // One box around the visible meshes, in the prop's own space.
        var bounds = new Bounds();
        bool first = true;
        var toLocal = go.transform.worldToLocalMatrix;
        foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
        {
            var b = filter.sharedMesh.bounds;
            var m = toLocal * filter.transform.localToWorldMatrix;
            for (int c = 0; c < 8; c++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                var p = m.MultiplyPoint3x4(corner);
                if (first) { bounds = new Bounds(p, Vector3.zero); first = false; }
                else bounds.Encapsulate(p);
            }
        }
        var box = go.AddComponent<BoxCollider>();
        box.center = bounds.center;
        // Never paper-thin: a 1.2 cm minimum keeps sheets from tunnelling or jittering.
        box.size = Vector3.Max(bounds.size, Vector3.one * 0.012f);
        var body = go.AddComponent<Rigidbody>();
        body.mass = mass;
        var prop = go.AddComponent<PickupProp>();
        prop.Configure(id, kind, display, category);
        return prop;
    }

    public static PlacementSocket MakeSocket(string id, string display, string kind, string requires, float radius, Vector3 position, Material pad, Material ghost, Transform parent, float yaw = 180f)
    {
        var go = new GameObject($"Socket_{id}");
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        go.layer = GameLayers.Socket;
        var socket = go.AddComponent<PlacementSocket>();
        socket.Configure(id, display, kind, requires, radius, pad, ghost);
        return socket;
    }

    public static Material TransparentUnlit(string name, Color colour)
    {
        string path = $"Assets/Art/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", colour);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Workers ----------------------------------------------------------------------

    public static WorkerLibrary BuildWorkerLibrary(StringBuilder report)
    {
        // One state per clip, no transitions: WorkerAvatar cross-fades by name.
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        var machine = controller.layers[0].stateMachine;
        var clips = AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/Characters/{WorkerModels[0]}.fbx")
            .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();
        foreach (var clip in clips)
        {
            var state = machine.AddState(clip.name);
            state.motion = clip;
            if (clip.name == WorkerAvatar.Idle)
                machine.defaultState = state;
        }

        var library = AssetDatabase.LoadAssetAtPath<WorkerLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<WorkerLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }
        library.models = WorkerModels.Select(m => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Characters/{m}.fbx")).ToArray();
        library.controller = controller;
        GameObject Part(string n) => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Hands/{n}.fbx");
        library.armUpper = Part("arm_upper");
        library.armForearm = Part("arm_forearm");
        library.handPalm = Part("hand_palm");
        library.fingerBase = Part("finger_base");
        library.fingerTip = Part("finger_tip");
        library.thumbBase = Part("thumb_base");
        library.thumbTip = Part("thumb_tip");
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        report.AppendLine($"workers: {library.models.Count(m => m != null)} models, controller states: {string.Join(", ", clips.Select(c => c.name))}");
        return library;
    }

    // HUD ---------------------------------------------------------------------------

    public static PanelSettings BuildPanelSettings()
    {
        if (!File.Exists(ThemePath))
        {
            File.WriteAllText(ThemePath, "@import url(\"unity-theme://default\");\n");
            AssetDatabase.ImportAsset(ThemePath);
        }
        var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
        if (panel == null)
        {
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panel, PanelPath);
        }
        panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panel.referenceResolution = new Vector2Int(1920, 1080);
        panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        panel.match = 0.5f;
        EditorUtility.SetDirty(panel);
        AssetDatabase.SaveAssets();
        return panel;
    }

    /// Names the physics layers the game uses (see GameLayers).
    public static void EnsureLayers()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        layers.GetArrayElementAtIndex(GameLayers.Prop).stringValue = "Prop";
        layers.GetArrayElementAtIndex(GameLayers.Socket).stringValue = "Socket";
        layers.GetArrayElementAtIndex(GameLayers.Player).stringValue = "Player";
        tagManager.ApplyModifiedProperties();
    }
}
