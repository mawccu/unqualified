using UnityEngine;

/// <summary>
/// The reference desk: one binder per help page (Deliveries, Smart plug lights,
/// DustBot lights, Refunds), each standing in its own slot with its spine label
/// toward the room. Pick one up to read it (the page shows while it is in your
/// hand); put it back in its slot. Each binder is one object with one place, so
/// they never pile into each other. The pages are the console's help pages.
/// </summary>
public class ReferenceShelf : MonoBehaviour
{
    [SerializeField] PickupProp[] binders;
    [SerializeField] PlacementSocket[] slots;

    public void Configure(PickupProp[] books, PlacementSocket[] places)
    {
        binders = books;
        slots = places;
    }

    void Start()
    {
        // Every binder starts standing in its own slot.
        for (int i = 0; i < binders.Length && i < slots.Length; i++)
        {
            if (!string.IsNullOrEmpty(binders[i].HolderId) || !string.IsNullOrEmpty(slots[i].OccupantId))
                continue;
            slots[i].SetOccupant(binders[i]);
            binders[i].PlaceAt(slots[i].SnapPosition, slots[i].transform.rotation, slots[i].EntityId);
        }
    }

    /// The page of a held binder, or null when it is not one.
    public static string PageFor(PickupProp held)
    {
        if (held == null || !held.Kind.StartsWith("binder_"))
            return null;
        string title = held.Kind.Substring("binder_".Length);
        foreach (var (pageTitle, body) in CaseLibrary.HelpPages)
            if (Slug(pageTitle) == title)
                return $"<size=13><color=#888888>BRIGHTLINE · AGENT REFERENCE</color></size>\n<size=22><b>{pageTitle}</b></size>\n\n{body}\n\n<size=12><color=#888888>Put it back in its slot on the reference desk.</color></size>";
        return null;
    }

    public static string Slug(string title) => title.ToLowerInvariant().Replace(' ', '_');
}
