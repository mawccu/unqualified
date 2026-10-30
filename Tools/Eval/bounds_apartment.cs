var sb = new System.Text.StringBuilder();
foreach (var n in new[]{"desk","computer_monitor","computer_keyboard","couch","desk_chair","rent_bill","door_leaf"}) {
  var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/Apartment/"+n+".fbx");
  foreach (var f in go.GetComponentsInChildren<UnityEngine.MeshFilter>()) {
    var b = f.sharedMesh.bounds;
    sb.AppendLine($"{n}/{f.name}: local pos {f.transform.localPosition} rot {f.transform.localEulerAngles} scale {f.transform.localScale} mesh min {b.min} max {b.max} verts {f.sharedMesh.vertexCount}");
  }
}
return sb.ToString();
