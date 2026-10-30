using UnityEngine;

/// <summary>A kitchen radio: off, then three stations of original music, then off again.</summary>
public class Radio : Interactable
{
    static readonly string[] Stations = { "Lounge", "Polka", "Racket" };
    AudioSource _audio;
    int _station = -1;

    void Awake()
    {
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 1f;
        _audio.minDistance = 1.5f;
        _audio.maxDistance = 14f;
        _audio.loop = true;
        _audio.volume = 0.55f;
        _audio.playOnAwake = false;
    }

    public override string Prompt(IInteractionActor actor) =>
        _station + 1 < Stations.Length ? $"Radio: {Stations[_station + 1].ToLowerInvariant()}" : "Turn the radio off";

    public override InteractionResult CanUse(IInteractionActor actor) => InteractionResult.Ok;

    public override void Use(IInteractionActor actor)
    {
        _station++;
        if (_station >= Stations.Length)
        {
            _station = -1;
            _audio.Stop();
            return;
        }
        _audio.clip = Tones.Tune(Stations[_station]);
        _audio.Play();
        if (Stations[_station] == "Racket")
            ShiftEvents.Noise(transform.position, 14f, "loud music in the kitchen");
    }
}
