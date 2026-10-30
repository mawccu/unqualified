#!/usr/bin/env bash
# Capture the live Game view (HUD included) to Docs/Screens/<name>.png.
cd "$(dirname "$0")/.."
export PATH="$PATH:/c/Users/USER/AppData/Local/Unity/bin"
PP="$(pwd -W 2>/dev/null || pwd)"
unity command capture game_view --project-path "$PP" --caller plugin --skill unity-cli --no-banner --format json -- --source screen --save_path "Docs/Screens/$1.png" >/dev/null 2>&1
mkdir -p Docs/Screens
for f in Assets/Docs/Screens/*.png; do [ -e "$f" ] && mv -f "$f" Docs/Screens/; done
rm -rf Assets/Docs Assets/Docs.meta
ls "Docs/Screens/$1.png"
