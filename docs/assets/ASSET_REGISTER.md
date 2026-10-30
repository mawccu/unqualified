# Asset register

Every visual and audio asset in the project, where it came from, and its status.
Rule from the art briefs: nothing third-party is downloaded, bought, copied or
imported without an identified source, a saved licence and an entry here.

Status words: PLACEHOLDER (original, temporary, to be replaced), FINAL.

## Current assets

| Asset | Where | Source | Licence | Status |
| --- | --- | --- | --- | --- |
| Worker character model: rigged low-poly body, 4 hair styles, 7 accessories, 9 clips | `assets/characters/worker.glb` | Original. Authored from scratch in Blender 4.5.10 by `tools/blender/make_worker.py`; clips baked from `scripts/avatar/worker_animations.gd` | Project's own | PLACEHOLDER (script-made, to be replaced by a character artist, see `WORKER_GLB_CONTRACT.md`) |
| Fallback worker mannequin (code-built, faceless) | `scripts/avatar/worker_avatar.gd` | Original, Godot primitives | Project's own | Debug and test fixture only |
| Worker animation clips (code-authored source) | `scripts/avatar/worker_animations.gd` | Original keyframes | Project's own | PLACEHOLDER |
| Costume palette | `scripts/avatar/worker_costumes.gd` | Original | Project's own | PLACEHOLDER |
| First-person interaction hands | `scripts/avatar/first_person_hands.gd` | Original primitives | Project's own | PLACEHOLDER |
| Coordinator NPC body | `scripts/npc/coordinator.gd` | Original primitives | Project's own | PLACEHOLDER |
| Hotel venue: ballroom, stage, tables, chairs, dessert station, photo corner, back of house | `scripts/venue/venue_builder.gd` | Original primitives and Label3D text | Project's own | PLACEHOLDER |
| Cake layers, table signs | `scripts/core/session.gd` | Original primitives | Project's own | PLACEHOLDER |
| Fonts | Godot's built-in default font | Bundled with Godot 4.7.2 | Godot's licence (MIT) with its bundled font licences | PLACEHOLDER |

No sounds, music, textures or third-party models are used.

## Tools used to make assets (not part of the game)

| Tool | Version | Source | Licence | Checked |
| --- | --- | --- | --- | --- |
| Blender (portable, run headless) | 4.5.10 LTS | https://download.blender.org/release/Blender4.5/blender-4.5.10-windows-x64.zip | GPL (the tool); assets made with it are the author's | SHA-256 verified against the official `blender-4.5.10.sha256` (`ef6d846b8015f47ade6df3f9322ce17419080a5d922fa562b6c966064fe30dce`) |

## Sourcing log

No third-party asset has entered the repository. Repositories named in the visual
reset brief were not used: Tumble Guys (MIT, Unity) and Tabletop Club (MIT, Godot)
are reference reading only, and Michelin Mayhem has no visible licence and must
not be copied.

Before any external asset is added, record here: the listing URL, the licence or
receipt saved with the asset, author, permitted use, whether it was modified,
format, polycount, scale and orientation, and the Godot import result.
