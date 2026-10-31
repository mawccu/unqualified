using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// The workshop in the real apartment: an order is paid once, knocks at the door
/// and unpacks once; a build goes together by hand on the bench (with a wrong
/// module found by the test and fixed); a reload mid-build puts every part back
/// where it was; the finished gadget sits on the rack and can be packed.
public class WorkshopTests
{
    GameSession _session;
    PlayerActor Player => _session.Player;
    InteractionService Service => _session.Service;
    static CrewState Crew => CrewState.Current;

    [UnitySetUp]
    public IEnumerator LoadApartment()
    {
        CrewState.StartOver();
        var crew = Crew;
        crew.billReceived = true;
        crew.hired = true;
        crew.job = Jobs.CallCenter;
        crew.savings = 200;
        crew.Save();
        PlayerPrefs.SetInt("intro_seen", 1);
        yield return Load();
    }

    IEnumerator Load()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSession.ApartmentScene);
        yield return null;
        yield return null;
        _session = GameSession.Instance;
        Assert.IsNotNull(_session);
        Assert.IsNotNull(AssemblyBench.Instance, "the flat has a workbench");
        Assert.IsNotNull(WorkshopRoom.Instance);
    }

    [UnityTearDown]
    public IEnumerator Restore()
    {
        Time.timeScale = 1f;
        var flat = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("AfterWorkshopTest_" + Time.frameCount));
        yield return SceneManager.UnloadSceneAsync(flat);
    }

    static IEnumerator WaitFor(System.Func<bool> condition, float seconds, string what)
    {
        for (float t = 0f; t < seconds && !condition(); t += Time.unscaledDeltaTime)
            yield return null;
        Assert.IsTrue(condition(), "timed out waiting: " + what);
    }

    InteractionResult Req(InteractionAction action, string target, int revision = -1) =>
        Service.Request(new InteractionRequest { ActorId = Player.ActorId, Action = action, TargetId = target, ExpectedRevision = revision });

    /// Pick a prop up and put it on a socket, the way the hands do.
    void Move(PickupProp prop, string socket)
    {
        Assert.AreEqual(InteractionResult.Ok, Req(InteractionAction.Pickup, prop.EntityId, prop.Revision), $"pick up {prop.EntityId}");
        Assert.AreEqual(InteractionResult.Ok, Req(InteractionAction.Place, socket), $"put {prop.EntityId} on {socket}");
    }

    PickupProp Piece(string part) => Service.Props.First(p => p.EntityId.StartsWith("build_" + part + "_"));

    /// What the crosshair lands on first, looking from the player's eye at a point.
    Collider FirstHit(Vector3 target)
    {
        Physics.SyncTransforms();
        Vector3 eye = Player.Camera.transform.position;
        var hits = Physics.RaycastAll(eye, (target - eye).normalized, 2.6f, GameLayers.AimMask, QueryTriggerInteraction.Collide);
        return hits.OrderBy(h => h.distance).Select(h => h.collider).FirstOrDefault();
    }

    void StandAtTheBench()
    {
        Player.Teleport(new Vector3(5.78f, 0.05f, -1.15f));
        Player.SetLook(90f, 35f);
    }

    IEnumerator OrderAndUnpack(params string[] parts)
    {
        int before = Crew.savings;
        var cart = parts.GroupBy(p => p).Select(g => new PartStack(g.Key, g.Count())).ToList();
        Assert.AreEqual(ShopResult.Ok, WorkshopLedger.Checkout(Crew, "Q-TEST1", cart, out var order));
        Assert.AreEqual(ShopResult.AlreadyPlaced, WorkshopLedger.Checkout(Crew, "Q-TEST1", cart, out _), "a double click");
        Assert.AreEqual(before - order.total, Crew.savings, "paid once");
        WorkshopRoom.Instance.OrderPlaced(order);
        Time.timeScale = 6f;
        yield return WaitFor(() => order.state == PartOrder.Delivered, 10f, "the courier");
        Time.timeScale = 1f;
        var parcel = Service.GetProp("parcel_Q-TEST1");
        Assert.IsNotNull(parcel, "a parcel by the door");
        Assert.Less(Vector3.Distance(parcel.transform.position, new Vector3(1f, 0f, -2.6f)), 0.8f, "on the doormat");
        parcel.GetComponent<Parcel>().UseHeld(Player);
        foreach (var group in parts.GroupBy(p => p))
            Assert.AreEqual(group.Count(), WorkshopLedger.Count(Crew, group.Key), group.Key);
        WorkshopRoom.Instance.Unpack(parcel.GetComponent<Parcel>(), Player);
        Assert.AreEqual(parts.Length, Crew.parts.Sum(s => s.count), "a second tear gives nothing");
        Assert.AreEqual("an empty box", parcel.DisplayName);
    }

    [UnityTest]
    public IEnumerator BuyDeliverUnpackAndBuildAWhisperBoxByHand()
    {
        var recipe = PartCatalog.Recipe(PartCatalog.WhisperBox);
        yield return OrderAndUnpack(recipe.Parts);
        var bench = AssemblyBench.Instance;
        StandAtTheBench();
        yield return null;
        Assert.AreEqual(BuildResult.Ok, bench.Begin(recipe.Id, Player));
        Assert.AreEqual(0, Crew.parts.Sum(s => s.count), "the parts left storage for the tray");
        yield return null;
        Assert.IsTrue(bench.CasingSeated, "the casing starts on the mat");
        var boardSocket = Service.GetSocket(AssemblyBench.BoardSocket);
        Assert.AreEqual(boardSocket, FirstHit(boardSocket.transform.position + Vector3.up * 0.02f)?.GetComponentInParent<PlacementSocket>(),
            "aiming into the casing finds the board's slot, not the casing around it");

        Move(Piece(PartCatalog.Board), AssemblyBench.BoardSocket);
        Assert.IsTrue(bench.BoardSeated);
        yield return null;
        yield return null;
        var screw = Service.GetInteractable("bench_screw_0");
        Assert.IsTrue(screw.gameObject.activeInHierarchy, "the screws show once the board is in");
        var hit = FirstHit(screw.transform.position + Vector3.up * 0.01f);
        Assert.AreEqual(screw, hit?.GetComponentInParent<Interactable>(), $"the crosshair can find a screw (hit {(hit != null ? hit.name : "nothing")})");
        var audioPort = Service.GetSocket(AssemblyBench.PortSocket(PartCatalog.PortAudio));
        Assert.AreEqual(audioPort, FirstHit(audioPort.transform.position + Vector3.up * 0.02f)?.GetComponentInParent<PlacementSocket>(), "and a port");
        // A slip: the speaker into the round sensor port (the ports take any module).
        Move(Piece(PartCatalog.Speaker), AssemblyBench.PortSocket(PartCatalog.PortSensor));
        Assert.AreNotEqual(InteractionResult.Ok, Req(InteractionAction.Use, "bench_lid"), "the lid waits for screws and the battery");

        var driver = bench.Screwdriver;
        Assert.AreEqual("tool_cup", driver.SocketId, "the screwdriver stands in its cup");
        Assert.AreNotEqual(InteractionResult.Ok, Req(InteractionAction.Use, "bench_screw_0"), "no screwdriver, no screw");
        Assert.AreEqual(InteractionResult.Ok, Req(InteractionAction.Pickup, driver.EntityId, driver.Revision));
        Assert.AreEqual(InteractionResult.Ok, Req(InteractionAction.Use, "bench_screw_0"));
        Assert.AreEqual(InteractionResult.Ok, Req(InteractionAction.Use, "bench_screw_1"));
        Assert.AreEqual(InteractionResult.Ok, Req(InteractionAction.Place, "tool_cup"), "back in the cup");
        Assert.AreEqual(2, bench.ScrewsFastened);
        yield return null;
        var board = Piece(PartCatalog.Board);
        Assert.AreEqual(InteractionResult.SocketLocked, Req(InteractionAction.Pickup, board.EntityId, board.Revision), "a screwed-down board stays put");

        Move(Piece(PartCatalog.Battery), AssemblyBench.BatterySocket);
        yield return null;
        Assert.AreEqual(InteractionResult.Ok, Req(InteractionAction.Use, "bench_lid"));
        Assert.IsTrue(bench.LidClosed);
        yield return null;
        var speaker = Piece(PartCatalog.Speaker);
        Assert.AreEqual(InteractionResult.SocketLocked, Req(InteractionAction.Pickup, speaker.EntityId, speaker.Revision), "sealed under the lid");

        Assert.AreEqual(InteractionResult.Ok, Req(InteractionAction.Use, "bench_jig"));
        yield return WaitFor(() => !bench.Testing, 5f, "the test");
        CollectionAssert.Contains(bench.Faults.ToList(), PartCatalog.PortAudio, "the audio port is empty");
        CollectionAssert.Contains(bench.Faults.ToList(), PartCatalog.PortSensor, "and the sensor port has the wrong module");
        Assert.AreEqual(0, Crew.gadgets.Count, "a failed test makes nothing and costs nothing");
        Assert.IsTrue(Crew.build.Active);

        // Fix it: open up, move the speaker to the triangle port, close, test again.
        Assert.AreEqual(InteractionResult.Ok, Req(InteractionAction.Use, "bench_lid"));
        yield return null;
        Move(speaker, AssemblyBench.PortSocket(PartCatalog.PortAudio));
        yield return null;
        Assert.AreEqual(InteractionResult.Ok, Req(InteractionAction.Use, "bench_lid"));
        Assert.AreEqual(InteractionResult.Ok, Req(InteractionAction.Use, "bench_jig"));
        yield return WaitFor(() => !bench.Testing, 5f, "the second test");
        Assert.AreEqual(1, Crew.gadgets.Count, "one gadget");
        Assert.AreEqual(PartCatalog.WhisperBox, Crew.gadgets[0].recipe);
        Assert.IsFalse(Crew.build.Active, "the bench is clear");
        Assert.AreEqual(0, Crew.parts.Sum(s => s.count), "its parts were used, none came back");
        Assert.IsFalse(Service.Props.Any(p => p.EntityId.StartsWith("build_")), "the stand-in parts are gone");

        // On the rack, packed for the next shift.
        Assert.AreEqual(BuildResult.Ok, WorkshopLedger.Equip(Crew, Crew.gadgets[0].id, Player.ActorId));
        Assert.AreEqual(Crew.gadgets[0], WorkshopLedger.EquippedBy(Crew, Player.ActorId));

        // And it is all on disk.
        var saved = CrewState.FromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.persistentDataPath, "crew.json")));
        Assert.AreEqual(1, saved.gadgets.Count);
        Assert.AreEqual(PartOrder.Opened, saved.orders.Single(o => o.id == "Q-TEST1").state);
    }

    [UnityTest]
    public IEnumerator AReloadMidBuildPutsEveryPartBackWhereItWas()
    {
        var recipe = PartCatalog.Recipe(PartCatalog.SteadyHand);
        foreach (var part in recipe.Parts)
            Crew.parts.Add(new PartStack(part, 1));
        var bench = AssemblyBench.Instance;
        StandAtTheBench();
        yield return null;
        bench.Begin(recipe.Id, Player);
        yield return null;
        Move(Piece(PartCatalog.Board), AssemblyBench.BoardSocket);
        yield return null;
        Move(Piece(PartCatalog.Motor), AssemblyBench.PortSocket(PartCatalog.PortMotor));
        Assert.AreEqual(InteractionResult.Ok, Req(InteractionAction.Pickup, bench.Screwdriver.EntityId, bench.Screwdriver.Revision));
        Assert.AreEqual(InteractionResult.Ok, Req(InteractionAction.Use, "bench_screw_0"));
        yield return null;
        yield return null;

        yield return Load();
        bench = AssemblyBench.Instance;
        yield return null;
        Assert.IsTrue(bench.HasBuild, "the build survived");
        Assert.AreEqual(recipe.Parts.Length, Service.Props.Count(p => p.EntityId.StartsWith("build_")), "every reserved part, once");
        Assert.AreEqual(0, Crew.parts.Sum(s => s.count), "none came back to storage, none were lost");
        Assert.IsTrue(bench.CasingSeated && bench.BoardSeated);
        Assert.AreEqual(PartCatalog.Motor, Crew.build.PartIn(AssemblyBench.PortSocket(PartCatalog.PortMotor)));
        Assert.AreEqual(1, bench.ScrewsFastened);
        Assert.AreEqual("", Crew.build.editor, "nobody is locked to the bench after a reload");

        Assert.AreEqual(BuildResult.Ok, bench.Cancel());
        Assert.AreEqual(recipe.Parts.Length, Crew.parts.Sum(s => s.count), "cancelling gives them all back");
        Assert.IsFalse(Service.Props.Any(p => p.EntityId.StartsWith("build_")));
    }

    [UnityTest]
    public IEnumerator ParcelsWaitByTheDoorAcrossAReloadAndOpenOnce()
    {
        WorkshopLedger.Checkout(Crew, "Q-WAIT", new[] { new PartStack(PartCatalog.Sensor, 2) }, out _);
        WorkshopLedger.Deliver(Crew, "Q-WAIT");
        Crew.Save();
        yield return Load();
        yield return null;
        var parcel = Service.GetProp("parcel_Q-WAIT");
        Assert.IsNotNull(parcel, "still by the door after the reload");
        parcel.GetComponent<Parcel>().UseHeld(Player);
        Assert.AreEqual(2, WorkshopLedger.Count(Crew, PartCatalog.Sensor));
        yield return Load();
        yield return null;
        Assert.IsNull(Service.GetProp("parcel_Q-WAIT"), "an opened parcel does not come back");
        Assert.AreEqual(2, WorkshopLedger.Count(Crew, PartCatalog.Sensor));
    }

    [UnityTest]
    public IEnumerator TheRackChargesForFreeAndTheDoorWaitsForTheRent()
    {
        Crew.gadgets.Add(new GadgetInstance { id = "G-T", recipe = PartCatalog.SteadyHand, charge = 0.2f });
        int savings = Crew.savings;
        Time.timeScale = 8f;
        yield return WaitFor(() => Crew.gadgets[0].charge >= 1f, 10f, "charging");
        Time.timeScale = 1f;
        Assert.AreEqual(savings, Crew.savings);

        Crew.daysLeft = 0;
        var door = Object.FindFirstObjectByType<FrontDoor>();
        Assert.AreEqual(InteractionResult.NotNow, door.CanUse(Player), "rent first");
        Crew.savings = 900;
        Assert.IsTrue(WorkshopLedger.PayRent(Crew));
        Assert.AreEqual(InteractionResult.Ok, door.CanUse(Player));
    }
}
