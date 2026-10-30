using System.Collections.Generic;
using UnityEngine;

/// <summary>What sort of thing a prop is, for how it weighs, falls, sounds and is held.</summary>
public enum PropCategory
{
    /// A sheet or envelope: light, drifts and flutters, pinched at an edge.
    Paper,
    /// Something small held in one hand (a box, a sign).
    Small,
    /// Something wide and heavy carried with both hands underneath (a cake layer, a tray).
    Wide,
    /// Furniture that is dragged along the floor by its back (a chair). Stays upright.
    Furniture,
}

/// <summary>
/// Per-category physical behaviour: mass, damping, friction, bounce, how low the
/// centre of mass sits, and the sound a hit makes. Tune here, not per prop.
/// </summary>
public static class PropPhysics
{
    public struct Profile
    {
        public float Mass, LinearDamping, AngularDamping, Friction, Bounce, CentreOfMassDrop;
        public float ImpactPitch;
    }

    public static Profile For(PropCategory category) => category switch
    {
        PropCategory.Paper => new Profile { Mass = 0.05f, LinearDamping = 2.6f, AngularDamping = 1.6f, Friction = 0.75f, Bounce = 0f, ImpactPitch = 0f },
        PropCategory.Wide => new Profile { Mass = 3f, LinearDamping = 0.05f, AngularDamping = 0.6f, Friction = 0.85f, Bounce = 0f, CentreOfMassDrop = 0.35f, ImpactPitch = 0.55f },
        PropCategory.Furniture => new Profile { Mass = 7f, LinearDamping = 2f, AngularDamping = 4f, Friction = 0.6f, Bounce = 0f, CentreOfMassDrop = 0.5f, ImpactPitch = 0.75f },
        _ => new Profile { Mass = 0.4f, LinearDamping = 0.05f, AngularDamping = 0.25f, Friction = 0.55f, Bounce = 0.08f, CentreOfMassDrop = 0.15f, ImpactPitch = 1f },
    };

    static readonly Dictionary<PropCategory, PhysicsMaterial> Materials = new();

    public static void Apply(Rigidbody body, Collider[] colliders, PropCategory category)
    {
        var p = For(category);
        body.mass = p.Mass;
        body.linearDamping = p.LinearDamping;
        body.angularDamping = p.AngularDamping;
        if (!Materials.TryGetValue(category, out var material))
        {
            material = new PhysicsMaterial(category.ToString())
            {
                staticFriction = p.Friction,
                dynamicFriction = p.Friction * 0.85f,
                bounciness = p.Bounce,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
            Materials[category] = material;
        }
        var bounds = new Bounds();
        bool first = true;
        foreach (var c in colliders)
        {
            c.sharedMaterial = material;
            if (c is BoxCollider box)
            {
                var b = new Bounds(box.center, box.size);
                if (first) { bounds = b; first = false; }
                else bounds.Encapsulate(b);
            }
        }
        if (!first && p.CentreOfMassDrop > 0f)
        {
            body.centerOfMass = bounds.center - new Vector3(0f, bounds.extents.y * p.CentreOfMassDrop, 0f);
        }
    }

    // Impact sounds, synthesised once per category.
    static readonly Dictionary<PropCategory, AudioClip> Impacts = new();

    public static AudioClip ImpactClip(PropCategory category)
    {
        if (Impacts.TryGetValue(category, out var clip))
            return clip;
        const int rate = 44100;
        float seconds = category == PropCategory.Paper ? 0.22f : 0.3f;
        var data = new float[(int)(seconds * rate)];
        var random = new System.Random(11 + (int)category);
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)rate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            data[i] = category switch
            {
                // A soft scrape: filtered noise that swells and fades.
                PropCategory.Paper => noise * 0.18f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / seconds)),
                // A dull wooden or cardboard thud.
                PropCategory.Wide => (Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.8f + noise * 0.15f) * Mathf.Exp(-t * 20f),
                // A small tap with a little ring.
                _ => (Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.35f + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.4f + noise * 0.3f) * Mathf.Exp(-t * 34f),
            };
        }
        if (category == PropCategory.Paper)
        {
            // Smooth the noise so it rustles rather than hisses.
            for (int i = 1; i < data.Length; i++)
                data[i] = data[i - 1] * 0.7f + data[i] * 0.3f;
        }
        clip = AudioClip.Create($"impact_{category}", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        Impacts[category] = clip;
        return clip;
    }
}
