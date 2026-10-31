"""The flat's sliding window, in parts (original, made here): the frame with its
reveal through the wall, one sash (used twice: fixed on the back track, sliding
on the front one), a pull handle, and the security grille outside.

  blender --background --factory-startup --python make_window.py -- <out_dir>

Frame: 1.3 x 1.12, the opening 1.16 x 0.96 starting 0.08 up. Front (-Y) faces
the room; the reveal runs back to +Y 0.2 (the outer face of a 0.15 m wall plus
the trim). Each sash is 0.6 wide; its origin is its bottom centre.
"""
import os
import sys

import bpy

OUT = sys.argv[sys.argv.index("--") + 1:][0]
os.makedirs(OUT, exist_ok=True)

COLOURS = {"Trim": (0.82, 0.80, 0.74), "Window glass": (0.62, 0.70, 0.74), "Steel": (0.55, 0.56, 0.58), "Grille black": (0.08, 0.08, 0.09)}
MATS = {}
parts = []


def mat(name):
    if name not in MATS:
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        r, g, b = COLOURS[name]
        bsdf.inputs["Base Color"].default_value = (r, g, b, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.8
        MATS[name] = m
    return MATS[name]


def box(size, loc, material, bevel=0.004):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    o = bpy.context.object
    o.scale = size
    bpy.ops.object.transform_apply(scale=True)
    o.data.materials.append(mat(material))
    if bevel > 0:
        mod = o.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = "ANGLE"
    parts.append(o)


def cyl(radius, depth, loc, material, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=depth, location=loc, rotation=rot, vertices=10)
    o = bpy.context.object
    o.data.materials.append(mat(material))
    parts.append(o)


def export(name):
    global parts
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.convert(target="MESH")
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=1.0, scale_to_bounds=False, correct_aspect=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + ".fbx"), use_selection=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, object_types={"MESH"},
                             mesh_smooth_type="FACE", path_mode="AUTO")
    bpy.ops.object.delete()
    parts = []
    print("PROP", name)


import math

bpy.ops.wm.read_factory_settings(use_empty=True)

# Frame: the room-side architrave, and the reveal lining the hole through the wall.
box((1.3, 0.04, 0.08), (0, -0.04, 0.04), "Trim", 0.008)
box((1.3, 0.04, 0.08), (0, -0.04, 1.08), "Trim", 0.008)
box((0.08, 0.04, 1.12), (-0.61, -0.04, 0.56), "Trim", 0.008)
box((0.08, 0.04, 1.12), (0.61, -0.04, 0.56), "Trim", 0.008)
box((1.22, 0.24, 0.02), (0, 0.1, 0.07), "Trim", 0.003)       # sill
box((1.22, 0.24, 0.02), (0, 0.1, 1.05), "Trim", 0.003)       # head
box((0.02, 0.24, 0.98), (-0.59, 0.1, 0.56), "Trim", 0.003)   # jambs
box((0.02, 0.24, 0.98), (0.59, 0.1, 0.56), "Trim", 0.003)
# Two tracks along the sill and head.
for y in (-0.005, 0.045):
    box((1.16, 0.006, 0.012), (0, y, 0.086), "Steel", 0.0)
    box((1.16, 0.006, 0.012), (0, y, 1.034), "Steel", 0.0)
box((1.3, 0.16, 0.03), (0, -0.08, 0.0), "Trim", 0.006)       # inner ledge
export("window_frame")

# A sash: four bars round a pane of grimy glass.
box((0.6, 0.035, 0.04), (0, 0, 0.02), "Trim", 0.004)
box((0.6, 0.035, 0.04), (0, 0, 0.92), "Trim", 0.004)
box((0.04, 0.035, 0.94), (-0.28, 0, 0.47), "Trim", 0.004)
box((0.04, 0.035, 0.94), (0.28, 0, 0.47), "Trim", 0.004)
box((0.53, 0.008, 0.86), (0, 0, 0.47), "Window glass", 0.0)
export("window_sash")

# A pull handle, screwed to the sliding sash's stile.
box((0.025, 0.02, 0.14), (0, -0.01, 0.07), "Steel", 0.004)
box((0.02, 0.035, 0.02), (0, -0.03, 0.02), "Steel", 0.003)
box((0.02, 0.035, 0.02), (0, -0.03, 0.12), "Steel", 0.003)
box((0.02, 0.012, 0.12), (0, -0.045, 0.07), "Steel", 0.003)
export("window_handle")

# The grille outside: bars a hand's width apart, two flats across.
for i in range(10):
    x = -0.54 + i * 0.12
    cyl(0.009, 1.0, (x, 0, 0.56), "Grille black")
for z in (0.14, 0.98):
    box((1.2, 0.012, 0.035), (0, 0, z), "Grille black", 0.002)
export("window_grille")
print("DONE")
