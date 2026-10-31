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
