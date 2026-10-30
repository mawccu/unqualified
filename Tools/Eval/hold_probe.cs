var p = GameSession.Instance.Player; var held = p.HeldProp; if (held == null) return "nothing held";
var sock = GameSession.Instance.Service.GetSocket(System.IO.File.ReadAllText("Tools/Eval/.sock").Trim());
sock.LandingFor(held, out float q);
var off = held.Centre - sock.transform.position; off.y = 0;
var cam = p.Camera.transform;
var spotLocal = cam.InverseTransformPoint(sock.transform.position);
return $"q {q*100:0}% off {off.magnitude:0.000} m | hold local {p.HoldPoint.localPosition} mag-from-chest {(p.HoldPoint.localPosition - new UnityEngine.Vector3(0,-0.25f,0)).magnitude:0.00} | spot local {spotLocal} | eye y {cam.position.y:0.00} spot y {sock.transform.position.y:0.00} | player {p.transform.position}";
