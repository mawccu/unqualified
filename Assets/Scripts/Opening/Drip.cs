using UnityEngine;

/// <summary>
/// The ceiling leak: a drop forms, falls into the bucket and plinks, forever.
/// </summary>
public class Drip : MonoBehaviour
{
    [SerializeField] float fallHeight = 2.2f;
    [SerializeField] float interval = 2.6f;
    [SerializeField] Material dropMaterial;

    public void Configure(float height, Material material)
    {
        fallHeight = height;
        dropMaterial = material;
    }

    Transform _drop;
    AudioSource _audio;
    float _t;

    void Start()
    {
        _drop = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        Destroy(_drop.GetComponent<Collider>());
        _drop.SetParent(transform, false);
        _drop.localScale = new Vector3(0.025f, 0.035f, 0.025f);
        var r = _drop.GetComponent<MeshRenderer>();
        r.sharedMaterial = dropMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.clip = ProceduralAudio.Drip;
        _audio.spatialBlend = 1f;
        _audio.minDistance = 0.6f;
        _audio.maxDistance = 9f;
        _audio.volume = 0.5f;
        _t = Random.value * interval;
    }

    void Update()
    {
        _t += Time.deltaTime;
        float form = interval - 0.5f;
        if (_t < form)
        {
            // Swelling at the ceiling.
            _drop.localPosition = new Vector3(0f, fallHeight, 0f);
            _drop.localScale = Vector3.one * Mathf.Lerp(0.005f, 0.03f, _t / form);
            return;
        }
        float fall = _t - form;
        float y = fallHeight - 0.5f * 9.8f * fall * fall;
        if (y <= 0.25f)
        {
            _audio.pitch = Random.Range(0.9f, 1.1f);
            _audio.Play();
            _t = 0f;
            return;
        }
        _drop.localPosition = new Vector3(0f, y, 0f);
        _drop.localScale = new Vector3(0.025f, 0.04f, 0.025f);
    }
}
