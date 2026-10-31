# Asset register

Where the game's assets come from. There was no register before this pass. It lists what the workshop and polish passes added, plus the sources of the older sets as recorded in the project notes.

## Made for this project (original, editable source in the repo)

| Asset | Source | Output |
|---|---|---|
| Beds (`bed_made`, `bed_slept_in`) | `Tools/Blender/make_bedding.py` | `Assets/Art/Apartment/bed_*.fbx` |
| Window (`window_frame`, `window_sash`, `window_handle`, `window_grille`) | `Tools/Blender/make_window.py` | `Assets/Art/Apartment/window_*.fbx` |
| Alley backdrop (`alley_wall`, `alley_sky`) | `Tools/make_alley_backdrop.py` (Python and PIL, drawn procedurally) | `Assets/Art/Apartment/alley_*.png` |
| Workshop props, parts, gadgets, parcels | `Tools/Blender/make_workshop_props.py` | `Assets/Art/Workshop/*.fbx` |
| Workshop signs, blueprint sheet | `Tools/make_workshop_signs.py` | `Assets/Art/Workshop/*.png` |
| World text shader | Written by hand | `Assets/Art/Workshop/WorldText.shader` |
| Office props, signs | `Tools/Blender/make_office_props.py`, `Tools/make_office_signs.py` | `Assets/Art/Props`, `Assets/Art/Office` |
| Apartment props | `Tools/Blender/make_apartment_props.py` | `Assets/Art/Apartment` |
| Hands | `Tools/Blender/make_hands.py` | `Assets/Art/Hands` |
| Rent bill texture | `Tools/make_bill_texture.py` | `Assets/Art/Apartment/bill_print.png` |
| Help-pages printout texture (quality gate) | `Tools/make_printout_texture.py` (reads the help pages from `CaseLibrary.cs`) | `Assets/Art/Office/printout_print.png`, material `Printout print` |
| Binder spine labels (quality gate) | `Tools/make_binder_spines.py` | `Assets/Art/Office/spine_*.png` |
| Clean-shaven worker bodies (quality gate) | Derived in the Editor from the four rigged workers (their licence row below applies) by `Assets/Editor/WorkerFaces.cs`: the same mesh without the two moustache pieces; rebuilt with the worker library | `Assets/Art/Characters/Faces/*_clean.asset` |
| Head extras: bun, hair puffs, glasses (quality gate) | Unity primitives placed at runtime by `WorkerAvatar.AddExtras` (no files) | none |
| All sound effects | Synthesised at runtime in `ProceduralAudio.cs` and `Tones.cs` | No audio files |

The signs and textures drawn with PIL use the Windows Arial and Comic Sans fonts to draw text into the images. The images are original; the font files are not shipped.

## From earlier sessions (see the project notes)

| Set | Where it came from | Note |
|---|---|---|
| Venue and character pack (`Assets/Art/Models`, `Assets/Art/Characters`) | Converted from the Godot project's glb files (`Tools/Blender/convert_glb_to_fbx.py`) | Licence: as recorded for the Godot project. Not re-checked in this pass. |
| 4 rigged workers and clips | From "Astra" (as recorded in the earlier notes) | Licence not re-checked in this pass. |
| 4 PBR texture sets (wood planks, wallpaper, fabric, brushed brass) | From "Astra" (as recorded in the earlier notes) | Licence not re-checked in this pass. |

Before a public release, the three rows above need their licences confirmed and written here.
