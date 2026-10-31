#!/usr/bin/env bash
# Rebuild Wedding.unity in the open Editor, bake its lighting and capture views.
# Usage: Tools/rebuild_and_bake.sh <label> [--no-bake]
# Screens land in Docs/Screens/<label>_<view>.png.
set -u
cd "$(dirname "$0")/.."
export PATH="$PATH:/c/Users/USER/AppData/Local/Unity/bin"
PP="$(pwd -W 2>/dev/null || pwd)"
LABEL="${1:-shot}"
ucmd() { unity command "$@" --project-path "$PP" --caller plugin --skill unity-cli --no-banner --format json 2>&1; }

ucmd script eval_file --file Tools/Eval/refresh.cs --timeout 600 >/dev/null
for i in $(seq 1 60); do
  st=$(ucmd script recompile_status | grep -o '"status": *"[a-z_]*"' | head -1)
  case "$st" in *completed*|*up_to_date*|*idle*) break;; esac; sleep 5
done
unity status --until-ready --project-path "$PP" --timeout 600 --no-banner --format json >/dev/null 2>&1
ucmd console get --level error --tail 5 | grep -o '"message": "[^"]\{0,240\}' | grep -v "asset refresh"
ucmd script eval_file --file Tools/Eval/build_venue.cs --timeout 1500 | grep -E '"result": "' | sed 's/\r\n/\n/g'

if [ "${2:-}" != "--no-bake" ]; then
  ucmd lighting bake >/dev/null
  start=$(date +%s)
  for i in $(seq 1 170); do
    s=$(ucmd lighting bake_status | grep -o '"status": *"[a-z]*"' | head -1)
    case "$s" in *completed*|*idle*) break;; esac; sleep 10
  done
  echo "bake: $s in $(( $(date +%s) - start ))s"
  # The bake links new lighting data into the scene: save it, or builds stop to ask.
  ucmd script eval_file --file Tools/Eval/save_scenes.cs >/dev/null
fi

mkdir -p Docs/Screens
ucmd script eval_file --file Tools/Eval/capture_views.cs --timeout 300 | grep -E '"result": "' | sed 's/\r\n/\n/g'
for f in Assets/Docs/Screens/*.png; do [ -e "$f" ] && mv -f "$f" Docs/Screens/; done
rm -rf Assets/Docs Assets/Docs.meta
