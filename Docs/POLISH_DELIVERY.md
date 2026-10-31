# Quality gate: delivery (v0.6.0)

Brief: `Docs/CLAUDE_EXISTING_LEVELS_QUALITY_GATE.md`, with
`Docs/CLAUDE_GRIPS_AND_CALL_CENTER_USABILITY.md`. No new levels or features. Every
issue, with how to reproduce it, its cause, the fix and the retest, is in
`Docs/POLISH_AUDIT.md` (QG-01 to QG-23, remaining QG-R1 to R5), together with the
room-by-room checklist. This is not a release-readiness claim.

## Exit gates

| Gate | Verdict | Evidence |
|---|---|---|
| Reported defects | **Pass** | All verified in the running game: tray grips (QG-03), merged books (QG-09), board and window (QG-08), Dana and the cast (QG-10), translation (QG-11), call controls (QG-12). |
| Scene quality | **Pass, with listed remainders** | Room-by-room checklist for the flat, the wedding and the call center (audit, "Inspected areas"). Placement scans after each rebuild; every flag explained or fixed. Remaining: QG-R1 to R3 (small grip details) and PA-19 (bill stamp). |
| Interaction quality | **Pass, with listed remainders** | Every grip family measured on the rendered hands and tested within 6 mm, through movement, for the tray, the chair and the cake (13 `GripTests`). Interruptions tested: wrong slot, drop, reload, skipped training, End on an unfixed call. Remaining: QG-R1 and QG-R2. |
| UI quality | **Partial** | The phone rail is on the right, in fixed places, and End call is visible at 1280x720 and 1920x1080 in the build (Console/build_call_*). The case and conversation columns scroll; Arabic is shaped right to left. **Not tested: keyboard focus order.** Only the pointer was used. |
| Gameplay clarity | **Partial (no human playtest)** | The training call and ordinary calls were solved in play using only what the screen shows (Console/t1 to t7, s1 to s6; walkthrough below). The cues never name the remedy (test). No first-time player has tried it. |
| Progression | **Pass** | `Tools/job_cycle.sh` for each job: apartment, front door, shift, payout, "Go home and rest", apartment, twice each, then a reload from disk with identical state. Call center: day 3, $240, 2 shifts. Wedding: day 3, $140, 2 shifts. |
| Reliability | **Pass** | A P0 found during the pass and fixed: at a low frame rate, landing sent the camera off into the sky (QG-01). No save, money or progression faults. No exceptions in the build logs or the Editor console from the play sessions. 72 EditMode and 45 PlayMode tests pass. |
| Reviewability | **Pass** | Audit with stable ids and origins. Matched before and after views (`before_after_gate.png`, Before2 and After2). Grip motion (`Grips/tray_motion_*.gif`, `Grips/motion/`). Call flows (`Console/training_call_flow.gif`, `ordinary_call_flow.gif`). Binder flow (`After2/binder_flow.gif`). |

Two gates are partial, so the milestone is not fully met: keyboard focus is untested and nobody new has played the calls. Both are listed for the user's review rather than called passed.

## What changed

**Hands and carrying** (`Docs/GRIPS.md`)
- **In view.** Everything carried is placed so the hand holding it is in view at the current field of view; before, it was all below the frame.
- **Grip profiles:**
  - paper held at its edges;
  - flat paper pinched;
  - books on both palms with the spine toward you;
  - tools in a fist round a measured handle;
  - things on the palm;
  - a mug round its side;
  - trays and cakes from below;
  - chairs by the backrest.
- **Tray.** Palms under its edges, thumbs over the rim. 0 to 3 mm of contact every frame while walking, turning, looking up and down, crouching and jumping.
- **Shoulders and carries.** Shoulders follow 30% of the look and carries 50% (75% for reading), so the arms reach when you look down and a glance up doesn't throw the tray.
- **Wobble.** A tray wobbles from how you move, not from where you look.
- **Low frame rates.** The landing dip and a carried thing's tilt are stable at any frame rate (`Spring`).

**The call center**
- **Board and binders.**
  - The whiteboard is off the window.
  - Four separate binders, labelled by topic, each with its own slot; the page shows while one is in your hand.
  - The printer prints the help pages, no longer the landlord's notice.
  - The status lamps sit on the monitors.
- **Cast.** Dana, Omar, Kim and the supervisor are distinct people (`Docs/CHARACTERS.md`). The supervisor no longer wears the wedding coordinator's clothes.
- **The console.**
  - **Four columns:** the conversation; the case; the company system; the phone rail on the right.
  - **The case column:** what the customer says against what the record says; a NEXT cue that never names the fix; "Use in search" for the order number; the phrasebook; the replies; how to finish.
  - **Ending is not resolving.** End call on an unfixed problem needs a second press and counts against you.
  - **Mute** blocks talking.
  - **Hold** gives 30 s of patience before it costs more than talking.
  - **Search messages** say what was searched and what to do next.
- **Translation.** Only the accessibility captions setting (Settings, off by default, labelled "Caption") puts English under a caller's lines. Anyone who had it on keeps it, with a one-time note. Nobody claiming a language now gets a phrasebook of general words, not the English of every line. The WhisperBox is a hint device, not a translator.
- **Training call.** The first shift starts with an English call at your desk. It is paid, patient, unscored and unwatched. Six steps appear one at a time, each pointing at its control; you can skip it, or replay it from "How does this work?". Calls in your CV's languages come after it.

**Elsewhere**
- The flat's corridor trash bags no longer pass through each other.
- New tools:
  - `grip_probe.cs`, `grip_session.sh`, `grip_survey.sh` (hand contact);
  - `job_cycle.sh`, `cycle_state.cs` (job cycles);
  - `shot_at.cs`, `face_npc.cs` (captures);
  - `make_printout_texture.py`, `make_binder_spines.py` (textures).

## Test results

- **EditMode: 72 of 72.** New since v0.5.0: `CallUsabilityTests` (13), `SpringTests` (2). One existing assertion was updated to the clearer search message.
- **PlayMode: 45 of 45.** New: `GripTests` (13) and `TrainingCallTests` (4). Two existing call-center test classes now mark the training as done, since they test transfers and gadgets.
- **The tests can fail.** With the wrist moved 5 cm off the tray, the tray tests failed from the first frame. Replaying the old spring code at a 10 fps frame diverged within 105 frames.
- **Build.** The Windows build runs the call check with 0 errors in `Player.log`.

## Performance (measured)

Same conditions as v0.5.0: i5-12400F, RTX 5060, 1920x1080 fullscreen, default quality, a 60 Hz monitor, about 30 s per scene with its shift running.

| Scene | vsync on: average | Long frames over 25 ms per 5 s (each paired with a short one) | vsync off: worst frame |
|---|---|---|---|
| Flat | 60 | 18 to 41 | 17 to 19 ms |
| Wedding | 60 | 12 to 38 | 18 to 19 ms (one 33 ms) |
| Call center | 60 | 5 to 29 | 17 to 19 ms |

These are the same as v0.5.0 (the same paired timing jitter, no dropped frames). The console's larger layout and the per-frame grip work cost nothing measurable.

## How a novice finds each clue (no human playtest)

Every case is solved from the screen:
- the customer's words (left column, in their language);
- the order record (Account tab, after a search);
- the help page for the problem (Help pages tab, or a binder or printout).

| Case | Ask | Where the clue is | Which policy line decides it |
|---|---|---|---|
| Delivery, not collected | Order number, then the tracking page | The record says "NOT collected"; the customer says the tracking page shows "label created" | Deliveries: rebook and a voucher |
| Delivery, held at the depot | Order number, then confirm the address | The record says "HELD at depot"; the customer gives the full flat number | Deliveries: confirm the address, then release |
| Smart plug lights | What the light is doing | The customer describes its colour and speed (the record shows nothing wrong) | Smart plug lights: the pattern gives the fix |
| DustBot lights | What the lights are doing | The customer gives the colour sequence | DustBot lights |
| Refund, within 30 days | Order number, then the receipt number | The record says "Bought 18 Oct (12 days ago)"; the customer gives the receipt | Refunds: within 30 days with the receipt, to the card |
| Refund, after 30 days | Order number, then when they bought it | The record says 45 days ago, with a fault and a 1-year warranty | Refunds: no refund after 30 days; a fault in warranty gets a replacement |

A caller in a language you don't speak still says the key words in their language; the phrasebook covers the general terms (colours, "blinks", months, "order"). Working them out is the job you claimed on the CV.

## Remaining limits

- **Not tested:** keyboard focus order (pointer only), and a first-time player.
- **Grip details:** QG-R1 (flat-paper fingertips), QG-R2 (the spatula's flat handle), QG-R3 (the held binder is large in view).
- **Dana and Kim.** Dana's characterisation is a proposal for review; Kim's pronouns are not defined anywhere.
- **Audio** was checked by reading the code, not by ear.
- **Multiplayer** does not exist, so it was not tested.
- **Rebuild churn.** Scene rebuilds still alter four unrelated assets; they are reverted by hand before each commit (QG-23).

---


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
