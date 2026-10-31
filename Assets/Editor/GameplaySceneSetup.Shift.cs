using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The shift's things in the venue's rooms (Unity coordinates, north +Z): the
/// audio rig at the sound desk, spares and tools in storage, the kitchen's
/// coffee, radio, fan, tray, mugs and break couch, poppers, bins, the
/// cloakroom's host stand with the guest book and the bell, and where guests
/// come in and wait.
/// </summary>
public static partial class GameplaySceneSetup
{
    const string Props = "Assets/Art/Props/{0}.fbx";
    const string Apartment = "Assets/Art/Apartment/{0}.fbx";
    const string Pack = "Assets/Art/Models/{0}.fbx";

    static GameObject StaticModel(string path, Vector3 pos, float yaw, Transform parent, bool collide = true)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        if (collide)
            FitBox(go);
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
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

    static T Usable<T>(GameObject go, string id) where T : Interactable
    {
        var c = go.AddComponent<T>();
        c.SetEntityId(id);
        return c;
    }

    static Transform Spot(string name, Vector3 pos, float yaw, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        return t;
    }

    static void AddShiftContent(Transform gameplay, WeddingShift shift, Material pad, Material ghost, StringBuilder report)
    {
        var rooms = new GameObject("ShiftContent").transform;
        rooms.SetParent(gameplay, false);
        string P(string n) => string.Format(Props, n);
        string A(string n) => string.Format(Apartment, n);
        string K(string n) => string.Format(Pack, n);

        // --- Prep kitchen (x -14..0, z -14..-8) ---
        StaticModel(A("kitchen_counter"), new Vector3(-11f, 0f, -13.55f), 0f, rooms);
        StaticModel(A("kitchen_counter"), new Vector3(-9.4f, 0f, -13.55f), 0f, rooms);
        StaticModel(A("fridge"), new Vector3(-13.5f, 0f, -12.2f), 90f, rooms);
        var coffee = StaticModel(A("microwave"), new Vector3(-11.5f, 0.92f, -13.65f), 0f, rooms);
        Usable<CoffeeMachine>(coffee, "coffee_machine");
        for (int i = 0; i < 3; i++)
        {
            var mug = MakeProp($"mug_{i}", "mug", "a mug", P("mug"), new Vector3(-10.85f + i * 0.2f, 0.925f, -13.45f), 0f, 0.3f, rooms, PropCategory.Small);
            mug.gameObject.AddComponent<Mug>();
        }
        var radio = StaticModel(P("radio"), new Vector3(-8.75f, 0.92f, -13.65f), 0f, rooms);
        Usable<Radio>(radio, "radio");
        MakeProp("tray", "tray", "a serving tray", P("tray"), new Vector3(-9.6f, 0.925f, -13.4f), 0f, 0.8f, rooms, PropCategory.Wide);
        MakeProp("cloth", "cloth", "a cleaning cloth", K("cleaning_cloth"), new Vector3(-10.2f, 0.925f, -13.75f), 20f, 0.1f, rooms, PropCategory.Small);
        // A fan on the counter end, pointed at the order slips on the next counter.
        var fan = StaticModel(P("fan_body"), new Vector3(-12.3f, 0.92f, -13.5f), 70f, rooms);
        var blades = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P("fan_blades")), fan.transform);
        blades.transform.localPosition = new Vector3(0f, 0.25f, 0.055f);
        Usable<DeskFan>(fan, "fan").Configure(blades.transform);
        for (int i = 0; i < 4; i++)
            MakeProp($"order_slip_{i}", "clutter", "an order slip", A("envelope"), new Vector3(-9.9f + i * 0.25f, 0.93f, -13.2f + (i % 2) * 0.08f), i * 37f, 0.05f, rooms, PropCategory.Paper);
        var kitchenBin = StaticModel(P("bin_body"), new Vector3(-1f, 0f, -13.4f), 0f, rooms);
        var kitchenLid = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P("bin_lid")), kitchenBin.transform);
        kitchenLid.transform.localPosition = new Vector3(0f, 0.42f, -0.17f);
        Usable<PedalBin>(kitchenBin, "bin_kitchen").Configure(kitchenLid.transform);
        // The break corner: a tired couch to sit on.
        var couch = StaticModel(A("couch"), new Vector3(-4.5f, 0f, -13.45f), 0f, rooms);
        Usable<Seat>(couch, "couch").Configure(Spot("CouchSeat", new Vector3(-4.5f, 0.05f, -13.3f), 0f, couch.transform), WorkerAvatar.CouchSeat);

        // --- Storage (x 0..7, z -13..-8) ---
        for (int i = 0; i < 3; i++)
            StaticModel(K("storage_shelf"), new Vector3(1.2f + i * 1.3f, 0f, -12.6f), 0f, rooms);
        StaticModel(K("prep_table"), new Vector3(5.3f, 0f, -12.4f), 0f, rooms);
        float[] spareX = { 4.75f, 5.42f, 5.95f };
        string[] spareIds = { "cake_base", "cake_middle", "cake_top" };
        string[] spareNames = { "the spare bottom layer", "the spare middle layer", "the spare top layer" };
        for (int i = 0; i < 3; i++)
            MakeProp(spareIds[i] + "_spare", "cake_tier", spareNames[i], spareIds[i], new Vector3(spareX[i], 0.903f, -12.4f), 0f, 2f, rooms, PropCategory.Wide);
        var drawerUnit = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P("drawer_unit")), rooms);
        drawerUnit.transform.SetPositionAndRotation(new Vector3(6.55f, 0f, -10.4f), Quaternion.Euler(0f, -90f, 0f));
        // The carcass is solid above the drawer; the drawer itself is a tray you can reach into.
        var top = drawerUnit.AddComponent<BoxCollider>();
        top.center = new Vector3(0f, 0.6f, 0f);
        top.size = new Vector3(0.5f, 0.4f, 0.45f);
        var back = drawerUnit.AddComponent<BoxCollider>();
        back.center = new Vector3(0f, 0.2f, 0.2f);
        back.size = new Vector3(0.5f, 0.4f, 0.05f);
        var drawer = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P("drawer")), drawerUnit.transform);
        drawer.transform.localPosition = new Vector3(0f, 0.1f, -0.02f);
        foreach (var (c, s) in new[] { (new Vector3(0f, 0.01f, 0f), new Vector3(0.44f, 0.02f, 0.4f)), (new Vector3(-0.21f, 0.09f, 0f), new Vector3(0.02f, 0.17f, 0.4f)),
                     (new Vector3(0.21f, 0.09f, 0f), new Vector3(0.02f, 0.17f, 0.4f)), (new Vector3(0f, 0.09f, 0.19f), new Vector3(0.44f, 0.17f, 0.02f)), (new Vector3(0f, 0.1f, -0.21f), new Vector3(0.44f, 0.21f, 0.02f)) })
        {
            var b = drawer.AddComponent<BoxCollider>();
            b.center = c;
            b.size = s;
        }
        Usable<Drawer>(drawerUnit, "drawer").Configure(drawer.transform);
        MakeProp("spatula", "spatula", "the cake spatula", P("spatula"), drawer.transform.TransformPoint(new Vector3(0f, 0.03f, 0f)), -90f, 0.15f, rooms, PropCategory.Small);
        MakeProp("mop", "mop", "a mop", K("mop"), new Vector3(0.6f, 0f, -9.2f), 0f, 1.2f, rooms, PropCategory.Small);
        StaticModel(K("mop_bucket"), new Vector3(1.2f, 0f, -9.1f), 10f, rooms);
        StaticModel(K("storage_crate"), new Vector3(5.6f, 0f, -9.0f), 5f, rooms);
        for (int i = 0; i < 3; i++)
        {
            var popper = MakeProp($"popper_{i}", "party_popper", "a party popper", P("party_popper"), new Vector3(5.45f + i * 0.12f, 0.465f, -9.0f), i * 30f, 0.05f, rooms, PropCategory.Small);
            popper.gameObject.AddComponent<PartyPopper>();
        }
        // The storage light is on a switch by the door.
        var storageLight = new GameObject("StorageLight").AddComponent<Light>();
        storageLight.transform.SetParent(rooms, false);
        storageLight.transform.position = new Vector3(3.5f, 2.7f, -10.5f);
        storageLight.type = LightType.Point;
        storageLight.range = 7f;
        storageLight.intensity = 2.5f;
        storageLight.color = new Color(1f, 0.92f, 0.78f);
        // No shadows: a point light's shadows re-render the room six times a frame.
        storageLight.shadows = LightShadows.None;
        storageLight.lightmapBakeType = LightmapBakeType.Realtime;
        var bulb = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(A("bare_bulb")), rooms);
        bulb.transform.position = new Vector3(3.5f, 3f, -10.5f);
        var lightSwitch = StaticModel(P("light_switch"), new Vector3(1.6f, 1.15f, -8.12f), 180f, rooms);
        Usable<LightSwitch>(lightSwitch, "storage_switch").Configure(storageLight, bulb.GetComponentInChildren<Renderer>());

        // --- Back of house bin ---
        var bohBin = StaticModel(P("bin_body"), new Vector3(-13f, 0f, -6.5f), 0f, rooms);
        var bohLid = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P("bin_lid")), bohBin.transform);
        bohLid.transform.localPosition = new Vector3(0f, 0.42f, -0.17f);
        Usable<PedalBin>(bohBin, "bin_back").Configure(bohLid.transform);

        // --- Service corridor (x 14..16.6, z -7..15): a spare chair to sit on, a crate ---
        var spare = StaticModel(K("guest_chair"), new Vector3(16.1f, 0f, 4f), -90f, rooms);
        Usable<Seat>(spare, "corridor_chair").Configure(Spot("CorridorSeat", new Vector3(16.05f, 0.05f, 4f), -90f, spare.transform));
        StaticModel(K("storage_crate"), new Vector3(16.1f, 0f, 7f), 12f, rooms);

        // --- Cloakroom (x -19..-14, z 0..8): host stand, guest book, bell, coat rack, the doors guests come in by ---
        StaticModel(K("prep_table"), new Vector3(-16.4f, 0f, 5.6f), 0f, rooms);
        MakeProp("guest_book", "guest_book", "the guest book", P("guest_book"), new Vector3(-16.6f, 0.903f, 5.55f), 0f, 0.6f, rooms, PropCategory.Small);
        var bell = StaticModel(P("desk_bell"), new Vector3(-15.85f, 0.9f, 5.6f), 0f, rooms);
        Usable<Bell>(bell, "host_bell");
        StaticModel(P("coat_rack"), new Vector3(-16.5f, 0f, 7.85f), 180f, rooms, false);
        StaticModel(A("door_frame"), new Vector3(-18.9f, 0f, 2.2f), 90f, rooms, false);
        StaticModel(A("door_leaf"), new Vector3(-18.92f, 0f, 1.75f), 90f, rooms, false);
        var door = Spot("GuestDoor", new Vector3(-18.3f, 0f, 2.2f), 90f, rooms);
        var waits = new Transform[6];
        for (int i = 0; i < waits.Length; i++)
            waits[i] = Spot($"GuestWait{i}", new Vector3(-17.8f + (i % 3) * 0.9f, 0f, 1.2f + (i / 3) * 0.9f), 0f, rooms);

        // --- The audio rig at the sound desk (Unity (7.5, 0, 11.2), facing south) ---
        Vector3 deskTop = new Vector3(7.5f, 0.95f, 11.2f);
        var audioGo = new GameObject("AudioDesk");
        audioGo.transform.SetParent(rooms, false);
        audioGo.transform.position = deskTop;
        var desk = audioGo.AddComponent<AudioDesk>();
        var player = StaticModel(P("radio"), deskTop + new Vector3(-0.75f, 0f, 0.15f), 180f, rooms);
        player.name = "MusicPlayer";
        var speakers = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            var sp = new GameObject($"HallSpeaker{i}").AddComponent<AudioSource>();
            sp.transform.SetParent(audioGo.transform, false);
            sp.transform.position = new Vector3(i == 0 ? -4.9f : 4.9f, 1.2f, 13.6f);
            sp.spatialBlend = 0.8f;
            sp.minDistance = 4f;
            sp.maxDistance = 45f;
            sp.playOnAwake = false;
            speakers[i] = sp;
        }
        var monitor = audioGo.AddComponent<AudioSource>();
        monitor.spatialBlend = 1f;
        monitor.maxDistance = 6f;
        monitor.playOnAwake = false;
        desk.Configure(speakers, monitor);
        string[] portIds = { AudioDesk.PortIn, AudioDesk.PortMain, AudioDesk.PortMonitor };
        string[] portNames = { "the INPUT port (round)", "the MAIN OUT port (square)", "the MONITOR port (triangle)" };
        for (int i = 0; i < 3; i++)
            MakeSocket(portIds[i], portNames[i], "cable", "", 0.07f, deskTop + new Vector3(-0.3f + i * 0.3f, 0.01f, 0.25f), pad, ghost, rooms);
        var lineMat = TransparentUnlit("Cable line", Color.white);
        lineMat.SetFloat("_Surface", 0f);
        lineMat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        lineMat.renderQueue = -1;
        var music = MakeProp(AudioDesk.CableMusic, "cable", "the MUSIC cable (round plug)", K("cable_yellow"), new Vector3(6.4f, 0f, 10.3f), 30f, 0.3f, rooms, PropCategory.Small);
        music.gameObject.AddComponent<CableLine>().Configure(player.transform, lineMat, new Color(0.95f, 0.8f, 0.2f));
        var speakerCable = MakeProp(AudioDesk.CableSpeakers, "cable", "the SPEAKER cable (square plug)", K("cable_red"), new Vector3(8.6f, 0f, 10.2f), -40f, 0.3f, rooms, PropCategory.Small);
        var speakerEnd = Spot("SpeakerCableEnd", new Vector3(4.9f, 0.4f, 13.4f), 0f, rooms);
        speakerCable.gameObject.AddComponent<CableLine>().Configure(speakerEnd, lineMat, new Color(0.85f, 0.2f, 0.18f));
        string[] controls = { "Gain", "Track", "Play" };
        var controlMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Charcoal.mat");
        for (int i = 0; i < 3; i++)
        {
            var c = GameObject.CreatePrimitive(i == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            c.name = controls[i];
            c.transform.SetParent(audioGo.transform, false);
            c.transform.position = deskTop + new Vector3(-0.25f + i * 0.25f, 0.06f, -0.1f);
            c.transform.localScale = i == 0 ? new Vector3(0.07f, 0.03f, 0.07f) : new Vector3(0.09f, 0.04f, 0.06f);
            c.GetComponent<MeshRenderer>().sharedMaterial = i == 2 ? AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Sage.mat") : controlMat;
            var control = c.AddComponent<AudioControl>();
            control.SetEntityId($"audio_{controls[i].ToLowerInvariant()}");
            control.Configure(desk, (AudioControl.Kind)i);
        }

        // Where things happen.
        var stage = Spot("CeremonySpot", new Vector3(0f, 0f, 11.3f), 180f, rooms);
        var cakeStation = Spot("CakeStation", new Vector3(12.7f, 1.2f, 5f), 90f, rooms);
        var soundStation = Spot("SoundStation", deskTop, 180f, rooms);
        var spillPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(K("frosting_spill"));
        shift.ConfigureShift(desk, spillPrefab, door, waits, stage, cakeStation, soundStation);
        report.AppendLine("shift content: kitchen (coffee, mugs, radio, fan, tray, cloth, bin, couch), storage (spares, drawer + spatula, mop, poppers, switched light), corridor seat, cloakroom (guest book, bell, guest door), audio rig (3 ports, 2 cables, 3 controls, 2 speakers)");
    }
}
