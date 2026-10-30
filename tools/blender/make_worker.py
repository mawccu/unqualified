"""Builds the UNQUALIFIED worker: an original, rigged, low-poly character.

Run headless:
  blender --background --factory-startup --python tools/blender/make_worker.py -- \
      <clips.json> <out.glb> [<preview_dir>]

Original model, authored from scratch with Blender's own modelling tools (no
third-party assets): chunky bevelled blocks for a toy-like silhouette, flat
shaded, one skeleton, rigid per-bone skinning (each part follows exactly one
bone, like a wooden toy), and the nine animation clips baked from the game's
own definitions (worker_clips.json, dumped by tools/dump_worker_clips.gd).

Contract with the game: docs/assets/WORKER_GLB_CONTRACT.md. Materials named
Shirt, Vest, Trousers, Shoes, Skin, Hair and Accent are tinted per costume;
Eye and Detail are never tinted. Hair_<style> and Acc_<name> meshes are shown
per costume. Bones: Hips, Torso, Head, ArmL, ElbowL, HandL, ArmR, ElbowR,
HandR, LegL, FootL, LegR, FootR.

The character faces -Y in Blender (front view), which glTF turns into +Z; the
game turns it half way round.
"""
import json
import math
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Quaternion, Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
CLIPS_PATH = argv[0]
OUT_PATH = argv[1]
PREVIEW_DIR = argv[2] if len(argv) > 2 else None

FPS = 30
BODY_SCALE = 1.1  # the game's fallback body is 1.1x its authored numbers

# Joint heights and offsets (metres), chosen so the proportions are toy-like:
# a big head, a compact torso, short legs.
HIP_Z = 0.55
TORSO_Z = 0.57
HEAD_Z = 1.19
SHOULDER_Z = 1.07
SHOULDER_X = 0.28
ELBOW_Z = 0.79
WRIST_Z = 0.52
LEG_X = 0.13
ANKLE_Z = 0.12

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = FPS

# Materials ----------------------------------------------------------------------

MATERIAL_COLORS = {
    "Shirt": (0.92, 0.90, 0.85),
    "Vest": (0.20, 0.20, 0.23),
    "Trousers": (0.15, 0.15, 0.17),
    "Shoes": (0.14, 0.13, 0.13),
    "Skin": (0.85, 0.69, 0.57),
    "Hair": (0.22, 0.14, 0.10),
    "Accent": (0.55, 0.14, 0.20),
    "Eye": (0.06, 0.06, 0.08),
    "Detail": (0.18, 0.18, 0.20),
}
materials = {}
for name, rgb in MATERIAL_COLORS.items():
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
    bsdf.inputs["Roughness"].default_value = 0.85
    bsdf.inputs["Metallic"].default_value = 0.0
    mat.diffuse_color = (rgb[0], rgb[1], rgb[2], 1.0)
    materials[name] = mat

# Armature -----------------------------------------------------------------------

arm_data = bpy.data.armatures.new("WorkerArmature")
arm_obj = bpy.data.objects.new("Armature", arm_data)
scene.collection.objects.link(arm_obj)
bpy.context.view_layer.objects.active = arm_obj
arm_obj.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")

BONES = [
    # name, parent, head, tail
    ("Hips", None, (0, 0, HIP_Z), (0, 0, HIP_Z + 0.07)),
    ("Torso", "Hips", (0, 0, TORSO_Z), (0, 0, HEAD_Z)),
    ("Head", "Torso", (0, 0, HEAD_Z), (0, 0, HEAD_Z + 0.52)),
]
for side, sx in (("L", 1.0), ("R", -1.0)):
    BONES += [
        ("Arm" + side, "Torso", (sx * SHOULDER_X, 0, SHOULDER_Z), (sx * SHOULDER_X, 0, ELBOW_Z)),
        ("Elbow" + side, "Arm" + side, (sx * SHOULDER_X, 0, ELBOW_Z), (sx * SHOULDER_X, 0, WRIST_Z)),
        ("Hand" + side, "Elbow" + side, (sx * SHOULDER_X, 0, WRIST_Z), (sx * SHOULDER_X, 0, WRIST_Z - 0.14)),
        ("Leg" + side, "Hips", (sx * LEG_X, 0, HIP_Z), (sx * LEG_X, 0, ANKLE_Z)),
        ("Foot" + side, "Leg" + side, (sx * LEG_X, 0, ANKLE_Z), (sx * LEG_X, -0.2, 0.06)),
    ]
edit_bones = {}
for name, parent, head, tail in BONES:
    eb = arm_data.edit_bones.new(name)
    eb.head = head
    eb.tail = tail
    if parent:
        eb.parent = edit_bones[parent]
    edit_bones[name] = eb
bpy.ops.object.mode_set(mode="OBJECT")

# Modelling helpers ---------------------------------------------------------------

parts = []  # (object, bone, group) group in {"body", "hair", "acc"}


def block(name, size, center, mat, bone, group="body", taper_top=1.0, taper_bottom=1.0,
          bevel=0.03, rot=(0.0, 0.0, 0.0)):
    """A rounded block: the building brick of the toy-like look."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x *= size[0]
        v.co.y *= size[1]
        v.co.z *= size[2]
        t = taper_top if v.co.z > 0 else taper_bottom
        v.co.x *= t
        v.co.y *= t
    offset = min(bevel, min(size) * 0.32)
    bmesh.ops.bevel(bm, geom=list(bm.edges), offset=offset, offset_type="OFFSET",
                    segments=2, profile=0.5, affect="EDGES")
    transform = Matrix.Translation(Vector(center)) @ Euler([math.radians(a) for a in rot], "XYZ").to_matrix().to_4x4()
    bmesh.ops.transform(bm, matrix=transform, verts=bm.verts)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for poly in mesh.polygons:
        poly.use_smooth = False
    obj = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(obj)
    obj.data.materials.append(materials[mat])
    parts.append((obj, bone, group))
    return obj


def mirror(fn, **kw):
    """Call fn once for the left (+X) side and once for the right (-X) side."""
    for side, sx in (("L", 1.0), ("R", -1.0)):
        fn(side, sx, **kw)


# Body ---------------------------------------------------------------------------

def feet_and_legs(side, sx):
    block("Shoe" + side, (0.19, 0.36, 0.13), (sx * LEG_X, -0.05, 0.065), "Shoes", "Foot" + side, bevel=0.045)
    block("Leg" + side, (0.17, 0.17, 0.44), (sx * LEG_X, 0.0, 0.34), "Trousers", "Leg" + side,
          taper_top=1.0, taper_bottom=0.9, bevel=0.03)


def arms(side, sx):
    block("Sleeve" + side, (0.13, 0.13, 0.31), (sx * SHOULDER_X, 0.0, 0.93), "Shirt", "Arm" + side, bevel=0.035)
    block("Forearm" + side, (0.12, 0.12, 0.28), (sx * SHOULDER_X, 0.0, 0.655), "Shirt", "Elbow" + side, bevel=0.035)
    block("Cuff" + side, (0.13, 0.13, 0.035), (sx * SHOULDER_X, 0.0, 0.54), "Accent", "Elbow" + side, bevel=0.012)
    block("Mitten" + side, (0.125, 0.085, 0.15), (sx * SHOULDER_X, 0.0, 0.455), "Skin", "Hand" + side, bevel=0.04)
    block("Thumb" + side, (0.04, 0.05, 0.07), (sx * (SHOULDER_X - 0.075), -0.045, 0.48), "Skin", "Hand" + side, bevel=0.015)


mirror(feet_and_legs)
mirror(arms)
block("Pelvis", (0.46, 0.30, 0.15), (0, 0, 0.61), "Trousers", "Hips", bevel=0.04)
block("Torso", (0.50, 0.32, 0.62), (0, 0, 0.88), "Shirt", "Torso", taper_top=0.94, bevel=0.05)
block("VestBody", (0.54, 0.35, 0.44), (0, 0, 0.89), "Vest", "Torso", taper_top=0.95, bevel=0.04)
block("Collar", (0.30, 0.22, 0.06), (0, 0, 1.17), "Shirt", "Torso", bevel=0.02)
block("Neck", (0.14, 0.14, 0.07), (0, 0, 1.21), "Skin", "Head", bevel=0.02)
block("PocketSquare", (0.075, 0.02, 0.075), (0.12, -0.18, 1.02), "Accent", "Torso", bevel=0.01, rot=(0, 0, 8))

# Head: a rounded block with a deliberately small, readable face.
block("Head", (0.50, 0.48, 0.52), (0, 0, 1.45), "Skin", "Head", bevel=0.09)
for side, sx in (("L", 1.0), ("R", -1.0)):
    block("Ear" + side, (0.05, 0.08, 0.11), (sx * 0.265, 0.0, 1.44), "Skin", "Head", bevel=0.015)
    block("Eye" + side, (0.065, 0.03, 0.095), (sx * 0.12, -0.238, 1.50), "Eye", "Head", bevel=0.012)
    block("Brow" + side, (0.115, 0.03, 0.028), (sx * 0.12, -0.24, 1.605), "Eye", "Head", bevel=0.008,
          rot=(0, 12.0 * sx, 0))
block("Nose", (0.07, 0.07, 0.07), (0, -0.265, 1.42), "Skin", "Head", bevel=0.02)
block("Mouth", (0.11, 0.03, 0.03), (0, -0.242, 1.32), "Eye", "Head", bevel=0.008)

# Hair variants (only one is shown per costume) ----------------------------------

cap = dict(mat="Hair", bone="Head", group="hair")
block("Hair_short", (0.54, 0.52, 0.13), (0, 0.01, 1.745), bevel=0.05, **cap)
block("Hair_short_back", (0.54, 0.10, 0.30), (0, 0.235, 1.56), bevel=0.04, **cap)
block("Hair_pompadour", (0.54, 0.52, 0.13), (0, 0.01, 1.745), bevel=0.05, **cap)
block("Hair_pompadour_quiff", (0.40, 0.26, 0.22), (0, -0.12, 1.85), bevel=0.06, rot=(-18, 0, 0), **cap)
block("Hair_ponytail", (0.54, 0.52, 0.13), (0, 0.01, 1.745), bevel=0.05, **cap)
block("Hair_ponytail_tail", (0.13, 0.13, 0.40), (0, 0.31, 1.46), bevel=0.04, rot=(-10, 0, 0), **cap)
block("Hair_beanie", (0.57, 0.55, 0.26), (0, 0.0, 1.76), bevel=0.08, **cap)
block("Hair_beanie_cuff", (0.59, 0.57, 0.07), (0, 0.0, 1.655), "Accent", "Head", "hair", bevel=0.025)

# Accessories (shown per costume) -------------------------------------------------

block("Acc_bow_tie", (0.075, 0.04, 0.06), (0.065, -0.185, 1.13), "Accent", "Torso", "acc", bevel=0.01, rot=(0, 0, 14))
block("Acc_bow_tie_b", (0.075, 0.04, 0.06), (-0.065, -0.185, 1.13), "Accent", "Torso", "acc", bevel=0.01, rot=(0, 0, -14))
block("Acc_lanyard", (0.035, 0.015, 0.36), (0, -0.19, 0.95), "Accent", "Torso", "acc", bevel=0.005)
block("Acc_lanyard_badge", (0.15, 0.02, 0.19), (0, -0.195, 0.74), "Shirt", "Torso", "acc", bevel=0.01)
block("Acc_apron", (0.47, 0.022, 0.42), (0, -0.185, 0.80), "Vest", "Torso", "acc", bevel=0.01)
block("Acc_apron_pocket", (0.18, 0.03, 0.11), (0, -0.2, 0.72), "Accent", "Torso", "acc", bevel=0.01)
block("Acc_headset", (0.57, 0.05, 0.05), (0, 0.0, 1.725), "Detail", "Head", "acc", bevel=0.015)
for side, sx in (("L", 1.0), ("R", -1.0)):
    block("Acc_headset_cup" + side, (0.07, 0.13, 0.15), (sx * 0.30, 0.0, 1.46), "Detail", "Head", "acc", bevel=0.025)
block("Acc_headset_mic", (0.025, 0.14, 0.025), (-0.24, -0.15, 1.34), "Detail", "Head", "acc", bevel=0.008)
block("Acc_radio", (0.09, 0.055, 0.17), (0.28, 0.0, 0.61), "Detail", "Hips", "acc", bevel=0.02)
block("Acc_belt", (0.51, 0.34, 0.055), (0, 0, 0.655), "Detail", "Hips", "acc", bevel=0.015)
block("Acc_belt_pouch", (0.11, 0.07, 0.11), (0.17, -0.18, 0.62), "Detail", "Hips", "acc", bevel=0.02)
block("Acc_one_glove", (0.14, 0.105, 0.165), (SHOULDER_X, 0.0, 0.455), "Shirt", "HandL", "acc", bevel=0.045)

# Skin everything to the armature ---------------------------------------------------


def finish(obj, bone):
    group = obj.vertex_groups.new(name=bone)
    group.add([v.index for v in obj.data.vertices], 1.0, "REPLACE")
    modifier = obj.modifiers.new("Armature", "ARMATURE")
    modifier.object = arm_obj
    obj.parent = arm_obj


for obj, bone, _group in parts:
    finish(obj, bone)

# Join the always-visible body parts into one mesh called Worker.
body_objects = [obj for obj, _b, group in parts if group == "body"]
bpy.ops.object.select_all(action="DESELECT")
for obj in body_objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = body_objects[0]
bpy.ops.object.join()
worker = bpy.context.view_layer.objects.active
worker.name = "Worker"
worker.data.name = "Worker"

# Animation ------------------------------------------------------------------------

with open(CLIPS_PATH) as handle:
    CLIPS = json.load(handle)

# Godot axes -> Blender axes: x -> -x, y -> z, z -> y (a proper rotation, so
# angles keep their sign and only the axes are renamed).
bpy.context.view_layer.objects.active = arm_obj
arm_obj.animation_data_create()
rest = {b.name: b.matrix_local.copy() for b in arm_data.bones}
heads = {b.name: b.head_local.copy() for b in arm_data.bones}
ORDER = [name for name, _p, _h, _t in BONES]
JOINT_TO_BONE = {"Hips": "Hips", "Torso": "Torso", "Head": "Head", "ArmL": "ArmL", "ElbowL": "ElbowL",
                 "ArmR": "ArmR", "ElbowR": "ElbowR", "LegL": "LegL", "LegR": "LegR"}


def sample(keys, t):
    """Linear sample of [[time, [x, y, z]], ...]."""
    if t <= keys[0][0]:
        return Vector(keys[0][1])
    for i in range(1, len(keys)):
        t0, v0 = keys[i - 1]
        t1, v1 = keys[i]
        if t <= t1:
            f = 0.0 if t1 == t0 else (t - t0) / (t1 - t0)
            return Vector(v0).lerp(Vector(v1), f)
    return Vector(keys[-1][1])


def pose_matrix(rot_godot, trans_blender):
    """Godot-style euler (YXZ order) as a Blender rotation matrix."""
    gx, gy, gz = rot_godot
    rz = Matrix.Rotation(gy, 4, "Z")
    rx = Matrix.Rotation(-gx, 4, "X")
    ry = Matrix.Rotation(gz, 4, "Y")
    return rz @ rx @ ry


for clip_name, clip in CLIPS.items():
    action = bpy.data.actions.new(clip_name)
    action.use_fake_user = True
    arm_obj.animation_data.action = action
    frames = max(1, int(round(clip["length"] * FPS)))
    tracks = {}
    for track in clip["tracks"]:
        tracks[(track["joint"], track["prop"])] = track["keys"]
    for pb in arm_obj.pose.bones:
        pb.rotation_mode = "QUATERNION"
    for frame in range(frames + 1):
        t = clip["length"] * frame / frames
        for bone_name in ORDER:
            rot = (0.0, 0.0, 0.0)
            keys = tracks.get((bone_name, "rotation"))
            if keys:
                rot = tuple(sample(keys, t))
            head = heads[bone_name]
            delta = Matrix.Translation(head) @ pose_matrix(rot, None) @ Matrix.Translation(-head)
            if bone_name == "Hips":
                pkeys = tracks.get(("Hips", "position"))
                if pkeys:
                    p = sample(pkeys, t)
                    # Godot (dx, dy, dz) relative to the rest hip height -> Blender (-dx, dz, dy), scaled.
                    offset = Vector((-p.x, p.z, p.y - 0.5)) * BODY_SCALE
                    delta = Matrix.Translation(offset) @ delta
            basis = rest[bone_name].inverted() @ delta @ rest[bone_name]
            loc, quat, _scale = basis.decompose()
            pb = arm_obj.pose.bones[bone_name]
            pb.location = loc
            pb.rotation_quaternion = quat
            pb.keyframe_insert("location", frame=frame)
            pb.keyframe_insert("rotation_quaternion", frame=frame)
    action.frame_range = (0, frames)
arm_obj.animation_data.action = None

print("built %d parts, %d bones, %d clips" % (len(parts), len(arm_data.bones), len(CLIPS)))

# Preview renders (so a person can look at it) -------------------------------------

if PREVIEW_DIR:
    import os
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 520
    scene.render.resolution_y = 620
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.world = bpy.data.worlds.new("W")
    scene.world.color = (0.55, 0.57, 0.58)
    for hair in ("Hair_pompadour", "Hair_ponytail", "Hair_beanie"):
        pass

    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 2.15
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam

    def show_only(hair, acc_names):
        for obj, _bone, group in parts:
            if group == "hair":
                obj.hide_render = not obj.name.startswith(hair)
            elif group == "acc":
                obj.hide_render = not any(obj.name.startswith("Acc_" + a) for a in acc_names)

    def shot(name, clip, frame, view):
        arm_obj.animation_data.action = bpy.data.actions[clip] if clip else None
        scene.frame_set(frame)
        if view == "front":
            cam.location = (0, -6, 0.93)
            cam.rotation_euler = (math.radians(90), 0, 0)
        elif view == "side":
            cam.location = (6, 0, 0.93)
            cam.rotation_euler = (math.radians(90), 0, math.radians(90))
        else:
            cam.location = (4.2, -4.2, 2.1)
            cam.rotation_euler = (math.radians(72), 0, math.radians(45))
        scene.render.filepath = os.path.join(PREVIEW_DIR, name + ".png")
        bpy.ops.render.render(write_still=True)

    show_only("Hair_short", ["bow_tie", "lanyard"])
    shot("rest_front", None, 1, "front")
    shot("rest_side", None, 1, "side")
    shot("rest_three_quarter", None, 1, "tq")
    shot("walk_side", "walk", 6, "side")
    shot("carry_front", "carry_idle", 4, "front")
    shot("stumble_side", "stumble", 4, "side")
    show_only("Hair_pompadour", ["headset", "radio"])
    shot("variant1_front", None, 1, "front")
    show_only("Hair_ponytail", ["apron", "lanyard"])
    shot("variant2_front", None, 1, "front")
    show_only("Hair_beanie", ["belt", "one_glove"])
    shot("variant3_front", None, 1, "front")
    arm_obj.animation_data.action = None
    for obj, _bone, group in parts:
        if group != "body":
            obj.hide_render = False

# Export ------------------------------------------------------------------------------

arm_obj.animation_data.action = None
bpy.ops.object.select_all(action="SELECT")
export_args = dict(
    filepath=OUT_PATH,
    export_format="GLB",
    use_selection=False,
    export_animations=True,
    export_animation_mode="ACTIONS",
    export_skins=True,
    export_yup=True,
    export_apply=False,
    export_cameras=False,
    export_lights=False,
)
try:
    bpy.ops.export_scene.gltf(**export_args)
except TypeError as error:
    print("export argument problem, retrying with fewer options:", error)
    bpy.ops.export_scene.gltf(filepath=OUT_PATH, export_format="GLB")
print("exported", OUT_PATH)
