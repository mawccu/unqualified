using UnityEngine;

/// <summary>A desk bell. Ding. Anyone nearby looks over; ringing it a lot gets you told off.</summary>
public class Bell : Interactable
{
    AudioSource _audio;
    float _last = -9f;
    int _rings;

    void Awake()
    {
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 1f;
        _audio.maxDistance = 25f;
        _audio.playOnAwake = false;
    }

    public override string Prompt(IInteractionActor actor) => "Ring the bell";
    public override InteractionResult CanUse(IInteractionActor actor) => Time.time - _last > 0.35f ? InteractionResult.Ok : InteractionResult.Busy;

    public override void Use(IInteractionActor actor)
    {
        _last = Time.time;
        _rings++;
        _audio.pitch = Random.Range(0.97f, 1.03f);
        _audio.PlayOneShot(Tones.Ding);
        ShiftEvents.Noise(transform.position, 16f, "a bell ringing");
        if (actor is PlayerActor p && p == GameSession.Instance.Player)
            p.Arms.SetOverride(FirstPersonArms.Side.Right, () => new Pose(transform.position + Vector3.up * 0.12f - p.transform.forward * 0.07f,
                Quaternion.LookRotation(p.transform.forward - Vector3.up, Vector3.up)), Grip.Push, 0.12f);
        if (actor is PlayerActor q)
            StartCoroutine(Release(q));
        if (_rings == 6)
            GameSession.Instance?.Hud.ShowSubtitle("Someone", "Who keeps ringing that?", 2f);
    }

    System.Collections.IEnumerator Release(PlayerActor p)
    {
        yield return new WaitForSeconds(0.28f);
        p.Arms.ClearOverride(FirstPersonArms.Side.Right, 0.25f);
    }
}
