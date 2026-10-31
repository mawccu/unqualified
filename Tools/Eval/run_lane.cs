// Faces the player down the longest clear line at chest height, gives back a fresh cake layer, and starts a scripted sprint.
var s = GameSession.Instance; var p = s.Player;
float best = 0f; float bestYaw = 0f;
for (int i = 0; i < 36; i++) {
    float yaw = i * 10f; var dir = UnityEngine.Quaternion.Euler(0f, yaw, 0f) * UnityEngine.Vector3.forward;
    float d = UnityEngine.Physics.SphereCast(p.transform.position + UnityEngine.Vector3.up * 1.0f, 0.4f, dir, out var hit, 20f, ~0, UnityEngine.QueryTriggerInteraction.Ignore) ? hit.distance : 20f;
    if (d > best) { best = d; bestYaw = yaw; }
}
p.SetLook(bestYaw, 10f);
if (s.Service.GetProp(p.HeldPropId) == null) {
    var prop = System.Linq.Enumerable.First(s.Service.Props, x => x.Kind == "cake_tier" && string.IsNullOrEmpty(x.SocketId) && string.IsNullOrEmpty(x.HolderId));
    s.Service.GiveProp(prop, p);
}
p.ScriptedMove = new UnityEngine.Vector2(0f, 1f); p.ScriptedSprint = true;
return "lane " + best.ToString("0.0") + " m at " + bestYaw + " held " + p.HeldPropId + " kit on " + GadgetKit.Instance.On;
