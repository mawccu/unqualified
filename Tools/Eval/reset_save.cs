var path = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "crew.json");
if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
UnityEngine.PlayerPrefs.DeleteKey("intro_seen");
UnityEngine.PlayerPrefs.Save();
return "save cleared: " + path;
