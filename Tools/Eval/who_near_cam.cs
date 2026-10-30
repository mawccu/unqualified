var cam = GameSession.Instance.Player.Camera.transform;
var list = new System.Collections.Generic.List<string>();
foreach (var a in UnityEngine.Object.FindObjectsByType<WorkerAvatar>(UnityEngine.FindObjectsSortMode.None))
{
    var head = a.FindBone("Head") ?? a.transform;
    var local = cam.InverseTransformPoint(head.position);
    list.Add($"{a.transform.parent?.name}/{a.name} head local {local} dist {local.magnitude:0.00} state {a.CurrentState}");
}
return "cam " + cam.position + " | " + string.Join(" | ", list);
