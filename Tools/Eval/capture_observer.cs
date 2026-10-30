// Renders an outside view in Play mode from Tools/Eval/.obs: "x y z yaw pitch name" -> Docs/Screens/<name>.png
var a = System.IO.File.ReadAllText("Tools/Eval/.obs").Trim().Split(' ');
UnityEngine.Camera cam = null;
foreach (var c in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Camera>())
    if (c.name == "PreviewCamera" && c.gameObject.scene.IsValid()) cam = c;
cam.gameObject.SetActive(true);
cam.transform.SetPositionAndRotation(new UnityEngine.Vector3(float.Parse(a[0]), float.Parse(a[1]), float.Parse(a[2])), UnityEngine.Quaternion.Euler(float.Parse(a[4]), float.Parse(a[3]), 0f));
var rt = new UnityEngine.RenderTexture(1280, 720, 24) { antiAliasing = 4 };
cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
var prev = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
var tex = new UnityEngine.Texture2D(1280, 720, UnityEngine.TextureFormat.RGB24, false);
tex.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
UnityEngine.RenderTexture.active = prev;
System.IO.File.WriteAllBytes("Docs/Screens/" + a[5] + ".png", tex.EncodeToPNG());
UnityEngine.Object.DestroyImmediate(tex); rt.Release();
cam.gameObject.SetActive(false);
return "Docs/Screens/" + a[5] + ".png";
