using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public const int CallCenterShiftPay = 120;

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
    /// The job the crew was hired for (Jobs ids). Saves from before there was a
    /// choice have it empty, which means the wedding.
    public string job = "";
    /// Each player's own language claims (the CV is shared, languages are not).
    /// Older saves have none: everyone speaks English only.
    public List<LanguageProfile> languageProfiles = new();

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
    public bool Truthful => claims.Count == 0 && languageProfiles.TrueForAll(p => p.languages.Count == 0);
    public string Job => string.IsNullOrEmpty(job) ? Jobs.Wedding : job;

    /// The languages (besides English) a player claimed. Never null.
    public List<string> LanguagesOf(string playerId)
    {
        languageProfiles ??= new List<LanguageProfile>();
        var profile = languageProfiles.Find(p => p.playerId == playerId);
        return profile != null ? profile.languages : new List<string>();
    }

    public void SetLanguages(string playerId, IEnumerable<string> languages)
    {
        languageProfiles ??= new List<LanguageProfile>();
        var profile = languageProfiles.Find(p => p.playerId == playerId);
        if (profile == null)
        {
            profile = new LanguageProfile { playerId = playerId };
            languageProfiles.Add(profile);
        }
        profile.languages = new List<string>();
        foreach (var id in languages)
            if (System.Array.IndexOf(CallLanguage.Claimable, id) >= 0 && !profile.languages.Contains(id))
                profile.languages.Add(id);
    }

    /// Reads a save, filling in anything an older version did not write.
    public static CrewState FromJson(string json)
    {
        var crew = JsonUtility.FromJson<CrewState>(json) ?? new CrewState();
        crew.claims ??= new List<string>();
        crew.languageProfiles ??= new List<LanguageProfile>();
        crew.job ??= "";
        return crew;
    }

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
                return FromJson(File.ReadAllText(SavePath));
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

[Serializable]
public class LanguageProfile
{
    public string playerId = "";
    public List<string> languages = new();
}

/// <summary>The jobs on the job site that actually have a shift behind them.</summary>
public static class Jobs
{
    public const string Wedding = "wedding";
    public const string CallCenter = "call_center";

    public static string Scene(string job) => job == CallCenter ? GameSession.CallCenterScene : GameSession.WeddingScene;
    public static string Title(string job) => job == CallCenter ? "Call Center Support Agent" : "Wedding Crew Staff";
    public static string Place(string job) => job == CallCenter ? "Brightline Home Support, 3rd floor" : "Grand Hotel Ballroom";
}

/// <summary>
/// The lies a CV can tell. Each one maps to something the shift actually does
/// with it; flavour claims are marked as such.
/// </summary>
public static class Claims
{
    public const string Cake = "claim_cake";
    public const string Seating = "claim_seating";
    public const string Audio = "claim_audio";
    public const string Pressure = "claim_pressure";
    public const string Technical = "claim_technical";
    public const string Service = "claim_service";

    public static readonly (string id, string text, string consequence)[] Wedding =
    {
        (Cake, "Professional wedding cake specialist", "You will build the cake to a high standard, topper included. Bonus for a beautiful cake."),
        (Seating, "Experienced seating coordinator", "You will seat every guest yourself. Bonus if they are all happy."),
        (Audio, "Audio technician", "You will wire the sound desk and hit the ceremony music cue. Bonus for a clean cue."),
        (Pressure, "I work well under pressure", "Expect an extra rush job on top. Bonus if you get it done."),
    };

    public static readonly (string id, string text, string consequence)[] CallCenter =
    {
        (Technical, "Technical support expert", "The hard technical calls are routed to you. Bonus for each one you fix."),
        (Service, "Award-winning customer service", "Bonus for calm, happy customers. The supervisor listens in on you more."),
        (Pressure, "I work well under pressure", "Expect a rush of extra calls. Bonus if you clear them."),
    };

    /// Every claim, for looking up its text.
    public static readonly (string id, string text, string consequence)[] All = Wedding.Concat(CallCenter.Where(c => c.id != Pressure)).ToArray();

    public static (string id, string text, string consequence)[] For(string job) => job == Jobs.CallCenter ? CallCenter : Wedding;

    /// The task each claim makes yours (TaskBoard ids).
    public static string TaskFor(string claim) => claim switch
    {
        Cake => TaskBoard.Dessert,
        Seating => TaskBoard.Seating,
        Audio => TaskBoard.Audio,
        _ => null,
    };
}
