using System.Collections;
using UnityEngine;

/// <summary>
/// Command-line options for testing a built game without playing up to a point:
///   -travel &lt;Scene&gt;   after the first scene loads, go to that scene (as the front door would)
///   -quit-after &lt;s&gt;   close the game after that many seconds
///   -call-check &lt;file&gt; in the call center: sit down, take an Arabic call alone,
///                       greet, and save a screenshot (checks the text shaping ships)
///   -fps-log            write the average frame rate to the log every 5 seconds
///                       (with -uncapped: vsync off, to see the real headroom)
/// Nothing happens without them.
/// </summary>
public static class LaunchOptions
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Apply()
    {
        string travel = Arg("-travel");
        string quit = Arg("-quit-after");
        string shot = Arg("-call-check");
        bool fps = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-fps-log") >= 0;
        if (travel == null && quit == null && !fps)
            return;
        var go = new GameObject("LaunchOptions");
        Object.DontDestroyOnLoad(go);
        var runner = go.AddComponent<Runner>();
        runner.StartCoroutine(Run(travel, quit != null && float.TryParse(quit, out var s) ? s : -1f));
        if (shot != null)
            runner.StartCoroutine(CallCheck(shot));
        if (fps)
            runner.StartCoroutine(FpsLog());
    }

    static IEnumerator Run(string travel, float quitAfter)
    {
        float start = Time.realtimeSinceStartup;
        if (travel != null)
        {
            yield return new WaitForSecondsRealtime(3f);
            Debug.Log($"[LaunchOptions] travelling to {travel}");
            GameSession.Instance?.TravelTo(travel);
        }
        if (quitAfter > 0f)
        {
            while (Time.realtimeSinceStartup - start < quitAfter)
                yield return null;
            Debug.Log("[LaunchOptions] quitting");
            Application.Quit();
        }
    }

    static IEnumerator FpsLog()
    {
        bool uncapped = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-uncapped") >= 0;
        while (true)
        {
            if (uncapped)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
            }
            int frames = 0, hitches = 0, catchUps = 0;
            float start = Time.realtimeSinceStartup, worst = 0f, last = start;
            // The first frame of a window carries the previous window's log write: not the game's.
            yield return null;
            start = last = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 5f)
            {
                yield return null;
                frames++;
                float now = Time.realtimeSinceStartup;
                worst = Mathf.Max(worst, now - last);
                if (now - last > 0.025f)
                    hitches++;
                // A long frame followed by a very short one is pacing jitter, not a dropped frame.
                if (now - last < 0.008f)
                    catchUps++;
                last = now;
            }
            float seconds = Time.realtimeSinceStartup - start;
            Debug.Log($"[fps] {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}: {frames / seconds:0} average, worst frame {worst * 1000f:0} ms, {hitches} frame(s) over 25 ms, {catchUps} under 8 ms");
        }
    }

    static IEnumerator CallCheck(string file)
    {
        while (CallCenterShift.Instance == null)
            yield return null;
        yield return new WaitForSecondsRealtime(2f);
        var shift = CallCenterShift.Instance;
        var session = GameSession.Instance;
        var desk = shift.Desks[0];
        session.Player.Teleport(desk.Seat.position - desk.Seat.forward * 0.5f);
        session.Submit(new InteractionRequest { RequestId = 900, ActorId = session.Player.ActorId, Action = InteractionAction.Use, TargetId = desk.EntityId, ExpectedRevision = -1 });
        yield return new WaitForSecondsRealtime(2f);
        foreach (var w in shift.Router.Workers)
            if (w.Simulated)
                shift.Router.SetBreak(w.Id, true);
        // Past the training call (it holds other languages back from a trainee).
        if (shift.TrainingCall != null)
        {
            shift.SkipTraining();
            yield return new WaitForSecondsRealtime(0.5f);
        }
        var call = shift.Router.Ring(CaseLibrary.Get("delivery_kettle"), CallLanguage.Arabic);
        yield return null;
        shift.Do(shift.PlayerId, "answer");
        shift.Do(shift.PlayerId, "greet");
        shift.Do(shift.PlayerId, "ask_order");
        yield return new WaitForSecondsRealtime(1.5f);
        ScreenCapture.CaptureScreenshot(file);
        Debug.Log($"[LaunchOptions] call check: call {call.Id} {call.State}, {call.Transcript.Count} lines, screenshot {file}");
    }

    static string Arg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name)
                return args[i + 1];
        return null;
    }

    sealed class Runner : MonoBehaviour { }
}
