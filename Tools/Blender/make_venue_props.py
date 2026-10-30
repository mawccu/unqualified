"""Model the venue's usable props (original, made here) and export each as FBX.

  blender --background --factory-startup --python make_venue_props.py -- <out_dir>

Same conventions as make_apartment_props.py: origin at the middle of the base,
front toward Blender -Y (Unity +Z), X mirrored on import, flat named colours.
Parts that move on their own (fan blades, bin lid, drawer) are separate files
with their pivot where they turn or slide.
"""
import math
import os
import sys

import bpy

OUT = sys.argv[sys.argv.index("--") + 1:][0]
os.makedirs(OUT, exist_ok=True)

COLOURS = {
    "Steel": (0.55, 0.56, 0.58), "Walnut": (0.449, 0.304, 0.206), "Black plastic": (0.08, 0.08, 0.09),
    "Mug glaze": (0.86, 0.84, 0.78), "Mug band": (0.25, 0.45, 0.6), "Brass knob": (0.75, 0.6, 0.3),
    "Radio body": (0.55, 0.22, 0.18), "Speaker cloth": (0.3, 0.26, 0.22), "Dial": (0.92, 0.88, 0.72),
    "Fan blue": (0.38, 0.55, 0.62), "Switch plate": (0.9, 0.88, 0.82), "Bin grey": (0.42, 0.44, 0.45),
    "Popper gold": (0.85, 0.66, 0.25), "Popper stripe": (0.75, 0.2, 0.35), "Paper": (0.94, 0.93, 0.88),
    "Book cover": (0.36, 0.18, 0.22), "Ink": (0.25, 0.25, 0.28), "Cabinet": (0.5, 0.58, 0.48),
    "Rubber seal": (0.12, 0.12, 0.12), "Mop head": (0.82, 0.8, 0.7), "Petrol": (0.172, 0.473, 0.485),
}
MATS = {}


def mat(name):
    if name not in MATS:
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        r, g, b = COLOURS[name]
        bsdf.inputs["Base Color"].default_value = (r, g, b, 1.0)
        MATS[name] = m
    return MATS[name]


parts = []


def finish(o, material, bevel):
    o.data.materials.append(mat(material))
    if bevel > 0:
        mod = o.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = "ANGLE"
    parts.append(o)
    return o


def box(size, loc, material, bevel=0.004, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    o = bpy.context.object
    o.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(o, material, bevel)


def cyl(r, depth, loc, material, bevel=0.003, rot=(0, 0, 0), verts=24, r2=None):
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=depth, location=loc, rotation=rot, vertices=verts)
    else:
        bpy.ops.mesh.primitive_cone_add(radius1=r, radius2=r2, depth=depth, location=loc, rotation=rot, vertices=verts)
    o = bpy.context.object
    bpy.ops.object.shade_smooth()
    return finish(o, material, bevel)


def torus(r, minor, loc, material, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=r, minor_radius=minor, location=loc, rotation=rot, major_segments=24, minor_segments=8)
    o = bpy.context.object
    bpy.ops.object.shade_smooth()
    return finish(o, material, 0)


def sphere(r, loc, material, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=loc, segments=18, ring_count=10)
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
    if len(parts) > 1:
        bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.context.scene.cursor.location = (0, 0, 0)
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

# Cake spatula: a long flat blade and a wooden handle, lying flat.
box((0.03, 0.15, 0.003), (0, -0.075, 0.006), "Steel", 0.0015)
box((0.02, 0.11, 0.018), (0, 0.06, 0.01), "Walnut", 0.006)
export("spatula")

# Mug.
cyl(0.042, 0.095, (0, 0, 0.0475), "Mug glaze", 0.004)
cyl(0.043, 0.02, (0, 0, 0.07), "Mug band", 0.0)
torus(0.028, 0.008, (0.05, 0, 0.05), "Mug glaze", rot=(math.pi / 2, 0, 0))
export("mug")

# Serving tray with a lip and handles.
box((0.46, 0.32, 0.012), (0, 0, 0.006), "Steel", 0.004)
for y in (-0.155, 0.155):
    box((0.46, 0.012, 0.03), (0, y, 0.02), "Steel", 0.003)
for x in (-0.225, 0.225):
    box((0.012, 0.32, 0.03), (x, 0, 0.02), "Steel", 0.003)
export("tray")

# Desk bell: a dome on a dark base, a plunger on top.
cyl(0.05, 0.015, (0, 0, 0.0075), "Black plastic", 0.003)
sphere(0.045, (0, 0, 0.018), "Brass knob", (1, 1, 0.75))
cyl(0.006, 0.02, (0, 0, 0.06), "Steel", 0.0)
sphere(0.009, (0, 0, 0.072), "Steel")
export("desk_bell")

# Radio: a red box, speaker grille, a big dial and an antenna.
box((0.26, 0.1, 0.16), (0, 0, 0.08), "Radio body", 0.012)
box((0.12, 0.004, 0.11), (-0.055, -0.051, 0.08), "Speaker cloth", 0.002)
cyl(0.03, 0.012, (0.07, -0.052, 0.09), "Dial", 0.002, rot=(math.pi / 2, 0, 0))
cyl(0.012, 0.012, (0.07, -0.052, 0.04), "Dial", 0.002, rot=(math.pi / 2, 0, 0))
cyl(0.003, 0.2, (0.1, 0.03, 0.25), "Steel", 0.0, rot=(0.0, 0.35, 0))
box((0.18, 0.02, 0.012), (0, 0, 0.165), "Black plastic", 0.003)
export("radio")

# Desk fan: base, stem and guard (the blades are separate so they can spin).
cyl(0.09, 0.025, (0, 0, 0.0125), "Fan blue", 0.006)
cyl(0.012, 0.2, (0, 0, 0.12), "Steel", 0.0)
cyl(0.04, 0.07, (0, 0.03, 0.25), "Fan blue", 0.006, rot=(math.pi / 2, 0, 0))
torus(0.13, 0.005, (0, -0.03, 0.25), "Steel", rot=(math.pi / 2, 0, 0))
torus(0.13, 0.005, (0, -0.075, 0.25), "Steel", rot=(math.pi / 2, 0, 0))
for k in range(8):
    a = k / 8 * math.tau
    box((0.004, 0.05, 0.004), (math.cos(a) * 0.13, -0.052, 0.25 + math.sin(a) * 0.13), "Steel", 0.0)
export("fan_body")
# Blades: pivot at the hub, spinning about Y (Blender) = the fan's axis.
cyl(0.02, 0.02, (0, 0, 0), "Fan blue", 0.0, rot=(math.pi / 2, 0, 0))
for k in range(3):
    a = k / 3 * math.tau
    box((0.05, 0.004, 0.1), (math.sin(a) * 0.055, 0, math.cos(a) * 0.055), "Fan blue", 0.002, rot=(0, a, 0))
export("fan_blades")

# Light switch on a wall plate (front toward -Y = into the room).
box((0.08, 0.012, 0.12), (0, 0, 0.06), "Switch plate", 0.004)
box((0.022, 0.014, 0.04), (0, -0.01, 0.06), "Switch plate", 0.003)
export("light_switch")

# Pedal bin: body and a lid hinged at the back.
cyl(0.15, 0.42, (0, 0, 0.21), "Bin grey", 0.006, r2=0.165)
box((0.08, 0.05, 0.015), (0, -0.17, 0.02), "Black plastic", 0.004)
export("bin_body")
cyl(0.17, 0.025, (0, -0.17, 0.0125), "Bin grey", 0.006)  # pivot at the back edge
export("bin_lid")

# Party popper: a striped cone with a string.
cyl(0.025, 0.12, (0, 0, 0.06), "Popper gold", 0.002, r2=0.008)
cyl(0.0255, 0.02, (0, 0, 0.03), "Popper stripe", 0.0)
cyl(0.002, 0.05, (0, 0, -0.02), "Paper", 0.0)
export("party_popper")

# Guest book: open, lying flat.
box((0.34, 0.24, 0.02), (0, 0, 0.01), "Book cover", 0.004)
box((0.155, 0.22, 0.012), (-0.08, 0, 0.024), "Paper", 0.002, rot=(0, 0.04, 0))
box((0.155, 0.22, 0.012), (0.08, 0, 0.024), "Paper", 0.002, rot=(0, -0.04, 0))
for i in range(6):
    box((0.12, 0.004, 0.0015), (-0.08, 0.08 - i * 0.03, 0.031), "Ink", 0.0)
export("guest_book")

# Drawer unit: carcass, and one drawer that slides out toward -Y.
box((0.5, 0.45, 0.8), (0, 0, 0.4), "Cabinet", 0.01)
box((0.44, 0.012, 0.3), (0, -0.226, 0.2), "Cabinet", 0.004)
box((0.12, 0.02, 0.02), (0, -0.24, 0.25), "Steel", 0.004)
export("drawer_unit")
box((0.44, 0.4, 0.17), (0, 0.0, 0.085), "Cabinet", 0.006)
box((0.44, 0.02, 0.21), (0, -0.21, 0.1), "Cabinet", 0.006)
box((0.12, 0.02, 0.02), (0, -0.23, 0.13), "Steel", 0.004)
export("drawer")

# Coat rack: a wall rail with hooks.
box((1.2, 0.04, 0.08), (0, 0.02, 1.6), "Walnut", 0.008)
for i in range(6):
    x = -0.5 + i * 0.2
    cyl(0.008, 0.09, (x, -0.04, 1.58), "Brass knob", 0.0, rot=(math.pi / 2 - 0.4, 0, 0))
export("coat_rack")

print("VENUE PROPS DONE")
