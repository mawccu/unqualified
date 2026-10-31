using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The workbench: the mat with its sockets, the screws, the lid and the test jig,
/// and the physical stand-ins for the parts of the build in progress.
///
/// The saved build (CrewState.build) is the truth: which parts are reserved and
/// where each one sits. The bench makes that visible (spawning the parts into
/// the tray, or straight into their sockets after a reload) and writes every
/// change the hands make back into it. A build takes about half a minute:
/// casing on the mat, board in the casing, each module into its keyed port,
/// two screws, the battery, close the lid, run the test. A misplaced module is
/// found by the test and fixed by opening the lid; it costs time, never parts.
/// </summary>
public class AssemblyBench : MonoBehaviour
{
    public const string MatSocket = "bench_mat", BoardSocket = "bench_board", BatterySocket = "bench_battery";
    public static string PortSocket(string port) => "bench_port_" + port;
    const float TestSeconds = 1.8f;
    /// The parts are modelled at real size; on the bench they are half as big again, so the
    /// ports and screws are comfortable to aim at.
    public const float PartScale = 1.5f;

    [SerializeField] PlacementSocket mat, board, battery;
    [SerializeField] PlacementSocket[] ports;
    [SerializeField] ScrewPoint[] screws;
    [SerializeField] CaseLid lid;
    [SerializeField] TestJig jig;
    [SerializeField] Transform[] trayPoints;
    [SerializeField] Renderer[] portLights;
    [SerializeField] PickupProp screwdriver;
    [SerializeField] AudioSource audioSource;
    [SerializeField] PlacementSocket toolCup;

    public static AssemblyBench Instance { get; private set; }
    public AudioSource Audio => audioSource;
    public bool HasBuild => Crew.build.Active;
    public bool LidClosed => lid != null && lid.Closed;
    public bool Testing { get; private set; }
    public RecipeDef Recipe => PartCatalog.Recipe(Crew.build.recipe);
    /// The faults the last test found, by port ("" when it passed or has not run).
    public IReadOnlyList<string> Faults => _faults;
    public PickupProp Screwdriver => screwdriver;

    static CrewState Crew => CrewState.Current;
    readonly List<PickupProp> _pieces = new();
    readonly List<string> _faults = new();
    string _lastLayout = "";

    public void Configure(PlacementSocket matSocket, PlacementSocket boardSocket, PlacementSocket batterySocket, PlacementSocket[] portSockets,
        ScrewPoint[] screwPoints, CaseLid caseLid, TestJig testJig, Transform[] tray, Renderer[] lights, PickupProp driver, AudioSource source)
    {
        mat = matSocket;
        board = boardSocket;
        battery = batterySocket;
        ports = portSockets;
        screws = screwPoints;
        lid = caseLid;
        jig = testJig;
        trayPoints = tray;
        portLights = lights;
        screwdriver = driver;
        audioSource = source;
    }

    public void ConfigureToolCup(PlacementSocket cup) => toolCup = cup;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        // Leaving the flat (or the game) frees the bench; the build itself stays.
        Crew.build.editor = "";
    }

    void Start()
    {
        // The screwdriver starts standing in its cup.
        if (toolCup != null && screwdriver != null && string.IsNullOrEmpty(screwdriver.HolderId))
        {
            toolCup.SetOccupant(screwdriver);
            screwdriver.PlaceAt(toolCup.SnapPosition, toolCup.transform.rotation, toolCup.EntityId);
        }
        if (HasBuild)
            Restore();
        else
            ShowSockets(false);
        Refresh();
    }

    // Starting, cancelling, finishing ------------------------------------------------------

    /// From the blueprint board: reserve the parts and lay them out in the tray.
    public BuildResult Begin(string recipeId, IInteractionActor actor)
    {
        var result = WorkshopLedger.BeginBuild(Crew, recipeId, actor.ActorId);
        if (result != BuildResult.Ok)
            return result;
        Crew.Save();
        _faults.Clear();
        SpawnReserved(false);
        ShowSockets(true);
        jig.Show("READY\nboard into\nthe casing", Color.white);
        Refresh();
        return BuildResult.Ok;
    }

    /// Gives the parts back to storage (once) and clears the bench, whatever state it is in.
    public BuildResult Cancel()
    {
        var result = WorkshopLedger.CancelBuild(Crew);
        if (result != BuildResult.Ok)
            return result;
        Crew.Save();
        ClearPieces();
        Refresh();
        jig.Show("", Color.white);
        return result;
    }

    public void RunTest(IInteractionActor actor)
    {
        if (Testing || !HasBuild || !LidClosed)
            return;
        WorkshopLedger.ClaimBuild(Crew, actor.ActorId);
        StartCoroutine(Test());
    }

    IEnumerator Test()
    {
        Testing = true;
        _faults.Clear();
        for (float t = 0f; t < TestSeconds; t += Time.deltaTime)
        {
            int dots = (int)(t / 0.3f) % 4;
            jig.Show("TESTING" + new string('.', dots) + $"\ncharge {Mathf.RoundToInt(Mathf.Clamp01(t / TestSeconds) * 100f)}%", new Color(0.6f, 1f, 0.7f));
            yield return null;
        }
        var recipe = Recipe;
        // Each needed port must hold its module; the others must be empty.
        foreach (var port in PartCatalog.Ports)
        {
            string inPort = Crew.build.PartIn(PortSocket(port));
            bool needed = recipe.Ports.Contains(port);
            if (needed && (inPort == "" || PartCatalog.Part(inPort).Port != port))
                _faults.Add(port);
            else if (!needed && inPort != "")
                _faults.Add(port);
        }
        Testing = false;
        if (_faults.Count > 0)
        {
            string port = _faults[0];
            string inPort = Crew.build.PartIn(PortSocket(port));
            string what = inPort == "" ? "nothing plugged in" : $"{PartCatalog.Part(inPort).Name.ToLowerInvariant()} in it";
            // The jig's screen is tiny: the port and a word. The toast says the rest.
            jig.Show($"FAULT\n{PartCatalog.PortLabel(port).Split(' ')[0]} port\n{(inPort == "" ? "empty" : "wrong part")}", new Color(1f, 0.45f, 0.35f));
            audioSource.PlayOneShot(ProceduralAudio.Buzz, 0.6f);
            GameSession.Instance?.Hud.ShowToast($"Test failed: the {PartCatalog.PortLabel(port)} port has {what}. Open the lid and swap it.");
            Refresh();
            yield break;
        }
        audioSource.PlayOneShot(ProceduralAudio.Beep, 0.6f);
        jig.Show($"PASS\n{recipe.Name}\nlink OK", new Color(0.55f, 1f, 0.6f));
        WorkshopLedger.CompleteBuild(Crew, out var gadget);
        Crew.Save();
        ClearPieces();
        Refresh();
        GadgetRack.Instance?.Refresh();
        GameSession.Instance?.Hud.ShowBanner($"{recipe.Name.ToUpperInvariant()} BUILT", $"It's on the charging rack. Pack it there to take it to {PartCatalog.JobLabel(recipe.Job)}.");
        StartCoroutine(HideBannerSoon());
        Debug.Log($"[workshop] built {gadget.recipe} {gadget.id}");
    }

    IEnumerator HideBannerSoon()
    {
        yield return new WaitForSeconds(5f);
        GameSession.Instance?.Hud.HideBanner();
    }

    // Physical parts -------------------------------------------------------------------------

    void SpawnReserved(bool restoring)
    {
        var art = WorkshopArt.Load();
        var reserved = Crew.build.reserved;
        int module = 0;
        for (int i = 0; i < reserved.Count; i++)
        {
            var part = PartCatalog.Part(reserved[i]);
            // The tray has a place for the board, the battery and three modules; the
            // casing is too big for it and starts on the mat.
            var spot = part.Kind switch
            {
                "board" => trayPoints[0],
                "battery" => trayPoints[1],
                "module" => trayPoints[Mathf.Min(2 + module++, trayPoints.Length - 1)],
                _ => mat.transform,
            };
            var prop = PartPiece.Spawn(art != null ? art.PartModel(part.Id) : null, $"build_{part.Id}_{i}", part.Id, part.Kind,
                part.Name.ToLowerInvariant(), spot.position, spot.rotation, 0.2f, PartScale);
            _pieces.Add(prop);
            // The casing is a tray: a box round all of it would hide its slots, the
            // screws and the battery bay from the crosshair. Only its floor is solid.
            if (part.Kind == "casing" && prop.GetComponent<BoxCollider>() is BoxCollider box)
            {
                box.center = new Vector3(box.center.x, 0.006f, box.center.z);
                box.size = new Vector3(box.size.x, 0.012f, box.size.z);
            }
            if (part.Kind == "casing" && !restoring && string.IsNullOrEmpty(mat.OccupantId))
            {
                mat.SetOccupant(prop);
                prop.PlaceAt(mat.SnapPosition, mat.transform.rotation, mat.EntityId);
            }
        }
        if (!restoring)
            audioSource.PlayOneShot(ProceduralAudio.SetDown, 0.6f);
    }

    /// After a reload: the parts come back, each in the socket it was in, in an order that respects what sits on what.
    void Restore()
    {
        SpawnReserved(true);
        ShowSockets(true);
        var order = new List<PlacementSocket> { mat, board, battery };
        order.AddRange(ports);
        foreach (var socket in order)
        {
            string part = Crew.build.PartIn(socket.EntityId);
            if (part == "")
                continue;
            var piece = _pieces.FirstOrDefault(p => p.GetComponent<PartPiece>().PartId == part && string.IsNullOrEmpty(p.SocketId));
            if (piece == null || !socket.Accepts(piece))
                continue;
            socket.SetOccupant(piece);
            piece.PlaceAt(socket.SnapPosition, socket.transform.rotation, socket.EntityId);
        }
        for (int i = 0; i < screws.Length; i++)
            screws[i].SetFastened(i < Crew.build.screws);
        lid.SetClosed(Crew.build.lidClosed, true);
        jig.Show(Crew.build.lidClosed ? "READY\npress to test" : "WORK IN\nPROGRESS", Color.white);
    }

    void ClearPieces()
    {
        foreach (var piece in _pieces)
            PartPiece.Remove(piece);
        _pieces.Clear();
        foreach (var socket in AllSockets())
        {
            socket.OccupantId = "";
            socket.Sealed = false;
        }
        foreach (var screw in screws)
            screw.SetFastened(false);
        lid.SetClosed(false, true);
        _faults.Clear();
        ShowSockets(false);
    }

    IEnumerable<PlacementSocket> AllSockets()
    {
        yield return mat;
        yield return board;
        yield return battery;
        foreach (var p in ports)
            yield return p;
    }

    void ShowSockets(bool on)
    {
        foreach (var socket in AllSockets())
            socket.gameObject.SetActive(on);
    }

    // Keeping the save in step with the hands ------------------------------------------------

    public bool CasingSeated => !string.IsNullOrEmpty(mat.OccupantId);
    public bool BoardSeated => !string.IsNullOrEmpty(board.OccupantId);
    public bool BatterySeated => !string.IsNullOrEmpty(battery.OccupantId);
    public int ScrewsFastened => screws.Count(s => s.Fastened);
    public int ModulesPlaced => ports.Count(p => !string.IsNullOrEmpty(p.OccupantId));
    int ModulesNeeded => Recipe != null ? Recipe.Ports.Length : 0;
    /// Screws can go in once the board is seated, while the lid is open.
    public bool ScrewsWanted => HasBuild && BoardSeated && !LidClosed;

    /// Empty when the lid may close; otherwise what is still missing.
    public string WhatStopsClosing()
    {
        if (!CasingSeated) return "Put the casing on the mat first.";
        if (!BoardSeated) return "The board goes in the casing first.";
        if (ModulesPlaced < ModulesNeeded) return ModulesNeeded - ModulesPlaced == 1 ? "A module still needs plugging in." : "Some modules still need plugging in.";
        if (ScrewsFastened < screws.Length) return "Fasten both screws first.";
        if (!BatterySeated) return "The battery isn't in yet.";
        return "";
    }

    string PartIn(PlacementSocket socket)
    {
        var prop = socket.OccupantProp;
        return prop != null && prop.GetComponent<PartPiece>() is PartPiece piece ? piece.PartId : "";
    }

    void Update()
    {
        if (!HasBuild)
            return;
        // Write where everything is into the save whenever it changes.
        var build = Crew.build;
        foreach (var socket in AllSockets())
            build.SetPlaced(socket.EntityId, PartIn(socket));
        build.screws = ScrewsFastened;
        build.lidClosed = LidClosed;
        string layout = string.Join(",", build.placed) + "|" + build.screws + "|" + build.lidClosed;
        if (layout != _lastLayout)
        {
            if (_lastLayout != "")
            {
                Crew.Save();
                if (!LidClosed)
                    _faults.Clear();
            }
            _lastLayout = layout;
            Refresh();
        }
    }

    /// What can be lifted out, what is showing, what the lights say.
    void Refresh()
    {
        bool active = HasBuild;
        mat.Sealed = BoardSeated;
        board.Sealed = BatterySeated || ModulesPlaced > 0 || ScrewsFastened > 0;
        battery.Sealed = LidClosed;
        foreach (var p in ports)
            p.Sealed = LidClosed;
        lid.Show(active && CasingSeated);
        foreach (var screw in screws)
            screw.Show(active && BoardSeated);
        for (int i = 0; i < ports.Length && i < portLights.Length; i++)
        {
            string port = PartCatalog.Ports[i];
            bool needed = active && Recipe.Ports.Contains(port);
            Color colour = !active ? new Color(0.1f, 0.1f, 0.1f)
                : _faults.Contains(port) ? new Color(1f, 0.15f, 0.1f)
                : needed ? PartCatalog.ModuleFor(port).Colour * 1.2f : new Color(0.15f, 0.15f, 0.15f);
            WorkshopArt.Tint(portLights[i], colour);
        }
    }

    public void OnScrewFastened(IInteractionActor actor)
    {
        WorkshopLedger.ClaimBuild(Crew, actor.ActorId);
    }

    public void OnLidChanged(IInteractionActor actor)
    {
        WorkshopLedger.ClaimBuild(Crew, actor.ActorId);
        jig.Show(LidClosed ? "READY\npress to test" : "WORK IN\nPROGRESS", Color.white);
    }

    /// The steps, ticked off as the hands do them (the HUD card and the blueprint board show this).
    public List<(bool done, string text)> Steps()
    {
        var steps = new List<(bool, string)>();
        var recipe = Recipe;
        if (recipe == null)
            return steps;
        steps.Add((CasingSeated, "Put the casing on the mat"));
        steps.Add((BoardSeated, "Seat the board in the casing"));
        foreach (var port in recipe.Ports)
        {
            var module = PartCatalog.ModuleFor(port);
            bool right = PartIn(ports[System.Array.IndexOf(PartCatalog.Ports, port)]) == module.Id;
            steps.Add((right, $"Plug the {module.Name.ToLowerInvariant()} into {PartCatalog.PortLabel(port)}"));
        }
        steps.Add((ScrewsFastened == screws.Length, $"Fasten both screws with the screwdriver ({ScrewsFastened}/{screws.Length})"));
        steps.Add((BatterySeated, "Put the battery in its bay"));
        steps.Add((LidClosed, "Close the lid"));
        steps.Add((false, _faults.Count > 0 ? "Test FAILED: open the lid, fix the red port, test again" : "Run the test on the jig"));
        return steps;
    }

    public void OnActorLeft(string actorId) => WorkshopLedger.ReleaseBuild(Crew, actorId);
}
