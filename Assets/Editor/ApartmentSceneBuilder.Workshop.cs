using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The workshop alcove off the flat's east wall (3 x 3.6 m, through an opening
/// beside the computer desk): a reused dining table as the workbench with the
/// assembly mat, its sockets, two screws, the lid and the test jig; the parts
/// bins on the north wall; the charging rack and the kit bag on the south wall;
/// and a doormat by the front door where parcels land. The parts' sockets sit at
/// the offsets the Blender models were made with (make_workshop_props.py), at
/// AssemblyBench.PartScale.
/// </summary>
public static partial class ApartmentSceneBuilder
{
    const string Work = "Assets/Art/Workshop/{0}.fbx";
    /// The opening in the flat's east wall, between these z values.
    public const float OpeningSouth = -2.85f, OpeningNorth = -1.45f;
    const float AlcoveEast = 7f, AlcoveNorth = 0.6f;

    static readonly Vector3 MatCentre = new(6.45f, 0.785f, -1.15f);
    static readonly Quaternion Frame = Quaternion.Euler(0f, -90f, 0f);

    /// A point in the casing's frame (Blender metres, see the props script) on the mat.
    static Vector3 Casing(float x, float y, float z) => MatCentre + Frame * (new Vector3(-x, z, -y) * AssemblyBench.PartScale);

    static void Workshop()
    {
        var root = Group("Workshop");
        Alcove(root);
        var pad = GameplaySceneSetup.TransparentUnlit("Socket pad", new Color(0.25f, 0.85f, 0.72f, 0.5f));
        var ghost = GameplaySceneSetup.TransparentUnlit("Socket ghost", new Color(0.4f, 1f, 0.7f, 0.38f));
        var art = BuildArt();
        string W(string n) => string.Format(Work, n);
        var steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Steel.mat");

        // The workbench, its mat, the tray, the jig, the lamp, the pegboard and the blueprints.
        var benchGo = new GameObject("Workbench");
        benchGo.transform.SetParent(root, false);
        Place(W("workbench"), new Vector3(6.525f, 0f, -1.25f), -90f, benchGo.transform);
        Place(W("assembly_mat"), new Vector3(MatCentre.x, 0.78f, MatCentre.z), -90f, benchGo.transform, false);
        var tray = Place(W("parts_tray"), new Vector3(6.5f, 0.78f, -1.74f), -90f, benchGo.transform, false);
        tray.transform.localScale = Vector3.one * 1.4f;
        var jigGo = Place(W("test_jig"), new Vector3(6.55f, 0.78f, -0.62f), -90f, benchGo.transform, false, false);
        jigGo.name = "TestJig";
        var jigBox = jigGo.AddComponent<BoxCollider>();
        jigBox.center = new Vector3(0f, 0.06f, 0f);
        jigBox.size = new Vector3(0.22f, 0.14f, 0.16f);
        Place(W("task_lamp"), new Vector3(6.82f, 0.78f, -0.42f), -153f, benchGo.transform, false);
        Place(W("power_strip"), new Vector3(6.85f, 0.78f, -1.5f), -90f, benchGo.transform, false);
        Place(W("pegboard"), new Vector3(6.9f, 1.0f, -1.25f), -90f, benchGo.transform, false);
        // Pinned between the hanging tools (left) and the spools (right).
        var blueprint = Sign("Blueprints", "blueprint_sheet", new Vector3(6.872f, 1.32f, -1.23f), 90f, 0.5f, benchGo.transform);
        var blueprintBox = blueprint.AddComponent<BoxCollider>();
        blueprintBox.size = new Vector3(1f, 1f, 0.05f);
        blueprint.AddComponent<BlueprintBoard>().SetEntityId("blueprint_board");

        var bench = benchGo.AddComponent<AssemblyBench>();
        var audio = benchGo.AddComponent<AudioSource>();
        audio.spatialBlend = 1f;
        audio.minDistance = 1f;
        audio.maxDistance = 12f;
        audio.playOnAwake = false;

        // Sockets, in the casing's frame on the mat.
        var sockets = Group("Sockets", benchGo.transform);
        float s = AssemblyBench.PartScale;
        PlacementSocket Socket(string id, string display, string kind, string requires, Vector3 at, Vector3 trigger, float radius = 0.1f)
        {
            var socket = GameplaySceneSetup.MakeSocket(id, display, kind, requires, radius, at, pad, ghost, sockets);
            socket.transform.rotation = Frame;
            socket.ConfigureTrigger(trigger, new Vector3(0f, trigger.y * 0.5f, 0f));
            return socket;
        }
        var mat = Socket(AssemblyBench.MatSocket, "the mat", "casing", "", MatCentre, new Vector3(0.3f * s, 0.08f, 0.2f * s), 0.16f);
        var board = Socket(AssemblyBench.BoardSocket, "the casing", "board", AssemblyBench.MatSocket, Casing(-0.025f, 0f, 0.018f), new Vector3(0.18f * s, 0.05f, 0.11f * s));
        var battery = Socket(AssemblyBench.BatterySocket, "the battery bay", "battery", AssemblyBench.BoardSocket, Casing(0.088f, 0f, 0.006f), new Vector3(0.05f * s, 0.05f, 0.08f * s));
        var ports = new List<PlacementSocket>();
        float[] portX = { -0.075f, -0.025f, 0.025f };
        for (int i = 0; i < PartCatalog.Ports.Length; i++)
        {
            string port = PartCatalog.Ports[i];
            ports.Add(Socket(AssemblyBench.PortSocket(port), $"the {PartCatalog.PortLabel(port)} port", "module", AssemblyBench.BoardSocket,
                Casing(portX[i], 0.02f, 0.032f), new Vector3(0.045f * s, 0.035f, 0.045f * s)));
        }

        // Two screws on the board's front corners, where nothing hides them.
        var screws = new List<ScrewPoint>();
        foreach (var (x, y) in new[] { (-0.1f, -0.04f), (0.05f, -0.04f) })
        {
            var at = Casing(x, y, 0.024f);
            var screwGo = new GameObject($"Screw_{screws.Count}");
            screwGo.transform.SetParent(benchGo.transform, false);
            screwGo.transform.position = at;
            var trigger = screwGo.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(0.035f, 0.03f, 0.035f);
            trigger.center = new Vector3(0f, 0.012f, 0f);
            var head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.name = "Head";
            head.transform.SetParent(screwGo.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.006f, 0f);
            head.transform.localScale = new Vector3(0.018f, 0.002f, 0.018f);
            head.GetComponent<MeshRenderer>().sharedMaterial = steel;
            var slot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(slot.GetComponent<Collider>());
            slot.transform.SetParent(head.transform, false);
            slot.transform.localPosition = new Vector3(0f, 1f, 0f);
            slot.transform.localScale = new Vector3(0.8f, 0.6f, 0.12f);
            slot.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Dark plastic.mat");
            var screw = screwGo.AddComponent<ScrewPoint>();
            screw.SetEntityId($"bench_screw_{screws.Count}");
            screw.Configure(bench, head.transform);
            screws.Add(screw);
        }

        // The lid, hinged along the casing's back edge, standing open.
        var lidGo = new GameObject("Lid");
        lidGo.transform.SetParent(benchGo.transform, false);
        lidGo.transform.SetPositionAndRotation(Casing(0f, 0.08f, 0.07f), Frame);
        var hinge = new GameObject("Hinge").transform;
        hinge.SetParent(lidGo.transform, false);
        var lidModel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(W("part_lid")), hinge);
        lidModel.transform.localPosition = Vector3.zero;
        lidModel.transform.localRotation = Quaternion.identity;
        lidModel.transform.localScale = Vector3.one * s;
        var lidBox = lidModel.AddComponent<BoxCollider>();
        lidBox.center = new Vector3(0f, 0.004f, 0.08f);
        lidBox.size = new Vector3(0.24f, 0.016f, 0.16f);
        var edge = new GameObject("Edge").transform;
        edge.SetParent(hinge, false);
        edge.localPosition = new Vector3(0f, 0.008f, 0.16f) * s;
        var lid = lidGo.AddComponent<CaseLid>();
        lid.SetEntityId("bench_lid");
        lid.Configure(bench, hinge, edge);

        // The jig's moving button, its screen, and one light per port.
        var button = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(button.GetComponent<Collider>());
        button.name = "Button";
        button.transform.SetParent(jigGo.transform, false);
        button.transform.localPosition = new Vector3(-0.06f, 0.06f, 0.03f);
        button.transform.localScale = new Vector3(0.04f, 0.006f, 0.04f);
        button.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Button red.mat");
        var screen = new GameObject("Screen").transform;
        screen.SetParent(jigGo.transform, false);
        screen.localPosition = new Vector3(0f, 0.093f, -0.04f);
        screen.localRotation = Quaternion.Euler(18f, 180f, 0f);
        var lights = new List<Renderer>();
        for (int i = 0; i < PartCatalog.Ports.Length; i++)
        {
            var led = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(led.GetComponent<Collider>());
            led.name = "PortLight_" + PartCatalog.Ports[i];
            led.transform.SetParent(jigGo.transform, false);
            led.transform.localPosition = new Vector3(0.075f - i * 0.025f, 0.052f, -0.005f);
            led.transform.localScale = new Vector3(0.014f, 0.006f, 0.01f);
            var r = led.GetComponent<MeshRenderer>();
            r.sharedMaterial = art.led;
            r.shadowCastingMode = ShadowCastingMode.Off;
            lights.Add(r);
        }
        var jig = jigGo.AddComponent<TestJig>();
        jig.SetEntityId("bench_jig");
        jig.Configure(bench, button.transform, screen);

        // Where reserved parts wait: the board, the battery, then up to three modules.
        var trayPoints = new List<Transform>();
        foreach (var at in new[] { new Vector3(6.44f, 0.79f, -1.86f), new Vector3(6.42f, 0.79f, -1.58f), new Vector3(6.6f, 0.79f, -1.95f), new Vector3(6.6f, 0.79f, -1.8f), new Vector3(6.6f, 0.79f, -1.63f) })
        {
            var t = new GameObject($"TrayPoint_{trayPoints.Count}").transform;
            t.SetParent(benchGo.transform, false);
            t.SetPositionAndRotation(at, Frame);
            trayPoints.Add(t);
        }

        // The screwdriver stands in a cup at the jig end.
        var cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(cup.GetComponent<Collider>());
        cup.name = "ToolCup";
        cup.transform.SetParent(benchGo.transform, false);
        cup.transform.position = new Vector3(6.3f, 0.83f, -0.5f);
        cup.transform.localScale = new Vector3(0.07f, 0.05f, 0.07f);
        cup.GetComponent<MeshRenderer>().sharedMaterial = Paint("Mug blue", new Color(0.25f, 0.4f, 0.65f), 0.4f);
        MakeStatic(cup);
        var cupSocket = GameplaySceneSetup.MakeSocket("tool_cup", "the tool cup", "screwdriver", "", 0.05f, new Vector3(6.3f, 0.79f, -0.5f), null, ghost, sockets, 0f);
        cupSocket.ConfigureTrigger(new Vector3(0.12f, 0.26f, 0.12f), new Vector3(0f, 0.13f, 0f));
        var driver = GameplaySceneSetup.MakeProp("screwdriver", "screwdriver", "the screwdriver", W("screwdriver"), new Vector3(6.3f, 0.79f, -0.5f), 0f, 0.15f, benchGo.transform);

        bench.Configure(mat, board, battery, ports.ToArray(), screws.ToArray(), lid, jig, trayPoints.ToArray(), lights.ToArray(), driver, audio);
        bench.ConfigureToolCup(cupSocket);

        // The parts bins.
        Shelf(root);

        // The charging rack, its cradles, the kit bag.
        var rackGo = Place(W("loadout_rack"), new Vector3(5.45f, 0f, -2.7f), 0f, root);
        rackGo.name = "ChargingRack";
        var cradles = new List<Transform>();
        foreach (float x in new[] { -0.3f, 0f, 0.3f })
        {
            var c = new GameObject($"Cradle_{cradles.Count}").transform;
            c.SetParent(rackGo.transform, false);
            c.position = new Vector3(5.45f - x, 0.84f, -2.7f);
            cradles.Add(c);
        }
        Place(W("duffel_bag"), new Vector3(6.38f, 0f, -2.66f), 0f, root);
        // A luggage tag on the bag's handle.
        var tag = new GameObject("BagTag").transform;
        tag.SetParent(root, false);
        tag.SetPositionAndRotation(new Vector3(6.38f, 0.3f, -2.52f), Quaternion.Euler(0f, 180f, 0f));
        var rack = rackGo.AddComponent<GadgetRack>();
        rack.SetEntityId("charging_rack");
        rack.Configure(cradles.ToArray(), tag, tag);
        Sign("RackSign", "sign_rack", new Vector3(5.45f, 1.3f, -2.91f), 180f, 0.62f, root);

        // Signs, and where parcels land.
        Sign("WorkshopSign", "sign_workshop", new Vector3(3.915f, 2.36f, -2.15f), 90f, 0.62f, root);
        Place(W("doormat"), new Vector3(1.0f, 0f, -2.6f), 0f, root, false);
        Sign("ParcelSign", "sign_parcels", new Vector3(1.0f, 1.15f, -2.915f), 180f, 0.34f, root);
        var drop = new GameObject("ParcelDrop").transform;
        drop.SetParent(root, false);
        drop.SetPositionAndRotation(new Vector3(0.85f, 0.03f, -2.6f), Quaternion.Euler(0f, 8f, 0f));

        // The room's director, and the panel the board and the rack open.
        var area = new GameObject("WorkshopArea").transform;
        area.SetParent(root, false);
        area.position = new Vector3((4f + AlcoveEast) * 0.5f, 0f, (-3f + AlcoveNorth) * 0.5f);
        var roomGo = new GameObject("WorkshopRoom");
        roomGo.transform.SetParent(root, false);
        roomGo.AddComponent<WorkshopRoom>().Configure(_door, drop, area, new Vector3(AlcoveEast - 4f, Height, AlcoveNorth + 3f), _doorAudio);
        _hudGo.AddComponent<WorkshopPanel>().Configure(_hud);
        Report.AppendLine("workshop: alcove 3 x 3.6 m, workbench (mat, 6 sockets, 2 screws, lid, jig, tray, screwdriver cup), parts bins, charging rack + bag, doormat");
    }

    static void Alcove(Transform root)
    {
        var shell = Group("Alcove", root);
        var lino = Paint("Workshop lino", new Color(0.36f, 0.33f, 0.28f), 0.25f);
        var wall = Paint("Apartment wall", new Color(0.68f, 0.67f, 0.56f));
        var ceiling = Paint("Apartment ceiling", new Color(0.7f, 0.68f, 0.6f));
        float h = Wall * 0.5f;
        Block("AlcoveFloor", new Vector3(4f, -0.3f, -3f), new Vector3(AlcoveEast, 0f, AlcoveNorth), lino, shell);
        Block("AlcoveWallEast", new Vector3(AlcoveEast - h, 0f, -3f - h), new Vector3(AlcoveEast + h, Height, AlcoveNorth + h), wall, shell);
        Block("AlcoveWallNorth", new Vector3(4f, 0f, AlcoveNorth - h), new Vector3(AlcoveEast, Height, AlcoveNorth + h), wall, shell);
        // The corridor's wall is the alcove's south wall: paint this side like the flat.
        Block("AlcoveWallSouthSkin", new Vector3(4f + h, 0f, -3f + h), new Vector3(AlcoveEast - h, Height, -3f + h + 0.01f), wall, shell, false);
        Block("AlcoveCeiling", new Vector3(4f - h, Height, -3.1f), new Vector3(AlcoveEast + 0.1f, Height + 0.15f, AlcoveNorth + 0.1f), ceiling, shell, false);
        var trim = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Trim.mat");
        Block("AlcoveSkirtingN", new Vector3(4f + h, 0f, AlcoveNorth - h - 0.03f), new Vector3(AlcoveEast - h, 0.1f, AlcoveNorth - h), trim, shell, false);
        var bulb = Place(string.Format(Props, "bare_bulb"), new Vector3(5.5f, Height, -1.2f), 0f, shell, false);
        foreach (var r in bulb.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = ShadowCastingMode.Off;
    }

    /// Six bins on a homemade shelf against the north wall, one per part family, labelled.
    static void Shelf(Transform root)
    {
        var shelfGo = new GameObject("PartsShelf");
        shelfGo.transform.SetParent(root, false);
        var wood = Paint("Shelf pine", new Color(0.6f, 0.47f, 0.32f), 0.2f);
        const float x0 = 4.9f, x1 = 6.1f, z0 = 0.17f, z1 = 0.5f;
        foreach (float x in new[] { x0, x1 - 0.03f })
            Block("Upright", new Vector3(x, 0f, z0), new Vector3(x + 0.03f, 1.35f, z1), wood, shelfGo.transform);
        foreach (float y in new[] { 0.05f, 0.45f, 0.9f, 1.32f })
            Block("Board", new Vector3(x0, y - 0.025f, z0), new Vector3(x1, y, z1), wood, shelfGo.transform);
        string[] bins = { "part_bin_red", "part_bin_blue", "part_bin_yellow" };
        var labels = new List<Transform>();
        var ids = new List<string>();
        for (int i = 0; i < PartCatalog.Parts.Length; i++)
        {
            float x = 5.12f + (i % 3) * 0.38f;
            float y = i < 3 ? 0.45f : 0.9f;
            Place(string.Format(Work, bins[i % 3]), new Vector3(x, y, 0.33f), 180f, shelfGo.transform, false);
            var label = new GameObject($"BinLabel_{PartCatalog.Parts[i].Id}").transform;
            label.SetParent(shelfGo.transform, false);
            label.SetPositionAndRotation(new Vector3(x, y + 0.035f, 0.183f), Quaternion.identity);
            labels.Add(label);
            ids.Add(PartCatalog.Parts[i].Id);
        }
        var shelf = shelfGo.AddComponent<PartShelf>();
        shelf.SetEntityId("parts_shelf");
        shelf.Configure(labels.ToArray(), ids.ToArray());
        Sign("PartsSign", "sign_parts", new Vector3(5.5f, 1.55f, AlcoveNorth - 0.085f), 0f, 0.6f, root);
    }

    /// A quad with one of the workshop's sign textures, facing along yaw.
    static GameObject Sign(string name, string texture, Vector3 pos, float yaw, float width, Transform parent)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Workshop/{texture}.png");
        string path = $"Assets/Art/Materials/Sign {texture}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.82f));
        mat.SetFloat("_AlphaClip", 1f);
        mat.SetFloat("_Cutoff", 0.4f);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.renderQueue = (int)RenderQueue.AlphaTest;
        EditorUtility.SetDirty(mat);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(parent, false);
        quad.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        float aspect = tex != null ? (float)tex.height / tex.width : 0.25f;
        quad.transform.localScale = new Vector3(width, width * aspect, 1f);
        var r = quad.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        if (tex == null)
            Report.AppendLine($"MISSING sign texture {texture}");
        return quad;
    }

    /// The models and materials runtime code needs in any scene (Resources/WorkshopArt).
    static WorkshopArt BuildArt()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        const string path = "Assets/Resources/WorkshopArt.asset";
        var art = AssetDatabase.LoadAssetAtPath<WorkshopArt>(path);
        if (art == null)
        {
            art = ScriptableObject.CreateInstance<WorkshopArt>();
            AssetDatabase.CreateAsset(art, path);
        }
        GameObject M(string n) => AssetDatabase.LoadAssetAtPath<GameObject>(string.Format(Work, n));
        art.parts = new[]
        {
            new WorkshopArt.Model { id = PartCatalog.Casing, model = M("part_casing") },
            new WorkshopArt.Model { id = PartCatalog.Board, model = M("part_board") },
            new WorkshopArt.Model { id = PartCatalog.Battery, model = M("part_battery") },
            new WorkshopArt.Model { id = PartCatalog.Sensor, model = M("part_sensor") },
            new WorkshopArt.Model { id = PartCatalog.Motor, model = M("part_motor") },
            new WorkshopArt.Model { id = PartCatalog.Speaker, model = M("part_speaker") },
        };
        art.gadgets = new[]
        {
            new WorkshopArt.Model { id = PartCatalog.WhisperBox, model = M("gadget_whisperbox") },
            new WorkshopArt.Model { id = PartCatalog.SteadyHand, model = M("gadget_steadyhand") },
        };
        art.parcel = M("parcel");
        art.parcelOpen = M("parcel_open");
        art.lid = M("part_lid");

        const string ledPath = "Assets/Art/Materials/Workshop LED.mat";
        art.led = AssetDatabase.LoadAssetAtPath<Material>(ledPath);
        if (art.led == null)
        {
            art.led = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(art.led, ledPath);
        }
        art.led.SetColor("_BaseColor", Color.white);
        const string textPath = "Assets/Art/Materials/Workshop text.mat";
        art.text = AssetDatabase.LoadAssetAtPath<Material>(textPath);
        if (art.text == null)
        {
            art.text = new Material(Shader.Find("Unqualified/World Text"));
            AssetDatabase.CreateAsset(art.text, textPath);
        }
        EditorUtility.SetDirty(art.led);
        EditorUtility.SetDirty(art.text);
        EditorUtility.SetDirty(art);
        AssetDatabase.SaveAssets();
        foreach (var model in art.parts)
            if (model.model == null)
                Report.AppendLine($"MISSING workshop model {model.id}");
        return art;
    }
}
