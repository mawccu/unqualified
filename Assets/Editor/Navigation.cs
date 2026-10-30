using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Walkable space for NPCs and solid, movable chairs.
///
/// The navigation mesh is baked from the world layer's colliders only (walls,
/// tables, counters, the stage). Chairs are props: each carves its own hole in
/// the mesh where it stands, so a chair moved into an aisle is walked round.
/// Agents are 0.3 m in radius, so aisles between tables stay open.
/// </summary>
public static class Navigation
{
    const float AgentRadius = 0.3f;
    const float AgentHeight = 1.75f;
    const float AgentClimb = 0.32f;

    public static void Bake(GameObject root, string assetPath, StringBuilder report)
    {
        SetAgentSize();
        var surface = root.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = root.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = 1 << GameLayers.World;
        surface.agentTypeID = 0;
        surface.BuildNavMesh();
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
        EditorUtility.SetDirty(surface);
        var triangles = NavMesh.CalculateTriangulation();
        report.AppendLine($"navigation: baked ({triangles.vertices.Length} vertices), agent radius {AgentRadius} m");
    }

    /// The default agent type: a person, not the much wider Unity default.
    static void SetAgentSize()
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset");
        if (asset == null || asset.Length == 0)
            return;
        var so = new SerializedObject(asset[0]);
        var settings = so.FindProperty("m_Settings");
        if (settings == null || settings.arraySize == 0)
            return;
        var first = settings.GetArrayElementAtIndex(0);
        first.FindPropertyRelative("agentRadius").floatValue = AgentRadius;
        first.FindPropertyRelative("agentHeight").floatValue = AgentHeight;
        first.FindPropertyRelative("agentClimb").floatValue = AgentClimb;
        so.ApplyModifiedProperties();
    }

    /// A guest chair: a solid seat-and-legs block and a backrest (no invisible box
    /// over the seat), draggable by its back, pushed a little when walked into,
    /// and cut out of the navigation mesh wherever it stops.
    public static PickupProp MakeChair(string id, Vector3 position, float yaw, Transform parent)
    {
        var prop = GameplaySceneSetup.MakeProp(id, "chair", "a chair", "guest_chair", position, yaw, 7f, parent, PropCategory.Furniture);
        var go = prop.gameObject;
        var whole = go.GetComponent<BoxCollider>();
        var b = new Bounds(whole.center, whole.size);
        Object.DestroyImmediate(whole);
        const float seat = 0.48f;
        var lower = go.AddComponent<BoxCollider>();
        lower.center = new Vector3(b.center.x, seat * 0.5f, b.center.z);
        lower.size = new Vector3(b.size.x, seat, b.size.z);
        var back = go.AddComponent<BoxCollider>();
        back.center = new Vector3(b.center.x, (seat + b.max.y) * 0.5f, b.min.z + 0.045f);
        back.size = new Vector3(b.size.x, b.max.y - seat, 0.09f);
        var obstacle = go.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = new Vector3(b.center.x, 0.5f, b.center.z);
        obstacle.size = new Vector3(b.size.x, 1f, b.size.z);
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        // Lit by the bake where it starts (moved chairs keep their own lighting).
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
        return prop;
    }
}
