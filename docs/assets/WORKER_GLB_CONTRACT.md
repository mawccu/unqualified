# Worker model contract

The game shows its workers from `res://assets/characters/worker.glb`. Anything
that follows this contract can replace the current model with no code change:
`GlbWorker` validates the file when the game starts and falls back to a code-built
mannequin (with a warning) if it does not follow the contract.

The test `tests/glb_worker_test.tscn` checks every point below against the real
file. Run it after replacing the model.

## What the file must contain

- **Format:** glTF binary (`.glb`), Y up, 1 unit = 1 metre. Authored facing the
  glTF forward direction (+Z); the game turns it half way round.
- **Size:** about 1.7 m tall, feet on the floor (lowest point at y = 0).
- **One `Skeleton3D`** with these bones (extra bones are fine):
  `Hips`, `Torso`, `Head`, `ArmL`, `ElbowL`, `HandL`, `ArmR`, `ElbowR`, `HandR`,
  `LegL`, `FootL`, `LegR`, `FootR`. Left is the character's left.
- **One `AnimationPlayer`** holding these nine clips by exactly these names, with
  roughly these lengths (seconds). Loop flags are set by the game, not the file.

  | Clip | Length | Plays |
  | --- | --- | --- |
  | `idle` | 2.6 | loops |
  | `walk` | 0.8 | loops |
  | `sprint` | 0.5 | loops |
  | `carry_idle` | 2.2 | loops |
  | `carry_walk` | 0.9 | loops |
  | `clean` | 1.0 | loops until stopped |
  | `interact` | 0.5 | once |
  | `stumble` | 0.4 | once (a recoverable slip) |
  | `celebrate` | 1.2 | once |

- **Materials** with these exact names. The ones marked "tinted" are recoloured
  per player from `scripts/avatar/worker_costumes.gd`; the others keep their colour.

  | Material | Tinted | Used for |
  | --- | --- | --- |
  | `Shirt` | yes | shirt, sleeves |
  | `Vest` | yes | vest, blazer, apron |
  | `Trousers` | yes | trousers |
  | `Shoes` | yes | shoes |
  | `Skin` | yes | face, hands |
  | `Hair` | yes | hair, beanie |
  | `Accent` | yes | the one small accent colour |
  | `Eye`, `Detail` | no | eyes, mouth, headset, radio, belt |

- **Costume meshes**, separate `MeshInstance3D`s so the game can show one per
  player. Names are matched ignoring case:
  - hair: `Hair_short`, `Hair_pompadour`, `Hair_ponytail`, `Hair_beanie`
    (any mesh whose name starts with `Hair_<style>` belongs to that style)
  - accessories: `Acc_bow_tie`, `Acc_lanyard`, `Acc_apron`, `Acc_headset`,
    `Acc_radio`, `Acc_belt`, `Acc_one_glove` (names may continue, like
    `Acc_lanyard_badge`)

## What the model must not do

- It must not carry collision. The player's collision shape, eye and hold point
  are separate and never depend on the mesh.
- It must not contain cameras or lights.
- Its animation must not move the root off the origin (the game moves the body).

## The model that is in the repository now

`assets/characters/worker.glb` is an original model authored for this project in
Blender 4.5 by `tools/blender/make_worker.py`: chunky bevelled low-poly blocks,
flat shaded, 13 bones, rigid per-bone skinning, 51 parts, 640 KB. It is a
placeholder made by a script, not hand-sculpted art. A human character artist
should replace it, to this contract.

Rebuild it:

```
godot --headless --path . -s tools/dump_worker_clips.gd -- tools/blender/worker_clips.json
blender --background --factory-startup --python tools/blender/make_worker.py -- \
    tools/blender/worker_clips.json assets/characters/worker.glb [preview_folder]
```

The first command exports the nine clips from the game's own definitions
(`scripts/avatar/worker_animations.gd`) so the Blender rig plays exactly the same
motion. Blender 4.5 LTS (GPL) was used from a portable copy in `C:\Users\USER\tools`;
it is not part of the repository.

## Command line switches

After `--`: `--fallback-workers` forces the code-built mannequin;
`--detailed-workers` makes that mannequin the older, busier debug version.
