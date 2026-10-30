using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

/// <summary>
/// Builds Assets/Scenes/Apartment.unity: the crew's awful flat (8 x 6 m), the
/// corridor outside the front door, the opening's characters and objects, light,
/// and the session. Unity coordinates (north +Z). The front door is in the south
/// wall; the desk is on the east wall, in view from the door and the couch.
/// Run: return ApartmentSceneBuilder.Build();
/// </summary>
public static class ApartmentSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Apartment.unity";
    const string Props = "Assets/Art/Apartment/{0}.fbx";
    const string Pack = "Assets/Art/Models/{0}.fbx";
    const float Height = 2.6f;
    const float Wall = 0.15f;
    const float DoorX = 2.0f;

    static Transform _root;
    static readonly StringBuilder Report = new();

    public static string Build()
    {
        Report.Clear();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        _root = new GameObject("Apartment").transform;
        GameplaySceneSetup.EnsureLayers();

        Shell();
        Hallway();
        Furniture();
        var camera = PreviewCamera();
        Story(camera);
        Lighting();
        PostProcessing();

        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == ScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Report.AppendLine($"saved {ScenePath} (first in build settings)");
        return Report.ToString();
    }

    // Helpers -------------------------------------------------------------------------

    static Transform Group(string name, Transform parent = null)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent != null ? parent : _root, false);
        return t;
    }

    /// A static model with a box collider around it (unless collide is false).
    static GameObject Place(string path, Vector3 pos, float yaw, Transform parent, bool collide = true, bool isStatic = true)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
        {
            Report.AppendLine($"MISSING {path}");
            return new GameObject("MISSING");
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        if (collide)
            FitBox(go);
        if (isStatic)
            MakeStatic(go);
        return go;
    }

    static BoxCollider FitBox(GameObject go)
    {
        var bounds = new Bounds();
        bool first = true;
        var toLocal = go.transform.worldToLocalMatrix;
        foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
        {
            var b = filter.sharedMesh.bounds;
            var m = toLocal * filter.transform.localToWorldMatrix;
            for (int c = 0; c < 8; c++)
            {
                var p = m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1)));
                if (first) { bounds = new Bounds(p, Vector3.zero); first = false; }
                else bounds.Encapsulate(p);
            }
        }
        var box = go.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = bounds.size;
        return box;
    }

    static void MakeStatic(GameObject go)
    {
        var flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic
            | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
    }

    static GameObject Block(string name, Vector3 min, Vector3 max, Material material, Transform parent, bool collide = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = (min + max) * 0.5f;
        go.transform.localScale = max - min;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (!collide)
            Object.DestroyImmediate(go.GetComponent<Collider>());
        MakeStatic(go);
        return go;
    }

    static Material Paint(string name, Color colour, float smoothness = 0.12f)
    {
        string path = $"Assets/Art/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", colour);
        mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material Glow(string name, Color colour, float intensity)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Materials/{name}.mat");
        if (mat == null)
            return null;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", colour * intensity);
        // Glow only for the eye (and bloom); the real lights do the lighting, so
        // the bake does not sample tiny bright emitters into speckles.
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // The flat ------------------------------------------------------------------------

    static void Shell()
    {
        var shell = Group("Shell");
        var wall = Paint("Apartment wall", new Color(0.68f, 0.67f, 0.56f));
        var ceiling = Paint("Apartment ceiling", new Color(0.7f, 0.68f, 0.6f));
        var trim = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Trim.mat");
        var stain = Paint("Damp stain", new Color(0.42f, 0.37f, 0.27f));

        for (int x = -3; x <= 3; x += 2)
            for (int z = -2; z <= 2; z += 2)
                Place(string.Format(Pack, "floor_tile_2m"), new Vector3(x, 0f, z), 0f, shell, false);
        Block("FloorCollider", new Vector3(-4.2f, -0.3f, -3.2f), new Vector3(4.2f, 0f, 3.2f), wall, shell).GetComponent<MeshRenderer>().enabled = false;

        float h = Wall * 0.5f;
        Block("WallWest", new Vector3(-4f - h, 0f, -3f - h), new Vector3(-4f + h, Height, 3f + h), wall, shell);
        Block("WallEast", new Vector3(4f - h, 0f, -3f - h), new Vector3(4f + h, Height, 3f + h), wall, shell);
        Block("WallNorth", new Vector3(-4f, 0f, 3f - h), new Vector3(4f, Height, 3f + h), wall, shell);
        // South wall around the front door opening.
        Block("WallSouthA", new Vector3(-4f, 0f, -3f - h), new Vector3(DoorX - 0.54f, Height, -3f + h), wall, shell);
        Block("WallSouthB", new Vector3(DoorX + 0.54f, 0f, -3f - h), new Vector3(4f, Height, -3f + h), wall, shell);
        Block("WallSouthHeader", new Vector3(DoorX - 0.54f, 2.16f, -3f - h), new Vector3(DoorX + 0.54f, Height, -3f + h), wall, shell);
        Block("Ceiling", new Vector3(-4.1f, Height, -3.1f), new Vector3(4.1f, Height + 0.15f, 3.1f), ceiling, shell, false);

        // Skirting, a damp patch over the bucket.
        Block("SkirtingN", new Vector3(-3.92f, 0f, 2.9f), new Vector3(3.92f, 0.1f, 2.93f), trim, shell, false);
        Block("SkirtingW", new Vector3(-3.93f, 0f, -2.92f), new Vector3(-3.9f, 0.1f, 2.92f), trim, shell, false);
        Block("SkirtingE", new Vector3(3.9f, 0f, -2.92f), new Vector3(3.93f, 0.1f, 2.92f), trim, shell, false);
        var damp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        damp.name = "DampPatch";
        Object.DestroyImmediate(damp.GetComponent<Collider>());
        damp.transform.SetParent(shell, false);
        damp.transform.position = new Vector3(0.3f, Height - 0.004f, 0.9f);
        damp.transform.localScale = new Vector3(0.9f, 0.002f, 0.7f);
        damp.GetComponent<MeshRenderer>().sharedMaterial = stain;
        MakeStatic(damp);
        Report.AppendLine("shell: 8 x 6 m flat, door opening in the south wall");
    }

    static void Hallway()
    {
        var hall = Group("Corridor");
        var lino = Paint("Corridor lino", new Color(0.3f, 0.32f, 0.29f), 0.35f);
        var wall = Paint("Corridor wall", new Color(0.44f, 0.48f, 0.43f));
        var ceiling = Paint("Corridor ceiling", new Color(0.55f, 0.56f, 0.52f));
        float h = Wall * 0.5f;
        Block("CorridorFloor", new Vector3(-1.2f, -0.3f, -5.3f), new Vector3(7.2f, 0f, -3f), lino, hall);
        Block("CorridorWallSouth", new Vector3(-1.2f, 0f, -5.2f - h), new Vector3(7.2f, Height, -5.2f + h), wall, hall);
        Block("CorridorWallWest", new Vector3(-1.2f - h, 0f, -5.2f), new Vector3(-1.2f + h, Height, -3f), wall, hall);
        Block("CorridorWallEast", new Vector3(7.2f - h, 0f, -5.2f), new Vector3(7.2f + h, Height, -3f), wall, hall);
        Block("CorridorWallNorthEast", new Vector3(4f, 0f, -3f - h), new Vector3(7.2f, Height, -3f + h), wall, hall);
        Block("CorridorCeiling", new Vector3(-1.2f, Height, -5.3f), new Vector3(7.2f, Height + 0.15f, -3f), ceiling, hall, false);
        // A neighbour's door across the corridor, for company.
        Place(string.Format(Props, "door_frame"), new Vector3(4.5f, 0f, -5.12f), 180f, hall, false);
        Place(string.Format(Props, "door_leaf"), new Vector3(4.05f, 0f, -5.14f), 180f, hall, false);
        Report.AppendLine("corridor: 8.4 x 2.2 m outside the front door");
    }

    static void Furniture()
    {
        var f = Group("Furniture");
        string P(string n) => string.Format(Props, n);
        Place(P("couch"), new Vector3(-3.45f, 0f, -1.6f), 90f, f);
        Place(P("mattress"), new Vector3(-3.3f, 0f, 1.7f), 0f, f);
        Place(P("mattress"), new Vector3(-1.1f, 0f, 2.3f), 90f, f);
        Place(P("rug"), new Vector3(-1.6f, 0f, -0.9f), 90f, f, false);
        Place(string.Format(Pack, "storage_crate"), new Vector3(-2.3f, 0f, -1.5f), 8f, f);
        Place(P("pizza_box"), new Vector3(-2.3f, 0.46f, -1.5f), 20f, f, false);
        Place(P("kitchen_counter"), new Vector3(2.4f, 0f, 2.6f), 180f, f);
        Place(P("fridge"), new Vector3(3.55f, 0f, 2.58f), 180f, f);
        Place(P("microwave"), new Vector3(1.9f, 0.9f, 2.68f), 180f, f);
        Place(P("trash_bag"), new Vector3(3.4f, 0f, -2.5f), 0f, f);
        Place(P("trash_bag"), new Vector3(3.62f, 0f, -1.95f), 50f, f);
        Place(P("dead_plant"), new Vector3(-3.6f, 0f, -0.1f), 0f, f);
        Place(P("window"), new Vector3(-3.87f, 0.9f, 0.6f), 90f, f, false);
        var bulb = Place(P("bare_bulb"), new Vector3(0f, Height, 0f), 0f, f, false);
        foreach (var r in bulb.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = ShadowCastingMode.Off;
        Place(string.Format(Pack, "mop_bucket"), new Vector3(0.3f, 0f, 0.9f), 15f, f);

        var drip = new GameObject("Leak");
        drip.transform.SetParent(f, false);
        drip.transform.position = new Vector3(0.3f, 0f, 0.9f);
        drip.AddComponent<Drip>().Configure(Height - 0.05f, Paint("Water", new Color(0.55f, 0.7f, 0.85f), 0.9f));

        // Clutter anyone can pick up and throw about.
        var clutter = Group("Clutter", f);
        GameplaySceneSetup.MakeProp("envelope_1", "clutter", "an unpaid bill", P("envelope"), new Vector3(1.6f, 0.01f, -2.55f), 20f, 0.1f, clutter);
        GameplaySceneSetup.MakeProp("envelope_2", "clutter", "a final final notice", P("envelope"), new Vector3(2.35f, 0.01f, -2.35f), -35f, 0.1f, clutter);
        GameplaySceneSetup.MakeProp("envelope_3", "clutter", "a letter for \"The Occupant\"", P("envelope"), new Vector3(2.0f, 0.01f, -2.75f), 80f, 0.1f, clutter);
        GameplaySceneSetup.MakeProp("takeout_1", "clutter", "a takeout box (old)", P("takeout_box"), new Vector3(-2.15f, 0.52f, -1.38f), 30f, 0.3f, clutter);
        GameplaySceneSetup.MakeProp("takeout_2", "clutter", "a takeout box (older)", P("takeout_box"), new Vector3(-0.9f, 0.01f, -1.9f), -60f, 0.3f, clutter);
        GameplaySceneSetup.MakeProp("takeout_3", "clutter", "a takeout box (do not open)", P("takeout_box"), new Vector3(2.9f, 0.92f, 2.5f), 10f, 0.3f, clutter);
        Report.AppendLine("furniture: couch, 2 mattresses, kitchen, fridge, microwave, desk corner, leak + bucket, 6 pickups");
    }

    static Camera PreviewCamera()
    {
        var go = new GameObject("PreviewCamera");
        go.transform.SetParent(_root, false);
        go.transform.SetPositionAndRotation(new Vector3(-2.6f, 1.6f, -2.4f), Quaternion.Euler(8f, 60f, 0f));
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 62f;
        cam.nearClipPlane = 0.05f;
        var data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        return cam;
    }

    // The story -------------------------------------------------------------------------

    static void Story(Camera previewCamera)
    {
        var story = Group("Story");
        string P(string n) => string.Format(Props, n);
        var library = GameplaySceneSetup.BuildWorkerLibrary(Report);
        var pad = GameplaySceneSetup.TransparentUnlit("Socket pad", new Color(0.25f, 0.85f, 0.72f, 0.5f));
        var ghost = GameplaySceneSetup.TransparentUnlit("Socket ghost", new Color(0.4f, 1f, 0.7f, 0.38f));

        // Front door: a frame, and a leaf that swings into the flat on its hinge.
        var doorGo = new GameObject("FrontDoor");
        doorGo.transform.SetParent(story, false);
        doorGo.transform.position = new Vector3(DoorX, 0f, -3f);
        Place(P("door_frame"), doorGo.transform.position, 0f, doorGo.transform, false);
        var pivot = new GameObject("Hinge").transform;
        pivot.SetParent(doorGo.transform, false);
        // The leaf extends toward -X from its hinge (Unity mirrors the modelled +X).
        pivot.position = new Vector3(DoorX + 0.45f, 0f, -3f);
        var leaf = Place(P("door_leaf"), pivot.position, 0f, pivot, true, false);
        leaf.name = "Leaf";
        var door = doorGo.AddComponent<FrontDoor>();
        door.SetEntityId("front_door");
        door.Configure(pivot);
        var audio = doorGo.AddComponent<AudioSource>();
        audio.spatialBlend = 1f;
        audio.minDistance = 1.5f;
        audio.maxDistance = 20f;
        audio.playOnAwake = false;
        // Nobody walks out into the corridor before there is somewhere to go. On the
        // player layer, so it stops bodies but not the aim (the landlord stays clickable).
        var threshold = new GameObject("Threshold");
        threshold.transform.SetParent(doorGo.transform, false);
        threshold.transform.position = new Vector3(DoorX, 1f, -3.12f);
        threshold.layer = GameLayers.Player;
        threshold.AddComponent<BoxCollider>().size = new Vector3(1.1f, 2f, 0.1f);

        // The landlord, with spots to knock from, stand in and leave to.
        Transform Spot(string name, Vector3 at, float yaw)
        {
            var t = new GameObject(name).transform;
            t.SetParent(story, false);
            t.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            return t;
        }
        var knock = Spot("LandlordKnockSpot", new Vector3(DoorX + 0.1f, 0f, -3.55f), 0f);
        var doorway = Spot("LandlordDoorwaySpot", new Vector3(DoorX, 0f, -3.42f), 0f);
        var exit = Spot("LandlordExitSpot", new Vector3(6.6f, 0f, -4.2f), 90f);
        var landlordGo = new GameObject("Landlord");
        landlordGo.transform.SetParent(story, false);
        landlordGo.transform.SetPositionAndRotation(knock.position, knock.rotation);
        landlordGo.AddComponent<CapsuleCollider>();
        var landlord = landlordGo.AddComponent<Landlord>();
        landlord.SetEntityId("landlord");
        landlord.Configure(library, knock, doorway, exit, audio);

        // The bill (in the landlord's hand from the start) and its spot on the desk.
        var bill = GameplaySceneSetup.MakeProp("rent_bill", "rent_bill", "the rent bill", P("rent_bill"), new Vector3(DoorX, 1f, -3.6f), 0f, 0.05f, story);

        // The desk corner.
        var deskGo = new GameObject("ComputerDesk");
        deskGo.transform.SetParent(story, false);
        deskGo.transform.position = new Vector3(3.6f, 0f, -0.6f);
        Place(P("desk"), new Vector3(3.6f, 0f, -0.6f), -90f, deskGo.transform, false);
        Place(P("computer_monitor"), new Vector3(3.72f, 0.75f, -0.62f), -90f, deskGo.transform, false);
        Place(P("computer_keyboard"), new Vector3(3.42f, 0.75f, -0.62f), -90f, deskGo.transform, false);
        Place(P("computer_tower"), new Vector3(3.62f, 0f, -1.12f), -90f, deskGo.transform, false);
        Place(P("desk_chair"), new Vector3(2.95f, 0f, -0.62f), 90f, story, false);
        // The desktop, and the monitor above it (the bill spot stays free to aim at).
        var deskBox = deskGo.AddComponent<BoxCollider>();
        deskBox.center = new Vector3(0f, 0.38f, 0f);
        deskBox.size = new Vector3(0.64f, 0.76f, 1.22f);
        var monitorBox = deskGo.AddComponent<BoxCollider>();
        monitorBox.center = new Vector3(0.1f, 0.94f, -0.02f);
        monitorBox.size = new Vector3(0.42f, 0.38f, 0.46f);
        var billSpot = GameplaySceneSetup.MakeSocket("bill_spot", "the desk", "rent_bill", "", 0.14f, new Vector3(3.62f, 0.752f, -0.05f), pad, ghost, story, 90f);
        var seat = Spot("DeskSeat", new Vector3(2.92f, 0.05f, -0.62f), 90f);
        var view = Spot("DeskScreenView", new Vector3(3.1f, 0.98f, -0.62f), 90f);

        // Waking up on the couch, head at the south end, looking up past your feet.
        var eye = Spot("WakeEye", new Vector3(-3.42f, 0.72f, -2.3f), 0f);
        eye.rotation = Quaternion.LookRotation(new Vector3(0.35f, 0.8f, 0.5f), Vector3.up) * Quaternion.Euler(0f, 0f, 12f);

        var spawns = new[]
        {
            Spot("Spawn0", new Vector3(-2.65f, 0.05f, -1.75f), 105f),
            Spot("Spawn1", new Vector3(-2.5f, 0.05f, 1.4f), 120f),
            Spot("Spawn2", new Vector3(-1.1f, 0.05f, 1.5f), 160f),
            Spot("Spawn3", new Vector3(0.6f, 0.05f, -1.0f), 150f),
        };

        // HUD, the computer's screen, the session and the director.
        var hudGo = new GameObject("HUD");
        hudGo.transform.SetParent(story, false);
        hudGo.AddComponent<UIDocument>().panelSettings = GameplaySceneSetup.BuildPanelSettings();
        var hud = hudGo.AddComponent<Hud>();
        var screen = hudGo.AddComponent<ComputerScreen>();
        screen.Configure(hud);
        var desk = deskGo.AddComponent<ComputerDesk>();
        desk.SetEntityId("computer_desk");
        desk.Configure(seat, view, screen);

        var sessionGo = new GameObject("Session");
        sessionGo.transform.SetParent(story, false);
        var session = sessionGo.AddComponent<GameSession>();
        session.Configure(library, hud, spawns, previewCamera);
        sessionGo.AddComponent<OpeningDirector>().Configure(session, landlord, door, bill, billSpot, eye);
        Report.AppendLine("story: front door, landlord + spots, rent bill + desk spot, computer desk, wake pose, 4 spawns, HUD, session, director");
    }

    // Light -------------------------------------------------------------------------------

    static void Lighting()
    {
        var lights = Group("Lights");
        Glow("Window glass", new Color(0.78f, 0.86f, 0.95f), 2.2f);
        Glow("Bulb", new Color(1f, 0.85f, 0.6f), 6f);

        Light Make(string name, LightType type, Vector3 pos, Quaternion rot, Color colour, float intensity, float range, float angle = 60f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(lights, false);
            go.transform.SetPositionAndRotation(pos, rot);
            var l = go.AddComponent<Light>();
            l.type = type;
            l.color = colour;
            l.intensity = intensity;
            l.range = range;
            l.spotAngle = angle;
            l.shadows = LightShadows.Soft;
            l.lightmapBakeType = LightmapBakeType.Baked;
            return l;
        }
        // Grey morning through a dirty window, a weak bulb, a buzzing corridor tube.
        Make("WindowLight", LightType.Spot, new Vector3(-3.7f, 1.55f, 0.6f), Quaternion.Euler(28f, 90f, 0f), new Color(0.75f, 0.84f, 1f), 14f, 9f, 120f);
        Make("BulbLight", LightType.Point, new Vector3(0f, Height - 0.6f, 0f), Quaternion.identity, new Color(1f, 0.8f, 0.55f), 5f, 8f);
        Make("KitchenFill", LightType.Point, new Vector3(2.6f, 2.0f, 1.6f), Quaternion.identity, new Color(1f, 0.85f, 0.65f), 1.2f, 5f);
        Make("CorridorTube", LightType.Point, new Vector3(2.5f, Height - 0.15f, -4.1f), Quaternion.identity, new Color(0.85f, 1f, 0.82f), 3.5f, 7f);
        Make("CorridorTubeFar", LightType.Point, new Vector3(5.8f, Height - 0.15f, -4.1f), Quaternion.identity, new Color(0.85f, 1f, 0.82f), 2.2f, 6f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.34f, 0.36f, 0.4f);
        RenderSettings.ambientEquatorColor = new Color(0.27f, 0.26f, 0.24f);
        RenderSettings.ambientGroundColor = new Color(0.15f, 0.13f, 0.11f);
        RenderSettings.skybox = null;

        var settings = new LightingSettings
        {
            name = "ApartmentLighting",
            lightmapper = LightingSettings.Lightmapper.ProgressiveGPU,
            bakedGI = true,
            realtimeGI = false,
            lightmapResolution = 24f,
            lightmapPadding = 4,
            lightmapMaxSize = 2048,
            directionalityMode = LightmapsMode.CombinedDirectional,
            ao = true,
            aoMaxDistance = 0.6f,
            maxBounces = 3,
            indirectScale = 1.3f,
            // A small room lit by a few close lights: plenty of samples and a
            // denoiser, or the walls and floor come out speckled.
            directSampleCount = 64,
            indirectSampleCount = 2048,
            environmentSampleCount = 512,
            filteringMode = LightingSettings.FilterMode.Advanced,
            denoiserTypeDirect = LightingSettings.DenoiserType.OpenImage,
            denoiserTypeIndirect = LightingSettings.DenoiserType.OpenImage,
            denoiserTypeAO = LightingSettings.DenoiserType.OpenImage,
            filterTypeDirect = LightingSettings.FilterType.None,
            filterTypeIndirect = LightingSettings.FilterType.None,
            filterTypeAO = LightingSettings.FilterType.None,
        };
        const string path = "Assets/Scenes/ApartmentLighting.lighting";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(settings, path);
        Lightmapping.lightingSettings = settings;

        var probes = new GameObject("LightProbes");
        probes.transform.SetParent(_root, false);
        var positions = new List<Vector3>();
        for (float x = -3.5f; x <= 3.5f; x += 1f)
            for (float z = -2.5f; z <= 2.5f; z += 1f)
                foreach (float y in new[] { 0.4f, 1.3f, 2.2f })
                    positions.Add(new Vector3(x, y, z));
        for (float x = -0.8f; x <= 6.8f; x += 1f)
            foreach (float y in new[] { 0.4f, 1.3f, 2.2f })
                positions.Add(new Vector3(x, y, -4.1f));
        probes.AddComponent<LightProbeGroup>().probePositions = positions.ToArray();

        var reflection = new GameObject("RoomReflections");
        reflection.transform.SetParent(_root, false);
        reflection.transform.position = new Vector3(0f, 1.3f, 0f);
        var probe = reflection.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Baked;
        probe.size = new Vector3(8f, 2.6f, 6f);
        probe.boxProjection = true;
        probe.resolution = 128;
        Report.AppendLine($"lights: window, bulb, kitchen fill, 2 corridor tubes (baked); probes {positions.Count}");
    }

    static void PostProcessing()
    {
        const string path = "Assets/Settings/ApartmentVolume.asset";
        AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1f);
        bloom.intensity.Override(0.5f);
        var colour = profile.Add<ColorAdjustments>(true);
        colour.postExposure.Override(0.7f);
        colour.contrast.Override(10f);
        colour.saturation.Override(-14f);
        profile.Add<WhiteBalance>(true).temperature.Override(-6f);
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.3f);
        vignette.smoothness.Override(0.5f);
        foreach (var component in profile.components)
            AssetDatabase.AddObjectToAsset(component, profile);
        AssetDatabase.SaveAssets();
        var go = new GameObject("PostProcessing");
        go.transform.SetParent(_root, false);
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
    }
}
