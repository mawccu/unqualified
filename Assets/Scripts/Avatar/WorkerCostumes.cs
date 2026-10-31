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
        /// The shared face has a moustache; false uses the clean-shaven body mesh.
        public bool Moustache = true;
        /// Rigid pieces on the head that make a character recognisable: "bun", "puffs", "glasses".
        public string[] Extras = System.Array.Empty<string>();

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
            Accent = new(0.75f, 0.60f, 0.28f), HairStyle = "pompadour", Accessories = new[] { "headset", "radio" }, Moustache = false,
        },
        new()
        {
            Name = "Brown apron, teal lanyard",
            Shirt = new(0.93f, 0.91f, 0.86f), Vest = new(0.30f, 0.24f, 0.20f), Trousers = new(0.15f, 0.15f, 0.17f),
            Shoes = new(0.18f, 0.15f, 0.13f), Skin = new(0.90f, 0.74f, 0.62f), Hair = new(0.62f, 0.45f, 0.25f),
            Accent = new(0.25f, 0.52f, 0.52f), HairStyle = "ponytail", Accessories = new[] { "apron", "lanyard" }, Moustache = false,
        },
        new()
        {
            Name = "Forest vest, rose pin",
            Shirt = new(0.91f, 0.92f, 0.88f), Vest = new(0.18f, 0.28f, 0.22f), Trousers = new(0.14f, 0.14f, 0.16f),
            Shoes = new(0.14f, 0.13f, 0.13f), Skin = new(0.74f, 0.55f, 0.42f), Hair = new(0.16f, 0.11f, 0.09f),
            Accent = new(0.78f, 0.45f, 0.50f), HairStyle = "beanie", Accessories = new[] { "belt", "one_glove" },
        },
    };

    // Named characters (Docs/CHARACTERS.md has the specification each one follows).

    /// The wedding coordinator (she/her in the game's text): dressed apart from the
    /// crew, dusty rose blazer, headset, hair tied back.
    public static readonly Costume Coordinator = new()
    {
        Name = "Coordinator",
        Shirt = new(0.94f, 0.92f, 0.88f), Vest = new(0.55f, 0.30f, 0.33f), Trousers = new(0.20f, 0.18f, 0.20f),
        Shoes = new(0.10f, 0.09f, 0.09f), Skin = new(0.80f, 0.62f, 0.50f), Hair = new(0.12f, 0.08f, 0.07f),
        Accent = new(0.85f, 0.75f, 0.55f), HairStyle = "ponytail", Accessories = new[] { "headset", "lanyard" },
        Moustache = false, Extras = new[] { "bun" },
    };

    /// The call center's floor supervisor (she/her in the game's text): navy
    /// cardigan, short grey-streaked hair, lanyard and a radio. Not the wedding coordinator.
    public static readonly Costume Supervisor = new()
    {
        Name = "Supervisor",
        Shirt = new(0.86f, 0.88f, 0.92f), Vest = new(0.16f, 0.22f, 0.36f), Trousers = new(0.24f, 0.24f, 0.27f),
        Shoes = new(0.10f, 0.09f, 0.09f), Skin = new(0.66f, 0.48f, 0.36f), Hair = new(0.42f, 0.40f, 0.40f),
        Accent = new(0.85f, 0.85f, 0.88f), HairStyle = "short", Accessories = new[] { "lanyard", "radio" },
        Moustache = false, Extras = new[] { "glasses" },
    };

    /// Dana, the senior agent on your pod who takes the technical calls (proposed: she/her).
    public static readonly Costume Dana = new()
    {
        Name = "Dana",
        Shirt = new(0.95f, 0.93f, 0.88f), Vest = new(0.70f, 0.50f, 0.18f), Trousers = new(0.18f, 0.18f, 0.22f),
        Shoes = new(0.15f, 0.12f, 0.11f), Skin = new(0.52f, 0.36f, 0.26f), Hair = new(0.08f, 0.06f, 0.06f),
        Accent = new(0.20f, 0.42f, 0.50f), HairStyle = "short", Accessories = new[] { "headset" },
        Moustache = false, Extras = new[] { "puffs" },
    };

    /// Omar, the cheerful one opposite you (proposed: he/him).
    public static readonly Costume Omar = new()
    {
        Name = "Omar",
        Shirt = new(0.88f, 0.92f, 0.96f), Vest = new(0.30f, 0.34f, 0.38f), Trousers = new(0.16f, 0.16f, 0.19f),
        Shoes = new(0.14f, 0.13f, 0.13f), Skin = new(0.70f, 0.52f, 0.40f), Hair = new(0.10f, 0.08f, 0.08f),
        Accent = new(0.80f, 0.35f, 0.25f), HairStyle = "pompadour", Accessories = new[] { "headset" },
        Moustache = true,
    };

    /// Kim, who is always on a break (pronouns not defined by the game).
    public static readonly Costume Kim = new()
    {
        Name = "Kim",
        Shirt = new(0.93f, 0.93f, 0.90f), Vest = new(0.20f, 0.36f, 0.26f), Trousers = new(0.16f, 0.16f, 0.18f),
        Shoes = new(0.14f, 0.13f, 0.13f), Skin = new(0.88f, 0.72f, 0.60f), Hair = new(0.80f, 0.45f, 0.50f),
        Accent = new(0.90f, 0.55f, 0.60f), HairStyle = "beanie", Accessories = new[] { "lanyard" },
        Moustache = false,
    };

    /// A named character's costume by worker id, or null for anyone generic.
    public static Costume Named(string workerId) => workerId switch
    {
        "npc_dana" => Dana,
        "npc_omar" => Omar,
        "npc_kim" => Kim,
        _ => null,
    };

    /// The landlord: yellowed vest top, a brown cardigan, grey hair, a belt doing a lot of work.
    public static readonly Costume Landlord = new()
    {
        Name = "Landlord",
        Shirt = new(0.86f, 0.82f, 0.66f), Vest = new(0.36f, 0.27f, 0.19f), Trousers = new(0.30f, 0.29f, 0.31f),
        Shoes = new(0.26f, 0.18f, 0.12f), Skin = new(0.82f, 0.62f, 0.52f), Hair = new(0.58f, 0.56f, 0.53f),
        Accent = new(0.40f, 0.30f, 0.18f), HairStyle = "short", Accessories = new[] { "belt" },
    };

    public static Costume ForSlot(int slot) => Variants[((slot % Variants.Length) + Variants.Length) % Variants.Length];
}
