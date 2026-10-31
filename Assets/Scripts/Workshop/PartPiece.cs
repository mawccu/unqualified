using UnityEngine;

/// <summary>
/// Marks a prop as one physical workshop part (or a parcel) and says which. The
/// physical thing is only a stand-in: the parts the crew owns live in
/// CrewState, so dropping one behind the sofa loses nothing.
/// </summary>
public class PartPiece : MonoBehaviour
{
    [SerializeField] string partId = "";

    public string PartId => partId;

    /// Makes a pickup from a workshop model at runtime.
    public static PickupProp Spawn(GameObject model, string entityId, string partId, string kind, string display, Vector3 position, Quaternion rotation, float mass = 0.2f, float scale = 1f)
    {
        var root = new GameObject(entityId);
        root.SetActive(false);
        root.transform.SetPositionAndRotation(position, rotation);
        GameObject look = model != null ? Instantiate(model, root.transform) : GameObject.CreatePrimitive(PrimitiveType.Cube);
        if (model == null)
        {
            look.transform.SetParent(root.transform, false);
            look.transform.localScale = Vector3.one * 0.06f;
            look.transform.localPosition = Vector3.up * 0.03f;
        }
        else
        {
            look.transform.localPosition = Vector3.zero;
            look.transform.localRotation = Quaternion.identity;
            look.transform.localScale = Vector3.one * scale;
        }
        return Finish(root, look, entityId, partId, kind, display, mass);
    }

    /// Turns a prepared, inactive object into a registered pickup: a box around the
    /// meshes of boundsFrom, a light rigid body, the prop itself.
    public static PickupProp Finish(GameObject root, GameObject boundsFrom, string entityId, string partId, string kind, string display, float mass, PropCategory category = PropCategory.Small)
    {
        foreach (var c in root.GetComponentsInChildren<Collider>(true))
            Destroy(c);
        GameLayers.SetRecursively(root, GameLayers.Prop);
        var bounds = new Bounds();
        bool first = true;
        var toLocal = root.transform.worldToLocalMatrix;
        foreach (var filter in boundsFrom.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null)
                continue;
            var b = filter.sharedMesh.bounds;
            var m = toLocal * filter.transform.localToWorldMatrix;
            for (int c = 0; c < 8; c++)
            {
                var p = m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1)));
                if (first) { bounds = new Bounds(p, Vector3.zero); first = false; }
                else bounds.Encapsulate(p);
            }
        }
        var box = root.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = Vector3.Max(bounds.size, Vector3.one * 0.012f);
        root.AddComponent<Rigidbody>().mass = mass;
        root.AddComponent<PartPiece>().partId = partId;
        var prop = root.AddComponent<PickupProp>();
        prop.Configure(entityId, kind, display, category);
        root.SetActive(true);
        GameSession.Instance?.Service.RegisterProp(prop);
        return prop;
    }

    /// Removes a spawned part for good (used up in a build, or the build cancelled).
    public static void Remove(PickupProp prop)
    {
        if (prop == null)
            return;
        GameSession.Instance?.Service.UnregisterProp(prop.EntityId);
        Destroy(prop.gameObject);
    }
}
