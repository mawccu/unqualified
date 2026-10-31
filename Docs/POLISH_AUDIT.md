# Polish audit

Brief: `Docs/CLAUDE_FULL_GAME_POLISH_HANDOFF.md`. Started from commit 84d402a (v0.4.0); finished as v0.5.0.

**Test conditions**
- **Machine.** Windows 11, i5-12400F, RTX 5060, a 60 Hz monitor.
- **Editor.** Unity 6000.3.27f1 (URP), Game view 1280x720.
- **Build.** The Windows build was run at 1920x1080 and 1280x720.
- **Saves.** Saves were throwaway test states. The save in use when the pass started was copied aside first and restored at the end.

**Evidence**
- Screenshots are in `Docs/Polish/Before` and `Docs/Polish/After`, taken from the same positions and the same view angle and resolution.
- `Tools/Eval/props_support.cs` lists every loose prop with the gap under it.
- The PlayMode tests are named in each row.

**Statuses:** verified (checked in play or by a test after the fix), observed (seen, not fixed), blocked (with the reason).

## P0: lost or corrupt saves, money, progression, crashes

None found.
- **Covered by tests:** save migration, reload during an order, a build or a packed gadget, double checkout, payout once, and a full call center shift to payout.
- **Logs:** the v0.5.0 build ran each scene for 25 to 30 s with no exceptions in `Player.log`.
- **Console:** three Editor play sessions (wedding, call center, flat) and every test run left the console without errors.

## P1: broken interactions, collision, recurring exceptions

| ID | Where | Steps | Expected / actual | Cause | Fix | Result |
|---|---|---|---|---|---|---|
| PA-01 | Flat kitchen counter | Load the flat and look at the takeout box on the counter. | Expected: it rests on the worktop. Actual: it hovered at 1.15 m, about 25 cm above it. Before: `kitchen_takeout`. | Confirmed: the counter's collider was one box around the whole model, including the tap. Its top was an invisible lid at tap height. Three builders had their own copy of this box fitting. | `Assets/Editor/ColliderFit.cs`: one shared helper. Models with something tall standing on a lower surface (counter, couch, office sofa, beds) get their own mesh as a static collider. Everything else keeps a box. | Verified. The takeout rests at 0.91 m (After: `kitchen_takeout`). Test `PolishTests.NothingInTheFlatFloats`. |
| PA-02 | Wedding prep kitchen, same counter model | Start the shift and look at the counter. | Expected: the cloth on the counter. Actual: it was wedged on its edge against the wall, 74 cm above the floor. | Same cause as PA-01: the cloth spawned inside the tall box and was pushed out sideways. | PA-01's fix, plus a wedding rebuild. | Verified. Test `PolishTests.NothingInTheWeddingFloats`. |
| PA-03 | Wedding prep kitchen, order slips | Same as PA-02. | Once the lid was gone, the 4 slips fell to the floor at the start. They had been placed beyond the counter's front edge, and only the invisible lid held them up. | Confirmed (positions). | Moved onto the worktop either side of the tray (`GameplaySceneSetup.Shift.cs`). | Verified (After: `wedding_kitchen_counter`). |
| PA-04 | Office kitchenette (mugs on the same counter) | Same model as PA-01. | Same risk as PA-01. | Same. | PA-01's fix, plus a call center rebuild. | Verified by the support probe (6 props, none unsupported). |
| PA-05 | Workshop casing (found in the last session) | Aim into the casing with the board. | The crosshair hit the casing's box, not the board slot inside it. | Box collider around a tray. | The casing's collider is its floor only. | Verified in the last session (`WorkshopTests` aim checks). |

## P2: visible defects, confusing feedback, weak materials, presentation

| ID | Where | Expected / actual | Cause | Fix | Result |
|---|---|---|---|---|---|
| PA-06 | Flat beds (reported) | Expected: a bed that reads as fabric. Actual: a flat slab; the blanket was a lumpy chrome ellipsoid with sharp reflections; the pillow was a white brick. Before: `mattresses`, `mattress_close`. | Weak placeholder mesh. Its "blanket" material reflected the room like metal. | Replaced with two original models (`Tools/Blender/make_bedding.py`). Each is a mattress on a pallet base with a fitted sheet, a softened pillow, and a duvet with rumples that drapes over the edges, turned back at the top. One bed is roughly made, one slept in. Smooth-shaded. Fabric texture set, tinted, matte. The pallets use the wood texture. Static mesh collider. | Verified (After: `mattresses`, `mattress_close`, `room_overview`). |
| PA-07 | Floor (flat and wedding) | Expected: dark gaps between planks. Actual: a bright white line along every plank seam. | Confirmed: the floor tile's gap strips use the shared light "Walnut" wood material, which is right for the furniture that also uses it. | A floor-only override in `MaterialLibrary` (`PerModel`): the floor tile's Walnut slot uses a dark "Plank gap". Kept when the library is rebuilt. | Verified (After: `mattresses`). |
| PA-08 | Flat walls | Expected: clean walls. Actual: green and pink smudges and mottling. | Suspected: a low lightmap resolution plus too few indirect samples under the denoiser. | Lightmap resolution 24 to 36 texels per metre; indirect samples 2048 to 4096. | Improved, not eliminated: a faint warm smudge remains high on the kitchen wall (After: `kitchen_takeout`). |
| PA-09 | Wedding start | Expected: you start looking into the room. Actual: you start facing a green wall 2.3 m away. Before: `wedding_hud`. | The spawn faced the divider of the back-of-house strip. | Spawns face east down the strip, toward where the coordinator walks up to greet you. | Verified (After: `wedding_hud`). |
| PA-10 | Every screen at 1280x720 | Expected: readable text. Actual: 12 to 14 px text drawn at 8 to 9 px. | The UI scales from a 1920x1080 reference. | Reference 1600x900: 20% larger at every size. Checked in the build at 720p and 1080p. | Verified (After: `build_console_720p`, `build_console_1080p`). |
| PA-11 | HUD card in the flat | Expected: the next action at a glance. Actual: four lines of text, and the card showed through every full-screen interface. | Design. | The card shows its title and one line; Tab shows the rest ("Tab details (2 more)"). A focus mode hides the card, status line and key help while the computer, console or a workshop sheet is open. The card also stopped rebuilding its text 4 times a second. | Verified (After: `hud_card`, `browser_*`). |
| PA-12 | CV page | Expected: the trade at a glance. Actual: every ticked claim's full consequence was open at once, and the lie meter was below the fold. Before: `browser_cv_claims`. | Design. | The lie meter heads the right-hand column, above the CV preview. Only the claim (or language) just ticked shows its consequence. Redraws keep the scroll position. | Verified (After: `browser_cv_claims`). |
| PA-13 | Parts shop | Expected: compact cards. Actual: each card had a blurb, stock, ownership and use as separate lines, and the page scrolled. | Design. | Card: name, price, "You have N · N left today", what it's for. The joke line moved to a tooltip. | Verified: fits on one screen at 720p (After: `browser_shop`). |
| PA-14 | Call console | Expected: closing the call always visible. Actual: the Finish actions were under the fold at 720p, and the 11 replies were one flat list. | Design. | Finish moved up with the phone controls. Replies are grouped (Open, Ask about, Smooth it over), and what you've already learned is ticked. Arabic right-to-left shaping and the mixed-direction order number checked in the build. | Verified (After: `console_arabic`, `build_console_*`). At 720p, "Ask a colleague" needs a scroll. |
| PA-15 | Window (requested) | Expected: an openable window. Actual: decoration on a solid wall. | Feature. | A hole through the wall with a frame and its reveal. A fixed sash and a sliding sash: the hand takes the handle and rides along with it. A grille outside (the gap is 0.55 x 0.96 m at a 0.98 m sill, which the player can't climb through). A narrow alley outside. Street sound fades from 0.04 to 0.40 as it opens. A press while it slides is refused; it won't close on a thing on the sill. | Verified (`PolishTests.TheWindowSlidesOpenAndShutAndWillNotCloseOnThings`; After: `window_closed`, `window_open`). |
| PA-16 | Window glass | Expected: matching frosted panes. Actual: the moving pane rendered pure white. | Confirmed: it was lit by light probes sitting in the window light's beam, while the fixed pane was lightmapped. | Both sashes use probe light from a point 1.3 m into the room; the glass glows less (2.2 down to 0.75). | Verified (After: `window_closed`). |
| PA-22 | Flat, anything moving near the window (hands, the sash, held props) | Expected: lit like the rest of the room. Actual: rendered blown-out white. | Confirmed by sampling the baked light probes: 19 to 25 by the window against 2 to 4 elsewhere. The window light was a strong spot 30 cm from the probe grid. | The window's daylight is now a baked area light the size of the glass. | Verified: the probes read 1.3 to 2.0; the hand on the handle looks normal (Motion: `10_window_sliding`). |
| PA-17 | Wedding task card | The name "Dessert display" wrapped and overlapped its description on the first frame. | A two-line name on the first layout pass. | Task names stay on one line. | Verified at the start of the shift (After: `wedding_task_card`). |
| PA-18 | Frame pacing (build, vsync on) | Every scene averages 60 fps, but each 5 s window has 2 to 38 frames timed at about 33 ms, every one paired with a frame under 8 ms. | CPU-side present-timing jitter. No frames are dropped: the average stays exactly 60, and the long and short frames match one for one. | Not fixed. The logger now reports these counts. | Observed. Not investigated with GPU timing. |

## P3: small cosmetic

| ID | Where | Note | Status |
|---|---|---|---|
| PA-19 | Rent bill texture | The bill has an "OVERDUE" stamp on day 1, next to "Due by Day 7". | Observed, not fixed. |
| PA-20 | Pillows | The first replacements were near-spherical. | Fixed: bevel before subdivision; verified (After: `mattress_close`). |
| PA-21 | Pallet blocks | They rendered flat white. | Fixed: wood texture and tint; verified. |
| PA-23 | Carrying | A held thing can't be held out over a counter and dropped onto it: the hands pull it back from any surface in front of them. Things are put down through placement spots instead. | Observed. This is how carrying is designed; a free "set it down here" would be a new feature. |
| PA-25 | Tools | Rebuilding the wedding saves `Cable line.mat` as opaque, although the builder asks for transparent, and regenerates `Worker.controller` in a different order. This happens on any wedding rebuild, before this pass too. | Observed. Both files were reverted to their committed state; the cause was not traced. |
| PA-24 | Tools | The flat's rebuild script could build before a recompile had finished, saving the old scene without saying so. Two of my rebuilds did this until I checked the scene's lights. | Fixed: `Tools/apt_rebuild.sh` waits for compilation to finish. Every scene in this delivery was checked for its new contents. |

## Not covered by this pass, and why

- **Animation and sound, item by item.** Not covered: landlord gestures, the bill contact, typing, the handset and footsteps. These behave as in the earlier sessions (BillHandoverTests and MovementTests pass), but nothing new was inspected or tuned apart from the window's hand and sounds. Resting-collision audio was not reviewed separately.
- **Controller navigation.** Not tested. The game has some gamepad bindings but doesn't advertise controller support.
- **A narrow or resized window and UI scale settings.** Not tested: the game has no UI scale setting, and only 1280x720 and 1920x1080 were checked.
- **Multiplayer.** There is none. The project is offline only. Nothing can be verified.
- **The full opening by hand.** The wake-up, landlord and bill hand-over weren't replayed by hand in this pass. Their tests pass.
- **A complete wedding shift played to payout by hand.** Not done in this pass. The call center shift reaching payout is a test (`GadgetTests`); the wedding's payout path is covered by `ShiftTests`.


---

# Quality gate: the existing levels (v0.6.0)

Brief: `Docs/CLAUDE_EXISTING_LEVELS_QUALITY_GATE.md`, with
`Docs/CLAUDE_GRIPS_AND_CALL_CENTER_USABILITY.md`.
- **Started:** commit cbba668 (v0.5.0).
- **Checkpoint:** caae3e8 (grips and identity).
- **Finished:** the commit after it.

The pass-or-fail verdict for each gate is in `Docs/POLISH_DELIVERY.md`.

**Test conditions**
- **Machine and editor.** As above: Windows 11, i5-12400F, RTX 5060, a 60 Hz monitor; Unity 6000.3.27f1 (URP). The Game view was 1280x720 at the default 80 degree FOV setting; the editor's own aspect gave a 44.5 degree vertical view.
- **Build.** The Windows build ran at 1920x1080 and 1280x720, windowed for the call check and fullscreen for timing.
- **Saves.** Every play session and test used a throwaway save: `CrewState.StartOver` or a test setup. The user's save (`crew.json` and `.bak`) was copied aside first and put back after every session that could have touched it, and at the end.

**Origin** says whether a defect was there before this pass (*pre-existing*) or came from a change during it (*regression in this pass*).

## Issues

| ID | Sev | Where | Reproduce | Expected / actual | Root cause | Fix | Retest | Origin |
|---|---|---|---|---|---|---|---|---|
| QG-01 | P0 | Every scene: the player's camera | Run at 10 fps or lower (or fast-forward), and land from any drop or jump. | Expected: a small dip on landing. Actual: the view drifts away until it shows only the sky colour (Before2/qg01_camera_lost). The camera was measured at y = 2.2e37. | Confirmed. The landing-dip spring was integrated in one step per frame, which is unstable once the frame passes 0.09 s; a replay of the old code diverged within 105 frames at 10 fps. A carried thing's tilt spring had the same flaw at very low frame rates. | `Spring.Step` advances both in steps of at most 1/60 s. | Verified. `SpringTests` (10, 5 and 3 fps); the wedding run at 12x speed kept the camera at 1.59 m. | Pre-existing (found during this pass) |
| QG-02 | P1 | All carried things (reported for the tray) | Pick up the tray (or a mug, guest book, cloth) and look ahead. | Expected: you see what you hold. Actual: neither the thing nor the hands were in the frame (Grips/tray_before_fp, survey/before_*_fp). | Confirmed. Every carry sat at a fixed camera-space height about 39 degrees below the middle of the view, which only reaches 22 to 25 degrees down. | Carries are placed from the live field of view, so the holding hand is just inside the frame; tall things keep their top below the line of sight (`PlayerActor.HoldOffset`). | Verified. `GripTests.TheTrayIsInView` and `OneHandedCarriesAreInView` at 70, 80 and 100 degree FOV; every survey/v1_*_fp capture. | Pre-existing |
| QG-03 | P1 | Tray, cake layers (reported) | Carry the tray; look from the side. | Expected: the palms support it. Actual: palms 29 mm off, fingertips 15 to 55 mm, thumbs 92 mm (`grip_probe`); the hands sat beside the tray. | Confirmed. The wrist target was 45 mm above the tray's bottom and outside its edge, palm up. | Palms under the side edges, fingers inward under it; on a flat tray the thumbs go up the side and over the rim (`Grip.Tray`, measured); on a tall cake they stay down (`Grip.Support`). | Verified. Palms +1 to +2 mm, tips 0 to +3 mm, thumbs on the rim. `GripTests.HandsStayOnTheTrayThroughMovement` checks every frame through walking, turning, looking up and down, crouching and a jump; `Grips/motion/*` and `tray_motion_*.gif`. | Pre-existing |
| QG-04 | P1 | Small carries | Carry the mop, takeout box, cable coil or guest book. | Expected: a grip fitting the thing. Actual: every one got "a hand on the right of its box": 15 cm off the mop's shaft; fingers 3 to 4 cm into the takeout box; the hand in the empty middle of the cable ring; books held like mugs. | Confirmed. One grip for every small object. | Grip profiles: tools in a fist round a measured handle (mop, screwdriver, spatula, popper, table sign by its stem, the cable by its near strand); things on the palm; books on both palms; the mug round its side (`Grips.Profile`, `Docs/GRIPS.md`). | Verified. Per-profile `GripTests` (sheet, binder, guest book, mop, sign, cable, cloth, mug, chair) and `survey/v2`, `v3`, `v4` captures. | Pre-existing |
| QG-05 | P1 | Dragging a chair | Drag a wedding chair and look down at it. | Expected: hands on the backrest. Actual: hands 9 to 14 cm short. Even when it could be reached, the target was 7 cm behind the backrest and 2.5 cm above it. | Confirmed. The shoulders turned fully with the head. | The shoulders follow 30% of the look; the chair comes closer (0.6 m); the palms go on the backrest's top. | Verified. `GripTests.ChairBackrestInReachLookingDown` at 0, 30 and 60 degrees down. | Pre-existing |
| QG-06 | P1 | Paper held up (bill, printouts) | Hold the bill to read it. | Expected: fingers on the back, thumb on the front. Actual: the palm was beside the sheet and the fingers 22 to 30 mm behind it. | Confirmed (`grip_probe`). | `Grip.Sheet`: palm and fingers on the back, thumbs round the edge, a third of the way up so the text stays clear. | Verified. 0 to 3 mm, thumbs +2 mm; `GripTests.SheetHeldAtItsEdges`; `paper_v2`, `apt_rent_bill_fp`. | Pre-existing |
| QG-07 | P2 | Tray wobble | Carry the tray and nod up and down quickly. | Expected: no wobble from looking. Actual: nodding counted as shaking it. | Confirmed. Wobble read the hold point, which rides on the head. | Wobble reads the carrying player's body. | Verified. The movement test keeps the tray through fast nods. | Pre-existing |
| QG-08 | P1 | Call center, north wall (reported) | Look at the whiteboard. | Expected: on solid wall. Actual: it overlapped the window (Before2/board_window). | Confirmed. The builder put the board at x 1.6; the window spans 0.85 to 2.15. | Moved to x 3.4 in `CallCenterSceneBuilder`. | Verified after a rebuild and a play reload: After2/board_window, board_window_angle. | Pre-existing |
| QG-09 | P1 | Call center reference desk (reported) | Look at the binders; read one. | Expected: separate books. Actual: six binder models 27 cm deep, 9 cm apart, passing through each other, read through one shared interactable. | Confirmed (positions). | Four binders, one per help page, each a real prop with a spine label and its own slot. Its page shows while it's in your hand; it returns only to its own slot; it's held on both palms with its spine toward you. | Verified in play: picking one up, reading it, returning it, a wrong slot refused, dropping one on the floor, and a scene reload (all four back in their slots). After2/reference_binders, binder_reading, binders_returned; survey/binder_v4_*. | Pre-existing |
| QG-10 | P1 | Call center cast (reported for Dana) | Look at Dana, Omar, Kim and the supervisor. | Expected: recognisable people. Actual: all were the same moustached placeholder recoloured by seat, and the supervisor wore the wedding coordinator's exact costume. | Confirmed. One face mesh with a baked-in moustache; costume by seat; the supervisor reused the coordinator's costume. | Clean-shaven body meshes, named costumes, head extras (`Docs/CHARACTERS.md`; Dana's design is a proposal). | Verified in the running scene after a rebuild (After2/dana, omar, kim, supervisor, supervisor_glasses) and from three angles (Screens/cast_*). | Pre-existing |
| QG-11 | P1 | Calls: translation (reported) | Take a Spanish or Arabic call. | Expected: the customer's language, with no free translation. Actual: there was a "Translate customer" toggle in the call toolbar, and the "phrase aid" printed the English of every line when nobody on shift claimed the language. | Confirmed (`SupportConsole`, `CallRouter`). | Captions moved to Settings as an accessibility option, labelled "Caption:". An existing "on" is kept, with a one-time note; it was off by default and could only have been set by the player. The phrase aid became a phrasebook of general words. One rule decides it (`CallTranslation`). The WhisperBox is a hint device that glosses key words, not a translator. | Verified. `CallUsabilityTests` (4 translation tests), `TrainingCallTests.AWhisperBoxLeftAtHomeOrFlatGivesNoHelp`. In play: Console/s2 (no captions), s3 (captions on), off again with 0 captions left. | Pre-existing |
| QG-12 | P1 | Call console layout (reported) | Use the phone controls on a call. | Expected: the phone in a fixed place, with ending kept apart from resolving. Actual: the phone buttons sat in the middle column, mixed with the replies; there was no plain "end call"; "Mark as resolved anyway" sat beside the phone. | Design. | Four columns: the conversation; the case (summary, next goal, replies, finishing); the company system; and a fixed phone rail on the right. The rail holds status, language, routing, patience, hold time, Answer, Mute, Hold, Transfer, and End call kept apart. A disabled button says why. End on an unfixed call needs a second press and counts against you (`CallOutcome.Dropped`). | Verified in play (Console/t1 to t7, s1 to s6) and in the build at 1920x1080 and 1280x720 (Console/build_call_*). `CallUsabilityTests.EndingAnUnfixedCallDropsItAndIsNotSuccess`. | Pre-existing |
| QG-13 | P1 | First call (reported) | Start a first call-center shift. | Expected: a guided first call. Actual: the first real call rang straight away. | Missing feature. | A training call in English, reserved for you, patient and unscored, with no supervisor visit. Its six steps show one at a time and point at the control to use. You can skip it, or replay it from "How does this work?". The CV's languages wait until it is done or skipped. | Verified. `TrainingCallTests` (4) and `CallUsabilityTests.TrainingWalksTheSixStepsInOrder`; played through in the editor (Console/t1 to t7). | Pre-existing (missing) |
| QG-14 | P2 | Hold | Put a caller on hold to research. | Expected: hold buys time to research. Actual: hold drained patience 4x faster than talking. | Confirmed (`CallRouter` drain rates). | 30 s of patient hold (slower than talking), then faster; the rail counts down. | Verified. `CallUsabilityTests.HoldHasAPatientAllowanceThenGetsWorse`; Console/s4. | Pre-existing |
| QG-15 | P2 | Mute | Mute, then ask the customer something. | Expected: they can't hear you. Actual: the conversation carried on. | Confirmed. | Speaking intents are refused while muted, with the reason; the account actions still work. | Verified. Test; Console/s5. | Pre-existing |
| QG-16 | P2 | Order search | Search a wrong number, or before asking. | Expected: what was searched and what to do. Actual: "No order 123 found." | Wording. | Each message says what was searched and the next step; "Use in search" copies the number the customer gave. | Verified. `CallUsabilityTests.SearchFailuresSayWhatWasSearchedAndWhatToDo`; Console/t3. | Pre-existing |
| QG-17 | P2 | Call center printouts | Print the help pages and read the sheet. | Expected: the help pages. Actual: the landlord's FINAL NOTICE. | Confirmed. The sheets reuse the bill model and its material. | Their own printed face (`Tools/make_printout_texture.py`, made from the help pages' text); the builder swaps the material. | Verified. survey/cc2_printout_2_fp. | Pre-existing |
| QG-18 | P2 | Call center desk lamps | Look at a monitor. | Expected: the lamp on the monitor. Actual: it was sunk into the screen (overlap scan). | Placement. | Raised onto the monitor's corner. | Verified. After2/status_lamp, status_lamp_side. | Pre-existing |
| QG-19 | P3 | Flat corridor trash bags | Overlap scan. | The two bags passed through each other. | Placement. | Spaced apart in the builder. | Verified. The scan after the rebuild no longer lists them. | Pre-existing |
| QG-20 | P3 | Supervisor's glasses, Dana's hair | Three-quarter and back views. | The temples floated beside the head; ponytail plus puffs read as one block. | The temples were placed at a fixed width; the head narrows behind the eyes (measured). | Temples run from the rim's hinge to the ear; Dana got short hair with puffs. | Verified. Screens/cast_3q, cast_back. | Regression in this pass (fixed before commit) |
| QG-21 | P3 | Phone rail | In the build at 1080p after a skipped training call. | The Answer button stayed outlined, and End's warning was cut off. | The pointer was cleared only on a training call; the warning was two lines in a one-line slot. | Cleared when training ends; shorter text. End call no longer shrinks out of a short screen. | Verified in the next build run. | Regression in this pass (fixed before commit) |
| QG-22 | P3 | Asset sources | Binder spine labels. | Made without a source script. | Process. | `Tools/make_binder_spines.py` (it reproduces the files byte for byte). | Verified. | This pass |
| QG-23 | P3 | Tools | Any scene rebuild. | Also changes `Cable line.mat`, `Worker.controller`, the volume profiles' sub-asset ids and `ProjectSettings` preloaded assets. | Not traced. | Reverted each time before committing. | Observed. | Pre-existing |

## Remaining (not fixed, reported for review)

| ID | Where | Note |
|---|---|---|
| QG-R1 | Flat paper (envelopes, order slips) | The palm is on the slip, but the fingertips sit 11 to 25 mm above it (the pinch from above). It reads as a pinch from the player's view; it is not measured contact. |
| QG-R2 | Spatula | The fist is fitted to a round handle; the real handle is a flat 30 x 18 mm. Fingertips are 6 to 9 mm off a round fit; the capture reads as a held handle. |
| QG-R3 | Held binder | It is large in view (31 x 27 cm at half a metre). It sits low and shows its spine label; the thumbs at its sides are small at the frame's edge. |
| QG-R4 | Kim | Pronouns are not defined anywhere; none were invented. |
| QG-R5 | Comprehension | There was no first-time human playtest. Every case was solved here only with what the game shows; see the walkthrough in `POLISH_DELIVERY.md`. |
| PA-19 | Rent bill | The OVERDUE stamp on day 1, from the last pass; still not changed. |

## Inspected areas

Method, in each scene:
- **Walk.** Walked at player height in play, approaching each station, with the HUD on.
- **Carry.** A representative wide thing carried (the tray in the wedding; the binders and printouts in the office).
- **Scans.** `Tools/Eval/overlaps.cs` (models passing through each other) and `props_support.cs` (loose props resting on nothing) after each rebuild.
- **Photos.** Close photos of anything a scan flagged.

"No defect" means looked at and nothing found. A scan hit that turned out to be a box-overlap artifact is listed with what it was.

### Flat

| Area | Checked | Result |
|---|---|---|
| Kitchen counter, takeout boxes, microwave | Support scan; scan hits (microwave and takeout inside the counter's box: things standing on it) | No defect. Last pass's fix holds (PA-01). |
| Beds, pillows, couch | Looked at in play (last pass's new models) | No defect |
| Computer desk, tower, chair | Scan hit (tower inside the desk's box: it stands under it); sitting at the desk | No defect |
| Front door, corridor, trash bags | Door scan hits (leaf in frame); bags | Bags fixed (QG-19) |
| Window and sashes | Scan hits (sashes in the frame) | No defect (PA-15, PA-16 and PA-22 hold) |
| Workshop bench, parts, parcels, rack | `WorkshopTests` and `GadgetTests` pass; not re-inspected by hand this pass | Covered by tests |
| Rent bill, envelopes, screwdriver, takeout (carried) | Grip survey | QG-04, QG-06 fixed; QG-R1 remains |
| Full cycle from here | `Tools/job_cycle.sh` (both jobs) | Passes (see the delivery doc) |

### Wedding

| Area | Checked | Result |
|---|---|---|
| Hall: tables, chairs, guests seated | Walked; guests photographed seated during the shift (After2/guests_tables_*) | No defect |
| Stage, steps, arch | Scan hit (steps against the platform); photo (After2/stage_steps) | No defect: flush |
| Sound desk, mixer, cable | Scan hits (knobs on the mixer); the cable carried | No defect; the cable grip was fixed (QG-04) |
| Prep kitchen: counter, tray, mugs, cloth, radio, slips | Scan hits (all standing on the counter); support scan | No defect |
| Cake tiers, stand, topper | Carried (tray, base layer, topper) | QG-03 fixed |
| Chairs carried | Dragged and looked down at | QG-05 fixed |
| Storage: mop, spatula, popper, signs | Carried | QG-04 fixed; QG-R2 remains |
| Coordinator | Cast sheet; she walks and talks in play | No defect |
| Drapes and fairy lights | Scan hit (light bulb inside the drape's box) | No defect |
| Full cycle | `Tools/job_cycle.sh wedding` | Passes |

### Call center

| Area | Checked | Result |
|---|---|---|
| North wall: windows, whiteboard | Photos from the same spot before and after, and an angled view | QG-08 fixed |
| Reference desk and binders | Pick up, read, return, wrong slot, drop, reload | QG-09 fixed |
| Printer and printouts | Printed sheet read | QG-17 fixed |
| Pod: four desks, partitions, monitors, phones, keyboards, chairs | Scan hits (each desk's box meets the partition's feet); photos (After2/pod_spine_*); lamp photos | Lamps fixed (QG-18); otherwise no defect |
| Seated coworkers (Dana, Omar), Kim on break, supervisor patrol | In-play photos | QG-10 fixed |
| Break corner: sofa, coffee machine, mugs, cooler, vending | Support scan; mugs carried | No defect |
| Console at 1280x720 and 1920x1080 | Played through and photographed in the build | QG-12 to QG-16 fixed |
| Supervisor listening | Training: she doesn't come over (test); ordinary calls unchanged | No defect |

## Not covered by this pass, and why

- **A first-time player.** No first-time human player tried the training call or the console (QG-R5).
- **Audio by ear.** I can't listen. Sound was checked only by reading the code: the phone buttons click, the hold complaint comes at 25 s, nothing plays across a scene change (every audio source belongs to its scene). Footsteps count distance actually covered (`UpdateSteps`). It was not reviewed at the player's distance.
- **Multiplayer.** There is none, so there is nothing to test.
- **Controller, other aspect ratios, very long strings.** Not tested beyond 1280x720 and 1920x1080 and the Arabic content.
- **The computer browser.** It is unchanged since v0.5.0, where it was verified; it was not re-checked this pass.
