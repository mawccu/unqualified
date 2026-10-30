var pst = System.Type.GetType("UnityEditor.UIElements.UIToolkitProjectSettings, UnityEditor.UIElementsModule");
var prop = pst.GetProperty("enableAdvancedText", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
prop.SetValue(null, true);
var save = pst.GetMethod("Save", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
save?.Invoke(null, null);
return "advanced text: " + prop.GetValue(null) + (save != null ? " (saved)" : " (no save method)");
