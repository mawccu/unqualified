"""The flat's two beds (original, made here): a mattress on a pallet, a fitted
sheet, a soft pillow and a rumpled duvet that drapes over the edges. One is
roughly made, one has been slept in and abandoned. Exports to Unity like the
other prop scripts (origin at the middle of the base, front toward -Y).

  blender --background --factory-startup --python make_bedding.py -- <out_dir>
"""
import math
import os
import random
import sys

import bpy
import bmesh
from mathutils import Vector, noise

OUT = sys.argv[sys.argv.index("--") + 1:][0]
os.makedirs(OUT, exist_ok=True)

COLOURS = {
    "Pallet pine": (0.58, 0.45, 0.30), "Pallet block": (0.45, 0.34, 0.22),
    "Mattress ticking": (0.62, 0.66, 0.70), "Bed sheet": (0.86, 0.85, 0.80),
    "Pillowcase": (0.90, 0.88, 0.82), "Duvet mustard": (0.78, 0.58, 0.22), "Duvet teal": (0.20, 0.42, 0.44),
}
MATS = {}
parts = []


def mat(name):
    if name not in MATS:
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        r, g, b = COLOURS[name]
        bsdf.inputs["Base Color"].default_value = (r, g, b, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.9
        MATS[name] = m
    return MATS[name]


def box(size, loc, material, bevel=0.004, segments=2):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    o = bpy.context.object
    o.scale = size
    bpy.ops.object.transform_apply(scale=True)
    o.data.materials.append(mat(material))
    if bevel > 0:
        mod = o.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = segments
        mod.limit_method = "ANGLE"
    parts.append(o)
    return o


def smooth(o):
    for p in o.data.polygons:
        p.use_smooth = True


def pallet(length):
    # Top boards across the width, three stringers, nine blocks.
    n = int(length / 0.14)
    for i in range(n):
        y = -length / 2 + 0.07 + i * (length - 0.14) / (n - 1)
        box((1.0, 0.1, 0.022), (0, y, 0.129), "Pallet pine", 0.003)
    for x in (-0.44, 0, 0.44):
        box((0.1, length, 0.022), (x, 0, 0.107), "Pallet pine", 0.003)
        for y in (-length / 2 + 0.06, 0, length / 2 - 0.06):
            box((0.1, 0.1, 0.096), (x, y, 0.048), "Pallet block", 0.004)


def mattress(z0):
    m = box((0.9, 1.9, 0.18), (0, 0, z0 + 0.09), "Mattress ticking", 0.045, 4)
    smooth(m)
    # The fitted sheet: a slightly larger skin over the top and the upper sides.
    s = box((0.915, 1.915, 0.07), (0, 0, z0 + 0.15), "Bed sheet", 0.03, 4)
    smooth(s)
    return z0 + 0.185


def pillow(loc, yaw, squash=1.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=(0, 0, math.radians(yaw)))
    o = bpy.context.object
    o.scale = (0.62, 0.4, 0.12 * squash)
    bpy.ops.object.transform_apply(scale=True)
    # Bevel first so the subdivision keeps a pillow's flat-ish faces instead of a ball.
    bev = o.modifiers.new("Edge", "BEVEL")
    bev.width = 0.05
    bev.segments = 2
    bpy.ops.object.modifier_apply(modifier="Edge")
    sub = o.modifiers.new("Soft", "SUBSURF")
    sub.levels = 2
    sub.render_levels = 2
    bpy.ops.object.modifier_apply(modifier="Soft")
    # Puff the middle, pinch the corners.
    for v in o.data.vertices:
        c = v.co - Vector(loc)
        edge = max(abs(c.x) / 0.29, abs(c.y) / 0.18)
        v.co.z += (1 - min(1, edge)) * 0.02 * (1 if c.z > 0 else -0.4)
    smooth(o)
    o.data.materials.append(mat("Pillowcase"))
    parts.append(o)


def duvet(top, y0, y1, material, rumple, seed, folded=0.0, shift=0.0):
    """A quilt laid over the mattress from y0 to y1: a grid that rumples on top and
    drapes down over the sides and foot, given a little thickness."""
    random.seed(seed)
    w = 1.04
    bm = bmesh.new()
    nx, ny = 40, int(40 * (y1 - y0) / w) + 2
    verts = []
    for j in range(ny + 1):
        row = []
        for i in range(nx + 1):
            x = -w / 2 + w * i / nx
            y = y0 + (y1 - y0) * j / ny
            row.append(bm.verts.new((x + shift, y, top)))
        verts.append(row)
    for j in range(ny):
        for i in range(nx):
            bm.faces.new((verts[j][i], verts[j][i + 1], verts[j + 1][i + 1], verts[j + 1][i]))
    off = Vector((seed * 3.1, seed * 1.7, 0))
    for row in verts:
        for v in row:
            x, y = v.co.x - shift, v.co.y
            # Rumples: low-frequency lumps plus a few long creases.
            lump = noise.noise(Vector((x * 3.0, y * 2.2, 0)) + off) * 0.035 * rumple
            crease = math.sin((x * 0.8 + y * 2.6) * 6 + seed) * 0.008 * rumple
            v.co.z += abs(lump) * 1.4 + crease + 0.012
            # Over the sides and the foot: fall down the edge, then hang.
            over = max(0.0, abs(x) - 0.43)
            if over > 0:
                v.co.z -= min(over, 0.06) * 1.2 + max(0.0, over - 0.06) * 3.2
                v.co.x = math.copysign(0.43 + min(over, 0.06) * 0.6 + max(0.0, over - 0.06) * 0.15, x) + shift
            if y < -0.9:
                v.co.z -= (-0.9 - y) * 2.6
                v.co.y = -0.9 - (-0.9 - y) * 0.25
            # The top edge turned back on itself.
            if folded > 0 and y > y1 - folded:
                t = (y - (y1 - folded)) / folded
                v.co.y = (y1 - folded) - t * folded * 0.85
                v.co.z += 0.03 + math.sin(t * math.pi) * 0.025
    mesh = bpy.data.meshes.new("duvet")
    bm.to_mesh(mesh)
    bm.free()
    o = bpy.data.objects.new("duvet", mesh)
    bpy.context.collection.objects.link(o)
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    solid = o.modifiers.new("Thick", "SOLIDIFY")
    solid.thickness = 0.025
    solid.offset = -1
    smooth(o)
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


bpy.ops.wm.read_factory_settings(use_empty=True)

# Made, more or less: the duvet pulled up to the pillow and turned back.
pallet(2.0)
top = mattress(0.14)
pillow((0.0, 0.72, top + 0.06), 4)
duvet(top, -0.95, 0.62, "Duvet mustard", 0.8, 3, folded=0.22)
export("bed_made")

# Slept in: the pillow skewed, the duvet shoved aside in a heap.
pallet(2.0)
top = mattress(0.14)
pillow((0.12, 0.7, top + 0.05), -14, 0.85)
duvet(top, -0.95, 0.35, "Duvet teal", 1.6, 7, folded=0.4, shift=-0.08)
export("bed_slept_in")
print("DONE")
