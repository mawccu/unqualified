using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The charging rack by the workshop door: finished gadgets sit in its cradles
/// with a charge light each (red, amber, green), and the one packed for the next
/// shift goes into the bag hanging beside it. Using the rack opens its sheet.
/// </summary>
public class GadgetRack : Interactable
{
    [SerializeField] Transform[] cradles;
    [SerializeField] Transform bag;
    [SerializeField] Transform bagTagSpot;
    TextMesh bagTag;

    public static GadgetRack Instance { get; private set; }

    readonly List<GameObject> _shown = new();
    readonly List<(Renderer light, GadgetInstance gadget)> _lights = new();

    public void Configure(Transform[] slots, Transform bagSpot, Transform tag)
    {
        cradles = slots;
        bag = bagSpot;
        bagTagSpot = tag;
    }

    void Awake()
    {
        Instance = this;
        if (bagTagSpot != null)
            bagTag = WorkshopArt.Label(bagTagSpot, Vector3.zero, Quaternion.identity, 0.022f, new Color(0.95f, 0.95f, 0.9f));
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start() => Refresh();

    /// Redraws what sits where (after a build, packing, taking one apart).
    public void Refresh()
    {
        foreach (var go in _shown)
            Destroy(go);
        _shown.Clear();
        _lights.Clear();
        var art = WorkshopArt.Load();
        var crew = CrewState.Current;
        int slot = 0;
        string packed = "";
        foreach (var gadget in crew.gadgets)
        {
            if (gadget.Equipped)
            {
                packed += (packed == "" ? "" : ", ") + PartCatalog.Recipe(gadget.recipe).Name;
                continue;
            }
            if (slot >= cradles.Length)
                continue;
            var cradle = cradles[slot++];
            var look = WorkshopArt.Show(art != null ? art.GadgetModel(gadget.recipe) : null, cradle, new Vector3(0f, 0.012f, 0f), Quaternion.identity);
            if (look != null)
            {
                // A little larger than life, so it reads from across the alcove.
                look.transform.localScale = Vector3.one * 1.6f;
                _shown.Add(look);
            }
            var light = WorkshopArt.Light(cradle, new Vector3(-0.07f, 0.014f, 0.07f), new Vector3(0.02f, 0.008f, 0.012f));
            _shown.Add(light.gameObject);
            _lights.Add((light, gadget));
        }
        if (bagTag != null)
            bagTag.text = packed == "" ? "BAG: empty" : "PACKED: " + packed;
        UpdateLights();
    }

    void Update() => UpdateLights();

    void UpdateLights()
    {
        foreach (var (light, gadget) in _lights)
        {
            var colour = gadget.charge >= 1f ? new Color(0.3f, 1f, 0.4f)
                : gadget.charge > 0.5f ? new Color(1f, 0.75f, 0.2f) : new Color(1f, 0.25f, 0.2f);
            // Charging blinks.
            if (gadget.charge < 1f && Mathf.Repeat(Time.time, 1f) > 0.6f)
                colour *= 0.35f;
            WorkshopArt.Tint(light, colour);
        }
    }

    public override string Prompt(IInteractionActor actor) => "Check the charging rack";

    public override InteractionResult CanUse(IInteractionActor actor) =>
        WorkshopPanel.Instance != null && !WorkshopPanel.Instance.IsOpen ? InteractionResult.Ok : InteractionResult.NotNow;

    public override void Use(IInteractionActor actor)
    {
        if (actor is PlayerActor player && player == GameSession.Instance.Player)
            WorkshopPanel.Instance.OpenRack(player);
    }
}
