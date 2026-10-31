using System;
using System.Collections.Generic;

/// <summary>A number of one part (storage, an order line, a reservation).</summary>
[Serializable]
public class PartStack
{
    public string part = "";
    public int count;

    public PartStack() { }
    public PartStack(string id, int n)
    {
        part = id;
        count = n;
    }
}

/// <summary>
/// A paid order. Its id is made when the cart is checked out and is the only
/// key the delivery and the parcel use, so a crash or a second click can never
/// charge, deliver or unpack it twice.
/// </summary>
[Serializable]
public class PartOrder
{
    public const string Paid = "paid";
    public const string Delivered = "delivered";
    public const string Opened = "opened";

    public string id = "";
    public string from = "Questionable Components";
    public List<PartStack> lines = new();
    public int total;
    public int day;
    /// paid (on its way) -> delivered (a parcel at the door) -> opened (parts in storage).
    public string state = Paid;
}

/// <summary>One finished device. It belongs to the crew; equipping reserves it for one player.</summary>
[Serializable]
public class GadgetInstance
{
    public string id = "";
    public string recipe = "";
    /// 0 flat to 1 full.
    public float charge = 1f;
    /// The player who packed it for the next shift ("" when it sits on the rack).
    public string equippedBy = "";
    public int builtDay;
    /// Times it was seen in use at work (for the rack's history line).
    public int timesSeen;

    public bool Equipped => !string.IsNullOrEmpty(equippedBy);
}

/// <summary>
/// The unfinished build on the bench: which recipe, the parts reserved for it
/// (taken out of storage), and how far the hands have got. Leaving the bench,
/// the flat or the game keeps it; cancelling gives the parts back once.
/// </summary>
[Serializable]
public class BuildState
{
    public string recipe = "";
    /// Taken out of storage for this build. Completing consumes them; cancelling returns them.
    public List<string> reserved = new();
    /// Where each placed part sits: "socket=part" (bench socket ids).
    public List<string> placed = new();
    public int screws;
    public bool lidClosed;
    /// Who is working on it right now. Not saved: a reload or a disconnect frees it.
    [NonSerialized] public string editor = "";

    public bool Active => !string.IsNullOrEmpty(recipe);

    public string PartIn(string socket)
    {
        string prefix = socket + "=";
        foreach (var p in placed)
            if (p.StartsWith(prefix, StringComparison.Ordinal))
                return p.Substring(prefix.Length);
        return "";
    }

    public void SetPlaced(string socket, string part)
    {
        placed.RemoveAll(p => p.StartsWith(socket + "=", StringComparison.Ordinal));
        if (!string.IsNullOrEmpty(part))
            placed.Add(socket + "=" + part);
    }
}
