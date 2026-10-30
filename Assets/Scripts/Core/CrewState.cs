using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Where the crew is in their life: money, rent, the day, and the lies on the
/// CV. Survives scene travel and is saved to disk, so a repeat visit can skip the
/// landlord and the shift can call back the claims. Claims are stored as ids,
/// never as display text. Once online, the host owns this and sends it to peers.
/// </summary>
[Serializable]
public class CrewState
{
    /// Proposed numbers (configurable): six shifts in seven days pay the rent.
    public const int StartingRent = 840;
    public const int RentDays = 7;
    public const int WeddingShiftPay = 140;

    public int day = 1;
    public int savings;
    public int rentDue = StartingRent;
    public int daysLeft = RentDays;
    public bool introSeen;
    public bool billReceived;
    public bool hired;
    public string cvName = "";
    public string cvTitle = "";
    public string cvExperience = "";
    public List<string> claims = new();
    public int shiftsCompleted;

    static CrewState _current;

    public static CrewState Current
    {
        get
        {
            if (_current == null)
                _current = Load();
            return _current;
        }
    }

    static string SavePath => Path.Combine(Application.persistentDataPath, "crew.json");

    public bool HasClaim(string id) => claims.Contains(id);
    public bool Truthful => claims.Count == 0;

    public void Save()
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(this, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not save the crew state: {e.Message}");
        }
    }

    static CrewState Load()
    {
        try
        {
            if (File.Exists(SavePath))
                return JsonUtility.FromJson<CrewState>(File.ReadAllText(SavePath)) ?? new CrewState();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not read the crew state, starting fresh: {e.Message}");
        }
        return new CrewState();
    }

    /// A brand new life: back to the couch, the landlord, an empty CV.
    public static void StartOver()
    {
        _current = new CrewState();
        _current.Save();
    }

    public string MoneyLine() => $"DAY {day}   ·   ${savings} SAVED   ·   RENT ${rentDue} DUE IN {daysLeft} DAYS";
}

/// <summary>
/// The lies a CV can tell. Each one maps to something the shift actually does
/// with it; flavour claims are marked as such.
/// </summary>
public static class Claims
{
    public const string Cake = "claim_cake";
    public const string Seating = "claim_seating";
    public const string Pressure = "claim_pressure"; // flavour only for now: changes a line, no system behind it

    public static readonly (string id, string text, string consequence)[] All =
    {
        (Cake, "Professional wedding cake specialist", "You will own the cake display."),
        (Seating, "Experienced seating coordinator", "You will own the table signs and seating."),
        (Pressure, "I work well under pressure", "Nobody will go easy on you."),
    };

    /// The task each claim makes yours (TaskBoard ids).
    public static string TaskFor(string claim) => claim switch
    {
        Cake => TaskBoard.Dessert,
        Seating => TaskBoard.Seating,
        _ => null,
    };
}
