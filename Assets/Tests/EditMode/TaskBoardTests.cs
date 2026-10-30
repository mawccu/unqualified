using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// Ported from the Godot tests/task_board_test.gd.
public class TaskBoardTests
{
    static readonly List<TaskBoard.Task> Tasks = TaskBoard.Catalog();

    static TaskBoard.Result Get(List<TaskBoard.Result> results, string id) => results.Single(r => r.Id == id);

    static List<TaskBoard.Result> Eval(params (string socket, string prop)[] placed) =>
        TaskBoard.Evaluate(Tasks, placed.ToDictionary(p => p.socket, p => p.prop));

    [Test]
    public void EmptyRoom()
    {
        var empty = Eval();
        Assert.AreEqual(TaskBoard.Status.NotStarted, Get(empty, TaskBoard.Dessert).Status);
        Assert.AreEqual(TaskBoard.Status.NotStarted, Get(empty, TaskBoard.Seating).Status);
        Assert.AreEqual(TaskBoard.Status.NotBuilt, Get(empty, TaskBoard.Audio).Status, "audio is reported as not built yet");
        Assert.IsFalse(TaskBoard.AllDone(empty));
    }

    [Test]
    public void OneRightLayerIsInProgress()
    {
        var one = Eval(("cake_socket_base", "cake_base"));
        Assert.AreEqual(TaskBoard.Status.InProgress, Get(one, TaskBoard.Dessert).Status);
        Assert.AreEqual("1 of 3 layers", Get(one, TaskBoard.Dessert).Text);
    }

    [Test]
    public void CakeInOrderIsDoneButNotTheShift()
    {
        var done = Eval(("cake_socket_base", "cake_base"), ("cake_socket_middle", "cake_middle"), ("cake_socket_top", "cake_top"));
        Assert.AreEqual(TaskBoard.Status.Done, Get(done, TaskBoard.Dessert).Status);
        Assert.AreEqual("Done", Get(done, TaskBoard.Dessert).Text);
        Assert.IsFalse(TaskBoard.AllDone(done), "cake alone does not finish the shift");
    }

    [Test]
    public void LayersInTheWrongOrderNeedFixing()
    {
        var swapped = Eval(("cake_socket_base", "cake_middle"), ("cake_socket_middle", "cake_base"));
        var dessert = Get(swapped, TaskBoard.Dessert);
        Assert.AreEqual(TaskBoard.Status.NeedsFixing, dessert.Status);
        Assert.AreEqual(2, dessert.Wrong);
        Assert.AreEqual(0, dessert.Correct);
    }

    [Test]
    public void SwappedSignsSayWhatIsWrong()
    {
        var signs = Eval(("table_socket_1", "sign_2"), ("table_socket_2", "sign_1"), ("table_socket_3", "sign_3"));
        var seating = Get(signs, TaskBoard.Seating);
        Assert.AreEqual(TaskBoard.Status.NeedsFixing, seating.Status);
        Assert.AreEqual(1, seating.Correct);
        Assert.AreEqual(2, seating.Wrong);
        Assert.AreEqual("1 of 3 signs right, 2 in the wrong place", seating.Text);
    }

    [Test]
    public void EverythingBuiltDoneFinishesTheShift()
    {
        var all = Eval(("cake_socket_base", "cake_base"), ("cake_socket_middle", "cake_middle"), ("cake_socket_top", "cake_top"),
            ("table_socket_1", "sign_1"), ("table_socket_2", "sign_2"), ("table_socket_3", "sign_3"));
        Assert.AreEqual(TaskBoard.Status.Done, Get(all, TaskBoard.Seating).Status);
        Assert.IsTrue(TaskBoard.AllDone(all), "cake and seating done, audio not built");
    }

    [Test]
    public void APropInAnUnknownSocketChangesNothing()
    {
        var stray = Eval(("somewhere_else", "sign_1"));
        Assert.AreEqual(TaskBoard.Status.NotStarted, Get(stray, TaskBoard.Seating).Status);
    }

    [Test]
    public void FixingTheOrderRemovesTheWrongCount()
    {
        var fixedUp = Eval(("cake_socket_base", "cake_base"), ("cake_socket_middle", "cake_middle"));
        Assert.AreEqual(0, Get(fixedUp, TaskBoard.Dessert).Wrong);
        Assert.AreEqual(2, Get(fixedUp, TaskBoard.Dessert).Correct);
    }

    [Test]
    public void CatalogHasThreeOutcomes() => Assert.AreEqual(3, Tasks.Count);
}
