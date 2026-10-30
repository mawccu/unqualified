class_name WorkerAnimations
extends RefCounted
## Authored in code so there is no external tool to open: every clip is a set of
## keyframes on the worker's joints. EVERY clip keys EVERY joint. A joint a clip
## does not move is keyed at its rest pose; otherwise it would keep whatever the
## previous clip left it at (a leg frozen mid-stride after a stumble).
##
## The performance is in the timing, not the polygons: a purposeful motion with
## a slight overshoot or delayed wobble. Angles are radians. Characters face -Z;
## a positive X rotation swings a limb forward, a negative one leans a torso forward.

const IDLE: StringName = &"idle"
const WALK: StringName = &"walk"
const SPRINT: StringName = &"sprint"
const CARRY_IDLE: StringName = &"carry_idle"
const CARRY_WALK: StringName = &"carry_walk"
const INTERACT: StringName = &"interact"
const CLEAN: StringName = &"clean"
const STUMBLE: StringName = &"stumble"
const CELEBRATE: StringName = &"celebrate"

## Clips that play a state of the body and loop until it changes.
const LOOPING: Array[StringName] = [IDLE, WALK, SPRINT, CARRY_IDLE, CARRY_WALK, CLEAN]
## Short actions that play once and return to idle on their own.
const ONE_SHOT: Array[StringName] = [INTERACT, STUMBLE, CELEBRATE]

const HIPS: String = "Body/Hips"
const TORSO: String = "Body/Hips/Torso"
const HEAD: String = "Body/Hips/Torso/HeadPivot"
const ARM_L: String = "Body/Hips/Torso/ArmL"
const ELBOW_L: String = "Body/Hips/Torso/ArmL/ElbowL"
const ARM_R: String = "Body/Hips/Torso/ArmR"
const ELBOW_R: String = "Body/Hips/Torso/ArmR/ElbowR"
const LEG_L: String = "Body/Hips/LegL"
const LEG_R: String = "Body/Hips/LegR"

const REST_HIPS_Y: float = 0.5
const STEPS: int = 8


## Every joint, the property the clips drive, and its rest value.
static func joints() -> Array[Dictionary]:
	return [
		{"path": HIPS, "prop": "position", "rest": Vector3(0.0, REST_HIPS_Y, 0.0)},
		{"path": HIPS, "prop": "rotation", "rest": Vector3.ZERO},
		{"path": TORSO, "prop": "rotation", "rest": Vector3.ZERO},
		{"path": HEAD, "prop": "rotation", "rest": Vector3(0.02, 0.0, 0.0)},
		{"path": ARM_L, "prop": "rotation", "rest": Vector3(0.0, 0.0, -0.08)},
		{"path": ELBOW_L, "prop": "rotation", "rest": Vector3(0.15, 0.0, 0.0)},
		{"path": ARM_R, "prop": "rotation", "rest": Vector3(0.0, 0.0, 0.08)},
		{"path": ELBOW_R, "prop": "rotation", "rest": Vector3(0.15, 0.0, 0.0)},
		{"path": LEG_L, "prop": "rotation", "rest": Vector3.ZERO},
		{"path": LEG_R, "prop": "rotation", "rest": Vector3.ZERO},
	]


static func clip_names() -> Array[StringName]:
	return [IDLE, WALK, SPRINT, CARRY_IDLE, CARRY_WALK, INTERACT, CLEAN, STUMBLE, CELEBRATE]


static func build_library() -> AnimationLibrary:
	var lib := AnimationLibrary.new()
	lib.add_animation(IDLE, _idle())
	lib.add_animation(WALK, _walk())
	lib.add_animation(SPRINT, _sprint())
	lib.add_animation(CARRY_IDLE, _carry_idle())
	lib.add_animation(CARRY_WALK, _carry_walk())
	lib.add_animation(INTERACT, _interact())
	lib.add_animation(CLEAN, _clean())
	lib.add_animation(STUMBLE, _stumble())
	lib.add_animation(CELEBRATE, _celebrate())
	return lib


# Clips -------------------------------------------------------------------------

static func _idle() -> Animation:
	# Weight shifts between the feet, a gentle chest bob, a tiny look around.
	var dur: float = 2.6
	return _clip(dur, true, {
		HIPS + ":position": _sine(dur, Vector3(0, REST_HIPS_Y, 0), Vector3(0, 0.012, 0), 2, 0.0),
		TORSO: _sine(dur, Vector3.ZERO, Vector3(0.02, 0.0, 0.035), 1, 0.0),
		HEAD: _sine(dur, Vector3(0.02, 0, 0), Vector3(0.03, 0.28, 0), 1, 0.6),
		ARM_L: _sine(dur, Vector3(0, 0, -0.08), Vector3(0.04, 0, 0.02), 1, 0.5),
		ARM_R: _sine(dur, Vector3(0, 0, 0.08), Vector3(0.04, 0, -0.02), 1, 2.1),
		ELBOW_L: _sine(dur, Vector3(0.15, 0, 0), Vector3(0.05, 0, 0), 1, 0.2),
		ELBOW_R: _sine(dur, Vector3(0.15, 0, 0), Vector3(0.05, 0, 0), 1, 1.7),
		LEG_L: _sine(dur, Vector3.ZERO, Vector3(0.02, 0, 0), 1, 0.0),
		LEG_R: _sine(dur, Vector3.ZERO, Vector3(0.02, 0, 0), 1, PI),
	})


static func _walk() -> Animation:
	# Slight toe-out, high-ish knees, swinging arms, feet landing a touch wide.
	var dur: float = 0.8
	return _clip(dur, true, {
		HIPS + ":position": _sine(dur, Vector3(0, REST_HIPS_Y, 0), Vector3(0, 0.035, 0), 2, 1.57),
		TORSO: _sine(dur, Vector3(0.0, 0, 0), Vector3(0.0, 0.12, 0.05), 1, 0.0),
		HEAD: _sine(dur, Vector3(0.02, 0, 0), Vector3(0.02, -0.08, 0), 1, 0.0),
		ARM_L: _sine(dur, Vector3(0, 0, -0.1), Vector3(-0.55, 0, 0), 1, 0.0),
		ARM_R: _sine(dur, Vector3(0, 0, 0.1), Vector3(-0.55, 0, 0), 1, PI),
		ELBOW_L: _sine(dur, Vector3(0.3, 0, 0), Vector3(0.18, 0, 0), 1, 0.4),
		ELBOW_R: _sine(dur, Vector3(0.3, 0, 0), Vector3(0.18, 0, 0), 1, PI + 0.4),
		LEG_L: _sine(dur, Vector3(0, 0.12, 0), Vector3(0.6, 0, 0), 1, 0.0),
		LEG_R: _sine(dur, Vector3(0, -0.12, 0), Vector3(0.6, 0, 0), 1, PI),
	})


static func _sprint() -> Animation:
	# Leans too far forward, pumps hard.
	var dur: float = 0.5
	return _clip(dur, true, {
		HIPS + ":position": _sine(dur, Vector3(0, REST_HIPS_Y - 0.03, 0), Vector3(0, 0.05, 0), 2, 1.57),
		HIPS + ":rotation": _sine(dur, Vector3(-0.1, 0, 0), Vector3(0, 0.05, 0), 1, 0.0),
		TORSO: _sine(dur, Vector3(-0.3, 0, 0), Vector3(0.0, 0.2, 0.06), 1, 0.0),
		HEAD: _sine(dur, Vector3(0.28, 0, 0), Vector3(0.03, -0.12, 0), 1, 0.0),
		ARM_L: _sine(dur, Vector3(0, 0, -0.12), Vector3(-0.95, 0, 0), 1, 0.0),
		ARM_R: _sine(dur, Vector3(0, 0, 0.12), Vector3(-0.95, 0, 0), 1, PI),
		ELBOW_L: _sine(dur, Vector3(0.95, 0, 0), Vector3(0.25, 0, 0), 1, 0.3),
		ELBOW_R: _sine(dur, Vector3(0.95, 0, 0), Vector3(0.25, 0, 0), 1, PI + 0.3),
		LEG_L: _sine(dur, Vector3(0, 0.1, 0), Vector3(0.95, 0, 0), 1, 0.0),
		LEG_R: _sine(dur, Vector3(0, -0.1, 0), Vector3(0.95, 0, 0), 1, PI),
	})


static func _carry_idle() -> Animation:
	# Elbows bent, shoulders strained, leaning back against the weight.
	var dur: float = 2.2
	return _clip(dur, true, {
		HIPS + ":position": _sine(dur, Vector3(0, REST_HIPS_Y - 0.01, 0), Vector3(0, 0.01, 0), 2, 0.0),
		TORSO: _sine(dur, Vector3(0.12, 0, 0), Vector3(0.02, 0.0, 0.03), 1, 0.0),
		HEAD: _sine(dur, Vector3(-0.12, 0, 0), Vector3(0.02, 0.1, 0), 1, 0.5),
		ARM_L: _sine(dur, Vector3(1.05, 0, 0.28), Vector3(0.03, 0, 0), 2, 0.0),
		ARM_R: _sine(dur, Vector3(1.05, 0, -0.28), Vector3(0.03, 0, 0), 2, 1.0),
		ELBOW_L: _sine(dur, Vector3(0.7, 0, 0), Vector3(0.04, 0, 0), 2, 0.3),
		ELBOW_R: _sine(dur, Vector3(0.7, 0, 0), Vector3(0.04, 0, 0), 2, 1.3),
		LEG_L: _sine(dur, Vector3.ZERO, Vector3(0.015, 0, 0), 1, 0.0),
		LEG_R: _sine(dur, Vector3.ZERO, Vector3(0.015, 0, 0), 1, PI),
	})


static func _carry_walk() -> Animation:
	var dur: float = 0.9
	return _clip(dur, true, {
		HIPS + ":position": _sine(dur, Vector3(0, REST_HIPS_Y - 0.01, 0), Vector3(0, 0.03, 0), 2, 1.57),
		TORSO: _sine(dur, Vector3(0.15, 0, 0), Vector3(0.0, 0.08, 0.06), 1, 0.0),
		HEAD: _sine(dur, Vector3(-0.15, 0, 0), Vector3(0.02, -0.05, 0), 1, 0.0),
		ARM_L: _sine(dur, Vector3(1.05, 0, 0.28), Vector3(0.05, 0, 0), 2, 0.0),
		ARM_R: _sine(dur, Vector3(1.05, 0, -0.28), Vector3(0.05, 0, 0), 2, 0.5),
		ELBOW_L: _sine(dur, Vector3(0.7, 0, 0), Vector3(0.06, 0, 0), 2, 0.3),
		ELBOW_R: _sine(dur, Vector3(0.7, 0, 0), Vector3(0.06, 0, 0), 2, 0.8),
		LEG_L: _sine(dur, Vector3(0, 0.1, 0), Vector3(0.45, 0, 0), 1, 0.0),
		LEG_R: _sine(dur, Vector3(0, -0.1, 0), Vector3(0.45, 0, 0), 1, PI),
	})


static func _interact() -> Animation:
	# A purposeful jab, then a tiny recoil.
	return _clip(0.5, false, {
		TORSO: [[0.0, Vector3.ZERO], [0.12, Vector3(-0.1, 0.1, 0)], [0.22, Vector3(-0.16, 0.14, 0)], [0.36, Vector3(-0.04, 0.04, 0)], [0.5, Vector3.ZERO]],
		ARM_R: [[0.0, Vector3(0, 0, 0.08)], [0.12, Vector3(1.35, 0, 0.1)], [0.22, Vector3(1.5, 0, 0.1)], [0.36, Vector3(0.7, 0, 0.08)], [0.5, Vector3(0, 0, 0.08)]],
		ELBOW_R: [[0.0, Vector3(0.15, 0, 0)], [0.12, Vector3(0.3, 0, 0)], [0.22, Vector3(0.05, 0, 0)], [0.36, Vector3(0.5, 0, 0)], [0.5, Vector3(0.15, 0, 0)]],
		HEAD: [[0.0, Vector3(0.02, 0, 0)], [0.2, Vector3(0.12, 0, 0)], [0.5, Vector3(0.02, 0, 0)]],
	})


static func _clean() -> Animation:
	# Overcommitted scrubbing with a visible rhythm, knees bent into it.
	var dur: float = 1.0
	return _clip(dur, true, {
		HIPS + ":position": _sine(dur, Vector3(0, REST_HIPS_Y - 0.05, 0), Vector3(0, 0.012, 0), 2, 0.0),
		TORSO: _sine(dur, Vector3(-0.28, 0, 0), Vector3(0.04, 0.22, 0), 2, 0.0),
		HEAD: _sine(dur, Vector3(0.3, 0, 0), Vector3(0.02, -0.1, 0), 2, 0.0),
		ARM_R: _sine(dur, Vector3(0.95, 0, 0.1), Vector3(0.1, 0.5, 0), 2, 0.0),
		ELBOW_R: _sine(dur, Vector3(0.8, 0, 0), Vector3(0.12, 0, 0), 2, 1.57),
		ARM_L: _sine(dur, Vector3(0.5, 0, -0.15), Vector3(0.03, 0, 0), 1, 0.0),
		ELBOW_L: _sine(dur, Vector3(0.6, 0, 0), Vector3(0.03, 0, 0), 1, 0.0),
		LEG_L: _sine(dur, Vector3(0.05, 0, 0), Vector3(0.0, 0, 0), 1, 0.0),
		LEG_R: _sine(dur, Vector3(-0.05, 0, 0), Vector3(0.0, 0, 0), 1, 0.0),
	})


static func _stumble() -> Animation:
	# A recoverable foot slip. Short enough that nobody blames it for a missed grab.
	return _clip(0.4, false, {
		HIPS + ":position": [[0.0, Vector3(0, REST_HIPS_Y, 0)], [0.1, Vector3(0, 0.42, 0.03)], [0.24, Vector3(0, 0.47, -0.02)], [0.4, Vector3(0, REST_HIPS_Y, 0)]],
		HIPS + ":rotation": [[0.0, Vector3.ZERO], [0.1, Vector3(-0.35, 0.1, 0)], [0.24, Vector3(0.2, -0.05, 0)], [0.4, Vector3.ZERO]],
		TORSO: [[0.0, Vector3.ZERO], [0.1, Vector3(-0.4, 0.15, 0)], [0.24, Vector3(0.18, -0.1, 0)], [0.4, Vector3.ZERO]],
		HEAD: [[0.0, Vector3(0.02, 0, 0)], [0.12, Vector3(0.3, 0.1, 0)], [0.26, Vector3(-0.1, 0, 0)], [0.4, Vector3(0.02, 0, 0)]],
		ARM_L: [[0.0, Vector3(0, 0, -0.08)], [0.1, Vector3(1.5, 0, -0.9)], [0.24, Vector3(1.1, 0, -0.6)], [0.4, Vector3(0, 0, -0.08)]],
		ARM_R: [[0.0, Vector3(0, 0, 0.08)], [0.1, Vector3(1.3, 0, 0.9)], [0.24, Vector3(0.9, 0, 0.6)], [0.4, Vector3(0, 0, 0.08)]],
		LEG_L: [[0.0, Vector3.ZERO], [0.1, Vector3(0.8, 0, 0)], [0.24, Vector3(-0.2, 0, 0)], [0.4, Vector3.ZERO]],
		LEG_R: [[0.0, Vector3.ZERO], [0.1, Vector3(-0.4, 0, 0)], [0.24, Vector3(0.3, 0, 0)], [0.4, Vector3.ZERO]],
	})


static func _celebrate() -> Animation:
	# "It is all under control": a thumbs-up and a reassuring nod.
	return _clip(1.2, false, {
		TORSO: [[0.0, Vector3.ZERO], [0.3, Vector3(0.06, 0.15, 0)], [0.9, Vector3(0.06, 0.15, 0)], [1.2, Vector3.ZERO]],
		HEAD: [[0.0, Vector3(0.02, 0, 0)], [0.3, Vector3(-0.1, 0, 0)], [0.5, Vector3(0.18, 0, 0)], [0.7, Vector3(-0.08, 0, 0)], [0.9, Vector3(0.12, 0, 0)], [1.2, Vector3(0.02, 0, 0)]],
		ARM_R: [[0.0, Vector3(0, 0, 0.08)], [0.3, Vector3(2.5, 0, 0.2)], [0.9, Vector3(2.5, 0, 0.2)], [1.2, Vector3(0, 0, 0.08)]],
		ELBOW_R: [[0.0, Vector3(0.15, 0, 0)], [0.3, Vector3(0.45, 0, 0)], [0.9, Vector3(0.45, 0, 0)], [1.2, Vector3(0.15, 0, 0)]],
	})


# Helpers -----------------------------------------------------------------------

## base + amp * sin(...) sampled around one loop. freq must be a whole number so
## the last key equals the first and the loop has no seam.
static func _sine(length: float, base: Vector3, amp: Vector3, freq: int, phase: float) -> Array:
	var keys: Array = []
	for i: int in STEPS + 1:
		var t: float = length * float(i) / float(STEPS)
		var s: float = sin(TAU * float(freq) * float(i) / float(STEPS) + phase)
		keys.append([t, base + amp * s])
	# Make the loop seam exact despite floating point.
	keys[STEPS][1] = keys[0][1]
	return keys


## spec maps "Joint/Path" or "Joint/Path:position" to keys [[time, Vector3], ...].
## Any joint not in spec is keyed constantly at rest.
static func _clip(length: float, loop: bool, spec: Dictionary) -> Animation:
	var anim := Animation.new()
	anim.length = length
	anim.loop_mode = Animation.LOOP_LINEAR if loop else Animation.LOOP_NONE
	for joint: Dictionary in joints():
		var prop: String = str(joint["prop"])
		var key: String = str(joint["path"]) if prop == "rotation" else "%s:%s" % [joint["path"], prop]
		var fallback: Array = [[0.0, joint["rest"]], [length, joint["rest"]]]
		var keys: Array = spec.get(key, spec.get("%s:%s" % [joint["path"], prop], fallback))
		var idx: int = anim.add_track(Animation.TYPE_VALUE)
		anim.track_set_path(idx, NodePath("%s:%s" % [joint["path"], prop]))
		anim.track_set_interpolation_type(idx, Animation.INTERPOLATION_CUBIC if loop else Animation.INTERPOLATION_LINEAR)
		anim.value_track_set_update_mode(idx, Animation.UPDATE_CONTINUOUS)
		for item: Array in keys:
			anim.track_insert_key(idx, float(item[0]), item[1])
	return anim
