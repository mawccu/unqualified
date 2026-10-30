using System.Collections.Generic;

/// <summary>
/// Pure task logic: given which prop sits in which socket, work out how each of
/// the shift's essential outcomes is doing. No scene objects, no network, no
/// input, so it can be tested directly and every peer computes the same answer
/// from the same replicated socket state.
///
/// A task is placement based: each requirement says which prop belongs in which
/// socket. Anything else in that socket counts as wrong, not missing, so swapped
/// signs or a layer in the wrong order are visible mistakes the crew can notice
/// and fix, not blocked actions.
/// </summary>
public static class TaskBoard
{
    public enum Status { NotStarted, InProgress, NeedsFixing, Done, NotBuilt }

    public const string Dessert = "dessert";
    public const string Audio = "audio";
    public const string Seating = "seating";

    public sealed class Task
    {
        public string Id;
        public string Name;
        public string Goal;
        public string Unit;
        public Dictionary<string, string> Placements = new();
        public bool NotBuilt;
    }

    public readonly struct Result
    {
        public readonly string Id, Name, Goal, Text;
        public readonly Status Status;
        public readonly int Correct, Wrong, Total;

        public Result(Task task, Status status, int correct, int wrong, int total)
        {
            Id = task.Id;
            Name = task.Name;
            Goal = task.Goal;
            Status = status;
            Correct = correct;
            Wrong = wrong;
            Total = total;
            Text = StatusText(status, correct, wrong, total, task.Unit);
        }
    }

    /// The three essential outcomes of the Wedding Crew shift.
    public static List<Task> Catalog() => new()
    {
        new Task
        {
            Id = Dessert, Name = "Dessert display", Unit = "layers",
            Goal = "Stack the three cake layers on the stand, biggest at the bottom",
            Placements = { { "cake_socket_base", "cake_base" }, { "cake_socket_middle", "cake_middle" }, { "cake_socket_top", "cake_top" } },
        },
        new Task
        {
            Id = Audio, Name = "Ceremony audio", Unit = "steps",
            Goal = "Set up the sound desk for the ceremony", NotBuilt = true,
        },
        new Task
        {
            Id = Seating, Name = "Guest seating", Unit = "signs",
            Goal = "Put each table sign on its own table",
            Placements = { { "table_socket_1", "sign_1" }, { "table_socket_2", "sign_2" }, { "table_socket_3", "sign_3" } },
        },
    };

    /// occupancy maps socket id to the id of the prop sitting in it (absent when empty).
    public static List<Result> Evaluate(IReadOnlyList<Task> tasks, IReadOnlyDictionary<string, string> occupancy)
    {
        var results = new List<Result>(tasks.Count);
        foreach (var task in tasks)
            results.Add(EvaluateOne(task, occupancy));
        return results;
    }

    static Result EvaluateOne(Task task, IReadOnlyDictionary<string, string> occupancy)
    {
        int total = task.Placements.Count, correct = 0, wrong = 0;
        foreach (var pair in task.Placements)
        {
            if (!occupancy.TryGetValue(pair.Key, out var occupant) || string.IsNullOrEmpty(occupant))
                continue;
            if (System.Array.IndexOf(pair.Value.Split('|'), occupant) >= 0)
                correct++;
            else
                wrong++;
        }
        var status = Status.NotStarted;
        if (task.NotBuilt)
            status = Status.NotBuilt;
        else if (correct == total && total > 0)
            status = Status.Done;
        else if (wrong > 0)
            status = Status.NeedsFixing;
        else if (correct > 0)
            status = Status.InProgress;
        return new Result(task, status, correct, wrong, total);
    }

    public static string StatusText(Status status, int correct, int wrong, int total, string unit) => status switch
    {
        Status.Done => "Done",
        Status.NeedsFixing => $"{correct} of {total} {unit} right, {wrong} in the wrong place",
        Status.InProgress => $"{correct} of {total} {unit}",
        Status.NotBuilt => "Coming in the next build",
        _ => "Not started",
    };

    /// True when every built task is done.
    public static bool AllDone(IReadOnlyList<Result> results)
    {
        foreach (var result in results)
            if (result.Status != Status.Done && result.Status != Status.NotBuilt)
                return false;
        return true;
    }
}
