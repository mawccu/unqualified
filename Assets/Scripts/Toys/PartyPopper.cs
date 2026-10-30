using UnityEngine;

/// <summary>
/// A party popper: pull the string for a bang and a burst of confetti. Everyone
/// nearby turns to look (a distraction), and the confetti is a mess to clean.
/// One use; then it is just rubbish.
/// </summary>
public class PartyPopper : HeldTool
{
    bool _used;

    public override string UsePrompt => "Pull the string";
    public override bool CanUseHeld => !_used;

    public override void UseHeld(PlayerActor user)
    {
        if (_used)
            return;
        _used = true;
        Vector3 at = transform.position + Vector3.up * 0.1f;
        AudioSource.PlayClipAtPoint(Tones.Pop, at, 1f);
        Confetti(at, user.Camera.transform.forward);
        ShiftEvents.Noise(at, 20f, "a bang");
        // Where it lands: a little in front.
        ShiftEvents.Mess(user.transform.position + user.transform.forward * 1.4f, "confetti all over the floor", 6f, false);
        var prop = GetComponent<PickupProp>();
        if (prop != null)
            prop.Configure(prop.EntityId, "popper_used", "a spent party popper", prop.Category);
    }

    static void Confetti(Vector3 at, Vector3 direction)
    {
        var go = new GameObject("Confetti");
        go.transform.position = at;
        go.transform.rotation = Quaternion.LookRotation(direction + Vector3.up * 0.6f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = 2.2f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
        main.gravityModifier = 0.35f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.35f, 0.5f), new Color(0.35f, 0.75f, 0.95f));
        main.startRotation3D = true;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 160) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        ps.Play();
        Destroy(go, 3f);
    }
}
