// Windows build for testers: Builds/Windows/UNQUALIFIED.exe
// Save first: a modified open scene makes the build stop and ask (and hang this call).
UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
UnityEditor.PlayerSettings.productName = "UNQUALIFIED";
UnityEditor.PlayerSettings.companyName = "mawccu";
UnityEditor.PlayerSettings.bundleVersion = System.IO.File.ReadAllText("Tools/Eval/.version").Trim();
UnityEditor.PlayerSettings.fullScreenMode = UnityEngine.FullScreenMode.FullScreenWindow;
var scenes = new[] { "Assets/Scenes/Apartment.unity", "Assets/Scenes/Wedding.unity", "Assets/Scenes/CallCenter.unity" };
UnityEditor.EditorBuildSettings.scenes = System.Array.ConvertAll(scenes, s => new UnityEditor.EditorBuildSettingsScene(s, true));
var options = new UnityEditor.BuildPlayerOptions { scenes = scenes, locationPathName = "Builds/Windows/UNQUALIFIED.exe", target = UnityEditor.BuildTarget.StandaloneWindows64, options = UnityEditor.BuildOptions.None };
var report = UnityEditor.BuildPipeline.BuildPlayer(options);
var s2 = report.summary;
if (s2.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) System.IO.File.Copy("Tools/TESTERS.txt", "Builds/Windows/READ ME FIRST.txt", true);
var errors = new System.Text.StringBuilder();
foreach (var step in report.steps) foreach (var m in step.messages) if (m.type == UnityEngine.LogType.Error) errors.Append(m.content.Substring(0, System.Math.Min(200, m.content.Length)) + " || ");
return $"{s2.result} in {s2.totalTime.TotalSeconds:0}s, {s2.totalSize / 1048576} MB, errors {s2.totalErrors} {errors}";
