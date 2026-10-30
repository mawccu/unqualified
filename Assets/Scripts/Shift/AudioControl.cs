using UnityEngine;

/// <summary>One control on the sound desk: the gain knob, the track button or play/stop.</summary>
public class AudioControl : Interactable
{
    public enum Kind { Gain, Track, Play }

    [SerializeField] AudioDesk desk;
    [SerializeField] Kind kind;

    public void Configure(AudioDesk owner, Kind control)
    {
        desk = owner;
        kind = control;
    }

    public override string Prompt(IInteractionActor actor)
    {
        string action = kind switch
        {
            Kind.Gain => "Turn the gain",
            Kind.Track => "Next track",
            _ => desk.Playing ? "Stop" : $"Play \"{desk.TrackName}\"",
        };
        return $"{action}\n{desk.Readout()}";
    }

    public override InteractionResult CanUse(IInteractionActor actor) => InteractionResult.Ok;

    public override void Use(IInteractionActor actor)
    {
        switch (kind)
        {
            case Kind.Gain: desk.TurnGain(); break;
            case Kind.Track: desk.NextTrack(); break;
            default: desk.TogglePlay(); break;
        }
    }
}
