var list = new System.Collections.Generic.List<string>();
foreach (var g in UnityEngine.Object.FindObjectsByType<Guest>(UnityEngine.FindObjectsSortMode.None)) list.Add($"{g.Profile.Name} {g.State} at {g.transform.position.ToString("F1")}");
return string.Join(" | ", list);
