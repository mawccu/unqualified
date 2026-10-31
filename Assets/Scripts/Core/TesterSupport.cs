using System.IO;
using UnityEngine;

/// <summary>
/// Help for people testing builds: finding the log to send, and being told on
/// the next start when the game crashed last time (the log of that run is kept
/// as Player-prev.log next to the new one).
/// </summary>
public static class TesterSupport
{
    /// Where Player.log and the save live (the same folder on Windows).
    public static string LogFolder => Application.persistentDataPath;

    public static void OpenLogFolder() => Application.OpenURL("file:///" + LogFolder.Replace('\\', '/'));

    static System.Collections.IEnumerator HideLater(Hud hud)
    {
        yield return new WaitForSecondsRealtime(12f);
        hud.HideBanner();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CheckLastRun()
    {
        if (Application.isEditor)
            return;
        try
        {
            string previous = Path.Combine(LogFolder, "Player-prev.log");
            if (!File.Exists(previous) || !File.ReadAllText(previous).Contains("Crash!!!"))
                return;
            var session = GameSession.Instance;
            if (session == null)
                return;
            session.Hud.ShowBanner("THE GAME CRASHED LAST TIME",
                "Sorry. Esc, then \"Report a problem\" opens the folder: send Player-prev.log to the developer.");
            session.StartCoroutine(HideLater(session.Hud));
            Debug.Log("[tester] the previous run crashed; its log is Player-prev.log");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[tester] could not read the previous log: {e.Message}");
        }
    }
}
