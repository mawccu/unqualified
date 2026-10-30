// A top-down plan of the wedding venue (ceilings hidden) to Docs/Screens/plan.png.
var hidden = new System.Collections.Generic.List<UnityEngine.Renderer>();
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None))
    if (r.name.Contains("Ceiling") || r.bounds.min.y > 2.8f) { if (r.enabled) { r.enabled = false; hidden.Add(r); } }
var go = new UnityEngine.GameObject("PlanCam");
var cam = go.AddComponent<UnityEngine.Camera>();
cam.orthographic = true; cam.orthographicSize = 17.5f;
go.transform.SetPositionAndRotation(new UnityEngine.Vector3(-1.2f, 30f, 0.5f), UnityEngine.Quaternion.Euler(90f, 0f, 0f));
cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor; cam.backgroundColor = UnityEngine.Color.black;
var rt = new UnityEngine.RenderTexture(1800, 1700, 24) { antiAliasing = 4 };
cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
var prev = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
var tex = new UnityEngine.Texture2D(1800, 1700, UnityEngine.TextureFormat.RGB24, false);
tex.ReadPixels(new UnityEngine.Rect(0, 0, 1800, 1700), 0, 0); tex.Apply(); UnityEngine.RenderTexture.active = prev;
System.IO.File.WriteAllBytes("Docs/Screens/plan.png", tex.EncodeToPNG());
UnityEngine.Object.DestroyImmediate(go); rt.Release();
foreach (var r in hidden) r.enabled = true;
return "plan";
