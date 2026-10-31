#!/usr/bin/env bash
# Both jobs, apartment -> shift -> payout -> home, twice each, then a reload from
# disk, through the real flow (the front door, the shift, the payout screen's
# button). Uses a throwaway save: back the real one up first (the caller does).
#   bash Tools/job_cycle.sh <call_center|wedding>
cd "$(dirname "$0")/.."
source Tools/shift_driver.sh
job=$1
state() { ucmd script eval_file --file Tools/Eval/cycle_state.cs | grep -o '"result": "[^"]*"' | sed 's/"result": //'; }
waitfor() {  # waitfor <substring> <seconds>
  for i in $(seq 1 "$2"); do s=$(state); case "$s" in *"$1"*) echo "  $s"; return 0;; esac; sleep 1; done
  echo "  TIMEOUT waiting for '$1': $s"; return 1
}
ucmd editor.playmode stop >/dev/null; sleep 2
ucmd script eval --code "CrewState.StartOver(); var c = CrewState.Current; c.billReceived = true; c.hired = true; c.job = \"$job\"; c.introSeen = true; c.Save(); UnityEngine.PlayerPrefs.SetInt(\"intro_seen\", 1); UnityEditor.SceneManagement.EditorSceneManager.OpenScene(\"Assets/Scenes/Apartment.unity\"); return \"fresh save, job $job\";" | grep -o '"result": "[^"]*"'
ucmd editor.playmode start >/dev/null; sleep 12
for round in 1 2; do
  echo "== round $round"
  waitfor "stage Hired" 30 || exit 1
  step "near front_door 0.8" >/dev/null; step "use front_door" | cut -c1-60
  if [ "$job" = "call_center" ]; then
    waitfor "phase calls:" 40 || exit 1
    ucmd script eval --code 'var s = CallCenterShift.Instance; s.PlayerSatDown(); if (s.TrainingCall != null) s.SkipTraining(); UnityEngine.Time.timeScale = 20f; return "fast";' >/dev/null
  else
    waitfor "phase wedding:" 40 || exit 1
    ucmd script eval --code 'UnityEngine.Time.timeScale = 20f; return "fast";' >/dev/null
  fi
  waitfor "Payout" 120 || waitfor "Exposed" 5 || exit 1
  ucmd script eval --code 'UnityEngine.Time.timeScale = 1f; return "normal";' >/dev/null
  sleep 1
  step "click Go home and rest" | cut -c1-60
  waitfor "scene Apartment" 30 || exit 1
done
echo "== reload from disk"
before=$(state)
ucmd editor.playmode stop >/dev/null; sleep 2
ucmd script eval --code 'var f = typeof(CrewState).GetField("_current", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static); f.SetValue(null, null); var c = CrewState.Current; return $"loaded: day {c.day} savings {c.savings} shifts {c.shiftsCompleted} job {c.job} training {c.callTrainingDone}";' | grep -o '"result": "[^"]*"'
ucmd editor.playmode start >/dev/null; sleep 12
waitfor "stage Hired" 30
echo "  before reload: $before"
