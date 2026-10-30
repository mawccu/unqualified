using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

/// <summary>
/// Builds Assets/Scenes/CallCenter.unity: Brightline Home Support's office floor
/// (14 x 10 m). A pod of four desks in the middle (yours, Dana's, Omar's and
/// Kim's), the supervisor's desk and whiteboard in the north-east, the reference
/// shelf and printer on the west wall, and a break corner in the south-east.
/// Unity coordinates (north +Z); the way in is the south-west door.
/// Run: return CallCenterSceneBuilder.Build();
/// </summary>
public static class CallCenterSceneBuilder
{
    const string ScenePath = "Assets/Scenes/CallCenter.unity";
    const string Office = "Assets/Art/Props/{0}.fbx";
    const string Flat = "Assets/Art/Apartment/{0}.fbx";
    const string Pack = "Assets/Art/Models/{0}.fbx";
    const float Height = 2.8f;
    const float Wall = 0.15f;
    const float HalfX = 7f, HalfZ = 5f;
    const float DoorX = -5f;

    static Transform _root;
    static readonly StringBuilder Report = new();
    static Material _pad, _ghost;

    public static string Build()
    {
        Report.Clear();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        _root = new GameObject("CallCenter").transform;
        GameplaySceneSetup.EnsureLayers();
        _pad = GameplaySceneSetup.TransparentUnlit("Socket pad", new Color(0.25f, 0.85f, 0.72f, 0.5f));
        _ghost = GameplaySceneSetup.TransparentUnlit("Socket ghost", new Color(0.4f, 1f, 0.7f, 0.38f));

        Shell();
        var library = GameplaySceneSetup.BuildWorkerLibrary(Report);
        var hud = MakeHud(out var console);
        var desks = Pod(library, console);
        BackOffice();
        var breakSpot = BreakCorner();
        var supervisor = MakeSupervisor(library);
        var camera = PreviewCamera();
        Session(library, hud, desks, supervisor, breakSpot, camera);
        Lighting();
        PostProcessing();
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/CallCenter"))
            AssetDatabase.CreateFolder("Assets/Scenes", "CallCenter");
        Navigation.Bake(_root.gameObject, "Assets/Scenes/CallCenter/NavMesh.asset", Report);

        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == ScenePath))
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Report.AppendLine($"saved {ScenePath}");
        return Report.ToString();
    }

    // Helpers -----------------------------------------------------------------------------

    static Transform Group(string name, Transform parent = null)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent != null ? parent : _root, false);
        return t;
    }

    static Transform Spot(string name, Vector3 at, float yaw, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
        return t;
    }

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

    static Material Glow(string name, Color colour, float intensity, bool create = false)
    {
        string path = $"Assets/Art/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            if (!create)
                return null;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", colour);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", colour * intensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// A printed sign or nameplate: a quad with a texture from Tools/make_office_signs.py.
    static GameObject Sign(string name, string texture, Vector3 pos, float yaw, float width, Transform parent)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Office/{texture}.png");
        string path = $"Assets/Art/Materials/Sign {texture}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
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

    // The room ---------------------------------------------------------------------------

    static void Shell()
    {
        var shell = Group("Shell");
        var carpet = Paint("Office carpet", new Color(0.27f, 0.31f, 0.36f), 0.05f);
        var wall = Paint("Office wall", new Color(0.82f, 0.81f, 0.76f));
        var accent = Paint("Office accent wall", new Color(0.2f, 0.42f, 0.55f));
        var ceiling = Paint("Office ceiling", new Color(0.86f, 0.86f, 0.84f));
        var trim = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Trim.mat") ?? wall;
        float h = Wall * 0.5f;

        Block("Floor", new Vector3(-HalfX - 0.2f, -0.3f, -HalfZ - 0.2f), new Vector3(HalfX + 0.2f, 0f, HalfZ + 0.2f), carpet, shell);
        Block("WallWest", new Vector3(-HalfX - h, 0f, -HalfZ - h), new Vector3(-HalfX + h, Height, HalfZ + h), wall, shell);
        Block("WallEast", new Vector3(HalfX - h, 0f, -HalfZ - h), new Vector3(HalfX + h, Height, HalfZ + h), wall, shell);
        Block("WallNorth", new Vector3(-HalfX, 0f, HalfZ - h), new Vector3(HalfX, Height, HalfZ + h), accent, shell);
        Block("WallSouthA", new Vector3(-HalfX, 0f, -HalfZ - h), new Vector3(DoorX - 0.54f, Height, -HalfZ + h), wall, shell);
        Block("WallSouthB", new Vector3(DoorX + 0.54f, 0f, -HalfZ - h), new Vector3(HalfX, Height, -HalfZ + h), wall, shell);
        Block("WallSouthHeader", new Vector3(DoorX - 0.54f, 2.16f, -HalfZ - h), new Vector3(DoorX + 0.54f, Height, -HalfZ + h), wall, shell);
        Block("Ceiling", new Vector3(-HalfX - 0.1f, Height, -HalfZ - 0.1f), new Vector3(HalfX + 0.1f, Height + 0.15f, HalfZ + 0.1f), ceiling, shell, false);
        Block("SkirtingN", new Vector3(-HalfX + 0.08f, 0f, HalfZ - 0.1f), new Vector3(HalfX - 0.08f, 0.1f, HalfZ - 0.07f), trim, shell, false);
        Block("SkirtingW", new Vector3(-HalfX + 0.07f, 0f, -HalfZ + 0.08f), new Vector3(-HalfX + 0.1f, 0.1f, HalfZ - 0.08f), trim, shell, false);
        Block("SkirtingE", new Vector3(HalfX - 0.1f, 0f, -HalfZ + 0.08f), new Vector3(HalfX - 0.07f, 0.1f, HalfZ - 0.08f), trim, shell, false);

        // The door you came in by (closed behind you), and night windows to the north.
        Place(string.Format(Flat, "door_frame"), new Vector3(DoorX, 0f, -HalfZ), 0f, shell, false);
        Place(string.Format(Flat, "door_leaf"), new Vector3(DoorX + 0.45f, 0f, -HalfZ), 0f, shell, true);
        Glow("Window glass night", new Color(0.12f, 0.18f, 0.32f), 1.2f, true);
        var night = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Window glass night.mat");
        foreach (float x in new[] { -4.5f, -1.5f, 1.5f })
        {
            var w = Place(string.Format(Flat, "window"), new Vector3(x, 1.0f, HalfZ - 0.12f), 180f, shell, false);
            foreach (var r in w.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.StartsWith("Window glass"))
                        mats[i] = night;
                r.sharedMaterials = mats;
            }
        }
        Sign("CompanySign", "sign_company", new Vector3(-6.9f, 1.95f, 0.5f), -90f, 2.4f, shell);
        Report.AppendLine("shell: 14 x 10 m office, door south-west, three night windows north");
    }

    // The four desks ----------------------------------------------------------------------

    static SupportDesk[] Pod(WorkerLibrary library, SupportConsole console)
    {
        var pod = Group("Pod");
        var lampMat = Glow("Phone lamp", new Color(0.2f, 0.2f, 0.2f), 0f, true);
        var holdMat = Paint("Phone hold button", new Color(0.95f, 0.62f, 0.12f), 0.4f);
        var muteMat = Paint("Phone mute button", new Color(0.62f, 0.62f, 0.64f), 0.4f);
        var transferMat = Paint("Phone transfer button", new Color(0.2f, 0.45f, 0.85f), 0.4f);
        var desks = new SupportDesk[4];
        // (centre, desk yaw): the south pair faces north, the north pair faces south.
        var layout = new (Vector3 at, float yaw, string id, string name, bool tech, int slot, bool onBreak)[]
        {
            (new Vector3(-1.45f, 0f, -0.42f), 180f, "you", "You", false, 0, false),
            (new Vector3(1.45f, 0f, -0.42f), 180f, "npc_dana", "Dana", true, 1, false),
            (new Vector3(-1.45f, 0f, 0.42f), 0f, "npc_omar", "Omar", false, 2, false),
            (new Vector3(1.45f, 0f, 0.42f), 0f, "npc_kim", "Kim", false, 3, true),
        };
        for (int i = 0; i < layout.Length; i++)
        {
            var (at, yaw, id, name, tech, slot, onBreak) = layout[i];
            var deskGo = new GameObject(i == 0 ? "YourDesk" : $"Desk_{name}");
            deskGo.transform.SetParent(pod, false);
            deskGo.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            var t = deskGo.transform;
            Vector3 L(float x, float y, float z) => t.TransformPoint(new Vector3(x, y, z));

            Place(string.Format(Office, "office_desk"), at, yaw, t, false);
            var box = deskGo.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.38f, 0f);
            box.size = new Vector3(1.4f, 0.76f, 0.7f);
            var monitor = Place(string.Format(Office, "flat_monitor"), L(0f, 0.75f, -0.18f), yaw, t, false);
            var monitorBox = deskGo.AddComponent<BoxCollider>();
            monitorBox.center = new Vector3(0f, 1.08f, -0.18f);
            monitorBox.size = new Vector3(0.58f, 0.4f, 0.08f);
            var keyboard = Place(string.Format(Office, "slim_keyboard"), L(0f, 0.75f, 0.14f), yaw, t, false);
            Place(string.Format(Office, "binder_" + (i % 2 == 0 ? "blue" : "red")), L(-0.58f, 0.75f, -0.2f), yaw + 90f, t, false);
            var mugModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Props/mug.fbx");
            if (i > 0 && mugModel != null)
                Place("Assets/Art/Props/mug.fbx", L(-0.42f, 0.75f, 0.05f), yaw + 40f, t, false);

            // The phone, its handset, three buttons you can press and a status lamp
            // on top of the monitor that can be read from anywhere in the room.
            var phoneGo = Place(string.Format(Office, "desk_phone"), L(0.47f, 0.75f, -0.02f), yaw, t, false, false);
            phoneGo.name = "Phone";
            var handset = Place(string.Format(Office, "phone_handset"), phoneGo.transform.TransformPoint(new Vector3(0f, 0.062f, -0.05f)), yaw, phoneGo.transform, false, false);
            handset.name = "Handset";
            Transform Button(string n, float x, Material m)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = n;
                Object.DestroyImmediate(b.GetComponent<Collider>());
                b.transform.SetParent(phoneGo.transform, false);
                b.transform.localPosition = new Vector3(x, 0.04f, 0.08f);
                b.transform.localRotation = Quaternion.Euler(-7f, 0f, 0f);
                b.transform.localScale = new Vector3(0.046f, 0.012f, 0.024f);
                b.GetComponent<MeshRenderer>().sharedMaterial = m;
                return b.transform;
            }
            var hold = Button("HoldButton", 0.07f, holdMat);
            var mute = Button("MuteButton", 0.015f, muteMat);
            var transfer = Button("TransferButton", -0.04f, transferMat);
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "StatusLamp";
            Object.DestroyImmediate(lamp.GetComponent<Collider>());
            lamp.transform.SetParent(t, false);
            lamp.transform.position = L(0.24f, 1.16f, -0.19f);
            lamp.transform.localScale = Vector3.one * 0.07f;
            lamp.GetComponent<MeshRenderer>().sharedMaterial = lampMat;
            lamp.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var phone = deskGo.AddComponent<DeskPhone>();
            phone.Configure(lamp.GetComponent<MeshRenderer>(), handset.transform, hold, mute, transfer);

            // Chair, seat and the view of the screen.
            var chair = Place(string.Format(Flat, "desk_chair"), L(0f, 0f, 0.82f), yaw + 180f, pod, true);
            chair.name = $"Chair_{name}";
            var seat = Spot("Seat", L(0f, 0.05f, 0.85f), yaw + 180f, t);
            var screenCentre = L(0f, 1.08f, -0.18f);
            var eye = L(0f, 1.2f, 0.74f);
            var view = Spot("ScreenView", eye, 0f, t);
            view.rotation = Quaternion.LookRotation(screenCentre - eye, Vector3.up);

            // Name on the partition, facing the person at the desk.
            Sign("Nameplate", i == 0 ? "plate_you" : "plate_" + name.ToLowerInvariant(), L(0f, 0.95f, -0.37f), yaw + 180f, 0.34f, t);

            var desk = deskGo.AddComponent<SupportDesk>();
            desk.SetEntityId(i == 0 ? "your_desk" : $"desk_{id}");
            desk.Configure(seat, view, keyboard.transform, chair.GetComponent<Collider>(), phone, console, null);
            if (i > 0)
            {
                var mate = deskGo.AddComponent<Coworker>();
                mate.Configure(id, name, tech, slot, library, onBreak, null, 150f);
            }
            desks[i] = desk;
        }

        // Partitions: a low one down the middle, short ones between neighbours, ends.
        var p = string.Format(Office, "cubicle_partition");
        foreach (float x in new[] { -1.45f, 1.45f })
        {
            var mid = Place(p, new Vector3(x, 0f, 0f), 0f, pod, true);
            mid.transform.localScale = new Vector3(1f, 0.85f, 1f);
        }
        foreach (float z in new[] { -0.6f, 0.6f })
            foreach (float x in new[] { 0f, -2.9f, 2.9f })
            {
                var side = Place(p, new Vector3(x, 0f, z), 90f, pod, true);
                side.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            }
        Report.AppendLine("pod: 4 desks (you, Dana, Omar, Kim) with phones, handsets, lamps, nameplates; 8 partitions");
        return desks;
    }

    static void BackOffice()
    {
        var back = Group("BackOffice");
        // Supervisor's corner: desk, monitor, whiteboard with tonight's rules.
        Place(string.Format(Office, "office_desk"), new Vector3(4.9f, 0f, 3.9f), 180f, back);
        Place(string.Format(Office, "flat_monitor"), new Vector3(4.9f, 0.75f, 4.08f), 180f, back, false);
        Place(string.Format(Flat, "desk_chair"), new Vector3(4.9f, 0f, 3.15f), 0f, back);
        Place(string.Format(Office, "whiteboard"), new Vector3(1.6f, 1.05f, HalfZ - 0.1f), 180f, back, false);
        Sign("WhiteboardText", "sign_whiteboard", new Vector3(1.6f, 1.55f, HalfZ - 0.13f), 0f, 1.45f, back);

        // Reference shelf (binders) and the printer, west wall.
        var shelfDesk = Place(string.Format(Office, "office_desk"), new Vector3(-6.4f, 0f, 1.4f), 90f, back);
        var colours = new[] { "red", "blue", "green", "yellow", "blue", "red" };
        for (int i = 0; i < colours.Length; i++)
            Place(string.Format(Office, "binder_" + colours[i]), new Vector3(-6.55f, 0.75f, 0.85f + i * 0.09f), 0f, back, false);
        var shelfGo = shelfDesk;
        var shelf = shelfGo.AddComponent<ReferenceShelf>();
        shelf.SetEntityId("reference_shelf");
        Sign("ShelfLabel", "sign_reference", new Vector3(-6.07f, 0.64f, 1.4f), -90f, 0.42f, back);

        Place(string.Format(Office, "office_desk"), new Vector3(-6.4f, 0f, -1.1f), 90f, back);
        var printerGo = Place(string.Format(Office, "office_printer"), new Vector3(-6.45f, 0.75f, -1.1f), 90f, back, true, false);
        var tray = Spot("PrinterTray", new Vector3(-6.2f, 0.84f, -1.1f), 90f, back);
        var sheets = new PickupProp[3];
        for (int i = 0; i < sheets.Length; i++)
            sheets[i] = GameplaySceneSetup.MakeProp($"printout_{i}", "printout", "a printout of the help pages", string.Format(Flat, "bill_sheet"),
                new Vector3(-6.62f, 1.03f + i * 0.01f, -1.1f), 90f, 0.05f, back, PropCategory.Paper);
        var printer = printerGo.AddComponent<OfficePrinter>();
        printer.SetEntityId("printer");
        printer.Configure(sheets, tray);

        foreach (var corner in new[] { new Vector3(-6.5f, 0f, 4.5f), new Vector3(6.5f, 0f, 4.5f), new Vector3(-6.5f, 0f, -4.4f) })
            Place(string.Format(Office, "office_plant"), corner, 0f, back);
        Report.AppendLine("back office: supervisor desk + whiteboard, reference binders (readable), printer (3 printouts), plants");
    }

    static Transform BreakCorner()
    {
        var corner = Group("BreakCorner");
        Place(string.Format(Office, "vending_machine"), new Vector3(6.45f, 0f, -4.3f), -90f, corner);
        Place(string.Format(Office, "water_cooler"), new Vector3(6.65f, 0f, -2.7f), -90f, corner);
        Place(string.Format(Office, "break_sofa"), new Vector3(4.2f, 0f, -4.5f), 0f, corner);
        Place(string.Format(Flat, "kitchen_counter"), new Vector3(2.0f, 0f, -4.65f), 0f, corner);
        var coffee = Place(string.Format(Flat, "microwave"), new Vector3(1.7f, 0.92f, -4.75f), 0f, corner, true, false);
        coffee.name = "CoffeeMachine";
        coffee.AddComponent<CoffeeMachine>().SetEntityId("coffee_machine");
        for (int i = 0; i < 3; i++)
        {
            var mug = GameplaySceneSetup.MakeProp($"mug_{i}", "mug", "a mug", "Assets/Art/Props/mug.fbx", new Vector3(2.25f + i * 0.2f, 0.925f, -4.6f), 0f, 0.3f, corner, PropCategory.Small);
            mug.gameObject.AddComponent<Mug>();
        }
        Sign("BreakSign", "sign_break", new Vector3(4.2f, 1.75f, -4.91f), 180f, 0.9f, corner);
        Report.AppendLine("break corner: vending machine, water cooler, sofa, counter with coffee machine and 3 mugs");
        return Spot("KimBreakSpot", new Vector3(6.0f, 0f, -2.7f), -90f, corner);
    }

    static Supervisor MakeSupervisor(WorkerLibrary library)
    {
        var group = Group("Supervisor");
        var stops = new[]
        {
            Spot("Stop_at her desk", new Vector3(4.9f, 0f, 2.9f), 0f, group),
            Spot("Stop_watching the pod", new Vector3(0f, 0f, -2.3f), 0f, group),
            Spot("Stop_at the printer", new Vector3(-5.4f, 0f, -1.1f), -90f, group),
            Spot("Stop_watching the pod", new Vector3(0f, 0f, 2.3f), 180f, group),
            Spot("Stop_checking the break area", new Vector3(4.0f, 0f, -2.8f), 180f, group),
        };
        var listen = Spot("ListenSpot", new Vector3(-0.7f, 0f, -1.95f), -30f, group);
        var go = new GameObject("FloorSupervisor");
        go.transform.SetParent(group, false);
        go.transform.position = stops[0].position;
        var supervisor = go.AddComponent<Supervisor>();
        supervisor.Configure(library, stops, listen);
        return supervisor;
    }

    static Camera PreviewCamera()
    {
        var go = new GameObject("PreviewCamera");
        go.transform.SetParent(_root, false);
        go.transform.SetPositionAndRotation(new Vector3(-6f, 2.3f, -4.3f), Quaternion.Euler(22f, 50f, 0f));
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 62f;
        cam.nearClipPlane = 0.05f;
        var data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        return cam;
    }

    static Hud MakeHud(out SupportConsole console)
    {
        var hudGo = new GameObject("HUD");
        hudGo.transform.SetParent(_root, false);
        hudGo.AddComponent<UIDocument>().panelSettings = GameplaySceneSetup.BuildPanelSettings();
        var hud = hudGo.AddComponent<Hud>();
        console = hudGo.AddComponent<SupportConsole>();
        console.Configure(hud);
        return hud;
    }

    static void Session(WorkerLibrary library, Hud hud, SupportDesk[] desks, Supervisor supervisor, Transform breakSpot, Camera preview)
    {
        var story = Group("Story");
        var spawns = new[]
        {
            Spot("Spawn0", new Vector3(DoorX, 0.05f, -4.1f), 35f, story),
            Spot("Spawn1", new Vector3(DoorX + 0.8f, 0.05f, -4.0f), 30f, story),
            Spot("Spawn2", new Vector3(DoorX - 0.6f, 0.05f, -3.6f), 40f, story),
            Spot("Spawn3", new Vector3(DoorX + 0.3f, 0.05f, -3.3f), 35f, story),
        };
        foreach (var desk in desks)
        {
            var mate = desk.GetComponent<Coworker>();
            if (mate != null && mate.StartsOnBreak)
                mate.Configure(mate.WorkerId, mate.DisplayName, mate.Technical, 3, library, true, breakSpot, 150f);
        }
        var sessionGo = new GameObject("Session");
        sessionGo.transform.SetParent(story, false);
        var session = sessionGo.AddComponent<GameSession>();
        session.Configure(library, hud, spawns, preview);
        sessionGo.AddComponent<CallCenterShift>().Configure(session, desks, supervisor, breakSpot);
        Report.AppendLine("story: session, call center shift, 4 spawns by the door");
    }

    // Light ----------------------------------------------------------------------------------

    static void Lighting()
    {
        var lights = Group("Lights");
        var panelGlow = Glow("Panel light", new Color(1f, 0.98f, 0.92f), 3f);
        foreach (float x in new[] { -4.5f, -1.5f, 1.5f, 4.5f })
            foreach (float z in new[] { -2.5f, 0f, 2.5f })
            {
                var panel = Place(string.Format(Office, "ceiling_panel"), new Vector3(x, Height, z), 0f, lights, false);
                foreach (var r in panel.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = ShadowCastingMode.Off;
            }

        Light Make(string name, Vector3 pos, Color colour, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(lights, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = colour;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.Soft;
            l.lightmapBakeType = LightmapBakeType.Baked;
            return l;
        }
        var cool = new Color(1f, 0.97f, 0.9f);
        foreach (float x in new[] { -4.5f, -1.5f, 1.5f, 4.5f })
            foreach (float z in new[] { -2.5f, 2.5f })
                Make($"Panel_{x}_{z}", new Vector3(x, Height - 0.35f, z), cool, 2.4f, 7.5f);
        Make("PodFill", new Vector3(0f, Height - 0.4f, 0f), cool, 2f, 6f);
        Make("BreakWarm", new Vector3(4.2f, 2.0f, -3.8f), new Color(1f, 0.82f, 0.6f), 1.6f, 5f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.42f, 0.44f, 0.48f);
        RenderSettings.ambientEquatorColor = new Color(0.33f, 0.33f, 0.32f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.17f, 0.16f);
        RenderSettings.skybox = null;

        var settings = new LightingSettings
        {
            name = "CallCenterLighting",
            lightmapper = LightingSettings.Lightmapper.ProgressiveGPU,
            bakedGI = true,
            realtimeGI = false,
            lightmapResolution = 16f,
            lightmapPadding = 4,
            lightmapMaxSize = 2048,
            directionalityMode = LightmapsMode.CombinedDirectional,
            ao = true,
            aoMaxDistance = 0.6f,
            maxBounces = 3,
            indirectScale = 1.2f,
            directSampleCount = 48,
            indirectSampleCount = 1024,
            environmentSampleCount = 256,
            filteringMode = LightingSettings.FilterMode.Advanced,
            denoiserTypeDirect = LightingSettings.DenoiserType.OpenImage,
            denoiserTypeIndirect = LightingSettings.DenoiserType.OpenImage,
            denoiserTypeAO = LightingSettings.DenoiserType.OpenImage,
            filterTypeDirect = LightingSettings.FilterType.None,
            filterTypeIndirect = LightingSettings.FilterType.None,
            filterTypeAO = LightingSettings.FilterType.None,
        };
        const string path = "Assets/Scenes/CallCenterLighting.lighting";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(settings, path);
        Lightmapping.lightingSettings = settings;

        var probes = new GameObject("LightProbes");
        probes.transform.SetParent(_root, false);
        var positions = new List<Vector3>();
        for (float x = -6.5f; x <= 6.5f; x += 1.3f)
            for (float z = -4.5f; z <= 4.5f; z += 1.3f)
                foreach (float y in new[] { 0.4f, 1.3f, 2.3f })
                    positions.Add(new Vector3(x, y, z));
        probes.AddComponent<LightProbeGroup>().probePositions = positions.ToArray();

        var reflection = new GameObject("RoomReflections");
        reflection.transform.SetParent(_root, false);
        reflection.transform.position = new Vector3(0f, 1.4f, 0f);
        var probe = reflection.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Baked;
        probe.size = new Vector3(14f, 2.8f, 10f);
        probe.boxProjection = true;
        probe.resolution = 128;
        Report.AppendLine($"lights: 12 ceiling panels, 10 baked point lights; probes {positions.Count}");
    }

    static void PostProcessing()
    {
        const string path = "Assets/Settings/CallCenterVolume.asset";
        AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1f);
        bloom.intensity.Override(0.4f);
        var colour = profile.Add<ColorAdjustments>(true);
        colour.postExposure.Override(0.55f);
        colour.contrast.Override(8f);
        colour.saturation.Override(-6f);
        profile.Add<WhiteBalance>(true).temperature.Override(-4f);
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.22f);
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
