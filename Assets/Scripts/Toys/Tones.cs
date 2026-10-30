using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sounds and music made in code, so nothing recorded or downloaded is needed:
/// a bell, a pop, a feedback squeal, a test tone, and four short original tunes
/// for the radio and the ceremony (all composed here, note by note).
/// </summary>
public static class Tones
{
    const int Rate = 44100;
    static readonly Dictionary<string, AudioClip> Cache = new();

    public static AudioClip Ding => Get("ding", 1.2f, t =>
        (Mathf.Sin(2f * Mathf.PI * 1568f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 3136f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 2349f * t)) * Mathf.Exp(-t * 3.5f) * 0.3f);

    public static AudioClip Pop => Get("pop", 0.5f, t =>
        ((Hash(t) * 2f - 1f) * Mathf.Exp(-t * 30f) * 0.8f + Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-t * 18f) * 0.5f));

    public static AudioClip Feedback => Get("feedback", 2.5f, t =>
        Mathf.Sin(2f * Mathf.PI * (2600f + 140f * Mathf.Sin(t * 7f)) * t) * Mathf.Clamp01(t * 2f) * 0.35f);

    public static AudioClip TestTone => Get("testtone", 1.5f, t => Mathf.Sin(2f * Mathf.PI * 1000f * t) * 0.25f);

    public static AudioClip Splash => Get("splash", 0.6f, t => (Hash(t) * 2f - 1f) * Mathf.Exp(-t * 7f) * 0.4f);

    /// Original tunes: a stately processional, a lounge loop, a polka and a racket.
    public static AudioClip Tune(string name) => name switch
    {
        "Processional" => Melody("processional", 72, new[] { 0, 5, 5, 5, 0, 7, 4, 5, 0, 5, 9, 9, 7, 5, 4, 5 }, 0.62f, Organ),
        "Lounge" => Melody("lounge", 64, new[] { 0, 4, 7, 11, 9, 7, 4, 2, 0, 4, 7, 9, 7, 4, 2, -1 }, 0.42f, Soft),
        "Polka" => Melody("polka", 67, new[] { 0, 4, 7, 4, 0, 4, 7, 12, 11, 7, 5, 2, 0, 2, 4, 0 }, 0.22f, Reedy),
        _ => Melody("racket", 52, new[] { 0, 0, 3, 0, 5, 3, 0, -2, 0, 0, 3, 0, 6, 5, 3, 0 }, 0.18f, Distorted),
    };

    public static readonly string[] Tracks = { "Processional", "Lounge", "Polka", "Racket" };

    delegate float Voice(float freq, float t, float noteT);

    static float Organ(float f, float t, float n) => (Mathf.Sin(2f * Mathf.PI * f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * f * 2f * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * f * 3f * t)) * 0.2f;
    static float Soft(float f, float t, float n) => Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-n * 2.5f) * 0.35f;
    static float Reedy(float f, float t, float n) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t)) * Mathf.Exp(-n * 6f) * 0.12f;
    static float Distorted(float f, float t, float n) => Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * f * t) * 4f, -1f, 1f) * 0.14f * Mathf.Exp(-n * 3f);

    static AudioClip Melody(string key, int rootMidi, int[] steps, float noteSeconds, Voice voice)
    {
        if (Cache.TryGetValue(key, out var cached))
            return cached;
        int perNote = Mathf.RoundToInt(noteSeconds * Rate);
        var data = new float[perNote * steps.Length];
        for (int i = 0; i < steps.Length; i++)
        {
            float freq = 440f * Mathf.Pow(2f, (rootMidi + steps[i] - 69) / 12f);
            float bass = freq / 2f;
            for (int k = 0; k < perNote; k++)
            {
                float t = (i * perNote + k) / (float)Rate;
                float n = k / (float)Rate;
                float fade = Mathf.Clamp01(n * 60f) * Mathf.Clamp01((noteSeconds - n) * 30f);
                data[i * perNote + k] = (voice(freq, t, n) + voice(bass, t, n) * 0.5f) * fade;
            }
        }
        var clip = AudioClip.Create(key, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        Cache[key] = clip;
        return clip;
    }

    static AudioClip Get(string key, float seconds, System.Func<float, float> wave)
    {
        if (Cache.TryGetValue(key, out var clip))
            return clip;
        int count = Mathf.CeilToInt(seconds * Rate);
        var data = new float[count];
        for (int i = 0; i < count; i++)
            data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
        clip = AudioClip.Create(key, count, 1, Rate, false);
        clip.SetData(data, 0);
        Cache[key] = clip;
        return clip;
    }

    static float Hash(float t)
    {
        float x = Mathf.Sin(t * 12.9898f * 1000f) * 43758.5453f;
        return x - Mathf.Floor(x);
    }
}
