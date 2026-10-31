# Polish pass: delivery (v0.5.0)

This pass finishes the current experience. It adds no new features except the requested window. The full issue list, with evidence and status, is in `Docs/POLISH_AUDIT.md`. It is not a release-readiness claim: the "Not covered" section of the audit lists what wasn't checked.

## What changed

**Physical defects**
- **Floating props, fixed at the source.** `Assets/Editor/ColliderFit.cs` replaces three copies of "one box around the model". The kitchen counter (in the flat, the wedding kitchen and the office), the couch, the office sofa and the beds now collide with their own shape, so nothing rests on an invisible lid above a worktop or seat.
  - The takeout box and the wedding cloth now rest where they belong.
  - The wedding's order slips had been placed past the counter's edge, so they were moved onto it.
- **Wedding start.** You no longer begin facing a wall; you face the coordinator walking up.

**Look**
- **Beds.** Two new original models: a mattress on a pallet, a fitted sheet, a pillow and a rumpled duvet. One is made, one slept in. Matte fabric replaces the chrome blob.
- **Floor.** The seams are dark gaps instead of bright lines.
- **Walls.** The flat's lightmap is sharper and quieter (36 texels per metre, 4096 indirect samples). A faint smudge remains (PA-08).
- **Window light.** Daylight comes from a window-sized area light, so things near the window are no longer lit white.

**Screens**
- **Scale.** The UI is drawn 20% larger, from a 1600x900 reference.
- **HUD.** The card shows the objective and one line; Tab shows the details. It steps aside while the computer, the phone console or a workshop sheet is open.
- **CV.** The lie meter comes first; only the claim you just ticked explains itself.
- **Shop.** Cards are short and the page fits one screen.
- **Phone console.** Finishing a call is always in view, replies are grouped, and what you've learned is ticked. Arabic still reads right to left.

**The window**
- A real opening with a sliding sash, a fixed sash and a grille.
- The hand takes the handle and the sash slides.
- An alley outside, with street sound that grows as it opens.
- It won't close on something left on the sill, and ignores presses mid-slide.

**Tests**
- `PolishTests` (3 new) check that nothing floats in the flat or the wedding, and that the window works.
- 57 EditMode and 28 PlayMode tests, all passing.

## Evidence

- **Screenshots.** `Docs/Polish/Before` and `Docs/Polish/After`, taken from the same positions.
- **Comparison sheet.** `Docs/Polish/before_after.png`, six pairs.
- **Motion.** `Docs/Polish/window_motion.gif` (the window, slowed to a fifth of its speed); `Docs/Polish/Motion/` has the frames.
- **Real build.** The console in the actual build at 1920x1080 and 1280x720: `After/build_console_1080p.png` and `After/build_console_720p.png`.

## Performance (measured)

Measured on the v0.5.0 build: i5-12400F, RTX 5060, 1920x1080 fullscreen, default quality, vsync on, a 60 Hz monitor. Each scene ran for 25 to 30 s while its shift was running.

| Scene | Average | Long frames (over 25 ms) per 5 s | Short frames (under 8 ms) per 5 s |
|---|---|---|---|
| Flat | 60 | 6 to 38 | the same number |
| Wedding | 60 | 8 to 30 | the same number |
| Call center | 60 | 2 to 12 | the same number |

Every long frame is paired with a short one, and the average holds at exactly 60, so no frames are dropped. It's timing jitter on the CPU side; it was not traced further with GPU tools.

With vsync off, the worst frames were 17 to 21 ms. No exceptions appeared in any build log.

## Remaining limitations

- No multiplayer exists, so none was tested.
- Controller navigation, UI scale settings and narrow windows were not tested.
- No new animation or sound review was done beyond the window: the landlord, typing, the handset and footsteps.
- A faint lightmap smudge remains on the flat's kitchen wall.
- The rent bill shows "OVERDUE" on day 1.
- Licences for the older imported asset sets need confirming before any public release (`Docs/ASSET_REGISTER.md`).
- The Spanish and Arabic lines are still not reviewed by native speakers.
