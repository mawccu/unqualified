# 0002 Engine version and M0 choices

Date: 2026-10-30. Status: ACCEPTED (pack default, no existing project to conflict with).

## Engine

Godot 4.7.2 stable, non-Mono, typed GDScript. Installed from the winget package `GodotEngine.GodotEngine` version 4.7.2. Pinned: do not upgrade without a new decision note. Greenfield project, so the pack default applies.

## Choices made while building M0

- Input actions are created in code, not in `project.godot`. Reason: the hand-written input map format is long and version sensitive. Gameplay only reads action names, so a rebinding screen can replace the defaults later.
- The graybox is built in code. Reason: no editor was needed to produce a runnable result. Revisit when art is added.
- Test runs as a scene (`tests/m0_selftest.tscn`), not with `-s`. Reason: autoloads are not available to scripts started with `-s`. The first attempt hung because of that. The scene version has a 60 second watchdog that fails instead of hanging.
- Players do not collide with props yet. See IMPLEMENTATION_STATUS.
- The ray that decides what the crosshair targets is 3.2 m, while the service accepts requests up to 3.6 m from the eye. The gap stops a prop whose centre is slightly further than its surface from being rejected.

## Export

Windows export templates are not installed. Export is therefore unverified. Procedure is in IMPLEMENTATION_STATUS.
