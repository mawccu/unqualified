"""Model the call center props (original, made here) and export each as FBX for Unity.

  blender --background --factory-startup --python make_office_props.py -- <out_dir>

Same conventions as make_apartment_props.py: chunky shapes, soft bevels, flat
colours named after their surface, origin at the middle of the base, front
toward Blender -Y (Unity +Z).
"""
import math
import os
import sys

import bpy

OUT = sys.argv[sys.argv.index("--") + 1:][0]
os.makedirs(OUT, exist_ok=True)

COLOURS = {
    "Office laminate": (0.74, 0.66, 0.52), "Steel": (0.55, 0.56, 0.58), "Dark plastic": (0.12, 0.12, 0.13),
    "Phone grey": (0.30, 0.31, 0.33), "Keycaps": (0.32, 0.31, 0.29), "Screen": (0.08, 0.10, 0.10),
    "Phone display": (0.55, 0.68, 0.52), "Hold amber": (0.95, 0.62, 0.12), "Transfer blue": (0.20, 0.45, 0.85),
    "Mute grey": (0.62, 0.62, 0.64), "Partition fabric": (0.36, 0.42, 0.50), "Aluminium": (0.72, 0.73, 0.74),
    "Binder red": (0.70, 0.16, 0.14), "Binder blue": (0.16, 0.30, 0.62), "Binder green": (0.20, 0.50, 0.30),
    "Binder yellow": (0.88, 0.70, 0.16), "Paper": (0.94, 0.93, 0.88), "Printer white": (0.86, 0.86, 0.84),
    "Cooler white": (0.88, 0.88, 0.86), "Water blue": (0.45, 0.65, 0.90), "Whiteboard": (0.96, 0.96, 0.95),
    "Marker red": (0.80, 0.15, 0.12), "Marker blue": (0.15, 0.30, 0.75), "Panel light": (0.98, 0.98, 0.95),
    "Leaf green": (0.24, 0.50, 0.22), "Warm terracotta": (0.66, 0.36, 0.25), "Soil": (0.22, 0.16, 0.11),
    "Vending red": (0.62, 0.12, 0.14), "Vending glass": (0.55, 0.66, 0.72), "Snack yellow": (0.95, 0.80, 0.20),
    "Snack orange": (0.92, 0.48, 0.14), "Cord": (0.1, 0.1, 0.1), "Cushion teal": (0.20, 0.45, 0.46),
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


bpy.ops.wm.read_factory_settings(use_empty=True)

# Office desk: 1.4 x 0.7, a modesty panel at the back (+Y) and a cable tray.
box((1.4, 0.7, 0.03), (0, 0, 0.735), "Office laminate", 0.006)
for x in (-0.66, 0.66):
    box((0.05, 0.62, 0.03), (x, 0, 0.03), "Steel", 0.004)
    box((0.05, 0.05, 0.7), (x, 0, 0.37), "Steel", 0.004)
box((1.3, 0.02, 0.38), (0, 0.32, 0.5), "Office laminate", 0.004)
box((0.9, 0.12, 0.04), (0, 0.24, 0.66), "Dark plastic", 0.004)
export("office_desk")

# Flat monitor on a stand: screen faces -Y.
box((0.56, 0.035, 0.34), (0, 0.0, 0.33), "Dark plastic", 0.01)
box((0.52, 0.004, 0.30), (0, -0.019, 0.33), "Screen", 0.0)
box((0.05, 0.03, 0.18), (0, 0.04, 0.12), "Dark plastic", 0.006)
box((0.22, 0.16, 0.015), (0, 0.03, 0.008), "Dark plastic", 0.005)
export("flat_monitor")

box((0.44, 0.14, 0.022), (0, 0, 0.011), "Dark plastic", 0.005)
for row in range(4):
    for col in range(12):
        box((0.026, 0.026, 0.01), (-0.18 + col * 0.033, -0.048 + row * 0.032, 0.026), "Keycaps", 0.003)
export("slim_keyboard")

# Desk phone base: the cradle runs along X on the left, keypad on the right,
# a small display, and three big buttons along the front: hold, mute, transfer.
box((0.24, 0.2, 0.04), (0, 0, 0.02), "Phone grey", 0.012, rot=(0.12, 0, 0))
box((0.22, 0.06, 0.025), (0, 0.05, 0.05), "Phone grey", 0.01)
for i in range(3):
    for j in range(4):
        box((0.018, 0.016, 0.008), (0.035 + i * 0.024, -0.045 + j * 0.02, 0.043 - j * 0.0024), "Mute grey", 0.002, rot=(0.12, 0, 0))
box((0.07, 0.035, 0.004), (-0.055, -0.005, 0.044), "Phone display", 0.0, rot=(0.12, 0, 0))
for k, colour in enumerate(("Hold amber", "Mute grey", "Transfer blue")):
    box((0.045, 0.022, 0.012), (-0.07 + k * 0.055, -0.08, 0.03), colour, 0.003, rot=(0.12, 0, 0))
export("desk_phone")

# Handset: lies along X in the cradle; earpiece at -X, mouthpiece at +X.
box((0.2, 0.045, 0.03), (0, 0, 0.015), "Phone grey", 0.012)
box((0.055, 0.055, 0.035), (-0.085, 0, 0.012), "Phone grey", 0.014)
box((0.055, 0.055, 0.035), (0.085, 0, 0.012), "Phone grey", 0.014)
export("phone_handset")

# Cubicle partition: fabric panel in an aluminium frame, 1.4 wide, 1.25 tall, 0.05 thick.
box((1.36, 0.04, 1.19), (0, 0, 0.64), "Partition fabric", 0.004)
box((1.4, 0.05, 0.03), (0, 0, 1.245), "Aluminium", 0.006)
for x in (-0.69, 0.69):
    box((0.03, 0.05, 1.26), (x, 0, 0.63), "Aluminium", 0.004)
for x in (-0.55, 0.55):
    box((0.06, 0.3, 0.02), (x, 0, 0.01), "Aluminium", 0.004)
export("cubicle_partition")

# Binders standing on a shelf, spine toward -Y.
for name, colour in (("binder_red", "Binder red"), ("binder_blue", "Binder blue"), ("binder_green", "Binder green"), ("binder_yellow", "Binder yellow")):
    box((0.06, 0.26, 0.31), (0, 0, 0.155), colour, 0.006)
    box((0.05, 0.25, 0.29), (0, 0.01, 0.155), "Paper", 0.002)
    box((0.04, 0.004, 0.08), (0, -0.131, 0.2), "Paper", 0.0)
    cyl(0.012, 0.004, (0, -0.132, 0.07), "Steel", 0.0, rot=(math.pi / 2, 0, 0), verts=12)
    export(name)

# Laser printer: paper tray at the back, output slot at the front top.
box((0.44, 0.38, 0.24), (0, 0, 0.12), "Printer white", 0.02)
box((0.36, 0.22, 0.02), (0, -0.04, 0.245), "Dark plastic", 0.006)
box((0.3, 0.18, 0.004), (0, -0.04, 0.257), "Paper", 0.0)
box((0.36, 0.06, 0.04), (0, -0.2, 0.08), "Dark plastic", 0.006)
box((0.06, 0.03, 0.004), (0.15, -0.17, 0.243), "Phone display", 0.0)
box((0.32, 0.2, 0.08), (0, 0.24, 0.2), "Printer white", 0.01, rot=(-0.5, 0, 0))
export("office_printer")

# Water cooler with a big blue bottle.
box((0.32, 0.32, 0.95), (0, 0, 0.475), "Cooler white", 0.03)
box((0.2, 0.04, 0.12), (0, -0.17, 0.7), "Dark plastic", 0.01)
cyl(0.13, 0.4, (0, 0, 1.17), "Water blue", 0.03)
cyl(0.05, 0.05, (0, 0, 0.97), "Water blue", 0.01)
cyl(0.012, 0.03, (-0.05, -0.19, 0.74), "Marker blue", 0.0, rot=(math.pi / 2, 0, 0), verts=12)
cyl(0.012, 0.03, (0.05, -0.19, 0.74), "Marker red", 0.0, rot=(math.pi / 2, 0, 0), verts=12)
export("water_cooler")

# Whiteboard for the wall (back at +Y), with a marker tray.
box((1.6, 0.03, 1.0), (0, 0, 0.5), "Aluminium", 0.008)
box((1.54, 0.01, 0.94), (0, -0.016, 0.5), "Whiteboard", 0.0)
box((0.6, 0.06, 0.02), (0, -0.04, 0.01), "Aluminium", 0.004)
cyl(0.01, 0.12, (-0.1, -0.04, 0.03), "Marker red", 0.002, rot=(0, math.pi / 2, 0), verts=10)
cyl(0.01, 0.12, (0.08, -0.04, 0.03), "Marker blue", 0.002, rot=(0, math.pi / 2, 0), verts=10)
export("whiteboard")

# Ceiling light panel (the face becomes a glowing material in Unity).
box((0.62, 0.62, 0.04), (0, 0, -0.02), "Aluminium", 0.004)
box((0.58, 0.58, 0.004), (0, 0, -0.042), "Panel light", 0.0)
export("ceiling_panel")

# A plant that is still alive (the office has someone who waters it).
cyl(0.14, 0.32, (0, 0, 0.16), "Warm terracotta", 0.012)
cyl(0.125, 0.02, (0, 0, 0.31), "Soil", 0.0)
for k in range(9):
    a = k / 9 * math.tau
    tilt = 0.35 + (k % 3) * 0.15
    sphere(0.09, (math.cos(a) * 0.12, math.sin(a) * 0.12, 0.55 + (k % 3) * 0.12), "Leaf green", (0.6, 1.4, 0.25))
    box((0.012, 0.012, 0.35), (math.cos(a) * 0.05, math.sin(a) * 0.05, 0.45), "Leaf green", 0.0, rot=(math.sin(a) * tilt, -math.cos(a) * tilt, 0))
export("office_plant")

# Vending machine for the break corner: glass front toward -Y.
box((0.9, 0.8, 1.85), (0, 0, 0.925), "Vending red", 0.02)
box((0.6, 0.02, 1.3), (-0.1, -0.4, 1.05), "Vending glass", 0.0)
for row in range(5):
    for col in range(4):
        box((0.1, 0.06, 0.14), (-0.33 + col * 0.15, -0.35, 0.55 + row * 0.24), ("Snack yellow", "Snack orange")[(row + col) % 2], 0.004)
box((0.14, 0.02, 0.4), (0.33, -0.405, 1.2), "Dark plastic", 0.004)
box((0.6, 0.02, 0.16), (-0.1, -0.405, 0.22), "Dark plastic", 0.004)
export("vending_machine")

# A low break-room sofa.
box((1.6, 0.75, 0.3), (0, 0, 0.25), "Cushion teal", 0.04)
box((1.6, 0.18, 0.45), (0, 0.29, 0.58), "Cushion teal", 0.05)
for x in (-0.74, 0.74):
    box((0.14, 0.75, 0.3), (x, 0, 0.5), "Cushion teal", 0.04)
for x in (-0.7, 0.7):
    for y in (-0.3, 0.3):
        cyl(0.025, 0.1, (x, y, 0.05), "Steel")
export("break_sofa")

print("OFFICE PROPS DONE")
