using System.Collections.Generic;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Builds Assets/Scenes/Wedding.unity: the hotel venue from the wedding asset pack.
/// The layout is ported from the Godot version (docs there: VenueLayout). Godot is
/// right-handed with -Z forward, Unity left-handed with +Z forward, so positions
/// go through G() (z flipped) and yaws through Yaw() (180 - Godot yaw). North,
/// where the stage is, is +Z here.
///
/// Architecture and furniture are static so they bake into lightmaps; moving
/// things (players, props, the coordinator) are lit by a light probe grid.
/// Run: return VenueSceneBuilder.Build();
/// </summary>
public static class VenueSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Wedding.unity";
    const string ModelPath = "Assets/Art/Models/{0}.fbx";
    const string MaterialPath = "Assets/Art/Materials/{0}.mat";

    // Layout, in Godot numbers (see the Godot VenueLayout).
    const float HalfWidth = 14f;
    const float NorthZ = -16f;
    const float SouthZ = 8f;
    const float DividerZ = 2f;
    const float WallHeight = 3f;
    const float DoorX = 7f;
    const float DoorWidth = 2f;
    const float TableTop = 0.83f;
    const float StageHeight = 0.3f;
    const float DessertTableTop = 0.94f;

    static readonly Vector3[] GuestTables = { new(-9f, 0f, -9f), new(-9f, 0f, -4f), new(9f, 0f, -9f) };
    static readonly Vector3[] DecorTables = { new(9f, 0f, -4f), new(-3.5f, 0f, -4f), new(3.5f, 0f, -4f), new(-3.5f, 0f, -9.5f), new(3.5f, 0f, -9.5f) };
    static readonly Vector3 SoundDesk = new(7.5f, 0f, -11.2f);
    static readonly Vector3 PhotoDisplay = new(-13.1f, 0f, -7f);
    static readonly Vector3 DessertStand = new(12.7f, 1.225f, -5f);
    static readonly Vector3 PrepTable = new(0f, 0f, 3.9f);
    static readonly Vector3 SignTable = new(-5.5f, 0f, 3.9f);

    static Transform _root;
    static readonly StringBuilder Report = new();

    static Vector3 G(float x, float y, float z) => new(x, y, -z);
    static Vector3 G(Vector3 godot) => new(godot.x, godot.y, -godot.z);
    static float Yaw(float godotDegrees) => 180f - godotDegrees;

    public static string Build()
    {
        Report.Clear();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        _root = new GameObject("Venue").transform;

        Shell();
        Stage();
        SoundArea();
        TablesAndChairs();
        DessertStation();
        PhotoCorner();
        BackOfHouse();
        Lighting();
        Probes();
        PostProcessing();
        GameplaySceneSetup.Add(_root, Camera(), Report);

        EditorSceneManager.SaveScene(scene, ScenePath);
        Navigation.Bake(_root.gameObject, "Assets/Scenes/Wedding/NavMesh.asset", Report);
        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == ScenePath))
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Report.AppendLine($"saved {ScenePath}");
        return Report.ToString();
    }

    // Helpers ---------------------------------------------------------------------

    static GameObject Place(string model, Vector3 unityPos, float unityYaw, Transform parent = null, float scale = 1f, bool isStatic = true)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(string.Format(ModelPath, model));
        if (asset == null)
        {
            Report.AppendLine($"MISSING model {model}");
            return new GameObject($"MISSING_{model}");
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent != null ? parent : _root);
        go.transform.SetPositionAndRotation(unityPos, Quaternion.Euler(0f, unityYaw, 0f));
        go.transform.localScale = Vector3.one * scale;
        if (isStatic)
            MakeStatic(go);
        return go;
    }

    static void MakeStatic(GameObject go)
    {
        var flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic
            | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
    }

    static GameObject Solid(string name, Vector3 size, Vector3 unityCenter, Quaternion rotation, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : _root, false);
        go.transform.SetPositionAndRotation(unityCenter, rotation);
        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.NavigationStatic);
        return go;
    }

    static GameObject Block(string name, Vector3 size, Vector3 unityCenter, string material, bool collider, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent != null ? parent : _root, false);
        go.transform.position = unityCenter;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = Mat(material);
        if (!collider)
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        MakeStatic(go);
        return go;
    }

    static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(string.Format(MaterialPath, name));

    static Material Emissive(string name, Color colour, float intensity)
    {
        string path = string.Format(MaterialPath, name);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", colour);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", colour * intensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// A plain matte painted surface.
    static Material Paint(string name, Color colour)
    {
        string path = string.Format(MaterialPath, name);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", colour);
        mat.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Transform Group(string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(_root, false);
        return t;
    }

    // Architecture ----------------------------------------------------------------

    static void Shell()
    {
        var shell = Group("Shell");
        // Wood floor tiles in the ballroom, a plain service floor behind the scenes.
        var floor = Group("FloorTiles");
        floor.SetParent(shell, false);
        for (int ix = -13; ix <= 13; ix += 2)
            for (int iz = -15; iz <= 1; iz += 2)
                Place("floor_tile_2m", G(ix, 0f, iz), 0f, floor);
        Solid("BallroomFloorCollider", new Vector3(HalfWidth * 2f + 0.4f, 0.3f, DividerZ - NorthZ + 0.2f), G(0f, -0.15f, (NorthZ + DividerZ) * 0.5f), Quaternion.identity, shell);
        Block("ServiceFloor", new Vector3(HalfWidth * 2f + 0.4f, 0.3f, SouthZ - DividerZ + 0.2f), G(0f, -0.15f, (DividerZ + SouthZ) * 0.5f), "Charcoal", true, shell)
            .GetComponent<MeshRenderer>().sharedMaterial = Paint("Service vinyl", new Color(0.5f, 0.48f, 0.44f));

        // Walls: pack panels facing inward, colliders separate.
        var walls = Group("Walls");
        walls.SetParent(shell, false);
        for (int ix = -13; ix <= 13; ix += 2)
        {
            Place("wall_panel_2m", G(ix, 0f, NorthZ), Yaw(0f), walls);
            Place("wall_panel_2m", G(ix, 0f, SouthZ), Yaw(180f), walls);
        }
        for (int iz = -15; iz <= 7; iz += 2)
        {
            Place("wall_panel_2m", G(-HalfWidth, 0f, iz), Yaw(90f), walls);
            Place("wall_panel_2m", G(HalfWidth, 0f, iz), Yaw(-90f), walls);
        }
        float gap = DoorWidth * 0.5f;
        var runs = new[] { new Vector2(-HalfWidth, -DoorX - gap), new Vector2(-DoorX + gap, DoorX - gap), new Vector2(DoorX + gap, HalfWidth) };
        foreach (var run in runs)
        {
            for (float x = run.x + 1f; x < run.y; x += 2f)
                Place("wall_panel_2m", G(x, 0f, DividerZ), Yaw(180f), walls);
            Solid("DividerCollider", new Vector3(run.y - run.x, WallHeight, 0.3f), G((run.x + run.y) * 0.5f, WallHeight * 0.5f, DividerZ), Quaternion.identity, walls);
        }
        Solid("NorthWallCollider", new Vector3(HalfWidth * 2f + 0.6f, WallHeight, 0.3f), G(0f, WallHeight * 0.5f, NorthZ), Quaternion.identity, walls);
        Solid("SouthWallCollider", new Vector3(HalfWidth * 2f + 0.6f, WallHeight, 0.3f), G(0f, WallHeight * 0.5f, SouthZ), Quaternion.identity, walls);
        Solid("WestWallCollider", new Vector3(0.3f, WallHeight, SouthZ - NorthZ + 0.6f), G(-HalfWidth, WallHeight * 0.5f, (NorthZ + SouthZ) * 0.5f), Quaternion.identity, walls);
        Solid("EastWallCollider", new Vector3(0.3f, WallHeight, SouthZ - NorthZ + 0.6f), G(HalfWidth, WallHeight * 0.5f, (NorthZ + SouthZ) * 0.5f), Quaternion.identity, walls);

        Block("Ceiling", new Vector3(HalfWidth * 2f + 0.6f, 0.2f, SouthZ - NorthZ + 0.6f), G(0f, WallHeight + 0.1f, (NorthZ + SouthZ) * 0.5f), "Plaster", false, shell)
            .GetComponent<MeshRenderer>().sharedMaterial = Paint("Ceiling paint", new Color(0.34f, 0.38f, 0.46f));
        foreach (float sx in new[] { -1f, 1f })
            Place("staff_doorway", G(sx * DoorX, 0f, DividerZ), Yaw(180f), shell);
        Report.AppendLine("shell: floor tiles, walls, ceiling, doorways");
    }

    static void Stage()
    {
        var stage = Group("Stage");
        foreach (int ix in new[] { -1, 0, 1 })
            foreach (float z in new[] { NorthZ + 0.915f, NorthZ + 2.745f })
                Place("stage_platform", G(ix * 3.8f, 0f, z), Yaw(0f), stage);
        const float depth = 3.66f;
        Solid("StageCollider", new Vector3(11.4f, StageHeight, depth), G(0f, StageHeight * 0.5f, NorthZ + depth * 0.5f), Quaternion.identity, stage);
        float front = NorthZ + depth;
        Place("stage_steps", G(0f, 0f, front + 0.35f), Yaw(0f), stage);
        float slope = Mathf.Atan(StageHeight / 0.7f) * Mathf.Rad2Deg;
        Solid("StageRamp", new Vector3(1f, 0.08f, 0.76f), G(0f, StageHeight * 0.5f - 0.02f, front + 0.35f), Quaternion.Euler(-slope, 0f, 0f), stage);

        Block("Backdrop", new Vector3(11.8f, 2.6f, 0.12f), G(0f, 1.5f, NorthZ + 0.12f), "Petrol", false, stage);
        foreach (float sx in new[] { -1f, 1f })
            Block("Drape", new Vector3(0.9f, 2.55f, 0.35f), G(sx * 6.3f, 1.5f, NorthZ + 0.35f), "Petrol", false, stage);
        Place("flower_arch", G(0f, StageHeight, NorthZ + 2f), Yaw(0f), stage, 0.68f);
        foreach (float sx in new[] { -1f, 1f })
        {
            var speaker = Place("speaker", G(sx * 4.9f, StageHeight, NorthZ + 2.4f), Yaw(0f), stage);
            speaker.AddComponent<BoxCollider>().center = new Vector3(0f, 0.49f, 0f);
        }
        // Fairy lights along the north wall: small emissive bulbs that bloom.
        var fairy = Emissive("Fairy bulb", new Color(1f, 0.82f, 0.5f), 4f);
        for (int i = 0; i < 21; i++)
        {
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "FairyLight";
            Object.DestroyImmediate(bulb.GetComponent<SphereCollider>());
            bulb.transform.SetParent(stage, false);
            bulb.transform.position = G(-10f + i, 2.78f - 0.1f * Mathf.Sin(i * 1.3f), NorthZ + 0.2f);
            bulb.transform.localScale = Vector3.one * 0.09f;
            bulb.GetComponent<MeshRenderer>().sharedMaterial = fairy;
            MakeStatic(bulb);
        }
        Report.AppendLine("stage: platforms, walkable ramp, backdrop, arch, speakers, fairy lights");
    }

    static void SoundArea()
    {
        var area = Group("SoundDesk");
        var desk = Place("sound_desk", G(SoundDesk), Yaw(0f), area);
        desk.AddComponent<BoxCollider>().center = new Vector3(0f, 0.47f, 0f);
        Place("audio_mixer", G(SoundDesk + new Vector3(0f, 0.94f, 0f)), Yaw(0f), area);
        Place("cable_red", G(SoundDesk + new Vector3(-1.1f, 0f, 0.8f)), Yaw(17f), area);
        Place("cable_blue", G(SoundDesk + new Vector3(-0.5f, 0f, 1.1f)), Yaw(69f), area);
        Place("cable_yellow", G(SoundDesk + new Vector3(0.3f, 0f, 1.0f)), Yaw(120f), area);
    }

    static void TablesAndChairs()
    {
        var tables = Group("Tables");
        var all = new List<Vector3>(GuestTables);
        all.AddRange(DecorTables);
        int chairs = 0;
        foreach (var godot in all)
        {
            Vector3 centre = G(godot);
            var table = Place("guest_table", centre, 0f, tables);
            var col = table.AddComponent<BoxCollider>();
            col.size = new Vector3(1.35f, TableTop, 1.35f);
            col.center = new Vector3(0f, TableTop * 0.5f, 0f);
            for (int c = 0; c < 6; c++)
            {
                float angle = c / 6f * Mathf.PI * 2f + 0.3f;
                Vector3 spot = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.12f;
                // The pack's chairs face their +Z, so turn each one to look at the table.
                Vector3 toward = centre - spot;
                float yaw = Mathf.Atan2(toward.x, toward.z) * Mathf.Rad2Deg;
                Navigation.MakeChair($"chair_{all.IndexOf(godot)}_{c}", spot, yaw, tables);
                chairs++;
            }
        }
        foreach (var godot in GuestTables)
            Place("flower_centerpiece", G(godot + new Vector3(0.42f, TableTop, 0.25f)), 0f, tables);
        foreach (var godot in DecorTables)
            Place("flower_centerpiece", G(godot + new Vector3(0f, TableTop, 0f)), 0f, tables);
        Report.AppendLine($"tables: {all.Count}, chairs: {chairs}");
    }

    static void DessertStation()
    {
        var area = Group("DessertStation");
        foreach (float dz in new[] { -1f, 1f })
        {
            var t = Place("dessert_table", G(DessertStand.x, 0f, DessertStand.z + dz), Yaw(90f), area);
            var col = t.AddComponent<BoxCollider>();
            col.size = new Vector3(2f, DessertTableTop, 0.85f);
            col.center = new Vector3(0f, DessertTableTop * 0.5f, 0f);
        }
        Place("cake_stand", G(DessertStand.x, DessertTableTop, DessertStand.z), 0f, area);
    }

    static void PhotoCorner()
    {
        var area = Group("PhotoCorner");
        Place("flower_arch", G(PhotoDisplay + new Vector3(0.5f, 0f, 0f)), Yaw(90f), area);
        var display = Place("photo_display", G(PhotoDisplay + new Vector3(1.6f, 0f, 0f)), Yaw(90f), area);
        display.AddComponent<BoxCollider>().center = new Vector3(0f, 0.88f, 0f);
    }

    static void BackOfHouse()
    {
        var area = Group("BackOfHouse");
        foreach (var godot in new[] { PrepTable, SignTable })
        {
            var t = Place("prep_table", G(godot), Yaw(0f), area);
            var col = t.AddComponent<BoxCollider>();
            col.size = new Vector3(1.8f, 0.9f, 0.75f);
            col.center = new Vector3(0f, 0.45f, 0f);
        }
        Place("clipboard", G(SignTable + new Vector3(0.7f, 0.9f, 0f)), Yaw(23f), area);
        for (int i = 0; i < 6; i++)
        {
            var shelf = Place("storage_shelf", G(-12.6f + i * 1.3f, 0f, SouthZ - 0.4f), Yaw(180f), area);
            var col = shelf.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 1.73f, 0.51f);
            col.center = new Vector3(0f, 0.86f, 0f);
        }
        for (int i = 0; i < 3; i++)
            Place("storage_crate", G(-11.4f + i * 0.7f, 0f, 5.2f), Yaw(i * 11f), area).AddComponent<BoxCollider>().center = new Vector3(0f, 0.23f, 0f);
        Place("storage_crate", G(-11f, 0.46f, 5.2f), Yaw(6f), area);
        Place("mop_bucket", G(9.5f, 0f, SouthZ - 0.6f), 0f, area);
        Place("mop", G(10.2f, 0f, SouthZ - 0.6f), Yaw(11f), area);
        Place("cleaning_cloth", G(8.6f, 0f, SouthZ - 0.6f), Yaw(29f), area);
        for (int i = 0; i < 2; i++)
            Place("storage_crate", G(12.2f, i * 0.46f, 5f), Yaw(i * 9f), area);
    }

    // Light -------------------------------------------------------------------------

    static void Lighting()
    {
        var lights = Group("Lights");
        // Flush ceiling discs in a grid over the ballroom (as in Astra's reference):
        // a glowing disc each, with a baked warm point light just below it. Everything
        // is baked, so the count costs nothing at runtime. Moving things are lit by the
        // probe grid.
        var disc = Emissive("Ceiling disc", new Color(1f, 0.93f, 0.82f), 3.5f);
        foreach (float x in new[] { -10.5f, -3.5f, 3.5f, 10.5f })
        {
            foreach (float z in new[] { -12.5f, -8f, -3.5f })
            {
                var c = new GameObject("CeilingLight").transform;
                c.SetParent(lights, false);
                c.position = G(x, WallHeight - 0.02f, z);
                var face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                face.name = "Disc";
                Object.DestroyImmediate(face.GetComponent<CapsuleCollider>());
                face.transform.SetParent(c, false);
                face.transform.localScale = new Vector3(0.9f, 0.02f, 0.9f);
                var renderer = face.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = disc;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                MakeStatic(face);
                var lightGo = new GameObject("Light");
                lightGo.transform.SetParent(c, false);
                lightGo.transform.localPosition = new Vector3(0f, -0.3f, 0f);
                var point = lightGo.AddComponent<Light>();
                point.type = LightType.Point;
                point.color = new Color(1f, 0.86f, 0.7f);
                point.range = 12f;
                point.intensity = 9f;
                point.shadows = LightShadows.Soft;
                point.shadowRadius = 0.25f;
                point.lightmapBakeType = LightmapBakeType.Baked;
            }
        }

        // Back of house: cool practical strip lights.
        var strip = Emissive("Strip light", new Color(0.92f, 0.96f, 1f), 3f);
        foreach (float x in new[] { -9f, -3f, 3f, 9f })
        {
            var s = Block("StripLight", new Vector3(2.2f, 0.06f, 0.25f), G(x, 2.9f, 5f), "Strip light", false, lights);
            s.GetComponent<MeshRenderer>().sharedMaterial = strip;
            var lightGo = new GameObject("StripLightSource");
            lightGo.transform.SetParent(lights, false);
            lightGo.transform.position = G(x, 2.45f, 5f);
            var point = lightGo.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = new Color(0.9f, 0.95f, 1f);
            point.range = 8f;
            point.intensity = 5f;
            point.shadows = LightShadows.Soft;
            point.lightmapBakeType = LightmapBakeType.Baked;
        }

        // Stage spotlight.
        var spotGo = new GameObject("StageSpot");
        spotGo.transform.SetParent(lights, false);
        spotGo.transform.position = G(0f, 2.9f, NorthZ + 6f);
        spotGo.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
        var spot = spotGo.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(1f, 0.92f, 0.8f);
        spot.range = 9f;
        spot.spotAngle = 55f;
        spot.intensity = 14f;
        spot.shadows = LightShadows.Soft;
        spot.lightmapBakeType = LightmapBakeType.Baked;

        // Ambient from a warm-dark gradient; the bounce light comes from the bake.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.36f, 0.36f, 0.38f);
        RenderSettings.ambientEquatorColor = new Color(0.3f, 0.27f, 0.24f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.14f, 0.12f);
        RenderSettings.skybox = null;

        var settings = new LightingSettings
        {
            name = "WeddingLighting",
            lightmapper = LightingSettings.Lightmapper.ProgressiveGPU,
            bakedGI = true,
            realtimeGI = false,
            mixedBakeMode = MixedLightingMode.Shadowmask,
            lightmapResolution = 16f,
            lightmapPadding = 4,
            lightmapMaxSize = 2048,
            directionalityMode = LightmapsMode.CombinedDirectional,
            ao = true,
            aoMaxDistance = 0.8f,
            maxBounces = 3,
            indirectScale = 1.3f,
        };
        const string settingsPath = "Assets/Scenes/WeddingLighting.lighting";
        AssetDatabase.DeleteAsset(settingsPath);
        AssetDatabase.CreateAsset(settings, settingsPath);
        Lightmapping.lightingSettings = settings;
        Report.AppendLine("lights: 12 ceiling discs, 4 strip lights, stage spot (all baked); GPU lightmapper, 3 bounces, AO");
    }

    static void Probes()
    {
        // A light probe grid so moving characters and props pick up the baked light.
        var probesGo = new GameObject("LightProbes");
        probesGo.transform.SetParent(_root, false);
        var group = probesGo.AddComponent<LightProbeGroup>();
        var positions = new List<Vector3>();
        for (float x = -13f; x <= 13f; x += 2f)
            for (float z = -15f; z <= 7f; z += 2f)
                foreach (float y in new[] { 0.4f, 1.4f, 2.5f })
                    positions.Add(G(x, y, z));
        group.probePositions = positions.ToArray();

        // Box-projected reflection probes: one for the ballroom, one behind the scenes.
        AddReflectionProbe("BallroomReflections", G(0f, 1.5f, (NorthZ + DividerZ) * 0.5f), new Vector3(28f, 3f, DividerZ - NorthZ));
        AddReflectionProbe("ServiceReflections", G(0f, 1.5f, (DividerZ + SouthZ) * 0.5f), new Vector3(28f, 3f, SouthZ - DividerZ));
        Report.AppendLine($"light probes: {positions.Count}, reflection probes: 2");
    }

    static void AddReflectionProbe(string name, Vector3 centre, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_root, false);
        go.transform.position = centre;
        var probe = go.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Baked;
        probe.size = size;
        probe.boxProjection = true;
        probe.resolution = 256;
    }

    static void PostProcessing()
    {
        const string profilePath = "Assets/Settings/WeddingVolume.asset";
        AssetDatabase.DeleteAsset(profilePath);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, profilePath);

        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.0f);
        bloom.intensity.Override(0.45f);
        bloom.scatter.Override(0.6f);
        var colour = profile.Add<ColorAdjustments>(true);
        colour.postExposure.Override(0.35f);
        colour.contrast.Override(12f);
        colour.saturation.Override(-4f);
        var white = profile.Add<WhiteBalance>(true);
        white.temperature.Override(8f);
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.22f);
        vignette.smoothness.Override(0.5f);
        foreach (var component in profile.components)
            AssetDatabase.AddObjectToAsset(component, profile);
        AssetDatabase.SaveAssets();

        var volumeGo = new GameObject("PostProcessing");
        volumeGo.transform.SetParent(_root, false);
        var volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
        Report.AppendLine("post-processing: ACES tonemapping, bloom, colour grading, warm white balance, vignette");
    }

    static UnityEngine.Camera Camera()
    {
        // A preview camera at the hero view (the west staff doorway), at eye height.
        var camGo = new GameObject("PreviewCamera");
        camGo.tag = "MainCamera";
        camGo.transform.position = G(-7f, 1.6f, 1.3f);
        // Godot cameras look down -Z (models face +Z), so a camera yaw only flips sign.
        camGo.transform.rotation = Quaternion.Euler(2f, 0.62f * Mathf.Rad2Deg, 0f);
        var cam = camGo.AddComponent<UnityEngine.Camera>();
        cam.fieldOfView = 62f;
        cam.nearClipPlane = 0.05f;
        var data = camGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.High;
        return cam;
    }
}
