// Renders the preview camera from the named views (Godot coords, camera yaw in degrees) to Docs/Screens.
var label = System.IO.File.Exists("Tools/Eval/.label") ? System.IO.File.ReadAllText("Tools/Eval/.label").Trim() : "shot";
var cam = UnityEngine.GameObject.Find("PreviewCamera").GetComponent<UnityEngine.Camera>();
var views = new (string name, UnityEngine.Vector3 godot, float yaw, float pitch)[] {
    ("hero", new UnityEngine.Vector3(-7f, 1.55f, 1.3f), 35.5f, 2f),
    ("stage", new UnityEngine.Vector3(0f, 1.55f, -1.5f), 0f, 4f),
    ("dessert", new UnityEngine.Vector3(8.5f, 1.55f, -2.5f), 60f, 8f),
    ("sound", new UnityEngine.Vector3(4f, 1.55f, -7f), 35f, 6f),
    ("backstage", new UnityEngine.Vector3(5f, 1.55f, 3.2f), -110f, 6f),
};
var sb = new System.Text.StringBuilder();
var oldPos = cam.transform.position; var oldRot = cam.transform.rotation;
foreach (var v in views) {
    cam.transform.position = new UnityEngine.Vector3(v.godot.x, v.godot.y, -v.godot.z);
    cam.transform.rotation = UnityEngine.Quaternion.Euler(v.pitch, v.yaw, 0f);
    var rt = new UnityEngine.RenderTexture(1600, 900, 24, UnityEngine.RenderTextureFormat.ARGB32);
    rt.antiAliasing = 4;
    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
    var prev = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(1600, 900, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
    UnityEngine.RenderTexture.active = prev;
    var path = "Docs/Screens/" + label + "_" + v.name + ".png";
    System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(tex); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    sb.AppendLine(path);
}
cam.transform.SetPositionAndRotation(oldPos, oldRot);
return sb.ToString();
