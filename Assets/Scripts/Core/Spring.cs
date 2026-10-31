using UnityEngine;

/// <summary>
/// A damped spring advanced by a frame's time in short steps. Taking a long frame
/// in one step is unstable once damping x dt passes about 2 (for the landing dip,
/// any frame over 0.09 s): at a low frame rate, or fast-forwarded, the value grew
/// every frame until the camera ended up 10^37 m in the air.
/// </summary>
public static class Spring
{
    /// The longest step taken at once (a 60 Hz frame).
    public const float MaxStep = 1f / 60f;

    public static void Step(ref float value, ref float velocity, float target, float stiffness, float damping, float dt)
    {
        for (float left = dt; left > 0f; left -= MaxStep)
        {
            float h = Mathf.Min(left, MaxStep);
            velocity += (stiffness * (target - value) - damping * velocity) * h;
            value += velocity * h;
        }
    }

    public static void Step(ref Vector2 value, ref Vector2 velocity, Vector2 target, float stiffness, float damping, float dt)
    {
        for (float left = dt; left > 0f; left -= MaxStep)
        {
            float h = Mathf.Min(left, MaxStep);
            velocity += (stiffness * (target - value) - damping * velocity) * h;
            value += velocity * h;
        }
    }
}
