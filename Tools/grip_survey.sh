#!/usr/bin/env bash
# For each prop id: pick it up, measure the hands on it, photograph first person and
# from the side, then put it down. grip_survey.sh <tag> <id>... (session already running)
cd "$(dirname "$0")/.."
source Tools/shift_driver.sh
tag=$1; shift
mkdir -p Docs/Polish/Grips/survey
for id in "$@"; do
  step "drop" >/dev/null
  ucmd script eval --code 'var p = GameSession.Instance.Player; p.ScriptedMove = null; return "ok";' >/dev/null
  step "near $id 0.7" >/dev/null; sleep 0.2
  r=$(step "pickup $id"); sleep 1.2
  ucmd script eval --code 'var p = GameSession.Instance.Player; p.SetLook(p.transform.eulerAngles.y, 8f); return "ok";' >/dev/null; sleep 0.5
  echo "measure" > Tools/Eval/.grip
  echo "$id: $r :: $(ucmd script eval_file --file Tools/Eval/grip_probe.cs | grep -o '"result": "[^"]*"')"
  for v in fp side; do echo "shot survey/${tag}_${id}_$v $v" > Tools/Eval/.grip; ucmd script eval_file --file Tools/Eval/grip_probe.cs >/dev/null; done
done
step "drop" >/dev/null
