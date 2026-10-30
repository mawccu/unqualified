var root = GameSession.Instance.Hud.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
var sv = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.ScrollView>(root, "computer-page-scroll");
sv.scrollOffset = new UnityEngine.Vector2(0f, float.Parse(System.IO.File.ReadAllText("Tools/Eval/.scroll").Trim()));
return "scrolled to " + sv.scrollOffset.y + " of " + sv.contentContainer.layout.height;
