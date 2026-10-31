using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Hands stay on what they carry. Measured every frame on the rendered hand
/// models against the carried thing's box: the palm and fingertips touch the
/// tray's underside and its thumbs rest on the rim, through standing, walking,
/// turning, looking up and down, crouching and jumping; the same palms stay
/// under a cake layer. (Before: palms 29 mm off the tray, fingertips 15 to 55 mm,
/// thumbs 92 mm, and the tray was below the bottom of the view.)
/// </summary>
public class GripTests
{
    const float Touch = 0.006f;

    [UnityTearDown]
    public IEnumerator Restore()
    {
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("AfterGripTest_" + Time.frameCount));
        yield return SceneManager.UnloadSceneAsync(scene);
    }

    static IEnumerator Load(string scene)
    {
        CrewState.StartOver();
        var crew = CrewState.Current;
        crew.billReceived = true;
        crew.hired = true;
        crew.job = Jobs.Wedding;
        PlayerPrefs.SetInt("intro_seen", 1);
        SceneManager.LoadScene(scene);
        yield return null;
        yield return null;
    }

    static int _request = 7100;

    /// Stands next to the prop (the first clear spot around it from which it can be
    /// picked up: things sit on counters and desks) and picks it up.
    static IEnumerator PickUp(PlayerActor player, string id)
    {
        var session = GameSession.Instance;
        var prop = session.Service.GetProp(id);
        for (int k = 0; k < 24 && player.HeldProp != prop; k++)
        {
            float angle = k * 45f + (k % 16 >= 8 ? 22.5f : 0f);
            var stand = prop.Centre + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (k < 8 ? 0.7f : k < 16 ? 0.9f : 1.1f);
            stand.y = 0.05f;
            if (Physics.CheckCapsule(stand + Vector3.up * 0.35f, stand + Vector3.up * 1.5f, 0.28f, 1 << GameLayers.World, QueryTriggerInteraction.Ignore))
                continue;
            player.Teleport(stand);
            yield return null;
            var request = new InteractionRequest { RequestId = ++_request, ActorId = player.ActorId, Action = InteractionAction.Pickup, TargetId = id, ExpectedRevision = prop.Revision };
            if (session.Service.Validate(request) != InteractionResult.Ok)
                continue;
            session.Submit(request);
            for (int i = 0; i < 30 && player.HeldProp != prop; i++)
                yield return null;
        }
        Assert.AreEqual(prop, player.HeldProp, "picked up " + id);
        // Let the reach blend in.
        yield return new WaitForSeconds(0.6f);
    }

    /// Signed distance from a point to the prop's box (positive outside).
    static float Gap(PickupProp prop, Vector3 world)
    {
        var b = prop.LocalBounds;
        var l = prop.transform.InverseTransformPoint(world);
        var outside = Vector3.Max(b.min - l, l - b.max);
        var o = Vector3.Max(outside, Vector3.zero);
        return o.sqrMagnitude > 0f ? o.magnitude : Mathf.Max(outside.x, Mathf.Max(outside.y, outside.z));
    }

    /// The palm-side surface of a hand part (palm: its middle; finger or thumb: its end).
    static Vector3 Surface(Transform part, bool palm)
    {
        var mf = part.GetComponentInChildren<MeshFilter>();
        var mb = mf.sharedMesh.bounds;
        var local = palm ? new Vector3(mb.center.x, mb.min.y, mb.center.z) : new Vector3(mb.center.x, mb.min.y * 0.5f, mb.max.z);
        return mf.transform.TransformPoint(local);
    }

    /// Every way the hands could come off the prop this frame (empty when they are on it).
    static List<string> Faults(PlayerActor player, PickupProp prop, bool thumbsOnRim)
    {
        var faults = new List<string>();
        var arms = player.Arms.transform;
        var b = prop.LocalBounds;
        foreach (var n in new[] { "R", "L" })
        {
            var hand = arms.Find("Hand" + n);
            float palm = Gap(prop, Surface(hand, true));
            if (Mathf.Abs(palm) > Touch)
                faults.Add($"{n} palm {palm * 1000f:0}mm");
            for (int i = 0; i < 4; i++)
            {
                float tip = Gap(prop, Surface(hand.Find($"Finger{i}{n}/Finger{i}Tip{n}"), false));
                if (Mathf.Abs(tip) > Touch)
                    faults.Add($"{n} finger {i} {tip * 1000f:0}mm");
            }
            if (thumbsOnRim)
            {
                var t = prop.transform.InverseTransformPoint(Surface(hand.Find($"Thumb{n}/ThumbTip{n}"), false));
                float inside = n == "R" ? b.max.x - t.x : t.x - b.min.x;
                float above = t.y - b.max.y;
                if (inside < 0.005f || above < -0.003f || above > 0.015f)
                    faults.Add($"{n} thumb {inside * 1000f:0}mm in, {above * 1000f:0}mm above the rim");
            }
        }
        return faults;
    }

    static IEnumerator Watch(PlayerActor player, PickupProp prop, bool thumbsOnRim, string doing, float seconds, List<string> log, System.Action<float> each = null)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            each?.Invoke(t);
            yield return new WaitForEndOfFrame();
            if (player.HeldProp != prop)
            {
                log.Add($"{doing} at {t:0.00}s: no longer held (wobble {prop.Wobble:0.00})");
                yield break;
            }
            foreach (var f in Faults(player, prop, thumbsOnRim))
                log.Add($"{doing} at {t:0.00}s: {f}");
        }
    }

    [UnityTest]
    public IEnumerator HandsStayOnTheTrayThroughMovement()
    {
        yield return Load("Wedding");
        var player = GameSession.Instance.Player;
        yield return PickUp(player, "tray");
        var tray = player.HeldProp;
        var log = new List<string>();
        // Out in the middle of the hall, clear of tables and walls.
        player.Teleport(new Vector3(0f, 0.05f, -1f));
        player.SetLook(0f, 5f);
        yield return new WaitForSeconds(0.4f);
        float yaw = 0f;

        yield return Watch(player, tray, true, "standing", 0.5f, log);
        player.ScriptedMove = new Vector2(0f, 1f);
        yield return Watch(player, tray, true, "walking", 1.2f, log);
        player.ScriptedMove = new Vector2(1f, 0.3f);
        yield return Watch(player, tray, true, "walking and turning", 1.2f, log, t => player.SetLook(yaw + t * 120f, 5f));
        player.ScriptedMove = null;
        yield return Watch(player, tray, true, "looking up and down", 1.6f, log, t => player.SetLook(yaw, Mathf.Sin(t * 4f) * 55f));
        player.SetLook(yaw, 5f);
        player.ScriptedCrouch = true;
        yield return Watch(player, tray, true, "crouching", 0.8f, log);
        player.ScriptedCrouch = false;
        yield return Watch(player, tray, true, "standing up", 0.6f, log);
        player.PressJump();
        yield return Watch(player, tray, true, "jumping", 1.0f, log);

        Assert.AreEqual(tray, player.HeldProp, "still carrying the tray: " + string.Join("; ", log.FindAll(l => l.Contains("no longer held"))));
        Assert.IsEmpty(log, "hands off the tray:\n" + string.Join("\n", log.GetRange(0, Mathf.Min(20, log.Count))));
    }

    [UnityTest]
    public IEnumerator PalmsStayUnderACakeLayer()
    {
        yield return Load("Wedding");
        var player = GameSession.Instance.Player;
        yield return PickUp(player, "cake_base_spare");
        var cake = player.HeldProp;
        var log = new List<string>();
        float yaw = player.transform.eulerAngles.y;
        yield return Watch(player, cake, false, "standing", 0.5f, log);
        player.ScriptedMove = new Vector2(0f, 0.6f);
        yield return Watch(player, cake, false, "walking", 1.0f, log, t => player.SetLook(yaw + t * 60f, Mathf.Sin(t * 3f) * 30f));
        player.ScriptedMove = null;
        Assert.IsEmpty(log, "hands off the cake:\n" + string.Join("\n", log.GetRange(0, Mathf.Min(20, log.Count))));
    }

    /// Whatever the field of view, a carried tray and the thumbs on its rim are in the frame.
    [UnityTest]
    public IEnumerator TheTrayIsInView()
    {
        yield return Load("Wedding");
        var player = GameSession.Instance.Player;
        yield return PickUp(player, "tray");
        float before = GameSettings.HorizontalFov;
        foreach (float fov in new[] { 70f, 80f, 100f })
        {
            GameSettings.HorizontalFov = fov;
            player.SetLook(player.transform.eulerAngles.y, 0f);
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForEndOfFrame();
            var cam = player.Camera;
            foreach (var n in new[] { "R", "L" })
            {
                var thumb = player.Arms.transform.Find($"Hand{n}/Thumb{n}/ThumbTip{n}");
                var v = cam.WorldToViewportPoint(thumb.position);
                Assert.That(v.z > 0f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f, $"{n} thumb on screen at fov {fov}: {v}");
            }
        }
        GameSettings.HorizontalFov = before;
    }

    // Every grip profile (Grips.Profile) ------------------------------------------------

    static IEnumerator LoadAs(string scene, string job)
    {
        yield return Load(scene);
        CrewState.Current.job = job;
    }

    /// Signed distance to the prop's box in the frame its grip is built in (a held
    /// book is laid down, cover up).
    static float LaidGap(PickupProp prop, Vector3 world)
    {
        var lay = Grips.ProfileOf(prop) == Grips.Profile.Book ? Grips.Lay(prop) : Quaternion.identity;
        var b = prop.LocalBounds;
        var laid = new Bounds(lay * b.center, Vector3.zero);
        for (int i = 0; i < 8; i++)
            laid.Encapsulate(lay * new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
        var l = lay * prop.transform.InverseTransformPoint(world);
        var outside = Vector3.Max(laid.min - l, l - laid.max);
        var o = Vector3.Max(outside, Vector3.zero);
        return o.sqrMagnitude > 0f ? o.magnitude : Mathf.Max(outside.x, Mathf.Max(outside.y, outside.z));
    }

    /// Distance from a point to a tool's handle surface (its axis, less its radius).
    static float HandleGap(PickupProp prop, Vector3 world)
    {
        Grips.TryHandle(prop, out var handle);
        var axis = Vector3.zero;
        axis[handle.Axis] = 1f;
        var l = prop.transform.InverseTransformPoint(world) - Grips.HandleCentre(prop.LocalBounds, handle);
        return (l - axis * Vector3.Dot(l, axis)).magnitude - handle.Radius;
    }

    /// The hands' contact on whatever is held, by its profile; empty when every part
    /// that should touch does (within Touch).
    static List<string> ProfileFaults(PlayerActor player)
    {
        var prop = player.HeldProp;
        var faults = new List<string>();
        var profile = Grips.ProfileOf(prop);
        bool twoHands = profile == Grips.Profile.Paper || profile == Grips.Profile.Book || profile == Grips.Profile.Wide || profile == Grips.Profile.Furniture;
        foreach (var n in twoHands ? new[] { "R", "L" } : new[] { "R" })
        {
            var hand = player.Arms.transform.Find("Hand" + n);
            var palmMesh = hand.GetComponentInChildren<MeshFilter>();
            var pb = palmMesh.sharedMesh.bounds;
            var tips = new List<Vector3>();
            for (int i = 0; i < 4; i++)
                tips.Add(Surface(hand.Find($"Finger{i}{n}/Finger{i}Tip{n}"), false));
            var thumb = Surface(hand.Find($"Thumb{n}/ThumbTip{n}"), false);
            switch (profile)
            {
                case Grips.Profile.Tool:
                {
                    // A fist: the handle against the front of the palm, the fingertips round it.
                    var palmFront = palmMesh.transform.TransformPoint(new Vector3(pb.center.x, pb.min.y, pb.max.z - 0.012f));
                    if (Mathf.Abs(HandleGap(prop, palmFront)) > Touch) faults.Add($"{n} palm {HandleGap(prop, palmFront) * 1000f:0}mm off the handle");
                    foreach (var tip in tips)
                        if (Mathf.Abs(HandleGap(prop, tip)) > Touch) faults.Add($"{n} fingertip {HandleGap(prop, tip) * 1000f:0}mm off the handle");
                    break;
                }
                case Grips.Profile.Furniture:
                    if (Mathf.Abs(Gap(prop, Surface(hand, true))) > Touch) faults.Add($"{n} palm {Gap(prop, Surface(hand, true)) * 1000f:0}mm off the backrest");
                    break;
                case Grips.Profile.Small:
                    if (Mathf.Abs(Gap(prop, Surface(hand, true))) > Touch) faults.Add($"{n} palm {Gap(prop, Surface(hand, true)) * 1000f:0}mm off the side");
                    break;
                case Grips.Profile.OnPalm:
                    if (Mathf.Abs(Gap(prop, Surface(hand, true))) > Touch) faults.Add($"{n} palm {Gap(prop, Surface(hand, true)) * 1000f:0}mm off the bottom");
                    break;
                default:
                {
                    // Flat against a face: the palm and every fingertip on it, the thumb
                    // on the other face or up the side, never inside.
                    float palm = LaidGap(prop, Surface(hand, true));
                    if (Mathf.Abs(palm) > Touch) faults.Add($"{n} palm {palm * 1000f:0}mm");
                    for (int i = 0; i < tips.Count; i++)
                    {
                        float tip = LaidGap(prop, tips[i]);
                        if (Mathf.Abs(tip) > Touch) faults.Add($"{n} fingertip {i} {tip * 1000f:0}mm");
                    }
                    // The thumb is measured at half its thickness (Surface), so up to
                    // 8 mm there is about 2 mm of skin from the surface.
                    float t = LaidGap(prop, thumb);
                    if (t < -0.003f || t > 0.008f) faults.Add($"{n} thumb {t * 1000f:0}mm");
                    break;
                }
            }
        }
        return faults;
    }

    static IEnumerator HoldAndCheck(string scene, string job, string id, Grips.Profile expected, params float[] pitches)
    {
        yield return LoadAs(scene, job);
        var player = GameSession.Instance.Player;
        yield return PickUp(player, id);
        Assert.AreEqual(expected, Grips.ProfileOf(player.HeldProp), id + "'s grip profile");
        float yaw = player.transform.eulerAngles.y;
        var log = new List<string>();
        foreach (float pitch in pitches.Length > 0 ? pitches : new[] { 5f })
        {
            player.SetLook(yaw, pitch);
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForEndOfFrame();
            foreach (var f in ProfileFaults(player))
                log.Add($"looking {pitch:0}: {f}");
        }
        Assert.IsEmpty(log, $"hands off {id}:\n" + string.Join("\n", log));
    }

    [UnityTest] public IEnumerator SheetHeldAtItsEdges() => HoldAndCheck("Apartment", Jobs.Wedding, "rent_bill", Grips.Profile.Paper, -20f, 5f, 40f);
    [UnityTest] public IEnumerator BinderLiesOnBothPalms() => HoldAndCheck("CallCenter", Jobs.CallCenter, "binder_1", Grips.Profile.Book, -20f, 5f, 40f);
    [UnityTest] public IEnumerator GuestBookLiesOnBothPalms() => HoldAndCheck("Wedding", Jobs.Wedding, "guest_book", Grips.Profile.Book, 5f, 40f);
    [UnityTest] public IEnumerator MopInAFist() => HoldAndCheck("Wedding", Jobs.Wedding, "mop", Grips.Profile.Tool, -30f, 5f, 50f);
    [UnityTest] public IEnumerator SignHeldByItsStem() => HoldAndCheck("Wedding", Jobs.Wedding, "sign_1", Grips.Profile.Tool, 5f);
    [UnityTest] public IEnumerator CoiledCableByItsNearStrand() => HoldAndCheck("Wedding", Jobs.Wedding, "cable_music", Grips.Profile.Tool, 5f);
    [UnityTest] public IEnumerator ClothOnThePalm() => HoldAndCheck("Wedding", Jobs.Wedding, "cloth", Grips.Profile.OnPalm, -30f, 5f, 50f);
    [UnityTest] public IEnumerator MugRoundItsSide() => HoldAndCheck("Wedding", Jobs.Wedding, "mug_2", Grips.Profile.Small, 5f, 40f);
    /// Dragging a chair while looking down at it: the shoulders no longer swing back
    /// with the head, so the palms still reach its backrest.
    [UnityTest] public IEnumerator ChairBackrestInReachLookingDown() => HoldAndCheck("Wedding", Jobs.Wedding, "chair_0_0", Grips.Profile.Furniture, 0f, 30f, 60f);

    /// Whatever the field of view, a one-handed carry shows the hand holding it.
    [UnityTest]
    public IEnumerator OneHandedCarriesAreInView()
    {
        yield return Load("Wedding");
        var player = GameSession.Instance.Player;
        float before = GameSettings.HorizontalFov;
        var misses = new List<string>();
        foreach (var id in new[] { "mug_2", "mop", "cloth", "spatula" })
        {
            yield return PickUp(player, id);
            // Out on the open floor (beside a counter the carry is rightly pulled in).
            player.Teleport(new Vector3(0f, 0.05f, 2f));
            foreach (float fov in new[] { 70f, 80f, 100f })
            {
                GameSettings.HorizontalFov = fov;
                player.SetLook(player.transform.eulerAngles.y, 0f);
                yield return new WaitForSeconds(0.5f);
                yield return new WaitForEndOfFrame();
                var v = player.Camera.WorldToViewportPoint(player.Arms.transform.Find("HandR").position);
                if (!(v.z > 0f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f))
                    misses.Add($"{id} at fov {fov}: hand at {v}");
            }
            GameSession.Instance.Submit(new InteractionRequest { RequestId = ++_request, ActorId = player.ActorId, Action = InteractionAction.Drop, TargetId = "" });
            yield return new WaitForSeconds(0.3f);
        }
        GameSettings.HorizontalFov = before;
        Assert.IsEmpty(misses, "hands out of view:\n" + string.Join("\n", misses));
    }
}
