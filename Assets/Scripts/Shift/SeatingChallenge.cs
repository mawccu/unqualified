using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Who a guest is and what they need from a seat.</summary>
public sealed class GuestProfile
{
    public enum Rule { Any, AwayFromSpeakers, With, NotWith, NearStage }

    public string Name;
    public Rule Need;
    public string Other;   // partner (With) or enemy (NotWith)
    public string Clue;    // as written in the guest book

    public override string ToString() => $"{Name} ({Need}{(Other != null ? " " + Other : "")})";
}

/// <summary>The facts about a table the rules care about.</summary>
public struct TableInfo
{
    public int Index;
    public bool NearSpeaker, NearStage;
    public int Seats;
}

/// <summary>
/// The seating puzzle: a small roster with three or four clear needs (keep her
/// away from the speakers, sit these two together, keep these two apart), all
/// written in the guest book and said by the guests themselves. Generated
/// rosters are always solvable with the tables and chairs that exist; a solver
/// checks it. A badly seated guest says exactly what is wrong.
/// </summary>
public static class SeatingChallenge
{
    static readonly string[] Names =
    {
        "Nan Wilhelmina", "Uncle Rob", "Aunt Carol", "Dev", "Priya", "Mr Okafor", "Cousin Lou", "Auntie Grace",
        "Dr Halvorsen", "Mrs Petrakis", "Big Sal", "Little Kev",
    };

    public static List<GuestProfile> Generate(int seed, IReadOnlyList<TableInfo> tables, int guestCount = 6)
    {
        var rng = new System.Random(seed);
        for (int attempt = 0; attempt < 50; attempt++)
        {
            var pool = Names.OrderBy(_ => rng.Next()).ToList();
            var roster = new List<GuestProfile>
            {
                new() { Name = pool[0], Need = GuestProfile.Rule.AwayFromSpeakers, Clue = "hearing aid. Keep her well away from the speakers." },
                new() { Name = pool[1], Need = GuestProfile.Rule.With, Other = pool[2], Clue = $"must sit with {pool[2]}." },
                new() { Name = pool[2], Need = GuestProfile.Rule.With, Other = pool[1], Clue = $"must sit with {pool[1]}." },
                new() { Name = pool[3], Need = GuestProfile.Rule.NotWith, Other = pool[4], Clue = $"NOT at the same table as {pool[4]}. (Don't ask.)" },
                new() { Name = pool[4], Need = GuestProfile.Rule.NotWith, Other = pool[3], Clue = $"NOT at the same table as {pool[3]}." },
                rng.Next(2) == 0
                    ? new() { Name = pool[5], Need = GuestProfile.Rule.NearStage, Clue = "wants to see the vows up close." }
                    : new() { Name = pool[5], Need = GuestProfile.Rule.Any, Clue = "happy anywhere." },
            };
            roster = roster.Take(guestCount).ToList();
            if (Solve(roster, tables) != null)
                return roster;
        }
        // Always solvable fallback: nobody minds.
        return Names.Take(guestCount).Select(n => new GuestProfile { Name = n, Need = GuestProfile.Rule.Any, Clue = "happy anywhere." }).ToList();
    }

    /// One valid assignment of guests to tables, or null if there is none.
    public static Dictionary<string, int> Solve(IReadOnlyList<GuestProfile> roster, IReadOnlyList<TableInfo> tables)
    {
        var assignment = new Dictionary<string, int>();
        var used = new int[tables.Count];
        return Search(0) ? assignment : null;

        bool Search(int i)
        {
            if (i == roster.Count)
                return true;
            for (int t = 0; t < tables.Count; t++)
            {
                if (used[t] >= tables[t].Seats)
                    continue;
                assignment[roster[i].Name] = t;
                used[t]++;
                if (Judge(roster[i], tables[t], assignment, roster, tables) == null && Search(i + 1))
                    return true;
                used[t]--;
                assignment.Remove(roster[i].Name);
            }
            return false;
        }
    }

    /// Null when the seat is fine; otherwise what the guest says about it.
    /// seated maps guest name to table index for everyone already sitting.
    public static string Judge(GuestProfile guest, TableInfo table, IReadOnlyDictionary<string, int> seated, IReadOnlyList<GuestProfile> roster, IReadOnlyList<TableInfo> tables)
    {
        switch (guest.Need)
        {
            case GuestProfile.Rule.AwayFromSpeakers when table.NearSpeaker:
                return "That's right beside the speaker! I'll be deaf by the toasts.";
            case GuestProfile.Rule.NearStage when !table.NearStage:
                return "I can't see a thing from here. I want to see the vows.";
            case GuestProfile.Rule.NotWith when seated.TryGetValue(guest.Other, out int enemyTable) && enemyTable == table.Index && enemyTable != -1:
                return $"I am NOT sitting with {guest.Other}.";
            case GuestProfile.Rule.With when seated.TryGetValue(guest.Other, out int partnerTable) && partnerTable != table.Index:
                return $"Where's {guest.Other}? We're supposed to be together.";
        }
        return null;
    }

    /// The guest book page.
    public static string BookText(IEnumerable<GuestProfile> roster)
    {
        var lines = new List<string> { "<size=22><b>Guest list</b></size>", "<size=12><color=#777777>Seating notes from the bride's mother</color></size>", "" };
        foreach (var g in roster)
            lines.Add($"<b>{g.Name}</b>  {g.Clue}");
        lines.Add("");
        lines.Add("<size=12><color=#777777>Tables nearest the stage have the speakers.</color></size>");
        return string.Join("\n", lines);
    }
}
