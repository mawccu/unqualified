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
    [Tooltip("0 snaps to the centre from anywhere in reach; above 0 the held thing must be lined up within this many metres, and lands where it is.")]
    [SerializeField] float alignTolerance;
    [Tooltip("Sits on top of whatever occupies this socket (cake tiers).")]
    [SerializeField] PlacementSocket stackOn;

    public float AlignTolerance => alignTolerance;
    public PlacementSocket StackOn => stackOn;
    /// The prop sitting here (set together with OccupantId by InteractionService).
    public PickupProp OccupantProp { get; private set; }

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
            if (string.IsNullOrEmpty(_occupant))
                OccupantProp = null;
            Refresh();
        }
    }

    BoxCollider _trigger;
    GameObject _pad;
    GameObject _ghost;
    PickupProp _ghostSource;

    public void ConfigureAlignment(float tolerance, PlacementSocket below)
    {
        alignTolerance = tolerance;
        stackOn = below;
    }

    public void SetOccupant(PickupProp prop)
    {
        OccupantProp = prop;
        OccupantId = prop != null ? prop.EntityId : "";
    }

    /// Within this of the centre a lined-up spot counts as perfect and the thing
    /// settles into the middle (a few centimetres: careful, not superhuman).
    public float PerfectRadius => Mathf.Max(0.035f, alignTolerance * 0.22f);

    /// Where a prop lands if placed now: lined-up spots keep the prop's own
    /// horizontal offset (within the tolerance), except close to the centre,
    /// where it settles into place; others take the centre.
    public Vector3 LandingFor(PickupProp prop, out float quality)
    {
        quality = 1f;
        if (alignTolerance <= 0f)
            return SnapPosition;
        Vector3 offset = prop.Centre - transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        float perfect = PerfectRadius;
        if (distance <= perfect)
            return SnapPosition;
        quality = Mathf.Clamp01(1f - (distance - perfect) / (alignTolerance - perfect));
        return SnapPosition + Vector3.ClampMagnitude(offset, alignTolerance);
    }

    public bool IsLinedUp(PickupProp prop)
    {
        if (alignTolerance <= 0f)
            return true;
        Vector3 offset = prop.Centre - transform.position;
        offset.y = 0f;
        return offset.magnitude <= alignTolerance;
    }

    void LateUpdate()
    {
        // Stack on what is below: follow its actual top, wherever it was placed.
        if (stackOn != null && stackOn.OccupantProp != null)
        {
            var below = stackOn.OccupantProp;
            float top = below.LocalBounds.max.y * below.transform.lossyScale.y;
            Vector3 p = below.transform.position + below.transform.up * top;
            transform.position = p;
        }
        if (_ghost != null && _ghost.activeSelf && _ghostSource != null)
        {
            _ghost.transform.position = LandingFor(_ghostSource, out float quality) - transform.position + transform.position;
            _ghost.transform.position = LandingFor(_ghostSource, out quality);
            var colour = !IsLinedUp(_ghostSource) ? new Color(1f, 0.3f, 0.25f, 0.4f)
                : quality >= 1f ? new Color(0.55f, 0.95f, 1f, 0.55f)
                : quality > 0.7f ? new Color(0.4f, 1f, 0.6f, 0.4f)
                : quality > 0.4f ? new Color(1f, 0.85f, 0.3f, 0.4f) : new Color(1f, 0.55f, 0.25f, 0.4f);
            _ghostBlock ??= new MaterialPropertyBlock();
            _ghostBlock.SetColor("_BaseColor", colour);
            foreach (var r in _ghost.GetComponentsInChildren<Renderer>())
                r.SetPropertyBlock(_ghostBlock);
        }
    }

    MaterialPropertyBlock _ghostBlock;

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
