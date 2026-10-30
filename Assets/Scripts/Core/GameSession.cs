using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Session root for the wedding shift. Finds the scene's props and sockets,
/// spawns the local player, wires the interaction service and the HUD, and owns
/// pause and restart. Solo play pauses time on the menu; once online the game
/// must keep running for everyone else.
/// </summary>
public class GameSession : MonoBehaviour
{
    const string OfflineActor = "peer_1";
    const float TaskRefresh = 0.2f;

    [SerializeField] WorkerLibrary workers;
    [SerializeField] Hud hud;
    [SerializeField] Coordinator coordinator;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] Camera previewCamera;

    public InteractionService Service { get; private set; }
    public PlayerActor Player { get; private set; }
    public bool MenuOpen { get; private set; }
    public IReadOnlyList<TaskBoard.Result> TaskResults => _taskResults;

    readonly List<TaskBoard.Task> _tasks = TaskBoard.Catalog();
    List<TaskBoard.Result> _taskResults = new();
    float _taskTimer;
    InputAction _pause, _restart, _stats;

    public void Configure(WorkerLibrary library, Hud hudComponent, Coordinator npc, Transform[] spawns, Camera preview)
    {
        workers = library;
        hud = hudComponent;
        coordinator = npc;
        spawnPoints = spawns;
        previewCamera = preview;
    }

    void Awake()
    {
        if (previewCamera != null)
            previewCamera.gameObject.SetActive(false);
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;

        Service = new InteractionService();
        foreach (var prop in FindObjectsByType<PickupProp>(FindObjectsSortMode.None))
            Service.RegisterProp(prop);
        foreach (var socket in FindObjectsByType<PlacementSocket>(FindObjectsSortMode.None))
            Service.RegisterSocket(socket);

        Player = SpawnPlayer(OfflineActor, 0);
        Player.Submit = Submit;
        Player.PromptChanged += hud.SetPrompt;
        Player.Feedback += hud.ShowToast;
        Player.AimChanged += hud.SetAim;

        hud.ResumeRequested += () => SetMenu(false);
        hud.RestartRequested += Restart;
        hud.QuitRequested += Quit;

        _pause = new InputAction("pause", InputActionType.Button, "<Keyboard>/escape");
        _pause.AddBinding("<Gamepad>/start");
        _restart = new InputAction("restart", InputActionType.Button, "<Keyboard>/f5");
        _stats = new InputAction("stats", InputActionType.Button, "<Keyboard>/f3");
        _pause.Enable();
        _restart.Enable();
        _stats.Enable();
    }

    void Start()
    {
        RefreshTasks();
        SetMenu(false);
    }

    void OnDestroy()
    {
        _pause?.Dispose();
        _restart?.Dispose();
        _stats?.Dispose();
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (_pause.WasPressedThisFrame())
            SetMenu(!MenuOpen);
        if (_restart.WasPressedThisFrame())
            Restart();
        if (_stats.WasPressedThisFrame())
            hud.ToggleStats();
        // A click back into the window recaptures the mouse.
        if (!MenuOpen && Cursor.lockState != CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            LockCursor(true);

        _taskTimer += Time.unscaledDeltaTime;
        if (_taskTimer >= TaskRefresh)
        {
            _taskTimer = 0f;
            RefreshTasks();
            if (coordinator != null)
                hud.SetCoordinator(coordinator.State);
        }
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
        if (result == InteractionResult.Ok)
            RefreshTasks();
    }

    public void Restart()
    {
        Service.ResetAll();
        Player.ResetToSpawn();
        SetMenu(false);
        RefreshTasks();
    }

    void RefreshTasks()
    {
        _taskResults = TaskBoard.Evaluate(_tasks, Service.Occupancy());
        hud.SetTasks(_taskResults);
    }

    void SetMenu(bool open)
    {
        if (MenuOpen && !open)
            GameSettings.Save();
        MenuOpen = open;
        Time.timeScale = open ? 0f : 1f;
        Player.ControlsEnabled = !open;
        hud.SetMenuVisible(open);
        LockCursor(!open);
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
