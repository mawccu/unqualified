// Renders the workshop from a few views with the preview camera to Docs/Screens/ws_<view>.png.
var cam = UnityEngine.GameObject.Find("PreviewCamera").GetComponent<UnityEngine.Camera>();
var views = new (string name, UnityEngine.Vector3 pos, float yaw, float pitch)[] {
    ("entry", new UnityEngine.Vector3(2.6f, 1.6f, -2.2f), 90f, 12f),
    ("bench", new UnityEngine.Vector3(5.75f, 1.55f, -1.15f), 90f, 35f),
    ("mat", new UnityEngine.Vector3(5.95f, 1.45f, -1.15f), 90f, 55f),
    ("rack", new UnityEngine.Vector3(5.4f, 1.6f, -1.2f), 180f, 25f),
    ("shelf", new UnityEngine.Vector3(5.5f, 1.5f, -1.4f), 0f, 12f),
    ("door", new UnityEngine.Vector3(2.4f, 1.6f, -0.8f), 200f, 20f),
};
var sb = new System.Text.StringBuilder();
cam.gameObject.SetActive(true);
foreach (var v in views) {
    cam.transform.SetPositionAndRotation(v.pos, UnityEngine.Quaternion.Euler(v.pitch, v.yaw, 0f));
    var rt = new UnityEngine.RenderTexture(1600, 900, 24, UnityEngine.RenderTextureFormat.ARGB32) { antiAliasing = 4 };
    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
    var prev = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(1600, 900, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
    UnityEngine.RenderTexture.active = prev;
    var path = "Docs/Screens/ws_" + v.name + ".png";
    System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(tex); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    sb.AppendLine(path);
}
return sb.ToString();
