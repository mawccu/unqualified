#!/usr/bin/env bash
# A skilled Wedding Crew shift with every claim, driven through the real input path.
# Run from the project folder with the Editor open on Wedding.unity and play mode stopped.
source Tools/shift_driver.sh
ucmd script eval_file --file Tools/Eval/set_cv.cs >/dev/null
ucmd console clear >/dev/null
ucmd editor.playmode start >/dev/null
unity status --until-ready --project-path "$PP" --timeout 120 --no-banner --format json >/dev/null 2>&1
step timescale 3 >/dev/null; waitphase Prep 40; step timescale 1 >/dev/null
# The cake, layer by layer, lined up by aiming at the middle of the spot.
for layer in base middle top; do
  step near cake_$layer 0.95 >/dev/null; step press >/dev/null; sleep 0.6
  step goto 11.88 5.0 90 20 >/dev/null; step aimat cake_socket_$layer >/dev/null; sleep 1.0
  [ "$layer" = "middle" ] && cap skill_01_lineup
  step place cake_socket_$layer
  sleep 0.4
done
step near cake_topper 0.9 >/dev/null; step press >/dev/null; sleep 0.5
step goto 11.88 5.0 90 20 >/dev/null; step aimat cake_socket_topper >/dev/null; sleep 0.6; step place cake_socket_topper
sleep 0.5; status | cut -c1-170
step goto 10.4 3.8 55 6 >/dev/null; sleep 0.4; cap skill_02_cake
