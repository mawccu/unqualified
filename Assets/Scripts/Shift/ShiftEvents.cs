using UnityEngine;

/// <summary>
/// One place for things in the world to say "something happened here" without
/// knowing who listens: noises bring the coordinator over, messes become
/// evidence. Quiet when there is no coordinator (the apartment).
/// </summary>
public static class ShiftEvents
{
    public static CoordinatorMind Mind;
    public static GameObject SpillPrefab;
    static int _spills;

    /// A sound with a hearing radius. It tells her where, not who or what.
    public static void Noise(Vector3 position, float radius, string what)
    {
        if (Mind != null)
            Mind.Suspicion.Noise(position, radius);
    }

    /// A mess on the floor under a point: cleanable, and evidence while it is there.
    public static Spill Mess(Vector3 near, string what, float severity, bool major)
    {
        Vector3 floor = near;
        if (Physics.Raycast(near + Vector3.up * 0.3f, Vector3.down, out var hit, 3f, 1 << GameLayers.World, QueryTriggerInteraction.Ignore))
            floor = hit.point;
        var spill = Spill.Create(SpillPrefab, floor, what, $"spill_{++_spills}");
        if (Mind != null)
            Mind.Suspicion.Report(spill.EntityId, what, floor, severity, major, () => spill != null && !spill.Clean);
        return spill;
    }
}
