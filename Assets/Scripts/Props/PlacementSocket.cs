using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A spot that accepts one kind of prop. It does not care WHICH prop of that
/// kind: putting the wrong one in is a mistake the crew can notice and fix, and
/// the task logic reports it. While the player aims at it with a matching prop
/// in hand it shows a ghost of that prop. Its trigger is switched off while
/// occupied so the placed prop itself can be aimed at and picked up.
///
/// RequiresSocket lets a layer wait for the one below it (cake tiers).
/// </summary>
public class PlacementSocket : MonoBehaviour
{
    const float SnapLift = 0.004f;

    [SerializeField] string entityId = "socket";
    [SerializeField] string displayName = "stand";
    [SerializeField] string acceptsKind = "cake_tier";
    [SerializeField] string requiresSocket = "";
    [SerializeField] float padRadius = 0.3f;
    [SerializeField] Material padMaterial;
    [SerializeField] Material ghostMaterial;

    public string EntityId => entityId;
    public string DisplayName => displayName;
    public string RequiresSocket => requiresSocket;
    public Vector3 SnapPosition => transform.position + transform.up * SnapLift;

    string _occupant = "";
    public string OccupantId
    {
        get => _occupant;
        set
        {
            _occupant = value ?? "";
            Refresh();
        }
    }

    BoxCollider _trigger;
    GameObject _pad;
    GameObject _ghost;
    PickupProp _ghostSource;

    public void Configure(string id, string display, string kind, string requires, float radius, Material pad, Material ghost)
    {
        entityId = id;
        displayName = display;
        acceptsKind = kind;
        requiresSocket = requires;
        padRadius = radius;
        padMaterial = pad;
        ghostMaterial = ghost;
    }

    void Awake()
    {
        gameObject.layer = GameLayers.Socket;
        _trigger = gameObject.AddComponent<BoxCollider>();
        _trigger.isTrigger = true;
        float r = Mathf.Max(padRadius, 0.2f);
        _trigger.size = new Vector3(r * 2f, 0.4f, r * 2f);
        _trigger.center = new Vector3(0f, 0.2f, 0f);

        // A layer that waits for the one below shows only the ghost when aimed
        // at, not a pad floating in mid-air.
        if (string.IsNullOrEmpty(requiresSocket) && padMaterial != null)
        {
            _pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _pad.name = "Pad";
            Destroy(_pad.GetComponent<Collider>());
            _pad.transform.SetParent(transform, false);
            _pad.transform.localPosition = new Vector3(0f, 0.004f, 0f);
            _pad.transform.localScale = new Vector3(padRadius * 2f, 0.002f, padRadius * 2f);
            var r2 = _pad.GetComponent<MeshRenderer>();
            r2.sharedMaterial = padMaterial;
            r2.shadowCastingMode = ShadowCastingMode.Off;
            r2.receiveShadows = false;
        }
        Refresh();
    }

    public bool Accepts(PickupProp prop) => prop.Kind == acceptsKind;

    /// Show or hide the ghost. prop is the one in the player's hand.
    public void SetPreview(bool on, PickupProp prop = null)
    {
        bool show = on && prop != null && string.IsNullOrEmpty(_occupant) && ghostMaterial != null;
        if (show && _ghostSource != prop)
            BuildGhost(prop);
        if (_ghost != null)
            _ghost.SetActive(show);
    }

    void BuildGhost(PickupProp prop)
    {
        if (_ghost != null)
            Destroy(_ghost);
        _ghostSource = prop;
        _ghost = new GameObject("Ghost");
        _ghost.transform.SetParent(transform, false);
        _ghost.transform.localPosition = new Vector3(0f, SnapLift, 0f);
        // Copy each visible mesh with its pose relative to the prop's base.
        var basis = Matrix4x4.TRS(prop.transform.position, prop.transform.rotation, Vector3.one).inverse;
        foreach (var filter in prop.VisualMeshes())
        {
            var part = new GameObject(filter.name);
            part.transform.SetParent(_ghost.transform, false);
            var local = basis * filter.transform.localToWorldMatrix;
            part.transform.localPosition = local.GetColumn(3);
            part.transform.localRotation = local.rotation;
            part.transform.localScale = local.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var renderer = part.AddComponent<MeshRenderer>();
            var mats = new Material[filter.sharedMesh.subMeshCount];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = ghostMaterial;
            renderer.sharedMaterials = mats;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    void Refresh()
    {
        bool empty = string.IsNullOrEmpty(_occupant);
        if (_trigger != null)
            _trigger.enabled = empty;
        if (_pad != null)
            _pad.SetActive(empty);
        if (!empty && _ghost != null)
            _ghost.SetActive(false);
    }
}
