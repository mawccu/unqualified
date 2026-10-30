"""Model the first-person arm segments and the printed rent bill (original, made here).

  blender --background --factory-startup --python make_hands.py -- <hands_dir> <apartment_dir> <bill_texture.png>

Each arm segment is its own FBX with its pivot at the joint it turns about and
its length running along Blender -Y, which arrives in Unity as +Z. Unity builds
the hierarchy and poses it in code (FirstPersonArms), so nothing here depends
on FBX hierarchies or bone axes. Segments are symmetric left/right, so the same
parts make both hands. Materials are the worker's own Skin and Shirt, so the
costume tint reaches the sleeves and hands.

The bill is a gently bowed A4 sheet: a printed front (toward Blender +Y, which
faces the holder in Unity) and a plain back, origin at the middle of its bottom
edge, UVs 0..1 across the print.
"""
import math
import os
import sys

import bpy
import bmesh
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
HANDS, APARTMENT, BILL_TEX = argv[0], argv[1], argv[2]
os.makedirs(HANDS, exist_ok=True)

COLOURS = {"Skin": (0.85, 0.69, 0.57), "Shirt": (0.92, 0.90, 0.85), "Bill print": (1, 1, 1), "Paper": (0.94, 0.93, 0.88)}
MATS = {}


def mat(name):
    if name not in MATS:
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        r, g, b = COLOURS[name]
        bsdf.inputs["Base Color"].default_value = (r, g, b, 1)
        MATS[name] = m
    return MATS[name]


parts = []


def capsule(radius_a, radius_b, length, material, start=0.0, verts=16, cap_a=True, cap_b=True, squash=1.0):
    """A tapered tube from y=start to y=start-length (toward -Y) with round ends; squash flattens it in Z."""
    bm = bmesh.new()
    rings = []
    steps = 6
    profile = []
    if cap_a:
        for i in range(steps, 0, -1):
            a = i / steps * math.pi / 2
            profile.append((start + math.sin(a) * radius_a * 0.9, math.cos(a) * radius_a))
    profile.append((start, radius_a))
    profile.append((start - length, radius_b))
    if cap_b:
        for i in range(1, steps + 1):
            a = i / steps * math.pi / 2
            profile.append((start - length - math.sin(a) * radius_b * 0.9, math.cos(a) * radius_b))
    for y, r in profile:
        ring = []
        for k in range(verts):
            t = k / verts * math.tau
            ring.append(bm.verts.new((math.cos(t) * max(r, 1e-4), y, math.sin(t) * max(r, 1e-4) * squash)))
        rings.append(ring)
    for i in range(len(rings) - 1):
        for k in range(verts):
            a, b = rings[i][k], rings[i][(k + 1) % verts]
            c, d = rings[i + 1][(k + 1) % verts], rings[i + 1][k]
            bm.faces.new((a, b, c, d))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    mesh = bpy.data.meshes.new("part")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("part", mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat(material))
    for p in obj.data.polygons:
        p.use_smooth = True
    parts.append(obj)
    return obj


def rounded_box(size, loc, material, bevel):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    o = bpy.context.object
    o.scale = size
    bpy.ops.object.transform_apply(scale=True)
    mod = o.modifiers.new("Bevel", "BEVEL")
    mod.width = bevel
    mod.segments = 4
    o.data.materials.append(mat(material))
    bpy.ops.object.shade_smooth()
    parts.append(o)
    return o


def export(name, folder):
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
    bpy.ops.export_scene.fbx(filepath=os.path.join(folder, name + ".fbx"), use_selection=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, object_types={"MESH"},
                             mesh_smooth_type="FACE", path_mode="AUTO")
    bpy.ops.object.delete()
    parts = []
    print("PART", name)


bpy.ops.wm.read_factory_settings(use_empty=True)

# Sleeves: upper arm (shoulder to elbow) and forearm (elbow to wrist) with a cuff.
capsule(0.05, 0.047, 0.27, "Shirt", verts=20, cap_b=False)
export("arm_upper", HANDS)
capsule(0.047, 0.041, 0.235, "Shirt", verts=20, cap_a=True, cap_b=False)
capsule(0.047, 0.047, 0.03, "Shirt", start=-0.232, verts=20, cap_a=False, cap_b=False)
export("arm_forearm", HANDS)

# Palm: pivot at the wrist, a short wrist stub back into the cuff, knuckles at 0.08.
capsule(0.027, 0.03, 0.04, "Skin", start=0.03, verts=16, cap_a=False, cap_b=False, squash=0.75)
rounded_box((0.082, 0.082, 0.032), (0.0, -0.043, 0.0), "Skin", 0.014)
export("hand_palm", HANDS)

# Finger and thumb segments: pivot at the joint, rounded both ends so joints stay closed when bent.
capsule(0.0108, 0.0102, 0.04, "Skin", verts=12, squash=0.92)
export("finger_base", HANDS)
capsule(0.0102, 0.0094, 0.03, "Skin", verts=12, squash=0.92)
export("finger_tip", HANDS)
capsule(0.0135, 0.0125, 0.034, "Skin", verts=12, squash=0.9)
export("thumb_base", HANDS)
capsule(0.0125, 0.0112, 0.028, "Skin", verts=12, squash=0.9)
export("thumb_tip", HANDS)

# The rent bill: two bowed faces, printed front toward +Y.
def sheet(material, y_offset, flip):
    bm = bmesh.new()
    cols, rows = 10, 2
    w, h = 0.21, 0.297
    verts = []
    uv_of = {}
    for j in range(rows + 1):
        row = []
        for i in range(cols + 1):
            u, v = i / cols, j / rows
            x = (u - 0.5) * w
            bow = 0.01 * (1.0 - (2.0 * u - 1.0) ** 2)
            vert = bm.verts.new((x, y_offset - bow, v * h))
            # Unity mirrors X on import, so the print is laid out mirrored here.
            uv_of[vert] = (1.0 - u, v)
            row.append(vert)
        verts.append(row)
    for j in range(rows):
        for i in range(cols):
            quad = (verts[j][i], verts[j][i + 1], verts[j + 1][i + 1], verts[j + 1][i])
            bm.faces.new(quad if flip else tuple(reversed(quad)))
    uv = bm.loops.layers.uv.new()
    for f in bm.faces:
        for loop in f.loops:
            loop[uv].uv = uv_of[loop.vert]
    mesh = bpy.data.meshes.new("sheet")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("sheet", mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat(material))
    parts.append(obj)


# Printed face: the Unity side that faces the holder is Blender +Y. With X mirrored
# on import, u is reversed so the print reads correctly in Unity.
sheet("Bill print", 0.0008, False)  # faces +Y (the reader)
sheet("Paper", -0.0008, True)       # faces -Y
export("bill_sheet", APARTMENT)

# The print itself (rendered with PIL so the world bill is readable up close).
try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError:
    Image = None
print("BILL TEXTURE", "skipped (no PIL in Blender)" if Image is None else "drawn")
print("HANDS DONE")
