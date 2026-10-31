"""Model the apartment workshop props (original, made here) and export each as FBX for Unity.

  blender --background --factory-startup --python make_workshop_props.py -- <out_dir>

Same conventions as make_office_props.py: chunky shapes, soft bevels, flat
colours named after their surface, origin at the middle of the base, front
toward Blender -Y (Unity +Z). Blender +X comes out as Unity -X.

The gadget parts share one frame: the casing's base centre is the origin, its
long side runs along X, the battery bay is at +X and the board's keyed ports
are in a row at the back (+Y). The Unity bench puts its sockets at the same
offsets (WorkshopLayout in ApartmentSceneBuilder.Workshop.cs).
"""
import math
import os
import sys

import bpy

OUT = sys.argv[sys.argv.index("--") + 1:][0]
os.makedirs(OUT, exist_ok=True)

COLOURS = {
    "Old pine": (0.55, 0.40, 0.25), "Odd leg": (0.30, 0.22, 0.15), "Steel": (0.55, 0.56, 0.58),
    "Dark plastic": (0.12, 0.12, 0.13), "Paper": (0.94, 0.93, 0.88), "Cord": (0.1, 0.1, 0.1),
    "Cutting mat": (0.16, 0.36, 0.30), "Mat grid": (0.55, 0.75, 0.65), "Pegboard": (0.62, 0.50, 0.36),
    "PCB green": (0.10, 0.38, 0.20), "Chip black": (0.05, 0.05, 0.06), "Solder": (0.75, 0.75, 0.72),
    "Casing grey": (0.42, 0.44, 0.47), "Battery yellow": (0.88, 0.70, 0.12), "Sensor blue": (0.18, 0.42, 0.85),
    "Motor orange": (0.92, 0.48, 0.12), "Speaker green": (0.32, 0.72, 0.32), "Label white": (0.95, 0.95, 0.92),
    "Handle red": (0.75, 0.12, 0.10), "Tool steel": (0.70, 0.71, 0.73), "Lamp shade": (0.20, 0.30, 0.55),
    "Drawer blue": (0.28, 0.42, 0.55), "Drawer mint": (0.55, 0.72, 0.62), "Bin red": (0.72, 0.18, 0.15),
    "Bin blue": (0.18, 0.35, 0.65), "Bin yellow": (0.90, 0.72, 0.15), "Rack paint": (0.30, 0.33, 0.30),
    "Dock rubber": (0.08, 0.08, 0.09), "Duffel": (0.20, 0.25, 0.20), "Cardboard": (0.66, 0.50, 0.32),
    "Packing tape": (0.80, 0.70, 0.50), "Doormat": (0.35, 0.27, 0.18), "LED green": (0.3, 0.95, 0.4),
    "Jig screen": (0.10, 0.20, 0.16), "Button red": (0.85, 0.12, 0.10), "Brass pin": (0.75, 0.62, 0.30),
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


def box(size, loc, material, bevel=0.004, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    o = bpy.context.object
    o.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(o, material, bevel)


def cyl(radius, depth, loc, material, bevel=0.002, rot=(0, 0, 0), verts=24):
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=depth, location=loc, rotation=rot, vertices=verts)
    return finish(bpy.context.object, material, bevel)


def cone(r1, r2, depth, loc, material, rot=(0, 0, 0), verts=24):
    bpy.ops.mesh.primitive_cone_add(radius1=r1, radius2=r2, depth=depth, location=loc, rotation=rot, vertices=verts)
    return finish(bpy.context.object, material, 0)


def text(body, size, loc, material, rot=(0, 0, 0)):
    bpy.ops.object.text_add(location=loc, rotation=rot)
    o = bpy.context.object
    o.data.body = body
    o.data.size = size
    o.data.extrude = 0.0006
    o.data.align_x = "CENTER"
    o.data.align_y = "CENTER"
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

# The workbench: a reused dining table, 1.6 x 0.8, top at 0.78. One leg is a
# different wood and stands on a paperback; a mismatched drawer unit under the
# right end (+X).
box((1.6, 0.8, 0.04), (0, 0, 0.76), "Old pine", 0.006)
box((1.5, 0.7, 0.06), (0, 0, 0.71), "Old pine", 0.004)
for x, y, mat_name in ((-0.72, -0.32, "Old pine"), (0.72, -0.32, "Old pine"), (-0.72, 0.32, "Old pine"), (0.72, 0.32, "Odd leg")):
    h = 0.66 if mat_name == "Odd leg" else 0.68
    box((0.06, 0.06, h), (x, y, 0.68 - h / 2 + (0.02 if mat_name == "Odd leg" else 0)), mat_name, 0.004)
box((0.13, 0.19, 0.02), (0.72, 0.32, 0.01), "Paper", 0.002)
# Drawer unit: three drawers, two of them from a different set.
box((0.42, 0.5, 0.6), (0.45, 0.0, 0.3), "Drawer blue", 0.006)
for i, colour in enumerate(("Drawer blue", "Drawer mint", "Drawer mint")):
    box((0.38, 0.02, 0.17), (0.45, -0.255, 0.1 + i * 0.19), colour, 0.004)
    box((0.08, 0.02, 0.02), (0.45, -0.272, 0.13 + i * 0.19), "Steel", 0.003)
export("workbench")

# Assembly mat: a green cutting mat with a grid.
box((0.5, 0.36, 0.004), (0, 0, 0.002), "Cutting mat", 0.001)
for i in range(1, 10):
    box((0.002, 0.34, 0.0008), (-0.25 + i * 0.05, 0, 0.0044), "Mat grid", 0)
for i in range(1, 7):
    box((0.48, 0.002, 0.0008), (0, -0.18 + i * 0.05, 0.0044), "Mat grid", 0)
export("assembly_mat")

# The unfinished-build tray: a shallow box parts wait in.
box((0.42, 0.26, 0.006), (0, 0, 0.003), "Bin blue", 0.002)
for x in (-0.207, 0.207):
    box((0.006, 0.26, 0.04), (x, 0, 0.02), "Bin blue", 0.002)
for y in (-0.127, 0.127):
    box((0.42, 0.006, 0.04), (0, y, 0.02), "Bin blue", 0.002)
export("parts_tray")

# Test jig: a base with clamp pins, a slanted little screen and a big red button.
box((0.2, 0.14, 0.05), (0, 0, 0.025), "Dark plastic", 0.006)
box((0.16, 0.012, 0.09), (0, 0.055, 0.09), "Dark plastic", 0.004, rot=(math.radians(-18), 0, 0))
box((0.14, 0.004, 0.07), (0, 0.047, 0.092), "Jig screen", 0, rot=(math.radians(-18), 0, 0))
cyl(0.018, 0.016, (0.06, -0.03, 0.058), "Button red", 0.003)
for x in (-0.07, -0.04):
    cyl(0.004, 0.02, (x, -0.04, 0.06), "Brass pin", 0)
cyl(0.003, 0.4, (-0.1, 0.07, 0.01), "Cord", 0, rot=(0, math.radians(90), 0), verts=8)
export("test_jig")

# Task lamp: weighted base, two arm segments, a dented shade.
cyl(0.07, 0.025, (0, 0, 0.0125), "Lamp shade", 0.004)
cyl(0.008, 0.32, (0, 0.0, 0.17), "Steel", 0, rot=(math.radians(-20), 0, 0), verts=12)
cyl(0.008, 0.3, (0, -0.12, 0.38), "Steel", 0, rot=(math.radians(-70), 0, 0), verts=12)
cone(0.075, 0.03, 0.12, (0, -0.25, 0.36), "Lamp shade", rot=(math.radians(-30), 0, 0))
export("task_lamp")

# Pegboard with a few hanging tools and wire spools.
box((1.2, 0.02, 0.62), (0, 0, 0.31), "Pegboard", 0.004)
for i in range(11):
    for j in range(6):
        box((0.008, 0.004, 0.008), (-0.55 + i * 0.11, -0.011, 0.04 + j * 0.105), "Dark plastic", 0)
box((0.03, 0.01, 0.16), (-0.42, -0.02, 0.4), "Handle red", 0.003)
box((0.012, 0.012, 0.1), (-0.42, -0.02, 0.27), "Tool steel", 0.002)
box((0.03, 0.01, 0.13), (-0.3, -0.02, 0.42), "Drawer blue", 0.003)
cyl(0.05, 0.035, (0.25, -0.03, 0.42), "Motor orange", 0.003, rot=(math.radians(90), 0, 0))
cyl(0.045, 0.035, (0.4, -0.03, 0.44), "Sensor blue", 0.003, rot=(math.radians(90), 0, 0))
cyl(0.02, 0.04, (0.32, -0.03, 0.42), "Cord", 0, rot=(math.radians(90), 0, 0))
box((0.24, 0.006, 0.16), (-0.02, -0.014, 0.44), "Paper", 0.001)
export("pegboard")

# A parts bin (open front, stacks on a shelf).
for colour, name in (("Bin red", "part_bin_red"), ("Bin blue", "part_bin_blue"), ("Bin yellow", "part_bin_yellow")):
    box((0.2, 0.28, 0.006), (0, 0, 0.003), colour, 0.002)
    box((0.006, 0.28, 0.12), (-0.097, 0, 0.06), colour, 0.002)
    box((0.006, 0.28, 0.12), (0.097, 0, 0.06), colour, 0.002)
    box((0.2, 0.006, 0.12), (0, 0.137, 0.06), colour, 0.002)
    box((0.2, 0.006, 0.06), (0, -0.137, 0.03), colour, 0.002)
    box((0.1, 0.002, 0.03), (0, -0.141, 0.03), "Label white", 0)
    export(name)

# Loadout rack: a low cabinet, three charging cradles on top, a hook for the bag.
box((1.0, 0.4, 0.82), (0, 0, 0.41), "Rack paint", 0.008)
box((0.96, 0.02, 0.36), (0, -0.2, 0.22), "Rack paint", 0.004)
box((0.96, 0.02, 0.36), (0, -0.2, 0.6), "Rack paint", 0.004)
for x in (-0.3, 0.0, 0.3):
    box((0.2, 0.16, 0.02), (x, 0.0, 0.83), "Dock rubber", 0.004)
    box((0.03, 0.01, 0.01), (x + 0.07, -0.07, 0.845), "LED green", 0)
box((0.02, 0.06, 0.02), (0.42, -0.2, 0.75), "Steel", 0.003)
box((0.12, 0.02, 0.02), (0, -0.22, 0.5), "Steel", 0.003)
cyl(0.006, 0.9, (0.0, 0.18, 0.8), "Cord", 0, rot=(0, math.radians(90), 0), verts=8)
box((0.3, 0.06, 0.04), (0.0, 0.17, 0.84), "Dark plastic", 0.004)
export("loadout_rack")

# The duffel bag the kit goes in.
cyl(0.13, 0.5, (0, 0, 0.13), "Duffel", 0.02, rot=(0, math.radians(90), 0))
box((0.32, 0.03, 0.01), (0, 0, 0.26), "Dark plastic", 0.003)
box((0.02, 0.02, 0.12), (-0.12, 0, 0.3), "Dark plastic", 0.002)
box((0.02, 0.02, 0.12), (0.12, 0, 0.3), "Dark plastic", 0.002)
export("duffel_bag")

# Power strip on the bench.
box((0.32, 0.06, 0.04), (0, 0, 0.02), "Label white", 0.006)
for i in range(4):
    box((0.03, 0.03, 0.004), (-0.11 + i * 0.07, 0, 0.041), "Dark plastic", 0)
box((0.012, 0.012, 0.006), (0.14, -0.02, 0.042), "Button red", 0)
export("power_strip")

# The doormat where parcels land.
box((0.72, 0.46, 0.014), (0, 0, 0.007), "Doormat", 0.004)
export("doormat")

# Parcel, sealed: a cardboard box with tape and a shipping label.
box((0.34, 0.26, 0.18), (0, 0, 0.09), "Cardboard", 0.006)
box((0.34, 0.06, 0.004), (0, 0, 0.181), "Packing tape", 0)
box((0.004, 0.06, 0.18), (0.171, 0, 0.09), "Packing tape", 0)
box((0.004, 0.06, 0.18), (-0.171, 0, 0.09), "Packing tape", 0)
box((0.12, 0.002, 0.08), (0.06, -0.131, 0.1), "Paper", 0)
export("parcel")

# Parcel, opened: the same box with its flaps up and nothing inside.
box((0.34, 0.26, 0.006), (0, 0, 0.003), "Cardboard", 0.002)
for x in (-0.168, 0.168):
    box((0.006, 0.26, 0.18), (x, 0, 0.09), "Cardboard", 0.002)
for y in (-0.128, 0.128):
    box((0.34, 0.006, 0.18), (0, y, 0.09), "Cardboard", 0.002)
for y, ang in ((-0.128, 35), (0.128, -35)):
    box((0.34, 0.006, 0.12), (0, y + (0.035 if ang < 0 else -0.035), 0.225), "Cardboard", 0.002, rot=(math.radians(ang), 0, 0))
export("parcel_open")

# --- The gadget parts, in the casing frame (see the docstring) ---------------------

# Casing: an open grey tray 0.24 x 0.16 x 0.07 (deep enough for a module on
# its port under the lid) with a battery bay wall.
box((0.24, 0.16, 0.006), (0, 0, 0.003), "Casing grey", 0.002)
for x in (-0.117, 0.117):
    box((0.006, 0.16, 0.07), (x, 0, 0.035), "Casing grey", 0.002)
for y in (-0.077, 0.077):
    box((0.24, 0.006, 0.07), (0, y, 0.035), "Casing grey", 0.002)
box((0.004, 0.148, 0.03), (0.062, 0, 0.018), "Casing grey", 0.001)
for x, y in ((-0.1, 0.04), (0.05, -0.04), (-0.1, -0.04), (0.05, 0.04)):
    cyl(0.006, 0.012, (x, y, 0.012), "Brass pin", 0, verts=10)
text("BATT", 0.012, (0.088, -0.05, 0.0065), "Label white")
export("part_casing")

# Its lid: a flat panel, hinge along its back edge at the origin, reaching forward (-Y).
box((0.24, 0.16, 0.008), (0, -0.08, 0.004), "Casing grey", 0.002)
box((0.24, 0.01, 0.01), (0, -0.005, 0.005), "Steel", 0.002)
box((0.06, 0.004, 0.02), (0, -0.16, 0.006), "Label white", 0)
export("part_lid")

# Controller board: origin at its own base centre. In the casing it sits on
# the standoffs at x -0.025. Ports at x -0.05, 0, +0.05 (board space), y +0.02.
box((0.17, 0.1, 0.006), (0, 0, 0.003), "PCB green", 0.001)
box((0.03, 0.03, 0.006), (0.0, -0.025, 0.009), "Chip black", 0.001)
box((0.02, 0.012, 0.005), (0.045, -0.03, 0.0085), "Chip black", 0.001)
for i in range(6):
    box((0.002, 0.004, 0.002), (-0.012 + i * 0.005, -0.042, 0.007), "Solder", 0)
# Keyed ports: round (sensor), triangle (audio), square (motor). Labels in front.
cyl(0.014, 0.008, (-0.05, 0.02, 0.01), "Sensor blue", 0.001, verts=20)
cyl(0.017, 0.008, (0.0, 0.02, 0.01), "Speaker green", 0.001, verts=3, rot=(0, 0, math.radians(90)))
box((0.026, 0.026, 0.008), (0.05, 0.02, 0.01), "Motor orange", 0.001)
text("SNS", 0.011, (-0.05, -0.006, 0.0065), "Label white")
text("AUD", 0.011, (0.0, -0.006, 0.0065), "Label white")
text("MTR", 0.011, (0.05, -0.006, 0.0065), "Label white")
# Screw pads on the two front corners (nothing stands in front of them).
for x, y in ((-0.075, -0.04), (0.075, -0.04)):
    cyl(0.005, 0.002, (x, y, 0.007), "Solder", 0, verts=10)
export("part_board")

# Battery pack.
box((0.04, 0.07, 0.025), (0, 0, 0.0125), "Battery yellow", 0.003)
box((0.012, 0.006, 0.006), (0, 0.037, 0.02), "Steel", 0)
text("+", 0.014, (0, -0.02, 0.0255), "Chip black")
export("part_battery")

# Modules: each on its keyed plug.
cyl(0.013, 0.006, (0, 0, 0.003), "Sensor blue", 0.001, verts=20)
cyl(0.018, 0.02, (0, 0, 0.016), "Sensor blue", 0.002)
cyl(0.007, 0.004, (0, 0, 0.028), "Chip black", 0.001)
export("part_sensor")

box((0.024, 0.024, 0.006), (0, 0, 0.003), "Motor orange", 0.001)
box((0.032, 0.032, 0.024), (0, 0, 0.018), "Motor orange", 0.003)
cyl(0.004, 0.012, (0, -0.02, 0.022), "Tool steel", 0, rot=(math.radians(90), 0, 0), verts=10)
box((0.01, 0.006, 0.01), (0, -0.028, 0.022), "Tool steel", 0.001)
export("part_motor")

cyl(0.016, 0.006, (0, 0, 0.003), "Speaker green", 0.001, verts=3, rot=(0, 0, math.radians(90)))
box((0.036, 0.03, 0.026), (0, 0, 0.019), "Speaker green", 0.003)
cyl(0.009, 0.003, (0, -0.0155, 0.02), "Chip black", 0, rot=(math.radians(90), 0, 0), verts=16)
box((0.016, 0.002, 0.007), (0, -0.0155, 0.029), "Jig screen", 0)
export("part_speaker")

# Screwdriver, held point down: the tip is the origin, the handle on top.
cyl(0.0025, 0.085, (0, 0, 0.0425), "Tool steel", 0, verts=10)
cone(0.0025, 0.001, 0.008, (0, 0, 0.004), "Tool steel", rot=(math.radians(180), 0, 0), verts=8)
cyl(0.012, 0.095, (0, 0, 0.13), "Handle red", 0.004, verts=16)
cyl(0.0125, 0.01, (0, 0, 0.09), "Dark plastic", 0.002, verts=16)
export("screwdriver")

# --- Finished gadgets ---------------------------------------------------------------

# WhisperBox: a pocketable box with an ear hook and an LED.
box((0.07, 0.04, 0.032), (0, 0, 0.016), "Casing grey", 0.005)
cyl(0.012, 0.012, (0.03, -0.022, 0.016), "Speaker green", 0.002, rot=(math.radians(90), 0, 0))
bpy.ops.mesh.primitive_torus_add(major_radius=0.025, minor_radius=0.003, location=(-0.045, 0, 0.03), rotation=(math.radians(90), 0, 0))
finish(bpy.context.object, "Dark plastic", 0)
box((0.006, 0.004, 0.006), (-0.015, -0.021, 0.026), "LED green", 0)
text("WB", 0.012, (0.0, -0.0205, 0.012), "Label white", rot=(math.radians(90), 0, 0))
export("gadget_whisperbox")

# SteadyHand: a flat box with a motor bump and two spring clips.
box((0.16, 0.11, 0.026), (0, 0, 0.013), "Casing grey", 0.005)
box((0.036, 0.036, 0.018), (0.04, 0.0, 0.034), "Motor orange", 0.003)
cyl(0.014, 0.014, (-0.04, 0.0, 0.032), "Sensor blue", 0.002)
for x in (-0.07, 0.07):
    box((0.012, 0.09, 0.006), (x, 0, 0.03), "Tool steel", 0.002)
box((0.006, 0.004, 0.006), (0.0, -0.056, 0.02), "LED green", 0)
export("gadget_steadyhand")

print("DONE")
