using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Wedding Crew shift: the task list, who is assigned what, the
/// coordinator's welcome, and the pay at the end. What you claimed on the CV
/// decides your assignments, and the coordinator says it to your face.
/// </summary>
public class WeddingShift : MonoBehaviour
{
    const float TaskRefresh = 0.2f;

    [SerializeField] GameSession session;
    [SerializeField] Coordinator coordinator;
    [SerializeField] Transform greetSpot;

    readonly List<TaskBoard.Task> _tasks = TaskBoard.Catalog();
    List<TaskBoard.Result> _results = new();
    readonly HashSet<string> _assigned = new();
    float _timer;
    bool _paid;

    public IReadOnlyList<TaskBoard.Result> Results => _results;
    public ICollection<string> Assigned => _assigned;

    public void Configure(GameSession owner, Coordinator npc, Transform greet)
    {
        session = owner;
        coordinator = npc;
        greetSpot = greet;
    }

    void Start()
    {
        var crew = CrewState.Current;
        foreach (var claim in crew.claims)
        {
            string task = Claims.TaskFor(claim);
            if (task != null)
                _assigned.Add(task);
        }
        session.Hud.SetHelp("WASD move    Mouse look    Shift sprint    E pick up    Click place    Q drop    Esc menu    F3 frame rate");
        if (coordinator != null && greetSpot != null)
            coordinator.Greet(session.Player.transform, greetSpot, GreetingLines(crew), (line, seconds) => session.Hud.ShowSubtitle("Coordinator", line, seconds));
        Refresh();
    }

    /// The welcome, built from the CV. At least one line repeats a claim back.
    public static List<(string pose, string line, float seconds)> GreetingLines(CrewState crew)
    {
        var lines = new List<(string, string, float)>();
        bool cake = crew.HasClaim(Claims.Cake);
        bool seating = crew.HasClaim(Claims.Seating);
        if (cake)
            lines.Add((WorkerAvatar.Point, "You're the cake expert, right? Good. That's yours.", 3.2f));
        if (seating)
            lines.Add((cake ? WorkerAvatar.Talk : WorkerAvatar.Point,
                cake ? "And seating coordinator too, it says. The table signs are also yours." : "You're our seating coordinator? Perfect. Table signs, all yours.", 3.6f));
        if (crew.HasClaim(Claims.Pressure))
            lines.Add((WorkerAvatar.Talk, "\"Works well under pressure.\" Wonderful. Tonight is nothing but pressure.", 3.4f));
        if (crew.Truthful)
            lines.Add((WorkerAvatar.Point, "You're the temp. No experience, it says. Just... carry things. Carefully.", 3.6f));
        lines.Add((WorkerAvatar.Talk, "Cake on the stand, signs on the tables. I'll be watching.", 3f));
        return lines;
    }

    void Update()
    {
        _timer += Time.unscaledDeltaTime;
        if (_timer < TaskRefresh)
            return;
        _timer = 0f;
        Refresh();
    }

    void Refresh()
    {
        var crew = CrewState.Current;
        _results = TaskBoard.Evaluate(_tasks, session.Service.Occupancy());
        session.Hud.SetTasks($"WEDDING CREW   ·   DAY {crew.day}", _results, _assigned);
        if (coordinator != null)
            session.Hud.SetStatus($"<color=#F2C88C>Coordinator</color>   {coordinator.State}");
        if (!_paid && TaskBoard.AllDone(_results))
            Pay(crew);
    }

    void Pay(CrewState crew)
    {
        _paid = true;
        crew.savings += CrewState.WeddingShiftPay;
        crew.shiftsCompleted++;
        crew.Save();
        session.Hud.ShowBanner("SHIFT COMPLETE",
            $"+${CrewState.WeddingShiftPay}   ·   ${crew.savings} saved of ${crew.rentDue} rent\nNobody suspects a thing. Yet.");
    }
}
