using UnityEngine;

/// <summary>
/// The four coworkers: one shared uniform language ("budget luxury event staff")
/// with small differences, so they read apart by silhouette first and by a single
/// small accent colour second. Saturated colour is reserved for interaction
/// feedback and UI, not for the workers. Same table as the Godot version.
/// </summary>
public static class WorkerCostumes
{
    public sealed class Costume
    {
        public string Name;
        public Color Shirt, Vest, Trousers, Shoes, Skin, Hair, Accent;
        public string HairStyle;
        public string[] Accessories;

        public bool TryGetRole(string materialName, out Color colour)
        {
            switch (materialName.ToLowerInvariant())
            {
                case "shirt": colour = Shirt; return true;
                case "vest": colour = Vest; return true;
                case "trousers": colour = Trousers; return true;
                case "shoes": colour = Shoes; return true;
                case "skin": colour = Skin; return true;
                case "hair": colour = Hair; return true;
                case "accent": colour = Accent; return true;
            }
            colour = default;
            return false;
        }
    }

    public static readonly Costume[] Variants =
    {
        new()
        {
            Name = "Charcoal vest, burgundy pocket square",
            Shirt = new(0.92f, 0.90f, 0.85f), Vest = new(0.20f, 0.20f, 0.23f), Trousers = new(0.15f, 0.15f, 0.17f),
            Shoes = new(0.14f, 0.13f, 0.13f), Skin = new(0.85f, 0.69f, 0.57f), Hair = new(0.22f, 0.14f, 0.10f),
            Accent = new(0.55f, 0.14f, 0.20f), HairStyle = "short", Accessories = new[] { "bow_tie", "lanyard" },
        },
        new()
        {
            Name = "Navy blazer, brass badge",
            Shirt = new(0.90f, 0.91f, 0.92f), Vest = new(0.17f, 0.20f, 0.28f), Trousers = new(0.14f, 0.14f, 0.17f),
            Shoes = new(0.14f, 0.13f, 0.13f), Skin = new(0.60f, 0.43f, 0.32f), Hair = new(0.10f, 0.08f, 0.08f),
            Accent = new(0.75f, 0.60f, 0.28f), HairStyle = "pompadour", Accessories = new[] { "headset", "radio" },
        },
        new()
        {
            Name = "Brown apron, teal lanyard",
            Shirt = new(0.93f, 0.91f, 0.86f), Vest = new(0.30f, 0.24f, 0.20f), Trousers = new(0.15f, 0.15f, 0.17f),
            Shoes = new(0.18f, 0.15f, 0.13f), Skin = new(0.90f, 0.74f, 0.62f), Hair = new(0.62f, 0.45f, 0.25f),
            Accent = new(0.25f, 0.52f, 0.52f), HairStyle = "ponytail", Accessories = new[] { "apron", "lanyard" },
        },
        new()
        {
            Name = "Forest vest, rose pin",
            Shirt = new(0.91f, 0.92f, 0.88f), Vest = new(0.18f, 0.28f, 0.22f), Trousers = new(0.14f, 0.14f, 0.16f),
            Shoes = new(0.14f, 0.13f, 0.13f), Skin = new(0.74f, 0.55f, 0.42f), Hair = new(0.16f, 0.11f, 0.09f),
            Accent = new(0.78f, 0.45f, 0.50f), HairStyle = "beanie", Accessories = new[] { "belt", "one_glove" },
        },
    };

    /// The client's coordinator: dressed apart from the crew, dusty rose blazer and a headset.
    public static readonly Costume Coordinator = new()
    {
        Name = "Coordinator",
        Shirt = new(0.94f, 0.92f, 0.88f), Vest = new(0.55f, 0.30f, 0.33f), Trousers = new(0.20f, 0.18f, 0.20f),
        Shoes = new(0.10f, 0.09f, 0.09f), Skin = new(0.80f, 0.62f, 0.50f), Hair = new(0.12f, 0.08f, 0.07f),
        Accent = new(0.85f, 0.75f, 0.55f), HairStyle = "ponytail", Accessories = new[] { "headset", "lanyard" },
    };

    public static Costume ForSlot(int slot) => Variants[((slot % Variants.Length) + Variants.Length) % Variants.Length];
}
