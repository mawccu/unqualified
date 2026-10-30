# Asset register

Every visual and audio asset in the project, where it came from, and its status.
Rule from the art brief: nothing third-party is downloaded, bought or imported
without an identified source, a saved licence and an entry here.

Status words: PLACEHOLDER (original, temporary, to be replaced), FINAL.

## Current assets

| Asset | Where | Source | Licence | Status |
| --- | --- | --- | --- | --- |
| Worker avatar: body, four costumes, accessories, faces | `scripts/avatar/worker_avatar.gd`, `worker_costumes.gd` | Original, built from Godot primitives in code (capsules, spheres, boxes) | Project's own | PLACEHOLDER |
| Worker animation clips (idle, walk, sprint, carry idle and walk, interact, clean, stumble, celebrate) | `scripts/avatar/worker_animations.gd` | Original keyframes authored in code | Project's own | PLACEHOLDER |
| First-person hands | `scripts/avatar/first_person_hands.gd` | Original primitives | Project's own | PLACEHOLDER |
| Coordinator NPC body | `scripts/npc/coordinator.gd` | Original primitives | Project's own | PLACEHOLDER |
| Hotel venue: ballroom, stage, tables, chairs, dessert station, photo corner, back of house | `scripts/venue/venue_builder.gd` | Original primitives and Label3D text | Project's own | PLACEHOLDER |
| Cake layers, table signs | `scripts/core/session.gd` | Original primitives | Project's own | PLACEHOLDER |
| Fonts | Godot's built-in default font | Bundled with Godot 4.7.2 | Godot's licence (MIT) with its bundled font licences | PLACEHOLDER |

No sounds, music, textures or third-party models are used yet.

## Sourcing log

None. The art brief lists marketplace search terms and a checklist for when real
assets are wanted. Before any asset enters the repository record here: the
listing and its licence terms (commercial use in a shipped game), a copy or link
saved with the asset, polycount, scale, orientation, skeleton and clips verified
in Godot 4.7.2 for characters, and whether it matches the palette.

## Notes

The worker avatar keeps the joint names and clip names a real rigged model would
need (`Hips`, `Torso`, `HeadPivot`, `ArmL/ElbowL`, `LegL`, and so on). A rigged
`.glb` can replace the primitives and keep the same animation state machine.
