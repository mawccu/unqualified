#!/usr/bin/env bash
# Rebuild Apartment.unity in the open Editor, bake its lighting and save it.
cd "$(dirname "$0")/.."
source Tools/shift_driver.sh
unity recompile --project-path "$PP" --no-banner 2>&1 | grep -v "^severity" | head -20
sleep 5; unity status --until-ready --project-path "$PP" --timeout 600 --no-banner --format json >/dev/null 2>&1
ucmd script eval_file --file Tools/Eval/build_apartment.cs --timeout 900 | python -c "import json,sys; d=json.load(sys.stdin); print((d.get(\"data\") or {}).get(\"result\") or d)"
ucmd lighting bake >/dev/null
for i in $(seq 1 100); do s=$(ucmd lighting bake_status | grep -o '"status": *"[a-z]*"' | head -1); case "$s" in *completed*|*idle*) break;; esac; sleep 6; done
echo "bake: $s"
# The bake links new lighting data into the scene: save it, or builds stop to ask.
ucmd script eval_file --file Tools/Eval/save_scenes.cs | grep -o '"result": "[^"]*"'
