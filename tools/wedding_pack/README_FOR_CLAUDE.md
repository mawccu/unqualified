# UNQUALIFIED — Wedding asset pack v0.1

Original Blender-created static art for the wedding vertical slice. This delivery contains individual glTF binary models, an assembled cutaway venue, two rendered previews, editable Blender source, reproducible generator scripts and a machine-readable asset manifest. No marketplace assets or external textures were used.

## Scope and limitations

Four workers and one coordinator share the same base body with uniform/accessory variations. They are STATIC joined meshes: no armatures, skin weights, animation clips, blend shapes or first-person hands. Do not replace animated player avatars with these meshes and claim animation is complete. Use them as approved visual reference or static NPC previews until rigging is done.

The venue is an ART LAYOUT, not a playable map. It is a roofless cutaway with the front/right edges open for inspection. Before gameplay use, close the bounds, build physics collision and navigation, check walking routes and add lighting. The Blender studio lights are excluded from the map GLB; reproduce suitable lighting in Godot. A studio render is not proof of identical engine appearance.

Props have material colours and mesh geometry. No collision, scripts, sound, particle systems, interaction sockets or gameplay state is included. Cable meshes are static coils, not simulated ropes. Each item is kept separate so integration can preserve host-authoritative game logic.

## Files

- `models/*.glb`: individual assets, origin generally at ground/contact centre. For item-specific bounds and collision suggestions, read `asset_manifest.json`.
- `wedding_venue_layout.glb`: assembled 12 x 10 metre cutaway with furniture and characters.
- `source/wedding_pack.blend`: editable master with hidden library meshes and visible venue instances.
- `source/build_worker.py` and `build_pack.py`: reproducible source. The build script runs in a fresh/background Blender session and clears that session's scene. Do not execute it inside an unsaved working scene.
- `previews/01_asset_inventory.png`: inventory presentation.
- `previews/02_wedding_venue.png`: assembled venue preview.
- `asset_manifest.json`: asset IDs, triangle counts, dimensions, purposes and collision hints.

## Gameplay mapping

| Game system | Assets | Integration requirement |
| --- | --- | --- |
| Dessert | cake_base, cake_middle, cake_top, cake_topper, cake_stand, dessert_table | Separate pickup bodies and ordered sockets. Mesh appearance never determines completion. |
| Evidence/cleanup | frosting_spill, cleaning_cloth, mop, mop_bucket | Spill should be non-solid, slightly above floor; host owns visibility and cleanup completion. |
| Seating | guest_table, guest_chair, table_sign_1..3, flower_centerpiece | Preserve stable table identity and movable sign IDs. |
| Audio | sound_desk, audio_mixer, speaker, cable_red/blue/yellow | Add controls and plug sockets independently; mixer currently exports as one mesh. |
| Cover-up | photo_display, flower_arch | Trigger distraction through existing gameplay service; decoration alone does nothing. |
| Coordinator | coordinator, clipboard | Rig first for animated patrols; perception and suspicion remain gameplay code. |
| Staff room | prep_table, storage_shelf, storage_crate, staff_doorway | Crate can obstruct an aisle; shelf/doorway need multiple simple collision shapes. |
| Venue | floor_tile_2m, wall_panel_2m, stage_platform, stage_steps | Reuse modules; ramp collision over stairs; verify a player capsule can traverse stage. |

## Godot import

1. Work in an isolated art-integration scene first. Import selected models into `assets/wedding/`; avoid importing source scripts and Blender runtime.
2. Units are metres. Blender source is Z-up; glTF export converts to Y-up. Do not apply another axis conversion blindly. Character fronts face Blender -Y (glTF +Z); rotate only the visual child if the controller expects -Z forward.
3. Use inherited/wrapper scenes for collision and scripts. Never hand-edit imported generated scenes.
4. For movable items use primitive or convex collision; static concave collision is inappropriate for dynamic carried props.
5. Keep network entity IDs and interaction service unchanged. Replace visuals beneath existing gameplay objects.
6. Test one cake layer first: pickup, placement, drop and remote display. Then expand to the remaining props.
7. Check material slot counts and draw calls. These are first art-pass assets, not optimized shipping meshes; combine compatible materials/atlas only after the visual direction is approved.

## Prompt

Read this README and asset_manifest.json. Import this pack in a separate Godot art-review scene, light it, and render it at the actual player camera height. Integrate the cake, stand, tables and signs as visual replacements while preserving host-authoritative interactions. Report actual import results and screenshots. Treat characters as unrigged static assets and the venue as a cutaway art layout. Do not claim finished animations, navigation or collision. Ask the owner to judge the art-review screenshots before propagating this visual style into every job.
