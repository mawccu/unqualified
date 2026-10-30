using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Session root for any playable scene (the apartment, the wedding). Finds the
/// scene's props, sockets and usable things, spawns the local player, wires the
/// interaction service and the HUD, and owns pause, restart and scene travel.
/// What happens in the scene (the landlord, the shift) lives in its own director.
/// Solo play pauses time on the menu; once online the game must keep running.
/// </summary>
public class GameSession : MonoBehaviour
{
    public const string ApartmentScene = "Apartment";
    public const string WeddingScene = "Wedding";
    public const string CallCenterScene = "CallCenter";
    /// The local player's id while offline (also their key in the crew's language profiles).
    public const string OfflineActor = "peer_1";

    [SerializeField] WorkerLibrary workers;
    [SerializeField] Hud hud;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] Camera previewCamera;

    public static GameSession Instance { get; private set; }
    public InteractionService Service { get; private set; }
    public PlayerActor Player { get; private set; }
    public Hud Hud => hud;
    public WorkerLibrary Workers => workers;
    public bool MenuOpen { get; private set; }
    public bool Travelling { get; private set; }

    InputAction _pause, _restart, _stats;

    public void Configure(WorkerLibrary library, Hud hudComponent, Transform[] spawns, Camera preview)
    {
        workers = library;
        hud = hudComponent;
        spawnPoints = spawns;
        previewCamera = preview;
    }

    void Awake()
    {
        Instance = this;
        if (previewCamera != null)
            previewCamera.gameObject.SetActive(false);
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;
        Time.timeScale = 1f;

        Service = new InteractionService();
        foreach (var prop in FindObjectsByType<PickupProp>(FindObjectsSortMode.None))
            Service.RegisterProp(prop);
        foreach (var socket in FindObjectsByType<PlacementSocket>(FindObjectsSortMode.None))
            Service.RegisterSocket(socket);
        foreach (var thing in FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            Service.RegisterInteractable(thing);

        Player = SpawnPlayer(OfflineActor, 0);
        Player.Submit = Submit;
        Player.PromptChanged += hud.SetPrompt;
        Player.Feedback += hud.ShowToast;
        Player.AimChanged += hud.SetAim;

        hud.ResumeRequested += () => SetMenu(false);
        hud.RestartRequested += Restart;
        hud.NewGameRequested += NewGame;
        hud.QuitRequested += Quit;

        _pause = new InputAction("pause", InputActionType.Button, "<Keyboard>/escape");
        _pause.AddBinding("<Gamepad>/start");
        _restart = new InputAction("restart", InputActionType.Button, "<Keyboard>/f5");
        _stats = new InputAction("stats", InputActionType.Button, "<Keyboard>/f3");
        _pause.Enable();
        _restart.Enable();
        _stats.Enable();
    }

    void Start() => SetMenu(false);

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        _pause?.Dispose();
        _restart?.Dispose();
        _stats?.Dispose();
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (_pause.WasPressedThisFrame())
        {
            if (!MenuOpen && Player.EscapeHandler != null && Player.EscapeHandler())
                return;
            SetMenu(!MenuOpen);
        }
        if (_restart.WasPressedThisFrame())
            Restart();
        if (_stats.WasPressedThisFrame())
            hud.ToggleStats();
        // A click back into the window recaptures the mouse.
        if (!MenuOpen && !Player.WantsCursor && Cursor.lockState != CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            LockCursor(true);
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused && !MenuOpen && !Application.isEditor)
            SetMenu(true);
    }

    PlayerActor SpawnPlayer(string actorId, int slot)
    {
        var spawn = spawnPoints != null && spawnPoints.Length > 0 ? spawnPoints[slot % spawnPoints.Length] : transform;
        var go = new GameObject($"Player_{actorId}");
        go.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        go.AddComponent<CharacterController>();
        var actor = go.AddComponent<PlayerActor>();
        actor.Setup(actorId, slot, Service, workers);
        return actor;
    }

    /// Local player intent. Offline (and later the host) applies it straight away.
    public void Submit(InteractionRequest req)
    {
        var result = Service.Request(req);
        Player.ReportResult(req.Action, result);
    }

    public void Restart() => TravelTo(SceneManager.GetActiveScene().name, 0.25f);

    void NewGame()
    {
        CrewState.StartOver();
        TravelTo(ApartmentScene, 0.4f);
    }

    /// Fade out and load another scene (going to work, coming home).
    public void TravelTo(string scene, float fadeSeconds = 0.8f)
    {
        if (Travelling)
            return;
        Travelling = true;
        StartCoroutine(TravelRoutine(scene, fadeSeconds));
    }

    IEnumerator TravelRoutine(string scene, float fadeSeconds)
    {
        Time.timeScale = 1f;
        hud.SetMenuVisible(false);
        Player.Lock();
        hud.Fade(1f, fadeSeconds);
        yield return new WaitForSecondsRealtime(fadeSeconds + 0.05f);
        GameSettings.Save();
        SceneManager.LoadScene(scene);
    }

    /// Call after changing Player.WantsCursor.
    public void RefreshCursor() => LockCursor(!MenuOpen && !Player.WantsCursor);

    void SetMenu(bool open)
    {
        if (MenuOpen && !open)
            GameSettings.Save();
        MenuOpen = open;
        Time.timeScale = open ? 0f : 1f;
        Player.ControlsEnabled = !open;
        hud.SetMenuVisible(open);
        RefreshCursor();
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    static void Quit()
    {
        GameSettings.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
