class_name WorkerPresenter
extends Node3D
## The presentation boundary for a worker's body. The player, the animation
## contracts and the networking only ever talk to this class: update_motion,
## trigger, stop_action, current_state, set_shadow_only, freeze_pose.
##
## Two implementations sit behind it:
##   GlbWorker     an authored, rigged .glb model (the real art; see
##                 docs/assets/WORKER_GLB_CONTRACT.md)
##   WorkerAvatar  a code-built fallback mannequin, kept as a debug and test
##                 fixture, never the shipped look
## WorkerFactory picks one.
##
## This class owns the AnimationTree state machine (idle, walk, sprint, carry,
## interact, clean, stumble, celebrate) so both implementations behave
## identically. It is presentation only: nothing here moves the player's
## collision shape, eye, hold point or any gameplay state.

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
var _smoothed_velocity: Vector3 = Vector3.ZERO
var _accel: Vector3 = Vector3.ZERO
var _pending_action: StringName = &""
var _pending_left: float = 0.0


## Call before add_child.
func setup(slot_index: int) -> void:
	slot = slot_index
	costume = WorkerCostumes.for_slot(slot_index)


func _ready() -> void:
	if costume.is_empty():
		costume = WorkerCostumes.for_slot(slot)
	_build_visuals()
	_player = _make_player()
	_build_state_machine()


# What a body implementation provides --------------------------------------------

## Build the visible body. Called once, from _ready.
func _build_visuals() -> void:
	pass


## The AnimationPlayer that holds the nine clips of WorkerAnimations. The
## default builds one from the code-authored clips; a model returns its own.
func _make_player() -> AnimationPlayer:
	var anim_player := AnimationPlayer.new()
	anim_player.name = "AnimationPlayer"
	add_child(anim_player)
	anim_player.add_animation_library(&"", WorkerAnimations.build_library())
	return anim_player


## Loose parts that lag behind motion (tie, lanyard). Empty when there are none.
func sway_nodes() -> Array[Node3D]:
	return []


# Public API ---------------------------------------------------------------------

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
	for node: Node in find_children("*", "GeometryInstance3D", true, false):
		(node as GeometryInstance3D).cast_shadow = mode as GeometryInstance3D.ShadowCastingSetting


## Hold one clip at one moment, for screenshots and tests.
func freeze_pose(clip: StringName, time: float) -> void:
	_tree.active = false
	_player.play(clip)
	_player.seek(time, true)
	_player.pause()


## True when the body has meshes a player could see.
func has_visible_geometry() -> bool:
	return not find_children("*", "MeshInstance3D", true, false).is_empty()


# State machine ------------------------------------------------------------------

func _travel(state: StringName) -> void:
	_ensure_playback()
	if _playback != null:
		_playback.travel(state)


func _ensure_playback() -> void:
	if _playback == null and _tree != null:
		_playback = _tree.get("parameters/playback") as AnimationNodeStateMachinePlayback


func _build_state_machine() -> void:
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
