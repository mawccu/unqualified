using UnityEngine;

/// <summary>Hand shapes. Each is a set of finger and thumb bends (Grips.Shape).</summary>
public enum Grip { Relaxed, Open, Pinch, Fist, Support, Keyboard, Wrap, Point, Push }

/// <summary>
/// Where hands go on things and how the fingers bend there. A hand's own frame:
/// +Z runs along the fingers, -Y is the palm side, and the thumb is on -X for
/// the right hand and +X for the left. A grip target is the WRIST pose that puts
/// the palm and fingers in the right place on the object.
/// </summary>
public static class Grips
{
    public struct Shape
    {
        /// Bend (degrees) at the knuckle and the middle joint for index, middle, ring, pinky.
        public Vector4 Base, Tip;
        /// 0 = thumb out to the side, 1 = across the palm.
        public float ThumbAcross;
        public float ThumbBend;
        public float Spread;
    }

    public static Shape Of(Grip grip) => grip switch
    {
        Grip.Open => new Shape { Base = new(3, 2, 3, 5), Tip = new(5, 5, 5, 6), ThumbAcross = 0f, ThumbBend = 2, Spread = 1.5f },
        // Paper held at its edge: fingers flat behind the sheet, the thumb across the front.
        Grip.Pinch => new Shape { Base = new(2, 0, 2, 6), Tip = new(4, 2, 4, 8), ThumbAcross = 0.8f, ThumbBend = 12, Spread = 0.2f },
        Grip.Fist => new Shape { Base = new(82, 85, 86, 86), Tip = new(88, 90, 90, 88), ThumbAcross = 0.75f, ThumbBend = 35, Spread = 0f },
        // Palm under a weight: fingers nearly flat, slightly apart.
        Grip.Support => new Shape { Base = new(10, 8, 10, 14), Tip = new(12, 10, 12, 16), ThumbAcross = 0.1f, ThumbBend = 6, Spread = 0.8f },
        Grip.Keyboard => new Shape { Base = new(30, 26, 30, 34), Tip = new(34, 32, 32, 34), ThumbAcross = 0.3f, ThumbBend = 10, Spread = 0.6f },
        // Around a box corner or a handle.
        Grip.Wrap => new Shape { Base = new(52, 58, 62, 66), Tip = new(52, 56, 60, 62), ThumbAcross = 0.55f, ThumbBend = 22, Spread = 0.1f },
        Grip.Point => new Shape { Base = new(2, 78, 84, 86), Tip = new(4, 84, 88, 88), ThumbAcross = 0.6f, ThumbBend = 28, Spread = 0f },
        // One finger pressing a key.
        Grip.Push => new Shape { Base = new(12, 70, 78, 80), Tip = new(14, 78, 84, 84), ThumbAcross = 0.5f, ThumbBend = 24, Spread = 0f },
        _ => new Shape { Base = new(20, 24, 28, 32), Tip = new(26, 30, 32, 35), ThumbAcross = 0.25f, ThumbBend = 14, Spread = 0.4f },
    };

    public static Shape Lerp(Shape a, Shape b, float t) => new()
    {
        Base = Vector4.Lerp(a.Base, b.Base, t),
        Tip = Vector4.Lerp(a.Tip, b.Tip, t),
        ThumbAcross = Mathf.Lerp(a.ThumbAcross, b.ThumbAcross, t),
        ThumbBend = Mathf.Lerp(a.ThumbBend, b.ThumbBend, t),
        Spread = Mathf.Lerp(a.Spread, b.Spread, t),
    };

    public struct Target
    {
        public bool Active;
        public Pose Wrist;
        public Grip Grip;
    }

    /// Where both hands go on a prop, given its category and current pose.
    public static void ForProp(PickupProp prop, out Target right, out Target left)
    {
        var t = prop.transform;
        var b = prop.LocalBounds;
        left = default;
        switch (prop.Category)
        {
            case PropCategory.Paper when b.size.y > b.size.z:
                right = PaperEdge(t, b, 1f);
                left = PaperEdge(t, b, -1f);
                break;
            case PropCategory.Paper:
                // Lying flat (an envelope): pinched from above at one end.
                right = Make(t, new Vector3(b.max.x - 0.03f, b.max.y + 0.018f, b.center.z - 0.075f), new Vector3(0f, -0.2f, 1f), Vector3.up, Grip.Pinch);
                break;
            case PropCategory.Wide:
                right = UnderSide(t, b, 1f);
                left = UnderSide(t, b, -1f);
                break;
            case PropCategory.Furniture:
                // Both hands over the top of the backrest (the side nearest the holder).
                right = Make(t, new Vector3(b.center.x + 0.13f, b.max.y + 0.025f, b.min.z - 0.07f), new Vector3(0f, -0.6f, 1f), Vector3.up, Grip.Wrap);
                left = Make(t, new Vector3(b.center.x - 0.13f, b.max.y + 0.025f, b.min.z - 0.07f), new Vector3(0f, -0.6f, 1f), Vector3.up, Grip.Wrap);
                break;
            default:
                // Hand on the right side of the box, fingers round its front.
                right = Make(t, new Vector3(b.max.x + 0.02f, b.center.y + 0.01f, b.center.z - 0.05f), new Vector3(0f, -0.12f, 1f), Vector3.right, Grip.Wrap);
                break;
        }
    }

    /// Holding a sheet at one side edge: fingers flat behind it pointing inward,
    /// palm toward the reader, thumb across the front. side +1 = right edge.
    public static Target PaperEdge(Transform sheet, Bounds b, float side)
    {
        float x = side > 0f ? b.max.x + 0.05f : b.min.x - 0.05f;
        var wrist = new Vector3(x, b.min.y + b.size.y * 0.42f, b.max.z + 0.034f);
        return Make(sheet, wrist, new Vector3(-side, 0.3f, 0f), Vector3.forward, Grip.Pinch);
    }

    /// A palm under one side of something wide, tipped up and in.
    static Target UnderSide(Transform t, Bounds b, float side)
    {
        float x = side > 0f ? b.max.x + 0.02f : b.min.x - 0.02f;
        var wrist = new Vector3(x, b.min.y + 0.045f, b.center.z - 0.06f);
        var palmNormal = new Vector3(-side * 0.65f, 0.75f, 0f);
        return Make(t, wrist, new Vector3(-side * 0.35f, 0.05f, 1f), -palmNormal, Grip.Support);
    }

    /// A wrist pose in the object's space: position, finger direction and the back-of-hand direction.
    static Target Make(Transform t, Vector3 localWrist, Vector3 localForward, Vector3 localUp, Grip grip) => new()
    {
        Active = true,
        Wrist = new Pose(t.TransformPoint(localWrist), t.rotation * Quaternion.LookRotation(localForward, localUp)),
        Grip = grip,
    };
}
