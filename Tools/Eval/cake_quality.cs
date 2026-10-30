var s = GameSession.Instance.Service;
var parts = new System.Collections.Generic.List<string>();
foreach (var id in new[] { "cake_base", "cake_middle", "cake_top" }) { var p = s.GetProp(id); parts.Add($"{id}: socket '{p.SocketId}' quality {p.PlacementQuality * 100f:0}%"); }
return string.Join(" | ", parts);
