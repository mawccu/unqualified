// Renders the four worker models side by side (front, then side) to Docs/Screens/workers_*.png. Opens a temporary scene.
UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
var names = new[] { "worker_catering", "worker_host", "worker_runner", "worker_sound" };
for (int i = 0; i < names.Length; i++) {
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/Characters/" + names[i] + ".fbx");
    var go = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(asset);
    go.transform.position = new UnityEngine.Vector3(i * 0.9f - 1.35f, 0f, 0f);
    go.transform.rotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
}
var lightGo = new UnityEngine.GameObject("Light"); var l = lightGo.AddComponent<UnityEngine.Light>(); l.type = UnityEngine.LightType.Directional; l.intensity = 1.3f; lightGo.transform.rotation = UnityEngine.Quaternion.Euler(30f, 160f, 0f);
UnityEngine.RenderSettings.ambientLight = new UnityEngine.Color(0.5f, 0.5f, 0.5f);
var camGo = new UnityEngine.GameObject("Cam"); var cam = camGo.AddComponent<UnityEngine.Camera>(); cam.backgroundColor = new UnityEngine.Color(0.3f,0.32f,0.35f); cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor; cam.fieldOfView = 30f;
var sb = new System.Text.StringBuilder();
foreach (var (tag, pos, rot) in new[] { ("full", new UnityEngine.Vector3(0f, 1.0f, -5.2f), UnityEngine.Quaternion.Euler(3f, 0f, 0f)), ("faces", new UnityEngine.Vector3(0f, 1.55f, -2.6f), UnityEngine.Quaternion.Euler(2f, 0f, 0f)) }) {
    cam.transform.SetPositionAndRotation(pos, rot);
    var rt = new UnityEngine.RenderTexture(1600, 800, 24); cam.targetTexture = rt; cam.Render();
    UnityEngine.RenderTexture.active = rt; var tex = new UnityEngine.Texture2D(1600, 800, UnityEngine.TextureFormat.RGB24, false); tex.ReadPixels(new UnityEngine.Rect(0,0,1600,800),0,0); tex.Apply(); UnityEngine.RenderTexture.active = null; cam.targetTexture = null;
    System.IO.File.WriteAllBytes("Docs/Screens/workers_" + tag + ".png", tex.EncodeToPNG()); sb.AppendLine(tag);
}
return sb.ToString();
