// One line of progression state for Tools/job_cycle.sh: scene, the apartment's
// stage, the shift's phase, and the save's day, money and counters.
var crew = CrewState.Current;
string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
var opening = OpeningDirector.Instance;
string phase = CallCenterShift.Instance != null ? "calls:" + CallCenterShift.Instance.Current
    : UnityEngine.Object.FindFirstObjectByType<WeddingShift>() is WeddingShift w ? "wedding:" + w.Current : "-";
bool travelling = GameSession.Instance != null && GameSession.Instance.Travelling;
return $"scene {scene} | stage {(opening != null ? opening.Stage.ToString() : "-")} | phase {phase} | travelling {travelling} | day {crew.day} savings {crew.savings} shifts {crew.shiftsCompleted} job {crew.job} hired {crew.hired} training {crew.callTrainingDone} rentDue {crew.rentDue} daysLeft {crew.daysLeft}";
