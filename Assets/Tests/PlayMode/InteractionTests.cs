using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// Ported from the Godot tests/m0_selftest.gd: the interaction rules on real
/// rigid bodies, in a tiny room built by the test.
public class InteractionTests
{
    sealed class FakeActor : IInteractionActor
    {
        public string ActorId { get; set; }
        public string HeldPropId { get; set; } = "";
        public Transform Body;
        public Transform HoldPoint { get; set; }
        public Vector3 EyePosition => Body.position + Vector3.up * 1.55f;
        public Vector3 DropVelocity => Vector3.zero;
    }

    readonly List<GameObject> _spawned = new();
    InteractionService _service;
    PickupProp _prop;
    PlacementSocket _socket, _upper;
    FakeActor _actor, _other;
    static readonly Vector3 PropStart = new(0f, 0.9f, 0f);
    static readonly Vector3 SocketPosition = new(2f, 1f, 0f);

    [SetUp]
    public void SetUp()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.localScale = new Vector3(40f, 1f, 40f);
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        _spawned.Add(floor);
        var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.transform.localScale = new Vector3(1f, 0.9f, 1f);
        table.transform.position = new Vector3(0f, 0.45f, 0f);
        _spawned.Add(table);

        _service = new InteractionService();
        _prop = MakeProp("cake_base", "cake_tier", PropStart);
        _socket = MakeSocket("cake_socket_base", "cake_tier", "", SocketPosition);
        _upper = MakeSocket("cake_socket_middle", "cake_tier", "cake_socket_base", SocketPosition + Vector3.up * 0.26f);
        _actor = MakeActor("peer_1", new Vector3(0f, 0f, -1.2f));
        _other = MakeActor("peer_2", new Vector3(0.5f, 0f, -1.2f));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in _spawned)
            Object.Destroy(go);
        _spawned.Clear();
    }

    PickupProp MakeProp(string id, string kind, Vector3 at)
    {
        var go = new GameObject(id);
        go.SetActive(false);
        go.transform.position = at;
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(0.6f, 0.26f, 0.6f);
        box.center = new Vector3(0f, 0.13f, 0f);
        go.AddComponent<Rigidbody>();
        var prop = go.AddComponent<PickupProp>();
        prop.Configure(id, kind, id);
        go.SetActive(true);
        _service.RegisterProp(prop);
        _spawned.Add(go);
        return prop;
    }

    PlacementSocket MakeSocket(string id, string kind, string requires, Vector3 at)
    {
        var go = new GameObject(id);
        go.SetActive(false);
        go.transform.position = at;
        var socket = go.AddComponent<PlacementSocket>();
        socket.Configure(id, id, kind, requires, 0.33f, null, null);
        go.SetActive(true);
        _service.RegisterSocket(socket);
        _spawned.Add(go);
        return socket;
    }

    FakeActor MakeActor(string id, Vector3 at)
    {
        var body = new GameObject(id).transform;
        body.position = at;
        var hold = new GameObject("Hold").transform;
        hold.SetParent(body, false);
        hold.localPosition = new Vector3(0.38f, 1.15f, 1.2f);
        _spawned.Add(body.gameObject);
        var actor = new FakeActor { ActorId = id, Body = body, HoldPoint = hold };
        _service.RegisterActor(actor);
        return actor;
    }

    InteractionRequest Pickup(FakeActor a, PickupProp p, int revision) =>
        new() { ActorId = a.ActorId, Action = InteractionAction.Pickup, TargetId = p.EntityId, ExpectedRevision = revision };

    InteractionRequest Place(FakeActor a, PlacementSocket s) =>
        new() { ActorId = a.ActorId, Action = InteractionAction.Place, TargetId = s.EntityId, ExpectedRevision = -1 };

    InteractionRequest Drop(FakeActor a) => new() { ActorId = a.ActorId, Action = InteractionAction.Drop, TargetId = "" };

    [UnityTest]
    public IEnumerator RejectionsChangeNothing()
    {
        yield return null;
        Assert.AreEqual("", _prop.HolderId);
        Assert.AreEqual(0, _prop.Revision, "prop starts free at revision 0");

        _actor.Body.position = new Vector3(0f, 0f, -8f);
        Assert.AreEqual(InteractionResult.OutOfRange, _service.Request(Pickup(_actor, _prop, 0)));
        _actor.Body.position = new Vector3(0f, 0f, -1.2f);
        Assert.AreEqual(InteractionResult.StaleRevision, _service.Request(Pickup(_actor, _prop, 7)));
        Assert.AreEqual(InteractionResult.NoTarget, _service.Request(new InteractionRequest { ActorId = "peer_1", Action = InteractionAction.Pickup, TargetId = "nope" }));
        Assert.AreEqual(InteractionResult.UnknownActor, _service.Request(new InteractionRequest { ActorId = "ghost", Action = InteractionAction.Pickup, TargetId = "cake_base" }));
        Assert.AreEqual(InteractionResult.NothingHeld, _service.Request(Place(_actor, _socket)));
        _service.Validate(Pickup(_actor, _prop, 0));
        Assert.AreEqual("", _prop.HolderId, "validate alone changes nothing");
        Assert.AreEqual(0, _prop.Revision);
    }

    [UnityTest]
    public IEnumerator PickupHoldPlaceDropReset()
    {
        yield return null;
        Assert.AreEqual(InteractionResult.Ok, _service.Request(Pickup(_actor, _prop, 0)));
        Assert.AreEqual("peer_1", _prop.HolderId);
        Assert.AreEqual("cake_base", _actor.HeldPropId);
        // It travels into the hands rather than jumping there.
        yield return null;
        Assert.Greater(Vector3.Distance(_prop.transform.position, _actor.HoldPoint.position), 0.05f, "a picked-up prop does not teleport to the hands");
        yield return new WaitForSeconds(PickupProp.GrabSeconds + 0.1f);
        Assert.Less(Vector3.Distance(_prop.transform.position, _actor.HoldPoint.position), 0.01f, "held prop follows the hold point");
        Assert.AreEqual(InteractionResult.HandsFull, _service.Request(Pickup(_actor, _prop, _prop.Revision)));
        Assert.AreEqual(InteractionResult.AlreadyHeld, _service.Request(Pickup(_other, _prop, _prop.Revision)), "a second actor cannot take a held prop");

        Assert.AreEqual(InteractionResult.SocketLocked, _service.Request(Place(_actor, _upper)), "a layer waits for the one below");
        _actor.Body.position = new Vector3(2f, 0f, -9f);
        Assert.AreEqual(InteractionResult.OutOfRange, _service.Request(Place(_actor, _socket)));
        _actor.Body.position = new Vector3(2f, 0f, -1.2f);
        Assert.AreEqual(InteractionResult.Ok, _service.Request(Place(_actor, _socket)));
        Assert.AreEqual("cake_base", _socket.OccupantId);
        Assert.AreEqual("", _actor.HeldPropId, "hands are empty after placing");
        yield return new WaitForSeconds(PickupProp.PlaceSeconds + 0.1f);
        for (int i = 0; i < 5; i++)
            yield return new WaitForFixedUpdate();
        Assert.Less(Vector3.Distance(_prop.transform.position, _socket.SnapPosition), 0.01f, "placed prop settles on the socket and stays");

        Assert.AreEqual(InteractionResult.Ok, _service.Request(Pickup(_actor, _prop, _prop.Revision)), "a placed prop can be picked up again");
        Assert.AreEqual("", _socket.OccupantId);
        Assert.AreEqual(InteractionResult.Ok, _service.Request(Drop(_actor)));
        Assert.AreEqual("", _prop.HolderId);
        Assert.IsFalse(_prop.GetComponent<Rigidbody>().isKinematic, "a dropped prop is physical");
        yield return new WaitForSeconds(1f);
        Assert.Greater(_prop.transform.position.y, -0.5f, "dropped prop settles above the floor");

        _service.ResetAll();
        yield return new WaitForFixedUpdate();
        Assert.Less(Vector3.Distance(_prop.transform.position, PropStart), 0.05f, "reset returns the prop to its start");
        Assert.AreEqual("", _actor.HeldPropId);
        Assert.AreEqual("", _socket.OccupantId);
    }

    [UnityTest]
    public IEnumerator WrongKindIsRejected()
    {
        var sign = MakeProp("sign_1", "table_sign", new Vector3(0.3f, 0.9f, 0.3f));
        yield return null;
        Assert.AreEqual(InteractionResult.Ok, _service.Request(Pickup(_actor, sign, 0)));
        _actor.Body.position = new Vector3(2f, 0f, -1.2f);
        Assert.AreEqual(InteractionResult.SocketRejects, _service.Request(Place(_actor, _socket)));
    }

    [UnityTest]
    public IEnumerator TwentyCyclesAllSucceed()
    {
        yield return null;
        _actor.Body.position = new Vector3(1f, 0f, -1.2f);
        for (int i = 0; i < 20; i++)
        {
            Assert.AreEqual(InteractionResult.Ok, _service.Request(Pickup(_actor, _prop, _prop.Revision)), $"cycle {i} pickup");
            yield return null;
            Assert.AreEqual(InteractionResult.Ok, _service.Request(Place(_actor, _socket)), $"cycle {i} place");
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(InteractionResult.Ok, _service.Request(Pickup(_actor, _prop, _prop.Revision)), $"cycle {i} pickup again");
            Assert.AreEqual(InteractionResult.Ok, _service.Request(Drop(_actor)), $"cycle {i} drop");
            _service.ResetAll();
            yield return new WaitForFixedUpdate();
            Assert.Less(Vector3.Distance(_prop.transform.position, PropStart), 0.05f, $"cycle {i}: reset puts the prop back");
        }
    }
}
