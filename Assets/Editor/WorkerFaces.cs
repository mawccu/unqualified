using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The worker models all share one face, and its moustache is baked into the body
/// mesh (two pieces of the dark "Detail" submesh just above the mouth). For
/// characters who have no moustache this builds a copy of each body mesh with
/// those two pieces removed: same vertices, skin weights and bones, so every
/// animation still fits. Saved next to the models and listed in the
/// WorkerLibrary; WorkerAvatar swaps it in when a costume says so.
/// </summary>
public static class WorkerFaces
{
    const string Folder = "Assets/Art/Characters/Faces";
    /// The moustache in the body mesh's bind pose (metres): two pieces either side
    /// of the middle, at mouth height, centred about 1.30 up and 0.046 out.
    const float MoustacheHeight = 1.301f, MoustacheSide = 0.046f, Tolerance = 0.03f;

    public static Mesh BuildCleanShaven(GameObject model)
    {
        var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name == "Worker");
        if (body == null)
            return null;
        var source = body.sharedMesh;
        int sub = System.Array.FindIndex(body.sharedMaterials, m => m != null && m.name == "Detail");
        if (sub < 0)
            return null;
        var verts = source.vertices;
        var tris = source.GetTriangles(sub);

        // Group the submesh's triangles into connected pieces (by position: split
        // normals duplicate vertices).
        var parent = new Dictionary<int, int>();
        var byPosition = new Dictionary<Vector3Int, int>();
        int Find(int x)
        {
            if (!parent.TryGetValue(x, out var p)) { parent[x] = x; return x; }
            return p == x ? x : parent[x] = Find(p);
        }
        int Key(int v)
        {
            var p = verts[v];
            var k = new Vector3Int(Mathf.RoundToInt(p.x * 2000f), Mathf.RoundToInt(p.y * 2000f), Mathf.RoundToInt(p.z * 2000f));
            if (!byPosition.TryGetValue(k, out var first)) { byPosition[k] = v; return v; }
            return first;
        }
        for (int t = 0; t < tris.Length; t += 3)
        {
            int a = Find(Key(tris[t])), b = Find(Key(tris[t + 1])), c = Find(Key(tris[t + 2]));
            parent[a] = b;
            parent[Find(c)] = Find(b);
        }
        var bounds = new Dictionary<int, Bounds>();
        for (int i = 0; i < tris.Length; i++)
        {
            int root = Find(Key(tris[i]));
            if (bounds.TryGetValue(root, out var b)) { b.Encapsulate(verts[tris[i]]); bounds[root] = b; }
            else bounds[root] = new Bounds(verts[tris[i]], Vector3.zero);
        }
        var moustache = new HashSet<int>(bounds.Where(p =>
            Mathf.Abs(p.Value.center.z - MoustacheHeight) < Tolerance && Mathf.Abs(Mathf.Abs(p.Value.center.x) - MoustacheSide) < Tolerance)
            .Select(p => p.Key));
        if (moustache.Count == 0)
        {
            Debug.LogWarning($"[faces] no moustache found in {model.name}");
            return null;
        }
        var kept = new List<int>();
        for (int t = 0; t < tris.Length; t += 3)
            if (!moustache.Contains(Find(Key(tris[t]))))
                kept.AddRange(new[] { tris[t], tris[t + 1], tris[t + 2] });

        var mesh = Object.Instantiate(source);
        mesh.name = source.name + "_clean";
        mesh.SetTriangles(kept, sub);
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Art/Characters", "Faces");
        string path = $"{Folder}/{model.name}_clean.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }
}
