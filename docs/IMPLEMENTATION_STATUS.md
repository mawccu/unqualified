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

## Visual reset (from the visual reset brief)

The earlier look was rejected as programmer art, and rightly. What changed:

- **An authored, rigged worker model** (`assets/characters/worker.glb`, original, made in Blender): chunky bevelled low-poly body, a small readable face, a vest over an off-white shirt, one small accent colour, 13 bones, nine clips, four hair styles, seven accessories. It sits behind a **presenter boundary** (`WorkerPresenter`, `GlbWorker`, `WorkerFactory`), so a better model from a character artist can replace it by following `docs/assets/WORKER_GLB_CONTRACT.md` with no code change. If the file is missing or breaks the contract the game warns and falls back
- **The code-built sphere/capsule worker is now only a fallback**: faceless mannequin, kept as a debug and test fixture (`--fallback-workers`; the older busier version is `--detailed-workers`)
- **Workers are four uniforms, not four clown colours**: charcoal, navy, brown and forest vests over off-white shirts and charcoal trousers, one small accent each, told apart by silhouette (hair, hat, apron, headset)
- **First-person hands are hidden** at rest, walking and carrying. For an action one small hand (half the old size) rises in the lower right corner and withdraws. A carried prop sits low in the right corner, further away
- **Calmer venue**: warm cream, walnut, muted burgundy and brass in the ballroom; cooler grey-green, plastic crates and steel behind the scenes; one soft warm key light, restrained bloom, dimmer bulbs. A defined **hero view** from the west archway
- **HUD**: the task board moved to the bottom-left, 19% of the screen width, see-through, smaller text, so it no longer covers signage

Secondary motion is limited to a loose tie, lanyard or apron on the fallback; the authored model has none yet. Not done here: no real artist-made character (the model is script-made and a placeholder), no footstep syncing, no facial animation, no ragdoll, nothing triggers a stumble yet, and stumble, clean and celebrate are not sent to other players.

## Checks actually run

- `godot --headless --path . --import`: no errors
- `tests/glb_worker_test.tscn` (headless): 88 checks, 0 failed. The real `worker.glb` against the contract: all 13 bones, all nine clips with matching lengths, every tinted material, every hair style and accessory, correct size and facing, costume tinting per slot, only the right hair and accessories shown, the state machine on the authored model, and the fallback switch
- `tests/visual_check.tscn` (real window): 11 checks, 0 failed. Rendered twice with and without the thing being judged: idle hands cover 0.00% of the centre lower third; a carried layer covers 7.2% of the central block (limit 20%) and nothing in the crosshair zone; one hand appears low in the right corner for an action and withdraws; the task board is 19% of the width, alpha 0.5, and overlaps none of the signs in the hero view
- `tests/avatar_test.tscn` (headless, fallback body): 99 checks, 0 failed. Spring maths (settles, overshoots once, never exceeds its limit, survives a huge time step), four distinct costumes that all build every joint, body proportions, all nine clips (every track points at a real joint, values finite and in range, exact loop seams, stumble 0.40 s), every state transition, the wobble, and that a stumble plus a carried prop leave the eye, hold point and position untouched. It found a real bug: a stumble requested in the same frame the body changed locomotion state could be overwritten before it started; fixed
- `tests/avatar_lineup.tscn`: real-window screenshots of the four coworkers in eight poses, first-person hands (empty, carrying, jab) and a remote player carrying a layer, all reviewed by eye. It found that the hands were first placed below the bottom of the camera view; fixed
- `tests/task_board_test.tscn` (pure logic, no window): 20 checks, 0 failed. Empty room, partial progress, done, wrong order, swapped signs, repairing a mistake, unknown sockets, and that every placement in the catalog names a socket and prop the venue really builds
- `tests/m0_selftest.tscn`, headless and in a real window: 46 checks, 0 failed, nothing on stderr. Interaction rules on the new venue, 20 full cycles, exactly six props with none duplicated
- `tests/run_net_tests.ps1`: 39 checks, 0 failed, nothing on stderr. Real separate Godot processes over loopback: 4 player lobby (and each client sees the coordinator walk), two clients racing for one prop for 24 rounds, host racing a client for 24 rounds, a client leaving while holding, a client killed while holding, out-of-range and teleport rejection
- `tests/screenshots.tscn`: nine real-window screenshots of the venue, reviewed by eye (ballroom, stage, dessert station, guest table, photo corner, back of house, task board with progress and a wrong sign, ghost preview)

## Bugs found by real runs and fixed

- M0: placing or resetting a prop looked fine headless but did nothing in a real window (a frozen body's transform set between physics steps is overwritten). Now all prop transforms are set inside the physics step
- M1: the host sent to peers that were mid-disconnect and logged engine errors. It now sends only to fully connected peers
- Visual reset: the headless engine logged "Parameter material is null" at shutdown because the authored worker leaves tinted materials attached to its meshes; it now releases them as it leaves the scene. The authored worker first built a material copy for tinting; a fresh material is used instead
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
