"""Convert the project's glTF models to FBX for Unity.

Run headless, one model per call (keeps every export independent):
  blender --background --factory-startup --python convert_glb_to_fbx.py -- <in.glb> <out.fbx> <kind>

kind:
  prop       static model: gets world-scale cube-projected UVs (1 UV unit = 1 metre)
             so the tileable PBR textures map at a predictable size in Unity
  character  rigged worker: keeps its skeleton, skin and every animation action

Why FBX: Unity's model importer can then generate lightmap UVs for baked lighting
and import the rig as a Generic Mecanim avatar with separate clips. Unity has no
built-in glTF importer.
"""
import sys

import bpy

argv = sys.argv[sys.argv.index("--") + 1:]
SOURCE, TARGET, KIND = argv[0], argv[1], argv[2]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SOURCE)

meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]

if KIND == "prop":
    # Cube-project UVs at world scale so a material tiling of 1 means 1 metre.
    for obj in meshes:
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.cube_project(cube_size=1.0, scale_to_bounds=False, correct_aspect=False)
        bpy.ops.object.mode_set(mode="OBJECT")

actions = list(bpy.data.actions)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(
    filepath=TARGET,
    use_selection=False,
    apply_scale_options="FBX_SCALE_UNITS",
    axis_forward="-Z",
    axis_up="Y",
    bake_space_transform=KIND == "prop",
    object_types={"MESH", "ARMATURE", "EMPTY"},
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    add_leaf_bones=False,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    bake_anim=KIND == "character",
    bake_anim_use_all_actions=KIND == "character",
    bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True,
    path_mode="AUTO",
)
print("CONVERTED %s -> %s (%s, %d meshes, %d actions)" % (SOURCE, TARGET, KIND, len(meshes), len(actions)))
