"""Model the apartment props (original, made here) and export each as FBX for Unity.

  blender --background --factory-startup --python make_apartment_props.py -- <out_dir>

Style matches the wedding pack: chunky shapes, soft bevels, flat colours named
after their surface (Unity's MaterialLibrary turns each name into one shared
material). Every prop has its origin at the middle of its base and its front
toward Blender -Y, which arrives in Unity facing +Z like the pack's props.
World-scale cube UVs (1 UV unit = 1 m) are added for any tiling texture.
"""
import math
import os
import sys

import bpy

OUT = sys.argv[sys.argv.index("--") + 1:][0]
os.makedirs(OUT, exist_ok=True)

COLOURS = {
    "Couch fabric": (0.42, 0.30, 0.18), "Couch cushion": (0.48, 0.35, 0.21), "Walnut": (0.449, 0.304, 0.206),
    "Mattress": (0.80, 0.76, 0.66), "Mattress stain": (0.62, 0.55, 0.38), "Pillow": (0.86, 0.85, 0.80),
    "Blanket": (0.30, 0.38, 0.50), "Laminate": (0.58, 0.47, 0.34), "Steel": (0.55, 0.56, 0.58),
    "Beige plastic": (0.76, 0.72, 0.62), "Screen": (0.08, 0.10, 0.10), "Keycaps": (0.32, 0.31, 0.29),
    "Charcoal fabric": (0.16, 0.16, 0.17), "Door paint": (0.47, 0.38, 0.29), "Brass knob": (0.75, 0.6, 0.3),
    "Trim": (0.82, 0.80, 0.74), "Counter": (0.66, 0.63, 0.56), "Cabinet": (0.50, 0.58, 0.48),
    "Fridge white": (0.86, 0.84, 0.78), "Rubber seal": (0.12, 0.12, 0.12), "Paper": (0.94, 0.93, 0.88),
    "Red stamp": (0.72, 0.13, 0.12), "Ink": (0.25, 0.25, 0.28), "Cardboard": (0.70, 0.54, 0.34),
    "Grease": (0.52, 0.38, 0.22), "Warm terracotta": (0.66, 0.36, 0.25), "Soil": (0.22, 0.16, 0.11),
    "Dead leaf": (0.45, 0.34, 0.16), "Window glass": (0.62, 0.70, 0.74), "Bulb": (1.0, 0.9, 0.7),
    "Cord": (0.1, 0.1, 0.1), "Trash bag": (0.07, 0.07, 0.08), "Rug": (0.48, 0.20, 0.18), "Rug border": (0.70, 0.55, 0.35),
    "Sink steel": (0.68, 0.69, 0.70),
}
MATS = {}


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


parts = []


def finish(obj, material, bevel):
    obj.data.materials.append(mat(material))
    if bevel > 0:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = "ANGLE"
    parts.append(obj)
    return obj


def box(size, loc, material, bevel=0.012, rot=(0, 0, 0)):
    """size (x, y, z) in metres; loc is the box centre."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    o = bpy.context.object
    o.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(o, material, bevel)


def cyl(radius, depth, loc, material, bevel=0.006, rot=(0, 0, 0), verts=24):
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=depth, location=loc, rotation=rot, vertices=verts)
    return finish(bpy.context.object, material, bevel)


def sphere(radius, loc, material, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, location=loc, segments=20, ring_count=12)
    o = bpy.context.object
    o.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    bpy.ops.object.shade_smooth()
    return finish(o, material, 0)


def export(name):
    global parts
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.convert(target="MESH")  # apply bevels
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    # Origin at the world origin = the middle of the prop's base.
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


bpy.ops.wm.read_factory_settings(use_empty=True)

# Sagging couch: front toward -Y, back cushion toward +Y.
box((2.0, 0.85, 0.32), (0, 0, 0.26), "Couch fabric", 0.04)
for i, x in enumerate((-0.62, 0.0, 0.62)):
    sag = 0.05 if i == 1 else 0.0
    box((0.6, 0.66, 0.14), (x, -0.06, 0.48 - sag), "Couch cushion", 0.05, rot=(0.04 if i == 1 else 0.0, 0, 0))
box((2.0, 0.24, 0.5), (0, 0.31, 0.62), "Couch fabric", 0.06, rot=(-0.12, 0, 0))
for x in (-0.94, 0.94):
    box((0.2, 0.85, 0.36), (x, 0, 0.56), "Couch fabric", 0.06)
for x in (-0.9, 0.9):
    for y in (-0.33, 0.33):
        cyl(0.03, 0.1, (x, y, 0.05), "Walnut")
export("couch")

# Mattress on the floor with a pillow and a balled-up blanket.
box((0.95, 1.95, 0.2), (0, 0, 0.1), "Mattress", 0.05)
box((0.4, 0.3, 0.004), (0.18, 0.25, 0.201), "Mattress stain", 0.0)
box((0.6, 0.35, 0.12), (0, 0.72, 0.26), "Pillow", 0.05)
sphere(0.32, (0.1, -0.35, 0.3), "Blanket", (1.1, 1.4, 0.45))
sphere(0.2, (-0.2, -0.65, 0.26), "Blanket", (1.2, 1.0, 0.5))
export("mattress")

# Cheap desk.
box((1.2, 0.6, 0.03), (0, 0, 0.735), "Laminate", 0.006)
for x in (-0.56, 0.56):
    for y in (-0.26, 0.26):
        box((0.035, 0.035, 0.72), (x, y, 0.36), "Steel", 0.004)
box((1.1, 0.02, 0.25), (0, 0.27, 0.55), "Laminate", 0.004)
export("desk")

# Old beige monitor: screen faces -Y (toward the person at the desk).
box((0.44, 0.34, 0.36), (0, 0.06, 0.22), "Beige plastic", 0.03)
box((0.36, 0.01, 0.27), (0, -0.115, 0.225), "Screen", 0.004)
box((0.2, 0.2, 0.03), (0, 0.04, 0.015), "Beige plastic", 0.008)
box((0.08, 0.08, 0.06), (0, 0.06, 0.05), "Beige plastic", 0.008)
export("computer_monitor")

box((0.44, 0.15, 0.03), (0, 0, 0.015), "Beige plastic", 0.006)
for row in range(4):
    for col in range(12):
        box((0.026, 0.026, 0.012), (-0.18 + col * 0.033, -0.05 + row * 0.033, 0.034), "Keycaps", 0.003)
export("computer_keyboard")

box((0.19, 0.44, 0.42), (0, 0, 0.21), "Beige plastic", 0.012)
box((0.15, 0.01, 0.06), (0, -0.221, 0.34), "Keycaps", 0.003)
cyl(0.012, 0.01, (0.05, -0.222, 0.22), "Red stamp", 0.0, rot=(math.pi / 2, 0, 0), verts=12)
export("computer_tower")

# Office chair with a split seat.
box((0.46, 0.46, 0.09), (0, 0, 0.47), "Charcoal fabric", 0.03)
box((0.44, 0.08, 0.48), (0, 0.24, 0.8), "Charcoal fabric", 0.03, rot=(-0.12, 0, 0))
cyl(0.03, 0.38, (0, 0, 0.24), "Steel")
for k in range(5):
    a = k / 5 * math.tau
    box((0.3, 0.04, 0.03), (math.cos(a) * 0.15, math.sin(a) * 0.15, 0.05), "Steel", 0.006, rot=(0, 0, a))
    sphere(0.025, (math.cos(a) * 0.29, math.sin(a) * 0.29, 0.025), "Rubber seal")
export("desk_chair")

# Front door leaf: origin on the hinge side so it swings; extends toward +X.
box((0.9, 0.045, 2.04), (0.45, 0, 1.02), "Door paint", 0.008)
for z in (0.55, 1.45):
    box((0.62, 0.012, 0.66), (0.45, -0.026, z), "Door paint", 0.01)
cyl(0.03, 0.08, (0.8, -0.05, 1.0), "Brass knob", 0.004, rot=(math.pi / 2, 0, 0))
cyl(0.012, 0.012, (0.45, -0.025, 1.6), "Brass knob", 0.0, rot=(math.pi / 2, 0, 0))
box((0.16, 0.006, 0.05), (0.45, -0.025, 1.3), "Brass knob", 0.0)
export("door_leaf")

box((0.08, 0.16, 2.12), (-0.5, 0, 1.06), "Trim", 0.01)
box((0.08, 0.16, 2.12), (0.5, 0, 1.06), "Trim", 0.01)
box((1.08, 0.16, 0.08), (0, 0, 2.12), "Trim", 0.01)
export("door_frame")

# Kitchen counter with dated green cabinets and a sink.
box((1.6, 0.6, 0.86), (0, 0, 0.43), "Cabinet", 0.01)
box((1.64, 0.64, 0.04), (0, 0.0, 0.88), "Counter", 0.008)
for x in (-0.6, -0.2, 0.2, 0.6):
    box((0.37, 0.01, 0.7), (x, -0.302, 0.42), "Cabinet", 0.008)
    box((0.1, 0.02, 0.02), (x, -0.315, 0.68), "Steel", 0.004)
box((0.5, 0.4, 0.02), (0.35, 0.0, 0.902), "Sink steel", 0.004)
cyl(0.015, 0.25, (0.35, 0.2, 1.02), "Sink steel")
box((0.02, 0.15, 0.02), (0.35, 0.13, 1.14), "Sink steel", 0.004)
export("kitchen_counter")

box((0.66, 0.66, 1.72), (0, 0, 0.86), "Fridge white", 0.04)
box((0.64, 0.01, 0.002), (0, -0.334, 1.15), "Rubber seal", 0.0)
box((0.03, 0.04, 0.4), (0.27, -0.35, 1.38), "Steel", 0.008)
box((0.03, 0.04, 0.6), (0.27, -0.35, 0.75), "Steel", 0.008)
export("fridge")

box((0.5, 0.36, 0.3), (0, 0, 0.15), "Beige plastic", 0.02)
box((0.3, 0.01, 0.2), (-0.06, -0.181, 0.15), "Screen", 0.004)
box((0.08, 0.01, 0.22), (0.17, -0.181, 0.15), "Keycaps", 0.004)
export("microwave")

# Paper things.
box((0.22, 0.11, 0.004), (0, 0, 0.002), "Paper", 0.0)
box((0.22, 0.02, 0.0045), (0, 0.035, 0.0025), "Red stamp", 0.0)
box((0.08, 0.02, 0.0045), (-0.04, -0.01, 0.0025), "Ink", 0.0)
export("envelope")

# The rent bill: an upright A4 sheet, printed side toward +Y (so a holder reads it).
box((0.21, 0.003, 0.297), (0, 0, 0.1485), "Paper", 0.0)
box((0.21, 0.0035, 0.045), (0, 0.0002, 0.27), "Red stamp", 0.0)
for i, w in enumerate((0.15, 0.17, 0.12, 0.16, 0.09)):
    box((w, 0.0036, 0.008), (-0.08 + w / 2, 0.0003, 0.215 - i * 0.025), "Ink", 0.0)
box((0.08, 0.0036, 0.03), (0.04, 0.0003, 0.06), "Red stamp", 0.0)
export("rent_bill")

box((0.2, 0.15, 0.07), (0, 0, 0.035), "Cardboard", 0.006)
box((0.2, 0.15, 0.01), (0, 0.06, 0.09), "Cardboard", 0.004, rot=(0.9, 0, 0))
box((0.07, 0.05, 0.002), (0.04, -0.03, 0.071), "Grease", 0.0)
export("takeout_box")

box((0.42, 0.42, 0.05), (0, 0, 0.025), "Cardboard", 0.006)
box((0.15, 0.12, 0.002), (-0.08, 0.06, 0.051), "Grease", 0.0)
export("pizza_box")

cyl(0.13, 0.22, (0, 0, 0.11), "Warm terracotta", 0.01)
cyl(0.115, 0.02, (0, 0, 0.21), "Soil", 0.0)
for k in range(5):
    a = k / 5 * math.tau
    box((0.012, 0.012, 0.3), (math.cos(a) * 0.05, math.sin(a) * 0.05, 0.33), "Dead leaf", 0.0, rot=(math.sin(a) * 0.6, math.cos(a) * 0.6, 0))
    box((0.07, 0.03, 0.006), (math.cos(a) * 0.16, math.sin(a) * 0.16, 0.3), "Dead leaf", 0.0, rot=(0.9, 0, a))
export("dead_plant")

# Window: frame and grimy glass (glass becomes a glowing material in Unity).
box((1.3, 0.12, 0.08), (0, 0, 0.04), "Trim", 0.01)
box((1.3, 0.12, 0.08), (0, 0, 1.08), "Trim", 0.01)
box((0.08, 0.12, 1.12), (-0.61, 0, 0.56), "Trim", 0.01)
box((0.08, 0.12, 1.12), (0.61, 0, 0.56), "Trim", 0.01)
box((0.05, 0.08, 1.0), (0, 0, 0.56), "Trim", 0.006)
box((1.16, 0.02, 0.96), (0, 0.01, 0.56), "Window glass", 0.0)
export("window")

cyl(0.006, 0.5, (0, 0, -0.25), "Cord", 0.0, verts=8)
sphere(0.05, (0, 0, -0.54), "Bulb")
export("bare_bulb")

sphere(0.3, (0, 0, 0.27), "Trash bag", (1.0, 0.9, 0.95))
sphere(0.12, (0.0, 0.0, 0.58), "Trash bag", (0.6, 0.6, 0.8))
export("trash_bag")

box((2.2, 1.5, 0.012), (0, 0, 0.006), "Rug border", 0.0)
box((2.0, 1.3, 0.014), (0, 0, 0.007), "Rug", 0.0)
export("rug")

print("APARTMENT PROPS DONE")
