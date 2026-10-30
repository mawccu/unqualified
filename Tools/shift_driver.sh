#!/usr/bin/env bash
# Helpers for driving a shift from the shell: source this, then call step/status/cap.
export PATH="$PATH:/c/Users/USER/AppData/Local/Unity/bin"
PP="C:\Users\USER\unqualified-unity"
ucmd() { unity command "$@" --project-path "$PP" --caller plugin --skill unity-cli --no-banner --format json 2>&1; }
step() { echo "$*" > Tools/Eval/.step; ucmd script eval_file --file Tools/Eval/play_opening.cs | grep -o '"result": "[^|]*' | cut -c11-110; }
status() { ucmd script eval_file --file Tools/Eval/shift_status.cs | grep -o '"result": "[^"]*"' | cut -c12-; }
cap() { Tools/capture.sh "$1" >/dev/null; }
waitphase() { for i in $(seq 1 ${2:-60}); do r=$(status); case "$r" in *"phase $1"*) echo "reached $1"; return;; esac; sleep 1; done; echo "timeout waiting for $1: $r"; }
ccs() { ucmd script eval_file --file Tools/Eval/cc_status.cs | grep -o '"result": "[^"]*"' | cut -c12-; }
