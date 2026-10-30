using UnityEngine;

/// <summary>
/// Small sounds synthesised at runtime, so the opening has a knock and a drip
/// without any recorded or downloaded audio.
/// </summary>
public static class ProceduralAudio
{
    const int Rate = 44100;
    static AudioClip _knock, _drip, _footstep;

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
