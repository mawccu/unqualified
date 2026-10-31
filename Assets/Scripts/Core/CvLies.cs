using System.Collections.Generic;

/// <summary>
/// The core trade of the game: every lie on the CV pays more and makes the shift
/// harder. Lies are counted the same way for every job: each skill claim, each
/// language claimed (where the job uses languages), an inflated title and an
/// inflated experience line. The count sets the pay grade and the heat: less
/// time, more work, a stricter boss who notices more and forgets less, and less
/// patient customers. The CV shows the trade before you send it.
/// </summary>
public static class CvLies
{
    public static readonly string[] Titles = { "Between opportunities", "Customer Experience Professional", "Senior Hospitality Consultant" };
    public static readonly string[] Experience = { "None", "6 months (a friend's party)", "5 years", "15 years, at least" };

    /// How much of a lie each title and experience line is (index-matched).
    static readonly int[] TitleLies = { 0, 1, 2 };
    static readonly int[] ExperienceLies = { 0, 1, 1, 2 };

    public const float PayPerLie = 0.12f;
    public const float ScrutinyPerLie = 0.12f;
    public const float TimePerLie = 0.035f;
    public const float PatiencePerLie = 0.06f;

    public static int Count(int title, int experience, int claims, int languages) =>
        TitleLies[Clamp(title, TitleLies.Length)] + ExperienceLies[Clamp(experience, ExperienceLies.Length)] + claims + languages;

    /// The lies on the saved CV for this player, for the job they were hired for.
    public static int Count(CrewState crew, string playerId)
    {
        int title = System.Array.IndexOf(Titles, crew.cvTitle);
        int experience = System.Array.IndexOf(Experience, crew.cvExperience);
        int claims = 0;
        foreach (var (id, _, _) in Claims.For(crew.Job))
            if (crew.HasClaim(id))
                claims++;
        int languages = crew.Job == Jobs.CallCenter ? crew.LanguagesOf(playerId).Count : 0;
        return Count(title < 0 ? 0 : title, experience < 0 ? 0 : experience, claims, languages);
    }

    /// Pay grade: base pay times this.
    public static float PayMultiplier(int lies) => 1f + PayPerLie * lies;

    /// The boss's strictness: suspicion gained times this, faded away divided by it.
    public static float Scrutiny(int lies) => 1f + ScrutinyPerLie * lies;

    /// Time to work with (prep time, the gaps between calls) times this.
    public static float TimeScale(int lies) => System.Math.Max(0.7f, 1f - TimePerLie * lies);

    /// How fast customers lose patience, times this.
    public static float Impatience(int lies) => 1f + PatiencePerLie * lies;

    public static int Pay(int basePay, int lies) => (int)System.Math.Round(basePay * PayMultiplier(lies));

    public static string Tier(int lies) => lies switch
    {
        0 => "Honest",
        <= 2 => "Stretching it",
        <= 4 => "Bold",
        <= 6 => "Outrageous",
        _ => "Total fraud",
    };

    /// What the heat means in plain words, for the CV and the briefing.
    public static List<string> Effects(int lies, string job)
    {
        var lines = new List<string>();
        if (lies == 0)
        {
            lines.Add("Basic pay, basic expectations. Nobody watches you closely.");
            return lines;
        }
        int timeCut = (int)System.Math.Round((1f - TimeScale(lies)) * 100f);
        int stricter = (int)System.Math.Round((Scrutiny(lies) - 1f) * 100f);
        lines.Add(job == Jobs.CallCenter ? $"Calls come {timeCut}% faster; customers run out of patience {(int)System.Math.Round((Impatience(lies) - 1f) * 100f)}% sooner." : $"{timeCut}% less time to get the venue ready.");
        lines.Add($"Your boss is {stricter}% stricter: mistakes count for more and are forgotten slower.");
        return lines;
    }

    static int Clamp(int i, int length) => i < 0 ? 0 : i >= length ? length - 1 : i;
}
