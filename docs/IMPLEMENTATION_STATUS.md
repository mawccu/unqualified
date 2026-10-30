# Implementation status

Updated 2026-10-30 (evening).

## Where the project is

M0 (foundation) and M1 (shared room over the network) are done and machine-checked. On top of them, **increment 1 of the Wedding Crew vertical slice** is built: the pink cake test rig has been replaced by a recognisable hotel wedding venue with real tasks. Nobody has played it yet; everything below was checked by machine and by looking at rendered screenshots.

Follows `docs/claude_brief` (prompt 1: readable venue, task board, data-backed task state). It is NOT the full shift yet: there is no timer, no suspicion, no witness logic, no distraction, no debrief, no audio task. See "What is not built".

## Versions

- Godot 4.7.2 stable (official build ed1daf0bf), Windows, Forward+ renderer, typed GDScript
- Networking: Godot high-level multiplayer over ENet (built in). No plugins

## What exists now

The venue (`scripts/venue/`), all built from simple shapes, no downloaded assets:
- Public ballroom: carpet, polished dance floor, wood panelling with gold rail, columns, chandeliers, fairy lights, a stage with red curtains, speakers, a flower arch and a "AMARA & JAMAL" banner, a sound desk with a mixer, three round guest tables with six chairs and centrepieces each, a dessert station, and a photo corner with a flower arch and gold frame
- Back of house behind a dividing wall with two archways (signs "STAFF ONLY" and "BALLROOM"): prep table, a table for signs, storage shelving, a wash-up nook, a notice board, crates, mop bucket
- Warm interior lighting with soft shadows and ambient occlusion

Tasks (`scripts/tasks/task_board.gd`, pure logic):
- **Dessert display:** three cake layers (big, medium, small) are on the prep table. Carry them through an archway to the stand and stack them. Each layer is only accepted once the layer below is in place. A layer in the wrong slot is allowed and shows as wrong
- **Guest seating:** three table signs are on the sign table. Put each on its own table. A swapped sign is allowed and shows as wrong, so the crew has to notice and fix it
- **Ceremony audio:** listed on the board as "Coming in the next build". The sound desk is there but not interactive yet
- A task board on the HUD shows each outcome, its progress ("2 of 3 layers", "0 of 3 signs right, 1 in the wrong place") and its goal

Other:
- The coordinator: a suited NPC with a clipboard and a floating state label. She walks a six stop route (stage, sound desk, desserts, dance floor, seating, photo corner) and pauses to look at each. The host moves her; clients see her walk. She does not see or judge anything yet
- Held-piece ghost: aiming at a free socket with a layer in hand shows a green ghost of that exact layer
- All of M0 and M1: first-person movement, pick up / place / drop through one validating service, pause menu with settings, host and join from the menu (up to 4), remote players as coloured bodies, disconnect cleanup, host-owned truth

## Character presentation (from the art brief)

Original placeholder art built in code, no third-party assets (see `docs/assets/ASSET_REGISTER.md`):
- **One chunky toy-like worker body** (about 1.8 m, head about a third of the height, mitten hands, big shoes, panicked eyebrows) in **four costume variants** chosen by player slot: red vest with bow tie and lanyard, blue blazer with headset and radio, yellow apron with lanyard, green beanie with utility belt and one glove
- **First-person hands**: chunky mittens with colour-matched cuffs. They rest low, bob while moving, lag behind looking around, grip a carried prop where it really is, jab on pick up, place and drop, scrub while cleaning, and fly up on a stumble
- **Animation**: nine authored clips (idle, walk, sprint, carry idle, carry walk, interact, clean, stumble, celebrate) in an `AnimationPlayer`, driven by an `AnimationTree` state machine. Short actions return to idle by themselves; clean loops until you move; a stumble can interrupt anything and cannot itself be interrupted. Every clip keys every joint so no limb is left frozen by the previous clip
- **Secondary wobble**: damped springs lag the head, tie, lanyard and apron behind acceleration, and held props lag their yaw a little and tilt slightly. All presentation only
- **Remote players** are these bodies, with walk, sprint, carry and interact derived locally on each peer from how their position changes and whether they hold something. The local player does not see their own body but it still throws a shadow
- Wobble never touches the eye, the hold point or the position (tested: zero drift while a stumble plays). A held prop's position still follows the hold point exactly

Not done here: no rigged model (the joint and clip names are ready for one), no footstep or foot-plant syncing (walk speed does not scale the clip), no facial animation, no ragdoll, no stumble trigger in the game yet (the animation and `Player.stumble()` exist but nothing causes one), and the stumble, clean and celebrate states are not sent over the network because nothing triggers them yet. Pick up and place do show on other players, derived from the held prop changing.

## Checks actually run

- `godot --headless --path . --import`: no errors
- `tests/avatar_test.tscn` (headless): 99 checks, 0 failed. Spring maths (settles, overshoots once, never exceeds its limit, survives a huge time step), four distinct costumes that all build every joint, body proportions, all nine clips (every track points at a real joint, values finite and in range, exact loop seams, stumble 0.40 s), every state transition, the wobble, and that a stumble plus a carried prop leave the eye, hold point and position untouched. It found a real bug: a stumble requested in the same frame the body changed locomotion state could be overwritten before it started; fixed
- `tests/avatar_lineup.tscn`: real-window screenshots of the four coworkers in eight poses, first-person hands (empty, carrying, jab) and a remote player carrying a layer, all reviewed by eye. It found that the hands were first placed below the bottom of the camera view; fixed
- `tests/task_board_test.tscn` (pure logic, no window): 20 checks, 0 failed. Empty room, partial progress, done, wrong order, swapped signs, repairing a mistake, unknown sockets, and that every placement in the catalog names a socket and prop the venue really builds
- `tests/m0_selftest.tscn`, headless and in a real window: 46 checks, 0 failed, nothing on stderr. Interaction rules on the new venue, 20 full cycles, exactly six props with none duplicated
- `tests/run_net_tests.ps1`: 39 checks, 0 failed, nothing on stderr. Real separate Godot processes over loopback: 4 player lobby (and each client sees the coordinator walk), two clients racing for one prop for 24 rounds, host racing a client for 24 rounds, a client leaving while holding, a client killed while holding, out-of-range and teleport rejection
- `tests/screenshots.tscn`: nine real-window screenshots of the venue, reviewed by eye (ballroom, stage, dessert station, guest table, photo corner, back of house, task board with progress and a wrong sign, ghost preview)

## Bugs found by real runs and fixed

- M0: placing or resetting a prop looked fine headless but did nothing in a real window (a frozen body's transform set between physics steps is overwritten). Now all prop transforms are set inside the physics step
- M1: the host sent to peers that were mid-disconnect and logged engine errors. It now sends only to fully connected peers
- This increment: the socket pad flickered against the top of a cake layer (same height). The pad is now lifted and hidden once a layer sits on it. The first lighting pass was far too bright and washed out; reduced. The flower arch covered the banner names; lowered

## What is not built (do not assume it works)

- Shift timer, briefing, claim selection and the promise card
- Suspicion, witness line of sight, attention, evidence, the frosting spill, the distraction display, cleaning, the final inspection, scoring, payout and the debrief
- Ceremony audio task and event cards
- Solo balancing (counts and carry rules do not scale with player count yet)
- Final art, sound or music. The venue and the workers are original primitives (placeholder)

## Not checked

- Anyone playing it: movement, aiming and carrying feel, and whether the venue reads well to a new player, are unknown
- Clicking Host and Join in the real menu (the tests call the same functions)
- Added latency and packet loss; two different PCs or networks; joining while a prop is held or placed; the host restarting with clients connected
- Whether the coordinator's route ever clips furniture (the route was chosen by hand and only seen in screenshots)
- Windows export (no export templates installed)

## Known limitations

- Movement is client-reported with a host sanity check (speed and bounds), not host-authoritative yet. See `docs/decisions/0003-network-architecture.md`
- The stage and sound desk area cannot be walked onto (the stage has no steps)
- Doors are open archways, so nothing blocks line of sight between public and private areas yet
- Players do not collide with props
- The venue is built in code; move to editor scenes before real art
- Others see a held prop at a fixed distance from the holder
- No request rate limits

## Next small playable increment (brief prompt 2)

The cake incident: a loose layer falls, leaves a frosting spill, the coordinator may see it (line of sight and attention), suspicion changes, the photo display distracts her, the spill is cleaned, the cake is restored, and a debrief log lists what happened. Needs the design decisions in `docs/decisions/0001-pack-review-findings.md` first.

## How to try it

Double-click `play_solo.bat`, or `play_two_windows.bat` for two windows (Esc, Host a game in one, Join in the other). Windows export is not set up.
