using UnityEngine;

/// <summary>A desk fan: switch it on and the blades spin up and loose paper in front of it drifts away.</summary>
public class DeskFan : Interactable
{
    const float Reach = 3.2f;
    const float Cone = 35f;

    [SerializeField] Transform blades;
    bool _on;
    float _spin;
    AudioSource _hum;

    public void Configure(Transform bladeTransform) => blades = bladeTransform;

    void Awake()
    {
        _hum = gameObject.AddComponent<AudioSource>();
        _hum.clip = Tones.TestTone;
        _hum.loop = true;
        _hum.pitch = 0.09f;
        _hum.volume = 0f;
        _hum.spatialBlend = 1f;
        _hum.maxDistance = 6f;
    }

    public override string Prompt(IInteractionActor actor) => _on ? "Switch the fan off" : "Switch the fan on";
    public override InteractionResult CanUse(IInteractionActor actor) => InteractionResult.Ok;

    public override void Use(IInteractionActor actor)
    {
        _on = !_on;
        if (_on && !_hum.isPlaying)
            _hum.Play();
    }

    void Update()
    {
        _spin = Mathf.MoveTowards(_spin, _on ? 1f : 0f, Time.deltaTime * 0.8f);
        if (blades != null)
            blades.Rotate(0f, 0f, _spin * 1500f * Time.deltaTime, Space.Self);
        _hum.volume = _spin * 0.12f;
    }

    void FixedUpdate()
    {
        if (_spin < 0.3f)
            return;
        // Light things in the airflow get pushed.
        foreach (var hit in Physics.OverlapSphere(transform.position, Reach, 1 << GameLayers.Prop, QueryTriggerInteraction.Ignore))
        {
            var rb = hit.attachedRigidbody;
            if (rb == null || rb.isKinematic || rb.mass > 0.5f)
                continue;
            Vector3 to = rb.worldCenterOfMass - transform.position;
            if (Vector3.Angle(transform.forward, to) > Cone)
                continue;
            float falloff = 1f - to.magnitude / Reach;
            rb.AddForce(transform.forward * (0.35f * falloff * _spin) + Vector3.up * 0.05f * falloff, ForceMode.Force);
            rb.WakeUp();
        }
    }
}
