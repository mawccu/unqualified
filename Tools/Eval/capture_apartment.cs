// Renders the apartment's preview camera from a few views (Unity coords) to Docs/Screens/apt_<view>.png.
var cam = UnityEngine.GameObject.Find("PreviewCamera").GetComponent<UnityEngine.Camera>();
var views = new (string name, UnityEngine.Vector3 pos, float yaw, float pitch)[] {
    ("couch", new UnityEngine.Vector3(-2.65f, 1.6f, -1.75f), 105f, 4f),
    ("room", new UnityEngine.Vector3(3.2f, 1.6f, -2.3f), -60f, 6f),
    ("desk", new UnityEngine.Vector3(1.6f, 1.6f, -0.3f), 95f, 14f),
    ("door", new UnityEngine.Vector3(1.0f, 1.6f, 0.8f), 160f, 6f),
};
var sb = new System.Text.StringBuilder();
foreach (var v in views) {
    cam.transform.SetPositionAndRotation(v.pos, UnityEngine.Quaternion.Euler(v.pitch, v.yaw, 0f));
    var rt = new UnityEngine.RenderTexture(1600, 900, 24, UnityEngine.RenderTextureFormat.ARGB32) { antiAliasing = 4 };
    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
    var prev = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(1600, 900, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
    UnityEngine.RenderTexture.active = prev;
    var path = "Docs/Screens/apt_" + v.name + ".png";
    System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(tex); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    sb.AppendLine(path);
}
return sb.ToString();
