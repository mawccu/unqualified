using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Collision for static models placed by the scene builders. Most furniture is
/// well served by one box around it. A few models have something tall standing
/// on a lower surface (a tap on a worktop, a backrest behind a seat, a pillow on
/// a mattress): one box would put an invisible lid at the tall part's height and
/// everything set on the surface would rest on that lid, floating. Those get
/// their own mesh as the collider (static, so concave is fine).
/// </summary>
public static class ColliderFit
{
    static readonly HashSet<string> Shaped = new()
    {
        "kitchen_counter", "couch", "break_sofa", "mattress", "bed_made", "bed_slept_in",
    };

    public static bool NeedsShape(GameObject go) => Shaped.Contains(ModelName(go));

    static string ModelName(GameObject go)
    {
        var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
        return source != null ? source.name : go.name;
    }

    /// The right collision for a placed model; returns the first collider added.
    public static Collider Fit(GameObject go)
    {
        if (!NeedsShape(go))
            return FitBox(go);
        Collider first = null;
        foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null)
                continue;
            var mesh = filter.gameObject.AddComponent<MeshCollider>();
            mesh.sharedMesh = filter.sharedMesh;
            first ??= mesh;
        }
        return first != null ? first : FitBox(go);
    }

    /// One box around all of a model's meshes, in its own space.
    public static BoxCollider FitBox(GameObject go)
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
}
