using UnityEngine;

/// <summary>
/// The shared parts bins: one bin per part family with a hand-written count on
/// its label. The counts are the crew's storage (CrewState.parts), the same
/// numbers the shop and the blueprints use.
/// </summary>
public class PartShelf : Interactable
{
    [SerializeField] Transform[] labelSpots;
    [SerializeField] string[] partIds;

    public static PartShelf Instance { get; private set; }
    TextMesh[] labels = System.Array.Empty<TextMesh>();

    public void Configure(Transform[] binLabels, string[] parts)
    {
        labelSpots = binLabels;
        partIds = parts;
    }

    void Awake()
    {
        Instance = this;
        labels = new TextMesh[labelSpots.Length];
        for (int i = 0; i < labelSpots.Length; i++)
            labels[i] = WorkshopArt.Label(labelSpots[i], Vector3.zero, Quaternion.identity, 0.028f, Color.black);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start() => Refresh();

    public void Refresh()
    {
        var crew = CrewState.Current;
        for (int i = 0; i < labels.Length && i < partIds.Length; i++)
        {
            var part = PartCatalog.Part(partIds[i]);
            int n = WorkshopLedger.Count(crew, part.Id);
            labels[i].text = $"{part.Id.ToUpperInvariant()}\n× {n}";
            labels[i].color = n > 0 ? new Color(0.1f, 0.1f, 0.12f) : new Color(0.5f, 0.5f, 0.5f);
        }
    }

    public override string Prompt(IInteractionActor actor) => "Count the parts";

    public override InteractionResult CanUse(IInteractionActor actor) => InteractionResult.Ok;

    public override void Use(IInteractionActor actor)
    {
        Refresh();
        var crew = CrewState.Current;
        var lines = new System.Collections.Generic.List<string>();
        foreach (var part in PartCatalog.Parts)
            lines.Add($"{WorkshopLedger.Count(crew, part.Id)} {part.Name.ToLowerInvariant()}");
        GameSession.Instance?.Hud.ShowToast("Parts bins: " + string.Join(", ", lines) + ".");
    }
}
