// The named cast through the runtime path (WorkerAvatar.Create + costume), side by side, to Docs/Screens/cast_*.png.
UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
var lib = UnityEditor.AssetDatabase.LoadAssetAtPath<WorkerLibrary>("Assets/Settings/Workers.asset");
if (lib == null) foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:WorkerLibrary")) lib = UnityEditor.AssetDatabase.LoadAssetAtPath<WorkerLibrary>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
var cast = new (string name, WorkerCostumes.Costume costume, int slot)[] { ("Landlord", WorkerCostumes.Landlord, 0), ("Coordinator", WorkerCostumes.Coordinator, 0), ("Supervisor", WorkerCostumes.Supervisor, 0), ("Dana", WorkerCostumes.Dana, 1), ("Omar", WorkerCostumes.Omar, 2), ("Kim", WorkerCostumes.Kim, 3) };
for (int i = 0; i < cast.Length; i++) {
    var holder = new UnityEngine.GameObject(cast[i].name).transform;
    holder.position = new UnityEngine.Vector3(i * 0.85f - 2.1f, 0f, 0f); holder.rotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
    WorkerAvatar.Create(lib, cast[i].slot, cast[i].costume, holder);
}
var lightGo = new UnityEngine.GameObject("Light"); var l = lightGo.AddComponent<UnityEngine.Light>(); l.type = UnityEngine.LightType.Directional; l.intensity = 1.3f; lightGo.transform.rotation = UnityEngine.Quaternion.Euler(30f, 160f, 0f);
UnityEngine.RenderSettings.ambientLight = new UnityEngine.Color(0.55f, 0.55f, 0.55f);
var camGo = new UnityEngine.GameObject("Cam"); var cam = camGo.AddComponent<UnityEngine.Camera>(); cam.backgroundColor = new UnityEngine.Color(0.3f,0.32f,0.35f); cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor; cam.fieldOfView = 30f;
// Front, then the same heads turned three-quarter and from behind (front-only checks hid floating pieces).
foreach (var (tag, pos, yaw) in new[] { ("full", new UnityEngine.Vector3(0f, 1.0f, -6.5f), 180f), ("faces", new UnityEngine.Vector3(0f, 1.5f, -3.4f), 180f), ("3q", new UnityEngine.Vector3(0f, 1.5f, -3.4f), 140f), ("back", new UnityEngine.Vector3(0f, 1.5f, -3.4f), 0f) }) {
    foreach (var c in cast) UnityEngine.GameObject.Find(c.name).transform.rotation = UnityEngine.Quaternion.Euler(0f, yaw, 0f);
    cam.transform.SetPositionAndRotation(pos, UnityEngine.Quaternion.Euler(2f, 0f, 0f));
    var rt = new UnityEngine.RenderTexture(1800, 700, 24); cam.targetTexture = rt; cam.Render();
    UnityEngine.RenderTexture.active = rt; var tex = new UnityEngine.Texture2D(1800, 700, UnityEngine.TextureFormat.RGB24, false); tex.ReadPixels(new UnityEngine.Rect(0,0,1800,700),0,0); tex.Apply(); UnityEngine.RenderTexture.active = null; cam.targetTexture = null;
    System.IO.File.WriteAllBytes("Docs/Screens/cast_" + tag + ".png", tex.EncodeToPNG());
}
return "ok " + (lib != null);
