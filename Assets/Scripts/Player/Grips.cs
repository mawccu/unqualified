using System.Collections.Generic;
using UnityEngine;

/// <summary>Hand shapes. Each is a set of finger and thumb bends (Grips.Shape).</summary>
public enum Grip { Relaxed, Open, Pinch, Fist, Support, Keyboard, Wrap, Point, Push, Tray, FistThin, FistThick, Sheet, Cover }

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
        /// Extra thumb bend toward the palm side at its base, and extra yaw at its base
        /// toward the fingers (degrees; negative turns it away): with a palm under an
        /// edge, these lift the thumb up and over it.
        public float ThumbLift, ThumbTurn;
    }

    public static Shape Of(Grip grip) => grip switch
    {
        Grip.Open => new Shape { Base = new(3, 2, 3, 5), Tip = new(5, 5, 5, 6), ThumbAcross = 0f, ThumbBend = 2, Spread = 1.5f },
        // A slip lying flat, pinched from above at one end.
        Grip.Pinch => new Shape { Base = new(2, 0, 2, 6), Tip = new(4, 2, 4, 8), ThumbAcross = 0.8f, ThumbBend = 12, Spread = 0.2f },
        // A sheet held up at its side edge: palm and fingers flat against the back,
        // the thumb round the edge onto the front. (Before, the palm was beside the
        // sheet and the fingers 22 to 30 mm behind it.) Measured with
        // Tools/Eval/grip_probe.cs: palm and fingertips 0 to 3 mm from the back,
        // thumb tip 2 mm from the front, 31 mm in from the edge.
        // Under a thick book (a binder): a thumb cannot reach over a 6 cm edge from
        // below, so each presses up the book's side instead (2 mm off it, 7 mm under
        // the cover), where it shows beside the book.
        Grip.Cover => new Shape { Base = new(12, 10, 12, 14), Tip = new(10, 8, 10, 12), ThumbAcross = 0f, ThumbBend = 0, Spread = 0.6f, ThumbLift = 85f, ThumbTurn = -40f },
        Grip.Sheet => new Shape { Base = new(12, 10, 12, 14), Tip = new(10, 8, 10, 12), ThumbAcross = 0f, ThumbBend = 90, Spread = 0.3f, ThumbLift = 70f, ThumbTurn = -100f },
        Grip.Fist => new Shape { Base = new(82, 85, 86, 86), Tip = new(88, 90, 90, 88), ThumbAcross = 0.75f, ThumbBend = 35, Spread = 0f },
        // Round a thin handle (a spatula) the fingertips meet it sooner, so they bend
        // less; round a thick one (a party popper's body) they bend further to come
        // back onto its surface instead of sinking in. Measured with
        // Tools/Eval/grip_probe.cs (closing more on the spatula swung the tips past it).
        Grip.FistThin => new Shape { Base = new(76, 79, 80, 80), Tip = new(80, 83, 83, 81), ThumbAcross = 0.75f, ThumbBend = 30, Spread = 0f },
        Grip.FistThick => new Shape { Base = new(88, 91, 92, 92), Tip = new(95, 97, 97, 94), ThumbAcross = 0.45f, ThumbBend = 20, Spread = 0f },
        // Palm up under something tall (a cake layer): fingers bent up just enough to
        // meet its underside, the thumb kept down and out of the cake.
        Grip.Support => new Shape { Base = new(12, 10, 12, 14), Tip = new(10, 8, 10, 12), ThumbAcross = 0f, ThumbBend = 0, Spread = 0.6f, ThumbLift = -20f, ThumbTurn = 40f },
        // The same under a tray's edge, with the thumb up the tray's side and over its
        // rim (where you can see it). Values measured against the tray with
        // Tools/Eval/grip_probe.cs: palm and fingertips 0 to 3 mm from the underside,
        // thumb tip 25 mm in from the edge and 4 mm above the rim.
        Grip.Tray => new Shape { Base = new(12, 10, 12, 14), Tip = new(10, 8, 10, 12), ThumbAcross = 0f, ThumbBend = 50, Spread = 0.6f, ThumbLift = 70f, ThumbTurn = -100f },
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
        ThumbLift = Mathf.Lerp(a.ThumbLift, b.ThumbLift, t),
        ThumbTurn = Mathf.Lerp(a.ThumbTurn, b.ThumbTurn, t),
    };

    public struct Target
    {
        public bool Active;
        public Pose Wrist;
        public Grip Grip;
    }

    /// The ways things are held. Each has its own hand placement, finger shape and
    /// carry pose (PlayerActor.HoldOffset); Docs/GRIPS.md lists them.
    public enum Profile { Paper, FlatPaper, Book, Small, OnPalm, Tool, Wide, Furniture }

    /// A tool's handle: its long axis in the tool's own space (0 x, 1 y, 2 z), where
    /// the fist closes along it (0 = the min end, 1 = the max end), the handle's
    /// radius there, and which end the thumb points to. Measured from the models.
    public readonly struct Handle
    {
        public readonly int Axis;
        public readonly float At, Radius;
        public readonly bool ThumbToMax;
        /// The handle runs along the thing's near edge (a coil's strand), not its middle.
        public readonly bool NearEdge;
        public Handle(int axis, float at, float radius, bool thumbToMax, bool nearEdge = false)
        {
            Axis = axis; At = at; Radius = radius; ThumbToMax = thumbToMax; NearEdge = nearEdge;
        }
    }

    /// Mop: the top of the shaft (the head is the wide bottom). Screwdriver: it
    /// stands on its tip, so the handle is the upper half. Spatula: the thick end
    /// (min z), blade forward. Party popper: round its body, string below.
    /// Table sign: its stem (0.04 to 0.22 m up, about 15 mm thick).
    static readonly Dictionary<string, Handle> Handles = new()
    {
        ["mop"] = new Handle(1, 0.8f, 0.018f, true),
        ["screwdriver"] = new Handle(1, 0.7f, 0.0125f, true),
        ["spatula"] = new Handle(2, 0.18f, 0.009f, true),
        ["party_popper"] = new Handle(1, 0.42f, 0.025f, true),
        // A table number: by its stem, between the foot and the card.
        ["table_sign"] = new Handle(1, 0.33f, 0.015f, true),
        // A coiled cable is a ring (nothing under its middle): held overhand by its
        // near strand, 35 mm thick.
        ["cable"] = new Handle(0, 0.62f, 0.0175f, false, true),
    };

    public static bool TryHandle(PickupProp prop, out Handle handle) => Handles.TryGetValue(prop.Kind, out handle);

    public static Profile ProfileOf(PickupProp prop)
    {
        switch (prop.Category)
        {
            case PropCategory.Paper: return prop.LocalBounds.size.y > prop.LocalBounds.size.z ? Profile.Paper : Profile.FlatPaper;
            case PropCategory.Wide: return Profile.Wide;
            case PropCategory.Furniture: return Profile.Furniture;
        }
        if (prop.Kind.StartsWith("binder_") || prop.Kind == "guest_book")
            return Profile.Book;
        if (Handles.ContainsKey(prop.Kind))
            return Profile.Tool;
        // A mug is held round its body; a box, a folded cloth or a coil sits on the
        // palm (a hand on a box's side curled its fingers 3 to 4 cm into it).
        return prop.Kind == "mug" ? Profile.Small : Profile.OnPalm;
    }

    /// How a book is turned while held so its cover lies on the palms facing up:
    /// a binder stands on its edge with its covers to the sides, so it is laid down
    /// with its labelled spine toward you (you can see which one you have); a guest
    /// book already lies flat.
    public static Quaternion Lay(PickupProp prop) =>
        prop.Kind.StartsWith("binder_") ? Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;

    /// How far a held book leans back toward the reader (degrees) and how far out
    /// it is held: an open guest book well back to read; a closed binder (its page
    /// is on screen) flatter and further out, so it does not fill the view.
    public static float BookTilt(PickupProp prop) => prop.Kind.StartsWith("binder_") ? 15f : 38f;
    public static float BookReach(PickupProp prop) => prop.Kind.StartsWith("binder_") ? 0.5f : 0.36f;

    /// Where both hands go on a prop, given its profile and current pose.
    public static void ForProp(PickupProp prop, out Target right, out Target left)
    {
        var t = prop.transform;
        var b = prop.LocalBounds;
        right = left = default;
        switch (ProfileOf(prop))
        {
            case Profile.Paper:
                right = PaperEdge(t, b, 1f);
                left = PaperEdge(t, b, -1f);
                break;
            case Profile.FlatPaper:
                // Lying flat (an envelope, an order slip): pinched from above at one end.
                right = Make(t, FlatPaperWrist(b), new Vector3(0f, -0.2f, 1f), Vector3.up, Grip.Pinch);
                break;
            case Profile.Wide:
                right = UnderSide(t.position, t.rotation, b, 1f);
                left = UnderSide(t.position, t.rotation, b, -1f);
                break;
            case Profile.Book:
            {
                // Only while in the hands: once let go it turns back upright into its
                // slot, and the hands let go of it.
                if (string.IsNullOrEmpty(prop.HolderId))
                    break;
                // In the book's laid-down frame it is a flat thing held from below.
                var lay = Lay(prop);
                var rotation = t.rotation * Quaternion.Inverse(lay);
                var laid = Rotate(b, lay);
                right = UnderSide(t.position, rotation, laid, 1f, true);
                left = UnderSide(t.position, rotation, laid, -1f, true);
                break;
            }
            case Profile.Furniture:
                // Both palms on top of the backrest (the side nearest the holder), the
                // fingers curled over its far side. (They were 7 cm behind it and
                // 2.5 cm above, so even a hand that reached it hovered.)
                right = Make(t, new Vector3(b.center.x + 0.13f, b.max.y + PalmHalfThickness, b.min.z - 0.015f), new Vector3(0f, -0.45f, 1f), Vector3.up, Grip.Wrap);
                left = Make(t, new Vector3(b.center.x - 0.13f, b.max.y + PalmHalfThickness, b.min.z - 0.015f), new Vector3(0f, -0.45f, 1f), Vector3.up, Grip.Wrap);
                break;
            case Profile.Tool:
            {
                TryHandle(prop, out var handle);
                FistFrame(handle, out var forward, out var up);
                var fist = handle.Radius < 0.012f ? Grip.FistThin : handle.Radius > 0.02f ? Grip.FistThick : Grip.Fist;
                right = Make(t, FistWrist(b, handle), forward, up, fist);
                break;
            }
            case Profile.OnPalm:
                // Resting on the right palm, fingers forward under it.
                right = Make(t, OnPalmWrist(b), Vector3.forward, Vector3.down, Grip.Support);
                break;
            default:
                // Hand round the right side of a mug, fingers round its front.
                right = Make(t, SmallWrist(b), new Vector3(0f, -0.12f, 1f), Vector3.right, Grip.Wrap);
                break;
        }
    }

    static Vector3 SmallWrist(Bounds b) => new(b.max.x + 0.02f, b.center.y + 0.01f, b.center.z - 0.05f);
    /// The palm's middle (about 3 cm along from the wrist) under the thing's middle.
    static Vector3 OnPalmWrist(Bounds b) => new(b.center.x, b.min.y - PalmHalfThickness - 0.001f, b.center.z - 0.03f);
    static Vector3 FlatPaperWrist(Bounds b) => new(b.max.x - 0.03f, b.max.y + 0.018f, b.center.z - 0.075f);

    /// For a one-handed hold, where the right wrist is in the prop's own space (so
    /// the carry can be placed with that hand in view); null for two hands.
    public static Vector3? RightWristLocal(PickupProp prop)
    {
        var b = prop.LocalBounds;
        switch (ProfileOf(prop))
        {
            case Profile.Small: return SmallWrist(b);
            case Profile.OnPalm: return OnPalmWrist(b);
            case Profile.FlatPaper: return FlatPaperWrist(b);
            case Profile.Tool:
                TryHandle(prop, out var handle);
                return FistWrist(b, handle);
        }
        return null;
    }

    /// Holding a sheet at one side edge, low enough that the thumbs leave its text
    /// clear: palm and fingers flat against the back pointing inward, the thumb
    /// round the edge onto the front. side +1 = right edge.
    public static Target PaperEdge(Transform sheet, Bounds b, float side)
    {
        float x = side > 0f ? b.max.x + WristOutside : b.min.x - WristOutside;
        var wrist = new Vector3(x, b.min.y + b.size.y * 0.3f, b.max.z + PalmHalfThickness + 0.001f);
        return Make(sheet, wrist, new Vector3(-side, 0.3f, 0f), Vector3.forward, Grip.Sheet);
    }

    /// The palm's half-thickness (the palm model is 46 mm thick, wrist at its middle).
    public const float PalmHalfThickness = 0.023f;
    /// How far the wrist stays outside an edge it holds from below.
    public const float WristOutside = 0.015f;
    /// Anything flatter than this is held like a tray (thumbs over the rim); a
    /// thicker thing (a binder, a cake) keeps the thumbs below, out of it.
    public const float FlatHeight = 0.045f;
    /// How far along the fingers a closed fist's hole is from the wrist.
    public const float FistDepth = 0.062f;
    /// How far toward the palm side the hole's middle is, beyond the handle's radius.
    public const float FistInset = PalmHalfThickness;

    /// One hand under one side of something wide: the wrist just outside the edge,
    /// palm up against the underside, fingers pointing inward (and a little forward)
    /// under it. The palm's surface touches the bottom; nothing floats beside it.
    static Target UnderSide(Vector3 position, Quaternion rotation, Bounds b, float side, bool book = false)
    {
        float x = side > 0f ? b.max.x + WristOutside : b.min.x - WristOutside;
        var wrist = new Vector3(x, b.min.y - PalmHalfThickness - 0.001f, b.center.z + 0.02f);
        var grip = b.size.y <= FlatHeight ? Grip.Tray : book ? Grip.Cover : Grip.Support;
        return Make(position, rotation, wrist, new Vector3(-side, 0f, 0.3f), Vector3.down, grip);
    }

    public static Vector3 HandleCentre(Bounds b, Handle handle)
    {
        var at = b.center;
        at[handle.Axis] = Mathf.Lerp(b.min[handle.Axis], b.max[handle.Axis], handle.At);
        if (handle.NearEdge)
            at.z = b.min.z + handle.Radius;
        return at;
    }

    /// A right fist round a handle: knuckles to the right (or up, over a handle
    /// that runs across), the handle through the fist across the fingers, the thumb
    /// toward the handle's top end.
    static void FistFrame(Handle handle, out Vector3 forward, out Vector3 up)
    {
        var axis = Vector3.zero;
        axis[handle.Axis] = handle.ThumbToMax ? 1f : -1f;
        // The hand's local X runs across the fingers (right hand: thumb on -X),
        // so -X is the thumb's way along the handle.
        var x = -axis;
        up = handle.Axis == 0 ? Vector3.up : Vector3.right;
        forward = Vector3.Cross(x, up);
    }

    /// The wrist that puts the handle through the middle of the closed fist.
    static Vector3 FistWrist(Bounds b, Handle handle)
    {
        FistFrame(handle, out var forward, out var up);
        return HandleCentre(b, handle) + up * (FistInset + handle.Radius) - forward * FistDepth;
    }

    /// The box around a box turned by a rotation (exact for quarter turns).
    static Bounds Rotate(Bounds b, Quaternion rotation)
    {
        var result = new Bounds(rotation * b.center, Vector3.zero);
        for (int i = 0; i < 8; i++)
            result.Encapsulate(rotation * new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
        return result;
    }

    /// A wrist pose in the object's space: position, finger direction and the back-of-hand direction.
    static Target Make(Transform t, Vector3 localWrist, Vector3 localForward, Vector3 localUp, Grip grip) =>
        Make(t.position, t.rotation, localWrist, localForward, localUp, grip);

    static Target Make(Vector3 position, Quaternion rotation, Vector3 localWrist, Vector3 localForward, Vector3 localUp, Grip grip) => new()
    {
        Active = true,
        Wrist = new Pose(position + rotation * localWrist, rotation * Quaternion.LookRotation(localForward, localUp)),
        Grip = grip,
    };
}
