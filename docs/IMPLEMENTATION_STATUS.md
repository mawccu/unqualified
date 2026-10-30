# Implementation status

Updated 2026-10-30.

## Current milestone

M0 foundation. Code written and checked by machine. Not yet played by a person.

## Versions

- Godot 4.7.2 stable (official build ed1daf0bf), Windows, Forward+ renderer
- Typed GDScript, with the untyped-declaration warning switched on in project settings

## What exists

- Graybox room (12 x 9 m), a side table with one cake section, a stand table with one socket
- First-person movement, mouse look, sprint
- Contextual prompt shown only when the action would be accepted
- Pick up, place on a socket (with ghost preview), drop, pick up again from the socket
- All of those go through `InteractionService.request()`, which validates and then applies. The player never changes props directly
- Pause menu on Esc with FOV and sensitivity sliders, saved to `user://settings.cfg`
- Mouse released when the window loses focus (opens the menu)
- Restart on F5 or from the menu
- Input actions are registered in code (`scripts/core/bindings.gd`), ready for a rebinding screen

## Checks actually run

- `godot --headless --path . --import`: all 8 classes registered, no errors
- `godot --headless --path . res://tests/m0_selftest.tscn` and the same scene in a real window: 46 checks, 0 failed, nothing on stderr. Covers out-of-range, stale revision, unknown target, unknown actor, hands full, second actor blocked on a held prop, place (and still placed 5 frames later), re-pickup, drop and settle, reset, and 20 full cycles each checking the reset position, with no duplicate props
- `tests/screenshots.tscn` renders four views in a real window (RTX 5060, Vulkan, Forward+) with no errors or warnings. Output reviewed by eye: prompts, ghost preview and placed prop look right

## Bugs found by the real-window run and fixed

- Placing a prop did not move it to the socket in a real window (it stayed at the hold point), and reset did not move it back to its start. Cause: a frozen body's transform set between physics steps is overwritten by the physics server. Fix: placement pins the prop inside `_physics_process`, and reset is applied on the next physics step. The first headless test passed both because it only looked at the position immediately, and headless did not reproduce the overwrite for reset. Lesson: physics behaviour must be checked in a real window, and after a few frames, not only headless

## Not checked

- Anyone playing it: how movement, aiming and carrying feel is unknown
- Focus loss to menu behaviour with a real window losing focus
- Slider changes updating the camera live
- Windows export. No export templates are installed yet, so no exported `.exe` has been built or run
- Anything on a second PC

## Looks wrong in the screenshots, not yet changed

- The held cake section is large in the lower right of the view (about 0.4 m wide at 0.95 m from the eye). Probably needs a smaller hold offset or scale. Left alone because it is a feel judgement for Malek

## Known limitations (deliberate for M0)

- The room and props are built in code (`scripts/core/session.gd`), not in editor scenes. Move them to scenes before art arrives
- Players do not collide with props (player collision mask is world only). This avoids getting stuck on dropped objects. Revisit in M1
- No crouch, no rotate-while-placing, no ping. Those arrive when a task needs them
- Menu pauses the whole tree. Correct for solo only. Multiplayer must not pause

## Windows export procedure (not yet run)

1. In the editor: Editor menu, Manage Export Templates, Download and Install (4.7.2).
2. Project menu, Export, Add Windows Desktop preset, Export Project.
3. Run the produced `.exe` on a PC without Godot installed.

## Next three tasks

1. Malek plays the build and reports how it feels (movement speed, carry position, prompt wording)
2. Decide on items 1 to 5 in `docs/decisions/0001-pack-review-findings.md`
3. M1: host and join over localhost, stable entity IDs already exist, add the network layer behind `InteractionService`
