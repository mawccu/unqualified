using UnityEngine;

/// <summary>
/// How good the cake looks, not just whether it is there: each tier's line-up
/// on the one below and its condition, and the topper. Experts are judged on it
/// (and paid a presentation bonus for it); temps only need a standing cake.
/// </summary>
public static class CakeChallenge
{
    public struct Report
    {
        public int Placed, Total, Smeared, Ruined;
        public bool Topper, TopperRequired;
        public float Presentation; // 0..1
        public string Line;
    }

    public static readonly string[] Sockets = { "cake_socket_base", "cake_socket_middle", "cake_socket_top" };
    public const string TopperSocket = "cake_socket_topper";

    public static Report Evaluate(InteractionService service, bool expert)
    {
        var report = new Report { Total = Sockets.Length, TopperRequired = expert };
        float score = 0f;
        foreach (var id in Sockets)
        {
            var socket = service.GetSocket(id);
            var prop = socket != null ? socket.OccupantProp : null;
            if (prop == null)
                continue;
            report.Placed++;
            float condition = prop.State switch
            {
                PickupProp.Condition.Smeared => 0.6f,
                PickupProp.Condition.Ruined => 0.15f,
                _ => 1f,
            };
            if (prop.State == PickupProp.Condition.Smeared) report.Smeared++;
            if (prop.State == PickupProp.Condition.Ruined) report.Ruined++;
            // Line-up counts for half: a slightly off-centre tier still looks like a cake.
            score += condition * (0.5f + 0.5f * prop.PlacementQuality);
        }
        var topper = service.GetSocket(TopperSocket);
        report.Topper = topper != null && !string.IsNullOrEmpty(topper.OccupantId);
        report.Presentation = score / report.Total * (expert && !report.Topper ? 0.85f : 1f);
        report.Line = report.Placed < report.Total ? $"{report.Placed} of {report.Total} layers"
            : report.Ruined > 0 ? "Standing, but a layer is ruined"
            : report.Smeared > 0 ? "Standing, frosting smeared"
            : expert && !report.Topper ? "Needs the topper"
            : $"Looks {Words(report.Presentation)}";
        return report;
    }

    public static string Words(float presentation) => presentation switch
    {
        >= 0.9f => "perfect",
        >= 0.75f => "lovely",
        >= 0.55f => "fine",
        _ => "a bit wonky",
    };
}
