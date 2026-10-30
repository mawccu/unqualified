var s = UnityEngine.Object.FindFirstObjectByType<GameSession>();
var p = s.Player;
p.Teleport(new UnityEngine.Vector3(-0.55f, 0.05f, -5.0f));
p.SetLook(0f, 27f);
return $"player at {p.transform.position} cam {p.Camera.transform.position} fov {p.Camera.fieldOfView:F1} coordinator: {UnityEngine.Object.FindFirstObjectByType<Coordinator>().transform.position}";
