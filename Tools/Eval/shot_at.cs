// Renders the open scene from a pose to Docs/Polish/After2/<name>.png. Tools/Eval/.shot: "name x y z yaw pitch [fov]".
var a = System.IO.File.ReadAllText("Tools/Eval/.shot").Trim().Split(' ');
var go = new UnityEngine.GameObject("ShotCam"); var cam = go.AddComponent<UnityEngine.Camera>();
cam.fieldOfView = a.Length > 6 ? float.Parse(a[6]) : 55f; cam.nearClipPlane = 0.02f;
go.transform.SetPositionAndRotation(new UnityEngine.Vector3(float.Parse(a[1]), float.Parse(a[2]), float.Parse(a[3])), UnityEngine.Quaternion.Euler(float.Parse(a[5]), float.Parse(a[4]), 0f));
var rt = new UnityEngine.RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render();
UnityEngine.RenderTexture.active = rt; var tex = new UnityEngine.Texture2D(1280, 720, UnityEngine.TextureFormat.RGB24, false);
tex.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); UnityEngine.RenderTexture.active = null;
cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
System.IO.Directory.CreateDirectory("Docs/Polish/After2");
System.IO.File.WriteAllBytes("Docs/Polish/After2/" + a[0] + ".png", tex.EncodeToPNG());
return "saved " + a[0];
