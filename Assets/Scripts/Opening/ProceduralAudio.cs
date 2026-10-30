using UnityEngine;

/// <summary>
/// Small sounds synthesised at runtime, so the game has knocks, drips, steps,
/// handling noises and room ambience without any recorded or downloaded audio.
/// </summary>
public static class ProceduralAudio
{
    const int Rate = 44100;
    static AudioClip _knock, _drip, _footstep, _pickup, _set, _key, _clunk, _creak, _latch, _swish, _roomTone, _crowd;

    /// Hands closing on something: a short soft rustle.
    public static AudioClip Pickup => _pickup ??= Make("pickup", 0.12f, t =>
        (Random.value * 2f - 1f) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.12f)) * 0.22f);

    /// Setting something down with care: a low soft thud and a tiny tap.
    public static AudioClip SetDown => _set ??= Make("set_down", 0.2f, t =>
        Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-t * 35f) * 0.45f
        + (Random.value * 2f - 1f) * Mathf.Exp(-t * 120f) * 0.15f);

    /// One key on a keyboard.
    public static AudioClip Key => _key ??= Make("key", 0.05f, t =>
        (Random.value * 2f - 1f) * Mathf.Exp(-t * 160f) * 0.35f + Mathf.Sin(2f * Mathf.PI * 2400f * t) * Mathf.Exp(-t * 200f) * 0.1f);

    /// A phone handset lifted from, or dropped into, its cradle.
    public static AudioClip Clunk => _clunk ??= Make("clunk", 0.15f, t =>
        (Mathf.Sin(2f * Mathf.PI * 310f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 520f * t)) * Mathf.Exp(-t * 40f) * 0.3f
        + (Random.value * 2f - 1f) * Mathf.Exp(-t * 150f) * 0.25f);

    /// A door swinging on dry hinges.
    public static AudioClip Creak => _creak ??= Make("creak", 0.9f, t =>
    {
        float f = 190f + 70f * Mathf.Sin(t * 5f) + 40f * t;
        float grain = Mathf.Sin(2f * Mathf.PI * 33f * t) > 0.2f ? 1f : 0.35f;
        return Mathf.Sin(2f * Mathf.PI * f * t) * grain * Mathf.Sin(Mathf.PI * t / 0.9f) * 0.12f;
    });

    /// A door latch clicking shut.
    public static AudioClip Latch => _latch ??= Make("latch", 0.25f, t => Thump(t) * 0.6f + (Random.value * 2f - 1f) * Mathf.Exp(-t * 220f) * 0.4f);

    /// A wet mop or cloth pushed across the floor (loops while cleaning).
    public static AudioClip Swish => _swish ??= Make("swish", 0.8f, t =>
        (Random.value * 2f - 1f) * (0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Mathf.PI * t / 0.4f))) * 0.16f);

    /// An office at night: ventilation hum and a fluorescent buzz (loops).
    public static AudioClip RoomTone => _roomTone ??= Make("room_tone", 4f, t =>
    {
        float hum = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.05f + Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.025f;
        float air = (Random.value * 2f - 1f) * 0.035f;
        return hum + air;
    });

    /// A room full of people talking, too far off to make out (loops).
    public static AudioClip Crowd => _crowd ??= Make("crowd", 6f, t =>
    {
        float sum = 0f;
        for (int v = 0; v < 7; v++)
        {
            float rate = 2.2f + v * 0.53f;
            float syllables = Mathf.Max(0f, Mathf.Sin(t * rate * Mathf.PI + v * 1.7f + Mathf.Sin(t * 0.7f + v) * 2f));
            float pitch = 140f + v * 23f + 15f * Mathf.Sin(t * (0.9f + v * 0.2f));
            sum += Mathf.Sin(2f * Mathf.PI * pitch * t + Mathf.Sin(2f * Mathf.PI * pitch * 2.01f * t)) * syllables;
        }
        // Soften: people in a big room, not speakers in your ear.
        return sum * 0.025f + (Random.value * 2f - 1f) * 0.012f;
    });

    /// A looping 2D background sound on this object.
    public static AudioSource Ambience(GameObject on, AudioClip clip, float volume)
    {
        var source = on.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.playOnAwake = false;
        source.Play();
        return source;
    }

    /// A soft shoe on a hard floor: a dull heel thump and a short scuff.
    public static AudioClip Footstep => _footstep ??= Make("footstep", 0.18f, t =>
        Mathf.Sin(2f * Mathf.PI * 75f * t) * Mathf.Exp(-t * 45f) * 0.5f
        + (Random.value * 2f - 1f) * Mathf.Exp(-t * 70f) * 0.18f);

    /// Two heavy knuckle thumps on a wooden door, a quarter second apart.
    public static AudioClip Knock => _knock ??= Make("knock", 0.6f, t => Thump(t) + Thump(t - 0.24f) * 1.1f);

    /// A single water drop into a bucket.
    public static AudioClip Drip => _drip ??= Make("drip", 0.35f, t =>
    {
        float f = Mathf.Lerp(1400f, 650f, Mathf.Clamp01(t / 0.08f));
        return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 28f) * 0.35f;
    });

    static float Thump(float t)
    {
        if (t < 0f)
            return 0f;
        // A low body resonance plus a short knuckle click.
        float body = (Mathf.Sin(2f * Mathf.PI * 92f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 141f * t)) * Mathf.Exp(-t * 22f);
        float click = (Random.value * 2f - 1f) * Mathf.Exp(-t * 180f) * 0.5f;
        return (body + click) * 0.55f;
    }

    static AudioClip Make(string name, float seconds, System.Func<float, float> wave)
    {
        int count = Mathf.CeilToInt(seconds * Rate);
        var data = new float[count];
        Random.State saved = Random.state;
        Random.InitState(7);
        for (int i = 0; i < count; i++)
            data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
        Random.state = saved;
        var clip = AudioClip.Create(name, count, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
