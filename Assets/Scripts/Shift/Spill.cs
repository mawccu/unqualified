using UnityEngine;

/// <summary>
/// A mess on the floor (frosting, coffee, confetti): evidence until someone
/// cleans it with a mop or a cloth by holding E over it. Cleaning shows progress
/// and the mess fades as it goes.
/// </summary>
public class Spill : Interactable
{
    public const float CleanSeconds = 2.2f;

    public string What { get; private set; } = "a mess";
    public bool Clean { get; private set; }

    Vector3 _scale;

    public static Spill Create(GameObject prefab, Vector3 floorPoint, string what, string id)
    {
        var go = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = id;
        go.transform.position = floorPoint + Vector3.up * 0.005f;
        go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Destroy(c);
        // A thin trigger so the crosshair can find it and nobody trips on it.
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.8f, 0.08f, 0.8f);
        box.center = new Vector3(0f, 0.04f, 0f);
        var spill = go.AddComponent<Spill>();
        spill.What = what;
        spill.SetEntityId(id);
        GameSession.Instance?.Service.RegisterInteractable(spill);
        return spill;
    }

    void Awake() => _scale = transform.localScale;

    public override float HoldSeconds => CleanSeconds;

    public override string Prompt(IInteractionActor actor) => $"Clean up {What}";

    public override InteractionResult CanUse(IInteractionActor actor)
    {
        if (Clean)
            return InteractionResult.NoTarget;
        var held = GameSession.Instance.Service.GetProp(actor.HeldPropId);
        return held != null && (held.Kind == "mop" || held.Kind == "cloth") ? InteractionResult.Ok : InteractionResult.NotNow;
    }

    public override string BlockedReason(IInteractionActor actor) => Clean ? "" : $"{Capital(What)}. A mop or a cloth would sort it.";

    /// How far along a clean-up is (0..1), for the look while someone works on it.
    public void ShowProgress(float t)
    {
        transform.localScale = Vector3.Lerp(_scale, _scale * 0.35f, t);
        // The sound of scrubbing while someone works on it.
        _scrub ??= MakeScrub();
        if (t > 0.01f && !_scrub.isPlaying)
            _scrub.Play();
        else if (t <= 0.01f && _scrub.isPlaying)
            _scrub.Stop();
    }

    AudioSource _scrub;

    AudioSource MakeScrub()
    {
        var go = new GameObject("Scrub");
        go.transform.position = transform.position;
        var source = go.AddComponent<AudioSource>();
        source.clip = ProceduralAudio.Swish;
        source.loop = true;
        source.spatialBlend = 1f;
        source.maxDistance = 10f;
        source.volume = 0.8f;
        return source;
    }

    public override void Use(IInteractionActor actor)
    {
        Clean = true;
        if (_scrub != null)
            Destroy(_scrub.gameObject);
        gameObject.SetActive(false);
    }

    static string Capital(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
}
