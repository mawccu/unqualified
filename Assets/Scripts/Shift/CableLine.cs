using UnityEngine;

/// <summary>
/// A drawn cable: while its plug sits in a port, a sagging line runs from the
/// port to the device it belongs to (the music player or a speaker).
/// </summary>
[RequireComponent(typeof(PickupProp))]
public class CableLine : MonoBehaviour
{
    [SerializeField] Transform device;
    [SerializeField] Material material;
    [SerializeField] Color colour = Color.red;
    LineRenderer _line;
    PickupProp _prop;

    public void Configure(Transform end, Material lineMaterial, Color lineColour)
    {
        device = end;
        material = lineMaterial;
        colour = lineColour;
    }

    void Awake()
    {
        _prop = GetComponent<PickupProp>();
        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 16;
        _line.widthMultiplier = 0.018f;
        _line.material = material;
        _line.startColor = _line.endColor = colour;
        _line.numCapVertices = 2;
        _line.enabled = false;
    }

    void LateUpdate()
    {
        bool plugged = !string.IsNullOrEmpty(_prop.SocketId) || !string.IsNullOrEmpty(_prop.HolderId);
        _line.enabled = plugged && device != null;
        if (!_line.enabled)
            return;
        Vector3 a = transform.position + Vector3.up * 0.03f;
        Vector3 b = device.position;
        float sag = Mathf.Min(0.6f, Vector3.Distance(a, b) * 0.25f);
        for (int i = 0; i < 16; i++)
        {
            float t = i / 15f;
            Vector3 p = Vector3.Lerp(a, b, t);
            p.y -= Mathf.Sin(t * Mathf.PI) * sag;
            p.y = Mathf.Max(p.y, 0.02f);
            _line.SetPosition(i, p);
        }
    }
}
