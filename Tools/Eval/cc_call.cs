var c = CallCenterShift.Instance.Router.Call(int.Parse(System.IO.File.ReadAllText("Tools/Eval/.call").Trim()));
return c == null ? "none" : $"{c} keys {string.Join(",", c.Transcript.Select(l => l.Speaker[0] + ":" + l.Key))}";
