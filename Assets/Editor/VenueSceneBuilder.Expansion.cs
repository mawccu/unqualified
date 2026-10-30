using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The venue's added rooms (about 30 % more floor), each with a job and a way
/// round: a prep kitchen and a storage room off the back of house, a service
/// corridor along the east side linking the back of house to the stage end (the
/// discreet route), and a cloakroom on the west where guests arrive. Openings are
/// 2 m wide so two-handed carries fit. These are Unity coordinates (north +Z);
/// the gaps in the old walls are given in the Godot numbers the shell uses.
/// </summary>
public static partial class VenueSceneBuilder
{
    // Panels left out of the original walls (Godot coordinates of the panel centre).
    static readonly int[] SouthGaps = { -7, 3 };   // back of house -> kitchen, storage
    static readonly int[] WestGaps = { -3 };       // ballroom -> cloakroom
    static readonly int[] EastGaps = { 5, -13 };   // back of house and stage end -> service corridor

    const float RoomHeight = 3f;
    const float Thin = 0.2f;

    /// A wall collider along a line, with 2 m openings at the gap centres (Godot numbers).
    static void GappedWall(string name, bool alongX, float line, float from, float to, int[] gaps, Transform parent)
    {
        var cuts = new List<float>(System.Array.ConvertAll(gaps, g => (float)g));
        cuts.Sort();
        float start = from;
        foreach (float gap in cuts)
        {
            Segment(start, gap - 1f);
            // A header over the opening, up to the ceiling.
            Vector3 headerCentre = alongX ? G(gap, (2.3f + WallHeight) * 0.5f, line) : G(line, (2.3f + WallHeight) * 0.5f, gap);
            var header = Block($"{name}Header", alongX ? new Vector3(2f, WallHeight - 2.3f, 0.3f) : new Vector3(0.3f, WallHeight - 2.3f, 2f), headerCentre, "Plaster", true, parent);
            header.GetComponent<MeshRenderer>().sharedMaterial = RoomPaint("Door header", new Color(0.62f, 0.6f, 0.52f));
            start = gap + 1f;
        }
        Segment(start, to);

        void Segment(float a, float b)
        {
            if (b - a < 0.05f)
                return;
            float mid = (a + b) * 0.5f;
            Vector3 centre = alongX ? G(mid, WallHeight * 0.5f, line) : G(line, WallHeight * 0.5f, mid);
            Vector3 size = alongX ? new Vector3(b - a, WallHeight, 0.3f) : new Vector3(0.3f, WallHeight, b - a);
            Solid(name, size, centre, Quaternion.identity, parent);
        }
    }

    static Material RoomPaint(string name, Color colour, float smoothness = 0.15f)
    {
        string path = string.Format(MaterialPath, name);
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

    /// A box room in Unity coordinates: floor, ceiling, and the listed walls.
    static Transform Room(string name, float x0, float x1, float z0, float z1, Material floor, Material wall, Material ceiling, string walls, (char side, float centre, float width)[] doors = null)
    {
        var room = Group(name);
        Box($"{name}Floor", new Vector3(x0, -0.3f, z0), new Vector3(x1, 0f, z1), floor, room, true);
        Box($"{name}Ceiling", new Vector3(x0, RoomHeight, z0), new Vector3(x1, RoomHeight + 0.15f, z1), ceiling, room, false);
        foreach (char side in walls)
        {
            float h = Thin * 0.5f;
            var span = side switch
            {
                'W' => (new Vector3(x0 - h, 0f, z0 - h), new Vector3(x0 + h, RoomHeight, z1 + h)),
                'E' => (new Vector3(x1 - h, 0f, z0 - h), new Vector3(x1 + h, RoomHeight, z1 + h)),
                'S' => (new Vector3(x0 - h, 0f, z0 - h), new Vector3(x1 + h, RoomHeight, z0 + h)),
                _ => (new Vector3(x0 - h, 0f, z1 - h), new Vector3(x1 + h, RoomHeight, z1 + h)),
            };
            // Doors in this wall split it.
            var cuts = new List<(float c, float w)>();
            if (doors != null)
                foreach (var d in doors)
                    if (d.side == side)
                        cuts.Add((d.centre, d.width));
            bool alongX = side == 'S' || side == 'N';
            float from = alongX ? span.Item1.x : span.Item1.z;
            float to = alongX ? span.Item2.x : span.Item2.z;
            cuts.Sort((a, b) => a.c.CompareTo(b.c));
            float start = from;
            foreach (var (c, w) in cuts)
            {
                WallPiece(start, c - w * 0.5f);
                var lo = span.Item1;
                var hi = span.Item2;
                if (alongX) { lo.x = c - w * 0.5f; hi.x = c + w * 0.5f; }
                else { lo.z = c - w * 0.5f; hi.z = c + w * 0.5f; }
                lo.y = 2.3f;
                Box($"{name}Header", lo, hi, wall, room, true);
                start = c + w * 0.5f;
            }
            WallPiece(start, to);

            void WallPiece(float a, float b)
            {
                if (b - a < 0.05f)
                    return;
                var lo = span.Item1;
                var hi = span.Item2;
                if (alongX) { lo.x = a; hi.x = b; }
                else { lo.z = a; hi.z = b; }
                Box($"{name}Wall{side}", lo, hi, wall, room, true);
            }
        }
        return room;
    }

    static GameObject Box(string name, Vector3 min, Vector3 max, Material material, Transform parent, bool collide)
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

    static void PointLight(string name, Vector3 at, Color colour, float intensity, float range, Transform parent, bool baked = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = at;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = colour;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;
        light.lightmapBakeType = baked ? LightmapBakeType.Baked : LightmapBakeType.Realtime;
    }

    static void Expansion()
    {
        var plaster = RoomPaint("Kitchen tile", new Color(0.84f, 0.85f, 0.82f), 0.35f);
        var kitchenFloor = RoomPaint("Kitchen floor", new Color(0.5f, 0.52f, 0.53f), 0.3f);
        var concrete = RoomPaint("Storage concrete", new Color(0.55f, 0.55f, 0.52f));
        var concreteFloor = RoomPaint("Storage floor", new Color(0.38f, 0.38f, 0.37f));
        var corridorWall = RoomPaint("Service corridor wall", new Color(0.5f, 0.56f, 0.52f));
        var corridorFloor = RoomPaint("Service corridor lino", new Color(0.3f, 0.32f, 0.31f), 0.35f);
        var cloakWall = RoomPaint("Cloakroom wall", new Color(0.46f, 0.2f, 0.22f));
        var carpet = RoomPaint("Cloakroom carpet", new Color(0.3f, 0.12f, 0.13f));
        var ceiling = RoomPaint("Back room ceiling", new Color(0.7f, 0.7f, 0.68f));

        var kitchen = Room("PrepKitchen", -14f, 0f, -14f, -8f, kitchenFloor, plaster, ceiling, "WES");
        var storage = Room("Storage", 0f, 7f, -13f, -8f, concreteFloor, concrete, ceiling, "ES");
        var corridor = Room("ServiceCorridor", 14f, 16.6f, -7f, 15f, corridorFloor, corridorWall, ceiling, "ENS");
        var cloak = Room("Cloakroom", -19f, -14f, 0f, 8f, carpet, cloakWall, ceiling, "WNS");

        var lights = Group("RoomLights");
        PointLight("KitchenLightA", new Vector3(-10f, 2.6f, -11f), new Color(0.95f, 0.97f, 1f), 4f, 9f, lights);
        PointLight("KitchenLightB", new Vector3(-4f, 2.6f, -11f), new Color(0.95f, 0.97f, 1f), 4f, 9f, lights);
        PointLight("CorridorLightA", new Vector3(15.3f, 2.6f, -2f), new Color(0.85f, 1f, 0.85f), 3f, 8f, lights);
        PointLight("CorridorLightB", new Vector3(15.3f, 2.6f, 9f), new Color(0.85f, 1f, 0.85f), 3f, 8f, lights);
        PointLight("CloakroomLight", new Vector3(-16.5f, 2.6f, 4f), new Color(1f, 0.82f, 0.6f), 4f, 8f, lights);
        // Storage has a switch, so its light is real-time (see GameplaySceneSetup).

        // Probes so people and props are lit in the new rooms too.
        var probes = new GameObject("RoomProbes").AddComponent<LightProbeGroup>();
        probes.transform.SetParent(_root, false);
        var points = new List<Vector3>();
        void Grid(float x0, float x1, float z0, float z1)
        {
            for (float x = x0 + 0.8f; x < x1; x += 2f)
                for (float z = z0 + 0.8f; z < z1; z += 2f)
                    foreach (float y in new[] { 0.4f, 1.4f, 2.5f })
                        points.Add(new Vector3(x, y, z));
        }
        Grid(-14f, 0f, -14f, -8f);
        Grid(0f, 7f, -13f, -8f);
        Grid(14f, 16.6f, -7f, 15f);
        Grid(-19f, -14f, 0f, 8f);
        probes.probePositions = points.ToArray();
        Report.AppendLine("expansion: prep kitchen, storage, service corridor, cloakroom (+209 m², about 31 %), openings 2 m wide");
    }
}
