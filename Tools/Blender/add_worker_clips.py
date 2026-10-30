"""Add the opening's animation clips to one rigged worker and export it as glTF.

Run headless, once per worker:
  blender --background --factory-startup --python add_worker_clips.py -- <worker_x.blend> <out.glb>

The .blend files are Astra's worker sources (Godot repo, tools/blender/astra_source).
Clips use the same joint format as the Godot project's worker_clips.json: Euler
rotations per joint in Godot axes (x+ raises an arm forward, z+ swings the right
arm out, elbow x+ bends the forearm up; torso x- leans forward; leg x+ swings
forward), relative to rest, and the hip position in Godot units (rest y = 0.5).
The conversion below is the one Astra used (make_worker.py), so the new clips sit
on the same rig exactly like the existing nine.

Running it again replaces clips of the same name, so it is safe to repeat.
"""
import math
import sys

import bpy
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1:]
BLEND, OUT = argv[0], argv[1]
FPS = 30
BODY_SCALE = 1.75 / 2.43

REST = {
    "Hips": (0.0, 0.0, 0.0), "Torso": (0.0, 0.0, 0.0), "Head": (0.02, 0.0, 0.0),
    "ArmL": (0.0, 0.0, -0.08), "ElbowL": (0.15, 0.0, 0.0), "ArmR": (0.0, 0.0, 0.08), "ElbowR": (0.15, 0.0, 0.0),
    "LegL": (0.0, 0.0, 0.0), "LegR": (0.0, 0.0, 0.0),
}


def smooth(points, length):
    """Authored (time, (x, y, z)) keys resampled at 30 fps with ease in/out between them."""
    out = []
    frames = int(round(length * FPS))
    for f in range(frames + 1):
        t = length * f / frames
        if t <= points[0][0]:
            out.append([t, list(points[0][1])])
            continue
        for i in range(1, len(points)):
            t0, v0 = points[i - 1]
            t1, v1 = points[i]
            if t <= t1 or i == len(points) - 1:
                u = 0.0 if t1 == t0 else min(1.0, (t - t0) / (t1 - t0))
                u = u * u * (3.0 - 2.0 * u)
                out.append([t, [a + (b - a) * u for a, b in zip(v0, v1)]])
                break
    return out


def clip(length, loop, **joints):
    """joints: name=[(t, (x, y, z)), ...] or name=(x, y, z) for a constant pose.
    Joints left out stay at rest. hips_pos=... keys the hip position."""
    tracks = []
    for joint in REST:
        spec = joints.get(joint, REST[joint])
        points = spec if isinstance(spec, list) else [(0.0, spec), (length, spec)]
        tracks.append({"joint": joint, "prop": "rotation", "keys": smooth(points, length)})
    pos = joints.get("hips_pos", (0.0, 0.5, 0.0))
    points = pos if isinstance(pos, list) else [(0.0, pos), (length, pos)]
    tracks.append({"joint": "Hips", "prop": "position", "keys": smooth(points, length)})
    return {"length": length, "loop": loop, "tracks": tracks}


CLIPS = {
    # Landlord: fist up at head height, forearm snapping forward twice, then a pause.
    "knock": clip(1.2, True,
        Torso=(-0.05, 0.05, 0.0), Head=(0.06, 0.0, 0.0),
        ArmR=(1.75, 0.0, 0.25), ArmL=(0.1, 0.0, -0.12), ElbowL=(0.3, 0.0, 0.0),
        ElbowR=[(0.0, (1.35, 0, 0)), (0.12, (0.7, 0, 0)), (0.24, (1.35, 0, 0)), (0.36, (0.7, 0, 0)),
                (0.5, (1.35, 0, 0)), (1.2, (1.35, 0, 0))]),
    # Talking with both hands.
    "talk": clip(2.4, True,
        ArmL=[(0.0, (0.45, 0, -0.3)), (0.6, (0.7, 0, -0.35)), (1.2, (0.4, 0, -0.28)), (1.8, (0.62, 0, -0.33)), (2.4, (0.45, 0, -0.3))],
        ElbowL=[(0.0, (1.2, 0, 0)), (0.6, (0.9, 0, 0)), (1.2, (1.3, 0, 0)), (1.8, (1.0, 0, 0)), (2.4, (1.2, 0, 0))],
        ArmR=[(0.0, (0.6, 0, 0.3)), (0.6, (0.35, 0, 0.26)), (1.2, (0.7, 0, 0.34)), (1.8, (0.4, 0, 0.28)), (2.4, (0.6, 0, 0.3))],
        ElbowR=[(0.0, (1.0, 0, 0)), (0.6, (1.35, 0, 0)), (1.2, (0.95, 0, 0)), (1.8, (1.3, 0, 0)), (2.4, (1.0, 0, 0))],
        Head=[(0.0, (0.03, 0.0, 0)), (0.6, (-0.03, 0.1, 0)), (1.2, (0.05, -0.05, 0)), (1.8, (0.0, 0.08, 0)), (2.4, (0.03, 0.0, 0))],
        Torso=[(0.0, (0, 0.0, 0)), (0.6, (0, 0.06, 0)), (1.2, (0, -0.04, 0)), (1.8, (0, 0.05, 0)), (2.4, (0, 0.0, 0))]),
    # Pointing straight ahead with a little jab, other hand on the hip.
    "point": clip(1.0, True,
        ArmR=[(0.0, (1.5, 0, -0.08)), (0.15, (1.6, 0, -0.08)), (0.35, (1.5, 0, -0.08)), (1.0, (1.5, 0, -0.08))],
        ElbowR=(0.05, 0.0, 0.0), Torso=(-0.04, -0.12, 0.0), Head=(0.0, -0.05, 0.0),
        ArmL=(0.25, 0.0, -0.45), ElbowL=(1.1, 0.0, 0.0)),
    # Holding the bill out at arm's length, a small impatient shake.
    "offer": clip(1.6, True,
        ArmR=[(0.0, (1.15, 0, 0.0)), (0.8, (1.2, 0, 0.02)), (1.6, (1.15, 0, 0.0))],
        ElbowR=[(0.0, (0.55, 0, 0)), (0.4, (0.62, 0, 0)), (0.8, (0.55, 0, 0)), (1.2, (0.62, 0, 0)), (1.6, (0.55, 0, 0))],
        Torso=(-0.07, 0.0, 0.0), Head=(-0.02, 0.0, 0.0), ArmL=(0.05, 0.0, -0.15), ElbowL=(0.25, 0.0, 0.0)),
    # Arms crossed, head shaking slowly, a foot tapping.
    "impatient": clip(1.8, True,
        ArmL=(0.75, 0.0, 0.62), ElbowL=(1.85, 0.0, 0.0), ArmR=(0.7, 0.0, -0.62), ElbowR=(1.85, 0.0, 0.0),
        Torso=(0.04, 0.0, 0.0),
        Head=[(0.0, (0.0, 0.12, 0)), (0.9, (0.0, -0.12, 0)), (1.8, (0.0, 0.12, 0))],
        LegR=[(0.0, (0, 0, 0)), (0.15, (0.14, 0, 0)), (0.3, (0, 0, 0)), (0.9, (0, 0, 0)), (1.05, (0.14, 0, 0)), (1.2, (0, 0, 0)), (1.8, (0, 0, 0))]),
    # Reading a sheet held in both hands.
    "hold_paper": clip(2.0, True,
        ArmL=(0.95, 0.0, 0.32), ElbowL=(1.05, 0.0, 0.0), ArmR=(0.95, 0.0, -0.32), ElbowR=(1.05, 0.0, 0.0),
        Head=[(0.0, (-0.3, 0.02, 0)), (1.0, (-0.32, -0.04, 0)), (2.0, (-0.3, 0.02, 0))],
        Torso=[(0.0, (-0.04, 0, 0)), (1.0, (-0.02, 0, 0)), (2.0, (-0.04, 0, 0))]),
    # Sitting at the desk, typing.
    "sit_type": clip(1.0, True,
        hips_pos=(0.0, -0.05, 0.12), LegL=(1.45, 0.0, 0.0), LegR=(1.45, 0.0, 0.0),
        Torso=(-0.12, 0.0, 0.0), Head=(-0.12, 0.0, 0.0),
        ArmL=[(0.0, (0.9, 0, -0.05)), (0.25, (0.95, 0, -0.05)), (0.5, (0.9, 0, -0.05)), (1.0, (0.9, 0, -0.05))],
        ElbowL=[(0.0, (0.5, 0, 0)), (0.25, (0.62, 0, 0)), (0.5, (0.5, 0, 0)), (1.0, (0.5, 0, 0))],
        ArmR=[(0.0, (0.9, 0, 0.05)), (0.5, (0.9, 0, 0.05)), (0.75, (0.95, 0, 0.05)), (1.0, (0.9, 0, 0.05))],
        ElbowR=[(0.0, (0.5, 0, 0)), (0.5, (0.5, 0, 0)), (0.75, (0.62, 0, 0)), (1.0, (0.5, 0, 0))]),
}

bpy.ops.wm.open_mainfile(filepath=BLEND)
arm_obj = next(o for o in bpy.data.objects if o.type == "ARMATURE")
arm_data = arm_obj.data
for name in CLIPS:
    old = bpy.data.actions.get(name)
    if old is not None:
        bpy.data.actions.remove(old)

# Conversion, as in make_worker.py (Godot axes -> Blender axes).
bpy.context.view_layer.objects.active = arm_obj
if arm_obj.animation_data is None:
    arm_obj.animation_data_create()
rest = {b.name: b.matrix_local.copy() for b in arm_data.bones}
heads = {b.name: b.head_local.copy() for b in arm_data.bones}
ORDER = [b.name for b in arm_data.bones]


def sample(keys, t):
    if t <= keys[0][0]:
        return Vector(keys[0][1])
    for i in range(1, len(keys)):
        t0, v0 = keys[i - 1]
        t1, v1 = keys[i]
        if t <= t1:
            f = 0.0 if t1 == t0 else (t - t0) / (t1 - t0)
            return Vector(v0).lerp(Vector(v1), f)
    return Vector(keys[-1][1])


def pose_matrix(rot_godot):
    gx, gy, gz = rot_godot
    return Matrix.Rotation(gy, 4, "Z") @ Matrix.Rotation(-gx, 4, "X") @ Matrix.Rotation(gz, 4, "Y")


for clip_name, spec in CLIPS.items():
    action = bpy.data.actions.new(clip_name)
    action.use_fake_user = True
    arm_obj.animation_data.action = action
    frames = max(1, int(round(spec["length"] * FPS)))
    tracks = {(tr["joint"], tr["prop"]): tr["keys"] for tr in spec["tracks"]}
    for pb in arm_obj.pose.bones:
        pb.rotation_mode = "QUATERNION"
    for frame in range(frames + 1):
        t = spec["length"] * frame / frames
        for bone_name in ORDER:
            keys = tracks.get((bone_name, "rotation"))
            rot = tuple(sample(keys, t)) if keys else (0.0, 0.0, 0.0)
            head = heads[bone_name]
            delta = Matrix.Translation(head) @ pose_matrix(rot) @ Matrix.Translation(-head)
            if bone_name == "Hips":
                pkeys = tracks.get(("Hips", "position"))
                if pkeys:
                    p = sample(pkeys, t)
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
for pb in arm_obj.pose.bones:
    pb.matrix_basis = Matrix.Identity(4)
bpy.context.view_layer.update()
bpy.ops.export_scene.gltf(filepath=OUT, export_animations=True, export_animation_mode="ACTIONS", export_skins=True,
                          export_yup=True, export_cameras=False, export_lights=False)
print("CLIPS ADDED %s -> %s: %s (total actions %d)" % (BLEND, OUT, ", ".join(CLIPS), len(bpy.data.actions)))
