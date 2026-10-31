using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// Suspicion, seating and task rules, without a scene.
public class ShiftRulesTests
{
    static Suspicion MakeSuspicion(System.Func<Vector3, bool> canSee, List<Vector3> investigated)
    {
        var go = new GameObject("Coordinator");
        var s = go.AddComponent<Suspicion>();
        s.Bind(canSee, p => investigated?.Add(p));
        return s;
    }

    [Test]
    public void HiddenMistakesCostNothing()
    {
        var s = MakeSuspicion(_ => false, null);
        s.Report("spill_1", "a spill", Vector3.zero, 10f, true, () => true);
        for (int i = 0; i < 100; i++) s.Tick(0.1f);
        Assert.AreEqual(0f, s.Value, "not seen, not counted");
        Assert.IsFalse(s.Incidents[0].Witnessed);
        Object.DestroyImmediate(s.gameObject);
    }

    [Test]
    public void SeenMistakesCountOnceHoweverOftenReported()
    {
        var s = MakeSuspicion(_ => true, null);
        for (int i = 0; i < 5; i++)
            s.Report("cake_fell", "a cake on the floor", Vector3.zero, 10f, true, () => true);
        for (int i = 0; i < 20; i++) s.Tick(0.05f);
        Assert.AreEqual(1, s.Incidents.Count, "one incident, deduplicated by key");
        Assert.AreEqual(10f, s.Value, 1.5f, "its severity counted once (minus a little fading)");
        Object.DestroyImmediate(s.gameObject);
    }

    [Test]
    public void ANoiseOnlyMakesHerLook()
    {
        var looked = new List<Vector3>();
        var s = MakeSuspicion(_ => false, looked);
        s.Noise(new Vector3(3f, 0f, 0f), 10f);
        s.Noise(new Vector3(30f, 0f, 0f), 10f);
        s.Tick(0.1f);
        Assert.AreEqual(1, looked.Count, "she investigates sounds within earshot only");
        Assert.AreEqual(0f, s.Value, "a sound is not evidence");
        Object.DestroyImmediate(s.gameObject);
    }

    [Test]
    public void CleaningUpResolvesAndAttentionFades()
    {
        var s = MakeSuspicion(_ => true, null);
        bool there = true;
        s.Report("spill_1", "a spill", Vector3.zero, 20f, true, () => there);
        s.Tick(0.1f);
        Assert.Greater(s.Value, 19f);
        for (int i = 0; i < 100; i++) s.Tick(0.1f);
        Assert.Greater(s.Value, 11f, "witnessed major evidence holds a floor while it is there");
        there = false;
        for (int i = 0; i < 800; i++) s.Tick(0.1f);
        Assert.IsTrue(s.Incidents[0].Resolved, "cleaned up");
        Assert.AreEqual(0f, s.Value, 0.01f, "and the attention fades away");
        Object.DestroyImmediate(s.gameObject);
    }

    [Test]
    public void AStricterBossNoticesMoreAndForgetsSlower()
    {
        var easy = MakeSuspicion(_ => true, null);
        var strict = MakeSuspicion(_ => true, null);
        strict.Strictness = CvLies.Scrutiny(5);
        easy.Raise("lie", "a lie", 10f);
        strict.Raise("lie", "a lie", 10f);
        Assert.AreEqual(10f, easy.Value, 0.01f);
        Assert.AreEqual(16f, strict.Value, 0.01f, "five lies: 60% stricter");
        for (int i = 0; i < 50; i++) { easy.Tick(0.1f); strict.Tick(0.1f); }
        Assert.AreEqual(10f, easy.Value, 0.01f, "nothing fades during the quiet spell");
        for (int i = 0; i < 100; i++) { easy.Tick(0.1f); strict.Tick(0.1f); }
        Assert.Less(easy.Value, 10f, "then it fades");
        Assert.Greater(16f - strict.Value, 0f);
        Assert.Less(16f - strict.Value, 10f - easy.Value, "and the strict boss forgets more slowly");
        Object.DestroyImmediate(easy.gameObject);
        Object.DestroyImmediate(strict.gameObject);
    }

    [Test]
    public void MoreLiesPayMoreAndMakeItHarder()
    {
        Assert.AreEqual(0, CvLies.Count(0, 0, 0, 0), "the truth");
        Assert.AreEqual(7, CvLies.Count(2, 3, 2, 1), "senior title 2 + 15 years 2 + two skills + a language");
        Assert.AreEqual(140, CvLies.Pay(140, 0));
        Assert.Greater(CvLies.Pay(140, 3), CvLies.Pay(140, 1), "more lies, more pay");
        Assert.Less(CvLies.TimeScale(3), CvLies.TimeScale(1), "and less time");
        Assert.Greater(CvLies.Scrutiny(3), CvLies.Scrutiny(1), "and a stricter boss");
        Assert.GreaterOrEqual(CvLies.TimeScale(50), 0.7f, "never impossible");
        var crew = new CrewState { job = Jobs.CallCenter, cvTitle = CvLies.Titles[1], cvExperience = CvLies.Experience[2] };
        crew.claims.Add(Claims.Technical);
        crew.SetLanguages("peer_1", new[] { CallLanguage.Spanish });
        Assert.AreEqual(4, CvLies.Count(crew, "peer_1"), "title, experience, technical, Spanish");
        crew.job = Jobs.Wedding;
        Assert.AreEqual(2, CvLies.Count(crew, "peer_1"), "at a wedding the call center claim and the language do not count");
    }

    [Test]
    public void EnoughWitnessedTroubleExposesTheCrew()
    {
        var s = MakeSuspicion(_ => true, null);
        var last = Suspicion.Level.Calm;
        s.Changed += l => last = l;
        for (int i = 0; i < 5; i++)
            s.Report($"disaster_{i}", "a disaster", Vector3.zero, 22f, true, () => true);
        s.Tick(0.1f);
        Assert.AreEqual(Suspicion.Level.Exposed, s.State);
        Assert.AreEqual(Suspicion.Level.Exposed, last);
        Object.DestroyImmediate(s.gameObject);
    }

    static List<TableInfo> Tables() => Enumerable.Range(0, 8).Select(i => new TableInfo
    {
        Index = i, Seats = 6, NearSpeaker = i >= 6, NearStage = i == 0 || i == 2 || i >= 6,
    }).ToList();

    [Test]
    public void EveryGeneratedRosterIsSolvable()
    {
        var tables = Tables();
        for (int seed = 0; seed < 200; seed++)
        {
            var roster = SeatingChallenge.Generate(seed, tables);
            Assert.AreEqual(6, roster.Count);
            var plan = SeatingChallenge.Solve(roster, tables);
            Assert.IsNotNull(plan, $"seed {seed} has a valid seating");
            Assert.AreEqual(roster.Count, plan.Count);
        }
    }

    [Test]
    public void GuestsSayExactlyWhatIsWrong()
    {
        var tables = Tables();
        var nan = new GuestProfile { Name = "Nan", Need = GuestProfile.Rule.AwayFromSpeakers };
        var rob = new GuestProfile { Name = "Rob", Need = GuestProfile.Rule.NotWith, Other = "Carol" };
        var dev = new GuestProfile { Name = "Dev", Need = GuestProfile.Rule.With, Other = "Priya" };
        var none = new Dictionary<string, int>();
        StringAssert.Contains("speaker", SeatingChallenge.Judge(nan, tables[6], none, null, tables));
        Assert.IsNull(SeatingChallenge.Judge(nan, tables[1], none, null, tables));
        StringAssert.Contains("Carol", SeatingChallenge.Judge(rob, tables[3], new Dictionary<string, int> { { "Carol", 3 } }, null, tables));
        Assert.IsNull(SeatingChallenge.Judge(rob, tables[2], new Dictionary<string, int> { { "Carol", 3 } }, null, tables));
        StringAssert.Contains("Priya", SeatingChallenge.Judge(dev, tables[1], new Dictionary<string, int> { { "Priya", 4 } }, null, tables));
    }

    [Test]
    public void AnImpossibleRosterHasNoSolution()
    {
        var oneTable = new List<TableInfo> { new() { Index = 0, Seats = 6 } };
        var roster = new List<GuestProfile>
        {
            new() { Name = "A", Need = GuestProfile.Rule.NotWith, Other = "B" },
            new() { Name = "B", Need = GuestProfile.Rule.NotWith, Other = "A" },
        };
        Assert.IsNull(SeatingChallenge.Solve(roster, oneTable));
    }

    [Test]
    public void ASpareLayerCountsForTheCake()
    {
        var tasks = TaskBoard.Catalog();
        var dessert = tasks.First(t => t.Id == TaskBoard.Dessert);
        dessert.Placements["cake_socket_base"] = "cake_base|cake_base_spare";
        var results = TaskBoard.Evaluate(tasks, new Dictionary<string, string>
        {
            { "cake_socket_base", "cake_base_spare" }, { "cake_socket_middle", "cake_middle" }, { "cake_socket_top", "cake_top" },
        });
        Assert.AreEqual(TaskBoard.Status.Done, results.First(r => r.Id == TaskBoard.Dessert).Status, "the spare replaces a ruined bottom layer");
    }
}
