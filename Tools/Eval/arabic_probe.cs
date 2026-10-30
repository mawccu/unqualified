// Renders sample Arabic and Spanish captions on the HUD overlay (play mode) for a visual check.
var hud = GameSession.Instance.Hud;
var box = new UnityEngine.UIElements.VisualElement();
box.style.position = UnityEngine.UIElements.Position.Absolute;
box.style.left = 300; box.style.top = 200; box.style.width = 700;
box.style.backgroundColor = new UnityEngine.Color(0, 0, 0, 0.8f);
box.style.paddingLeft = box.style.paddingRight = box.style.paddingTop = box.style.paddingBottom = 16;
string[] lines = {
  "مرحباً، طلبت غلاية كهربائية والطلب رقم 48213 لم يصل بعد.",
  "Hola, pedí un hervidor y el pedido 48213 todavía no ha llegado.",
  "Standard generator: مرحباً 48213",
};
for (int i = 0; i < lines.Length; i++) {
  var l = new UnityEngine.UIElements.Label(lines[i]);
  l.style.fontSize = 26; l.style.color = UnityEngine.Color.white; l.style.whiteSpace = UnityEngine.UIElements.WhiteSpace.Normal;
  if (i == 0) { l.style.unityTextGenerator = UnityEngine.TextGeneratorType.Advanced; l.languageDirection = UnityEngine.UIElements.LanguageDirection.RTL; l.style.unityTextAlign = UnityEngine.TextAnchor.MiddleRight; }
  if (i == 1) l.style.unityTextGenerator = UnityEngine.TextGeneratorType.Advanced;
  box.Add(l);
}
box.name = "ArabicProbe";
hud.Overlay.Add(box);
return "probe added";
