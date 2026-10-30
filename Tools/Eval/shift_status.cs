var shift = UnityEngine.Object.FindFirstObjectByType<WeddingShift>();
var s = GameSession.Instance;
var mind = shift.Mind;
var cake = CakeChallenge.Evaluate(s.Service, CrewState.Current.HasClaim(Claims.Cake));
var guests = string.Join(", ", System.Linq.Enumerable.Select(Guest.All, g => g.Profile.Name + ":" + g.State + (g.Satisfied ? "+" : "")));
var witnessed = string.Join("; ", System.Linq.Enumerable.Select(mind.Suspicion.Witnessed, i => i.What + (i.Resolved ? " (resolved)" : "")));
return $"phase {shift.Current} left {shift.PhaseLeft:0} | suspicion {mind.Suspicion.Value:0} {mind.Suspicion.State} '{mind.Doing}' | cake {cake.Line} {cake.Presentation:0.00} | guests [{guests}] | seen [{witnessed}] | held {(s.Player.HeldProp != null ? s.Player.HeldProp.EntityId + " wobble " + s.Player.HeldProp.Wobble.ToString("0.00") : "-")} | savings {CrewState.Current.savings}";
