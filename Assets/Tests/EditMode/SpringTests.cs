using NUnit.Framework;
using UnityEngine;

/// The camera's landing dip and a carried thing's tilt settle at any frame rate:
/// taken in one step, a long frame made them grow until the camera left the world.
public class SpringTests
{
    [Test]
    public void TheLandingDipSettlesEvenAtTheLongestFrame()
    {
        foreach (float dt in new[] { 1f / 60f, 0.1f, 0.2f, 1f / 3f })
        {
            float dip = 0f, velocity = -0.6f;
            for (int i = 0; i < 600; i++)
            {
                Spring.Step(ref dip, ref velocity, 0f, 120f, 22f, dt);
                Assert.Less(Mathf.Abs(dip), 0.1f, $"bounded at {dt} s frames");
            }
            Assert.Less(Mathf.Abs(dip), 1e-4f, $"settled at {dt} s frames");
        }
    }

    [Test]
    public void ATiltSettlesOnItsTargetAtAnyFrameRate()
    {
        foreach (float dt in new[] { 1f / 60f, 0.25f, 1f / 3f })
        {
            Vector2 tilt = Vector2.zero, velocity = new(3f, -3f);
            for (int i = 0; i < 600; i++)
                Spring.Step(ref tilt, ref velocity, new Vector2(0.1f, -0.05f), 110f, 7f, dt);
            Assert.Less((tilt - new Vector2(0.1f, -0.05f)).magnitude, 1e-3f, $"settled at {dt} s frames");
        }
    }
}
