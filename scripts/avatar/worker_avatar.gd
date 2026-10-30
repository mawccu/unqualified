class_name WorkerAvatar
extends Node3D
## A chunky, toy-like event worker built from original primitives (PLACEHOLDER
## art, see docs/assets/ASSET_REGISTER.md): a head about a third of the height,
## mitten hands, short legs, oversized shoes. One shared body; the costume
## (colour blocking and accessories) comes from WorkerCostumes by player slot.
##
## Animation: an AnimationPlayer holds the clips in WorkerAnimations, and an
## AnimationTree runs a state machine over them (idle, walk, sprint, carry,
## interact, clean, stumble, celebrate). On top of that a few spring-driven
## "sway" nodes lag behind motion for a head, a tie, a lanyard and an apron.
##
## This is presentation only. Nothing here moves the player's collision, eye
## or interaction origin, and nothing here changes gameplay state. A real rigged
## model can replace the primitives later and keep the same joint names, clips
## and state machine.

const BODY_SCALE: float = 1.1
const SPRINT_SPEED: float = 4.8
const MOVE_THRESHOLD: float = 0.25
const FADE_LOCOMOTION: float = 0.15
const FADE_ACTION: float = 0.08
const PENDING_SECONDS: float = 0.15

const LOCOMOTION: Array[StringName] = [
	WorkerAnimations.IDLE, WorkerAnimations.WALK, WorkerAnimations.SPRINT,
	WorkerAnimations.CARRY_IDLE, WorkerAnimations.CARRY_WALK,
]
const ACTIONS: Array[StringName] = [
	WorkerAnimations.INTERACT, WorkerAnimations.CLEAN, WorkerAnimations.STUMBLE, WorkerAnimations.CELEBRATE,
]

var slot: int = 0
var costume: Dictionary = {}

var _player: AnimationPlayer
var _tree: AnimationTree
var _playback: AnimationNodeStateMachinePlayback

var _head_sway: Node3D
var _tie_sway: Node3D
var _lanyard_sway: Node3D
var _apron_sway: Node3D
var _head_spring := SecondarySpring.new(120.0, 6.5, 0.32)
var _tie_spring := SecondarySpring.new(95.0, 4.5, 0.7)
var _lanyard_spring := SecondarySpring.new(85.0, 4.0, 0.7)
var _apron_spring := SecondarySpring.new(80.0, 5.0, 0.5)
var _smoothed_velocity: Vector3 = Vector3.ZERO
var _pending_action: StringName = &""
var _pending_left: float = 0.0
var _accel: Vector3 = Vector3.ZERO


## Call before add_child.
func setup(slot_index: int) -> void:
	slot = slot_index
	costume = WorkerCostumes.for_slot(slot_index)


func _ready() -> void:
	if costume.is_empty():
		costume = WorkerCostumes.for_slot(slot)
	_build_body()
	_build_animation()


func _process(delta: float) -> void:
	# Lag behind acceleration, then settle. Underdamped springs overshoot once.
	var local_accel: Vector3 = global_transform.basis.inverse() * _accel
	_head_spring.step(Vector3(clampf(-local_accel.z * 0.02, -0.3, 0.3), 0.0, clampf(-local_accel.x * 0.02, -0.3, 0.3)), delta)
	var loose := Vector3(clampf(local_accel.z * 0.05, -0.6, 0.6), 0.0, clampf(-local_accel.x * 0.05, -0.6, 0.6))
	_tie_spring.step(loose, delta)
	_lanyard_spring.step(loose, delta)
	_apron_spring.step(loose * 0.5, delta)
	_head_sway.rotation = _head_spring.value
	_tie_sway.rotation = _tie_spring.value
	_lanyard_sway.rotation = _lanyard_spring.value
	_apron_sway.rotation = _apron_spring.value


## Drive the locomotion states. Call every physics frame. world_velocity is the
## body's velocity; remote players derive it from how their position changes.
func update_motion(world_velocity: Vector3, sprinting: bool, carrying: bool, delta: float) -> void:
	var flat := Vector3(world_velocity.x, 0.0, world_velocity.z)
	var blend: float = 1.0 - exp(-12.0 * maxf(delta, 0.0001))
	var previous: Vector3 = _smoothed_velocity
	_smoothed_velocity = _smoothed_velocity.lerp(flat, blend)
	_accel = ((_smoothed_velocity - previous) / maxf(delta, 0.0001)).limit_length(30.0)

	var speed: float = flat.length()
	var moving: bool = speed > MOVE_THRESHOLD
	var current: StringName = current_state()
	# An action was just requested but the tree has not started it yet. Do not
	# let the locomotion choice below overwrite it in the same moment.
	if _pending_action != &"":
		_pending_left -= delta
		if current == _pending_action or _pending_left <= 0.0:
			_pending_action = &""
		else:
			return
	if ACTIONS.has(current):
		if current == WorkerAnimations.CLEAN and moving:
			stop_action()
		return
	var wanted: StringName = WorkerAnimations.IDLE
	if carrying:
		wanted = WorkerAnimations.CARRY_WALK if moving else WorkerAnimations.CARRY_IDLE
	elif moving:
		wanted = WorkerAnimations.SPRINT if (sprinting and speed > SPRINT_SPEED) else WorkerAnimations.WALK
	if wanted != current:
		_travel(wanted)


## Play a short action: interact, clean, stumble or celebrate. Stumble may
## interrupt anything; the others do not interrupt a stumble.
func trigger(action: StringName) -> void:
	if not ACTIONS.has(action):
		return
	var current: StringName = current_state()
	if (current == WorkerAnimations.STUMBLE or _pending_action == WorkerAnimations.STUMBLE) and action != WorkerAnimations.STUMBLE:
		return
	_travel(action)
	_pending_action = action
	_pending_left = PENDING_SECONDS


## Leave a looping action (clean) and go back to idle.
func stop_action() -> void:
	if ACTIONS.has(current_state()):
		_travel(WorkerAnimations.IDLE)


func current_state() -> StringName:
	_ensure_playback()
	return _playback.get_current_node() if _playback != null else &""


## The body stays out of first person view but still throws its shadow.
func set_shadow_only(on: bool) -> void:
	var mode: int = GeometryInstance3D.SHADOW_CASTING_SETTING_SHADOWS_ONLY if on else GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	for node: Node in find_children("*", "MeshInstance3D", true, false):
		(node as MeshInstance3D).cast_shadow = mode as GeometryInstance3D.ShadowCastingSetting


## Hold one clip at one moment, for screenshots and tests.
func freeze_pose(clip: StringName, time: float) -> void:
	_tree.active = false
	_player.play(clip)
	_player.seek(time, true)
	_player.pause()


func sway_nodes() -> Array[Node3D]:
	return [_head_sway, _tie_sway, _lanyard_sway, _apron_sway]


func _travel(state: StringName) -> void:
	_ensure_playback()
	if _playback != null:
		_playback.travel(state)


func _ensure_playback() -> void:
	if _playback == null and _tree != null:
		_playback = _tree.get("parameters/playback") as AnimationNodeStateMachinePlayback


# Animation setup ---------------------------------------------------------------

func _build_animation() -> void:
	_player = AnimationPlayer.new()
	_player.name = "AnimationPlayer"
	add_child(_player)
	_player.add_animation_library(&"", WorkerAnimations.build_library())

	var machine := AnimationNodeStateMachine.new()
	var column: float = 0.0
	for state: StringName in LOCOMOTION + ACTIONS:
		var node := AnimationNodeAnimation.new()
		node.animation = state
		machine.add_node(state, node, Vector2(column * 180.0, 0.0 if LOCOMOTION.has(state) else 160.0))
		column += 1.0
	# Locomotion states flow into each other.
	for a: StringName in LOCOMOTION:
		for b: StringName in LOCOMOTION:
			if a != b:
				machine.add_transition(a, b, _transition(FADE_LOCOMOTION))
	# Any state can start an action, and an action can interrupt another.
	for a: StringName in LOCOMOTION + ACTIONS:
		for b: StringName in ACTIONS:
			if a != b:
				machine.add_transition(a, b, _transition(FADE_ACTION))
	# One-shot actions return to idle by themselves; clean loops until stopped.
	for a: StringName in ACTIONS:
		var back := _transition(FADE_LOCOMOTION)
		if WorkerAnimations.ONE_SHOT.has(a):
			back.switch_mode = AnimationNodeStateMachineTransition.SWITCH_MODE_AT_END
			back.advance_mode = AnimationNodeStateMachineTransition.ADVANCE_MODE_AUTO
		machine.add_transition(a, WorkerAnimations.IDLE, back)

	_tree = AnimationTree.new()
	_tree.name = "AnimationTree"
	_tree.tree_root = machine
	add_child(_tree)
	_tree.anim_player = _tree.get_path_to(_player)
	_tree.active = true
	_ensure_playback()
	if _playback != null:
		_playback.start(WorkerAnimations.IDLE)


func _transition(fade: float) -> AnimationNodeStateMachineTransition:
	var t := AnimationNodeStateMachineTransition.new()
	t.xfade_time = fade
	return t


# Body --------------------------------------------------------------------------

func _build_body() -> void:
	var body := Node3D.new()
	body.name = "Body"
	body.scale = Vector3.ONE * BODY_SCALE
	add_child(body)
	var hips := Node3D.new()
	hips.name = "Hips"
	hips.position = Vector3(0.0, WorkerAnimations.REST_HIPS_Y, 0.0)
	body.add_child(hips)

	var trousers: Color = costume["trousers"]
	var skin: Color = costume["skin"]
	for side: int in 2:
		var sx: float = -1.0 if side == 0 else 1.0
		var leg := Node3D.new()
		leg.name = "LegL" if side == 0 else "LegR"
		leg.position = Vector3(sx * 0.13, 0.0, 0.0)
		hips.add_child(leg)
		_part(leg, _capsule(0.09, 0.4), trousers, Vector3(0.0, -0.2, 0.0))
		# Oversized non-slip shoe.
		_part(leg, _capsule(0.115, 0.4), costume["shoes"], Vector3(0.0, -0.44, -0.07), Vector3(90.0, 0.0, 0.0))

	var torso := Node3D.new()
	torso.name = "Torso"
	torso.position = Vector3(0.0, 0.02, 0.0)
	hips.add_child(torso)
	# A pear-shaped shirt with a vest or blazer front and back.
	_part(torso, _capsule(0.22, 0.58), costume["shirt"], Vector3(0.0, 0.3, 0.0))
	_part(torso, _box(Vector3(0.42, 0.4, 0.06)), costume["vest"], Vector3(0.0, 0.34, -0.19))
	_part(torso, _box(Vector3(0.42, 0.4, 0.05)), costume["vest"], Vector3(0.0, 0.34, 0.19))
	_part(torso, _box(Vector3(0.05, 0.4, 0.3)), costume["vest"], Vector3(-0.21, 0.34, 0.0))
	_part(torso, _box(Vector3(0.05, 0.4, 0.3)), costume["vest"], Vector3(0.21, 0.34, 0.0))

	for side: int in 2:
		var sx2: float = -1.0 if side == 0 else 1.0
		_build_arm(torso, side, sx2, skin)

	var head_pivot := Node3D.new()
	head_pivot.name = "HeadPivot"
	head_pivot.position = Vector3(0.0, 0.62, 0.0)
	torso.add_child(head_pivot)
	_head_sway = Node3D.new()
	_head_sway.name = "HeadSway"
	head_pivot.add_child(_head_sway)
	_build_head(_head_sway, skin)

	_tie_sway = Node3D.new()
	_tie_sway.name = "TieSway"
	_tie_sway.position = Vector3(0.0, 0.52, -0.22)
	torso.add_child(_tie_sway)
	_lanyard_sway = Node3D.new()
	_lanyard_sway.name = "LanyardSway"
	_lanyard_sway.position = Vector3(0.0, 0.56, -0.23)
	torso.add_child(_lanyard_sway)
	_apron_sway = Node3D.new()
	_apron_sway.name = "ApronSway"
	_apron_sway.position = Vector3(0.0, 0.34, -0.22)
	torso.add_child(_apron_sway)

	for accessory: StringName in costume["accessories"]:
		_add_accessory(accessory, hips, torso)


func _build_arm(torso: Node3D, side: int, sx: float, skin: Color) -> void:
	var arm := Node3D.new()
	arm.name = "ArmL" if side == 0 else "ArmR"
	arm.position = Vector3(sx * 0.28, 0.5, 0.0)
	torso.add_child(arm)
	_part(arm, _capsule(0.08, 0.32), costume["shirt"], Vector3(0.0, -0.15, 0.0))
	var elbow := Node3D.new()
	elbow.name = "ElbowL" if side == 0 else "ElbowR"
	elbow.position = Vector3(0.0, -0.28, 0.0)
	arm.add_child(elbow)
	_part(elbow, _capsule(0.075, 0.3), costume["shirt"], Vector3(0.0, -0.13, 0.0))
	var hand := Node3D.new()
	hand.name = "Hand"
	hand.position = Vector3(0.0, -0.27, 0.0)
	elbow.add_child(hand)
	# A chunky mitten with one thumb wedge.
	_part(hand, _sphere(0.115), skin, Vector3(0.0, -0.06, 0.0), Vector3.ZERO, Vector3(1.0, 1.1, 0.85))
	_part(hand, _sphere(0.05), skin, Vector3(-sx * 0.1, -0.03, -0.05))


func _build_head(parent: Node3D, skin: Color) -> void:
	_part(parent, _sphere(0.27), skin, Vector3(0.0, 0.22, 0.0))
	for side: float in [-1.0, 1.0]:
		_part(parent, _sphere(0.065), Color(1, 1, 1), Vector3(side * 0.1, 0.27, -0.225))
		_part(parent, _sphere(0.032), Color(0.05, 0.05, 0.08), Vector3(side * 0.1, 0.27, -0.28))
		_part(parent, _box(Vector3(0.13, 0.03, 0.03)), costume["hair"], Vector3(side * 0.1, 0.37, -0.235), Vector3(0.0, 0.0, -side * 12.0))
	_part(parent, _sphere(0.04), skin.darkened(0.08), Vector3(0.0, 0.2, -0.265))
	_part(parent, _box(Vector3(0.12, 0.03, 0.03)), Color(0.45, 0.12, 0.14), Vector3(0.0, 0.1, -0.255))
	_build_hair(parent)


func _build_hair(parent: Node3D) -> void:
	var hair: Color = costume["hair"]
	match costume["hair_style"]:
		&"short":
			_part(parent, _sphere(0.285), hair, Vector3(0.0, 0.27, 0.03), Vector3.ZERO, Vector3(1.0, 0.72, 1.0))
		&"pompadour":
			_part(parent, _sphere(0.285), hair, Vector3(0.0, 0.27, 0.03), Vector3.ZERO, Vector3(1.0, 0.72, 1.0))
			_part(parent, _capsule(0.13, 0.36), hair, Vector3(0.0, 0.52, -0.06), Vector3(75.0, 0.0, 0.0))
		&"ponytail":
			_part(parent, _sphere(0.285), hair, Vector3(0.0, 0.27, 0.03), Vector3.ZERO, Vector3(1.0, 0.72, 1.0))
			_part(parent, _capsule(0.07, 0.36), hair, Vector3(0.0, 0.18, 0.3), Vector3(25.0, 0.0, 0.0))
		&"beanie":
			_part(parent, _sphere(0.3), costume["accent"], Vector3(0.0, 0.3, 0.02), Vector3.ZERO, Vector3(1.0, 0.8, 1.0))
			_part(parent, _sphere(0.07), Color(0.97, 0.97, 0.97), Vector3(0.0, 0.54, 0.02))


func _add_accessory(accessory: StringName, hips: Node3D, torso: Node3D) -> void:
	var accent: Color = costume["accent"]
	match accessory:
		&"bow_tie":
			_part(_tie_sway, _box(Vector3(0.09, 0.07, 0.04)), accent, Vector3(-0.07, 0.0, 0.0), Vector3(0, 0, -15))
			_part(_tie_sway, _box(Vector3(0.09, 0.07, 0.04)), accent, Vector3(0.07, 0.0, 0.0), Vector3(0, 0, 15))
			_part(_tie_sway, _box(Vector3(0.05, 0.05, 0.05)), accent.darkened(0.3), Vector3.ZERO)
		&"lanyard":
			_part(_lanyard_sway, _box(Vector3(0.035, 0.42, 0.015)), accent, Vector3(0.0, -0.2, 0.0))
			_part(_lanyard_sway, _box(Vector3(0.16, 0.2, 0.02)), Color(0.98, 0.98, 0.95), Vector3(0.0, -0.46, -0.01))
			_part(_lanyard_sway, _box(Vector3(0.11, 0.04, 0.025)), accent, Vector3(0.0, -0.42, -0.015))
		&"apron":
			_part(_apron_sway, _box(Vector3(0.44, 0.4, 0.03)), accent, Vector3(0.0, -0.2, 0.0))
			_part(_apron_sway, _box(Vector3(0.16, 0.12, 0.03)), accent.darkened(0.2), Vector3(0.0, -0.22, -0.02))
		&"headset":
			var head_root: Node3D = _head_sway
			_part(head_root, _box(Vector3(0.6, 0.04, 0.05)), Color(0.1, 0.1, 0.12), Vector3(0.0, 0.5, 0.0))
			for side: float in [-1.0, 1.0]:
				_part(head_root, _sphere(0.08), Color(0.12, 0.12, 0.15), Vector3(side * 0.28, 0.24, 0.0), Vector3.ZERO, Vector3(0.5, 1.0, 1.0))
			_part(head_root, _capsule(0.012, 0.2), Color(0.12, 0.12, 0.15), Vector3(-0.22, 0.12, -0.12), Vector3(60.0, 0.0, 30.0))
			_part(head_root, _sphere(0.03), accent, Vector3(-0.15, 0.05, -0.2))
		&"radio":
			_part(hips, _box(Vector3(0.1, 0.17, 0.05)), Color(0.12, 0.12, 0.15), Vector3(-0.27, 0.14, 0.05))
			_part(hips, _capsule(0.012, 0.18), Color(0.12, 0.12, 0.15), Vector3(-0.27, 0.3, 0.05))
			_part(hips, _sphere(0.02), Color(0.2, 0.9, 0.3), Vector3(-0.27, 0.19, -0.02))
		&"belt":
			_part(hips, _box(Vector3(0.5, 0.07, 0.4)), Color(0.35, 0.22, 0.12), Vector3(0.0, 0.07, 0.0))
			for px: float in [-0.2, 0.2]:
				_part(hips, _box(Vector3(0.1, 0.13, 0.08)), Color(0.45, 0.3, 0.16), Vector3(px, 0.05, -0.22))
		&"one_glove":
			var glove: Node3D = torso.get_node_or_null("ArmL/ElbowL/Hand") as Node3D
			if glove != null:
				_part(glove, _sphere(0.13), Color(0.97, 0.97, 0.97), Vector3(0.0, -0.06, 0.0), Vector3.ZERO, Vector3(1.0, 1.1, 0.9))


# Primitive helpers -------------------------------------------------------------

func _part(parent: Node3D, mesh: Mesh, color: Color, pos: Vector3, rot_degrees: Vector3 = Vector3.ZERO,
		scl: Vector3 = Vector3.ONE) -> MeshInstance3D:
	var instance := MeshInstance3D.new()
	(mesh as PrimitiveMesh).material = VenueBuilder.material(color, 0.7)
	instance.mesh = mesh
	instance.position = pos
	instance.rotation_degrees = rot_degrees
	instance.scale = scl
	parent.add_child(instance)
	return instance


func _capsule(radius: float, height: float) -> CapsuleMesh:
	var m := CapsuleMesh.new()
	m.radius = radius
	m.height = maxf(height, radius * 2.0)
	return m


func _sphere(radius: float) -> SphereMesh:
	var m := SphereMesh.new()
	m.radius = radius
	m.height = radius * 2.0
	return m


func _box(size: Vector3) -> BoxMesh:
	var m := BoxMesh.new()
	m.size = size
	return m
