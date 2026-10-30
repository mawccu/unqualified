using UnityEngine;

/// <summary>
/// The sound desk: a small system you can understand by looking at it. Two
/// labelled cables (MUSIC, round plug; SPEAKERS, square plug) go into three
/// labelled ports (INPUT round, MAIN OUT square, MONITOR triangle). Gain has a
/// safe range. Wrong routing gives silence in the hall or a feedback squeal;
/// too much gain distorts. Test it before the guests arrive, then play the right
/// track on the coordinator's cue.
/// </summary>
public class AudioDesk : MonoBehaviour
{
    public enum Routing { Unplugged, Correct, DeskOnly, Feedback }

    public const string PortIn = "port_in", PortMain = "port_main", PortMonitor = "port_mon";
    public const string CableMusic = "cable_music", CableSpeakers = "cable_speakers";
    public static readonly string[] Tracks = { "Test tone", "Processional", "Lounge", "Polka" };

    [SerializeField] AudioSource[] hallSpeakers;
    [SerializeField] AudioSource deskMonitor;

    public int Gain { get; private set; } = 1;
    public int Track { get; private set; }
    public bool Playing { get; private set; }
    public bool Tested { get; private set; }
    public float Level { get; private set; }

    public string TrackName => Tracks[Track];
    public bool GainSafe => Gain >= 2 && Gain <= 3;

    /// The ceremony cue: open while the coordinator is waiting for the music.
    public bool CueOpen { get; set; }
    public string CueResult { get; private set; }

    public void Configure(AudioSource[] speakers, AudioSource monitor)
    {
        hallSpeakers = speakers;
        deskMonitor = monitor;
    }

    InteractionService Service => GameSession.Instance.Service;

    public Routing Route()
    {
        var input = Service.GetSocket(PortIn)?.OccupantId ?? "";
        var main = Service.GetSocket(PortMain)?.OccupantId ?? "";
        var monitor = Service.GetSocket(PortMonitor)?.OccupantId ?? "";
        if (input == CableMusic && main == CableSpeakers)
            return Routing.Correct;
        if (input == CableMusic && monitor == CableSpeakers)
            return Routing.DeskOnly;
        // Music fed back into an output, or the speaker line in the input: a howl.
        if (main == CableMusic || monitor == CableMusic || input == CableSpeakers)
            return Routing.Feedback;
        return Routing.Unplugged;
    }

    /// The hotel's sound person sets it up when nobody on the crew claimed to know audio.
    public void SetUpByStaff()
    {
        PlugIn(CableMusic, PortIn);
        PlugIn(CableSpeakers, PortMain);
        Gain = 3;
        Tested = true;
    }

    void PlugIn(string cable, string port)
    {
        var prop = Service.GetProp(cable);
        var socket = Service.GetSocket(port);
        if (prop == null || socket == null || !string.IsNullOrEmpty(socket.OccupantId))
            return;
        socket.SetOccupant(prop);
        prop.PlaceAt(socket.SnapPosition, socket.transform.rotation, port);
    }

    public void TurnGain()
    {
        Gain = Gain % 5 + 1;
        Apply();
    }

    public void NextTrack()
    {
        Track = (Track + 1) % Tracks.Length;
        if (Playing)
            Begin(Track);
    }

    public void TogglePlay()
    {
        if (Playing)
            Stop();
        else
            Begin(Track);
    }

    void Begin(int track)
    {
        Playing = true;
        var route = Route();
        AudioClip clip = Tracks[track] == "Test tone" ? Tones.TestTone : Tones.Tune(Tracks[track]);
        foreach (var s in hallSpeakers)
            s.Stop();
        deskMonitor.Stop();
        switch (route)
        {
            case Routing.Correct:
                foreach (var s in hallSpeakers)
                {
                    s.clip = clip;
                    s.loop = true;
                    s.Play();
                }
                if (Tracks[track] == "Test tone")
                    Tested = true;
                break;
            case Routing.DeskOnly:
                deskMonitor.clip = clip;
                deskMonitor.loop = true;
                deskMonitor.Play();
                break;
            case Routing.Feedback:
                foreach (var s in hallSpeakers)
                {
                    s.clip = Tones.Feedback;
                    s.loop = true;
                    s.Play();
                }
                ShiftEvents.Noise(hallSpeakers[0].transform.position, 40f, "a feedback squeal");
                ShiftEvents.Mind?.Suspicion.Report("audio_feedback", "a howling feedback squeal from the speakers", hallSpeakers[0].transform.position, 10f, false, () => Playing && Route() == Routing.Feedback);
                break;
            case Routing.Unplugged:
                Playing = false;
                break;
        }
        Apply();
        if (CueOpen && Playing)
            JudgeCue(route);
    }

    void Stop()
    {
        Playing = false;
        foreach (var s in hallSpeakers)
            s.Stop();
        deskMonitor.Stop();
    }

    void Apply()
    {
        float volume = Gain / 5f;
        foreach (var s in hallSpeakers)
        {
            s.volume = volume;
            // Over the safe range it clips: harsher, and people notice.
            s.pitch = Gain >= 5 ? 1.04f : 1f;
        }
        if (Playing && Gain >= 4 && Route() == Routing.Correct)
        {
            ShiftEvents.Noise(hallSpeakers[0].transform.position, 40f, "music far too loud");
            ShiftEvents.Mind?.Suspicion.Report("audio_clipping", "the music blaring and distorting", hallSpeakers[0].transform.position, 6f, false, () => Playing && Gain >= 4);
        }
    }

    void JudgeCue(Routing route)
    {
        CueOpen = false;
        if (route != Routing.Correct)
            CueResult = route == Routing.Feedback ? "feedback" : "silence";
        else if (TrackName != "Processional")
            CueResult = "wrong track";
        else if (!GainSafe)
            CueResult = Gain < 2 ? "too quiet" : "too loud";
        else
            CueResult = "perfect";
    }

    /// The cue window closed with nothing played.
    public void MissCue()
    {
        if (CueOpen)
        {
            CueOpen = false;
            CueResult = "missed";
        }
    }

    public void ResetCue() => CueResult = null;

    void Update()
    {
        float target = Playing ? (Route() == Routing.Correct ? Gain / 5f : Route() == Routing.Feedback ? 1f : 0.3f) : 0f;
        Level = Mathf.MoveTowards(Level, target * (0.85f + 0.15f * Mathf.Sin(Time.time * 13f)), Time.deltaTime * 4f);
    }

    /// What the desk shows: routing, a level meter and the gain.
    public string Readout()
    {
        int bars = Mathf.RoundToInt(Level * 5f);
        string meter = new string('|', bars) + new string('.', 5 - bars);
        string route = Route() switch
        {
            Routing.Correct => "hall speakers",
            Routing.DeskOnly => "desk only (speakers in MONITOR)",
            Routing.Feedback => "FEEDBACK (cable in the wrong port)",
            _ => "nothing connected",
        };
        string gain = Gain >= 4 ? $"<color=#FF7A5C>{Gain} CLIPPING</color>" : Gain < 2 ? $"{Gain} (quiet)" : $"{Gain} (safe)";
        return $"<size=13>[{meter}]  to {route}   gain {gain}   track: {TrackName}</size>";
    }
}
