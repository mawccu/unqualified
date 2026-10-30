using UnityEngine;

/// <summary>
/// A drawer that slides open and shut. It stops short if a body is in the way
/// rather than shoving anyone, and carries what is inside it.
/// </summary>
public class Drawer : Interactable
{
    const float Travel = 0.32f;

    [SerializeField] Transform drawer;
    Vector3 _closed;
    bool _open;
    float _t;
    Rigidbody _body;

    public void Configure(Transform slider) => drawer = slider;

    void Awake()
    {
        _closed = drawer.localPosition;
        _body = drawer.GetComponent<Rigidbody>();
        if (_body == null)
            _body = drawer.gameObject.AddComponent<Rigidbody>();
        _body.isKinematic = true;
    }

    public override string Prompt(IInteractionActor actor) => _open ? "Close the drawer" : "Open the drawer";
    public override InteractionResult CanUse(IInteractionActor actor) => InteractionResult.Ok;
    public override void Use(IInteractionActor actor) => _open = !_open;

    void FixedUpdate()
    {
        float target = _open ? 1f : 0f;
        float next = Mathf.MoveTowards(_t, target, Time.fixedDeltaTime * 3f);
        Vector3 local = _closed + Vector3.forward * (next * Travel);
        Vector3 world = drawer.parent.TransformPoint(local);
        // Never slide into a person.
        if (next > _t && Physics.CheckBox(world + drawer.parent.forward * 0.2f, new Vector3(0.2f, 0.08f, 0.05f), drawer.rotation, 1 << GameLayers.Player, QueryTriggerInteraction.Ignore))
            return;
        _t = next;
        _body.MovePosition(world);
    }
}
