var sb = new System.Text.StringBuilder();
foreach (var n in new[]{"floor_tile_2m","wall_panel_2m","guest_table","guest_chair"}) {
  var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/Models/"+n+".fbx");
  foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>()) {
    sb.Append(n+"/"+r.name+": ");
    foreach (var m in r.sharedMaterials) sb.Append(m.name+" "+m.GetColor("_BaseColor")+(m.GetTexture("_BaseMap")!=null?" tex":"")+"; ");
    sb.AppendLine();
  }
}
foreach (var t in new[]{"wood_planks","wallpaper","fabric"}) {
  var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/Art/Textures/"+t+"_albedo.png");
  var rt = UnityEngine.RenderTexture.GetTemporary(1,1); UnityEngine.Graphics.Blit(tex, rt);
  var prev = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
  var px = new UnityEngine.Texture2D(1,1); px.ReadPixels(new UnityEngine.Rect(0,0,1,1),0,0); UnityEngine.RenderTexture.active = prev; UnityEngine.RenderTexture.ReleaseTemporary(rt);
  sb.AppendLine(t+" avg-ish "+px.GetPixel(0,0));
}
return sb.ToString();
