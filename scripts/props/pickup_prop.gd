class_name PickupProp
extends RigidBody3D
## A carryable object. Only InteractionService changes its ownership state.
## While held it is frozen and follows the holder's hold point; when released
## it becomes a normal rigid body again.
##
## On a client (mirror = true) the prop never simulates: it only displays what
## the host last said, smoothing toward the latest snapshot.
##
## Every transform change happens inside _physics_process. A frozen body's
## transform set between physics steps is overwritten by the physics server.

signal state_changed(prop: PickupProp)

const FALL_LIMIT: float = -10.0
const MIRROR_SMOOTHING: float = 18.0
const HELD_YAW_SMOOTHING: float = 14.0
const HELD_MAX_LAG: float = 0.4

@export var entity_id: StringName = &"prop"
@export var kind: StringName = &"cake_section"
@export var display_name: String = "Prop"
## Half the prop's height: how far its centre sits above a surface it rests on.
@export var half_height: float = 0.1
## The visible shape, so a socket can show a ghost of exactly what you hold.
@export var visual_mesh: Mesh
## Where that mesh sits relative to the prop body (models from the asset pack
## have their origin at the bottom, not the middle).
var visual_transform: Transform3D = Transform3D.IDENTITY

## Bumped on every ownership change so stale requests can be rejected.
var revision: int = 0
var holder_id: StringName = &""
var socket_id: StringName = &""
var mirror: bool = false

var _start_transform: Transform3D
var _hold_target: Node3D
var _held_yaw: float = 0.0
var _held_tilt := SecondarySpring.new(110.0, 7.0, 0.25)
var _last_hold_position: Vector3 = Vector3.ZERO
var _snap_target: Transform3D
var _net_target: Transform3D
var _has_net_target: bool = false
var _reset_pending: bool = false
var _layer: int
var _mask: int


func _ready() -> void:
	_start_transform = global_transform
	_layer = collision_layer
	_mask = collision_mask
	continuous_cd = true


func _physics_process(delta: float) -> void:
	if _hold_target != null:
		# The position follows the holder exactly, so range checks and aiming never
		# change. Only the look lags: the yaw trails a turn by a little (never more
		# than HELD_MAX_LAG) and the prop tilts slightly with the holder's motion.
		var target_pos: Vector3 = _hold_target.global_position
		var yaw: float = _hold_target.global_rotation.y
		var lag: float = wrapf(yaw - _held_yaw, -PI, PI)
		lag *= 1.0 - exp(-HELD_YAW_SMOOTHING * delta)
		_held_yaw = yaw - clampf(wrapf(yaw - (_held_yaw + lag), -PI, PI), -HELD_MAX_LAG, HELD_MAX_LAG)
		var velocity_seen: Vector3 = (target_pos - _last_hold_position) / maxf(delta, 0.0001)
		_last_hold_position = target_pos
		var local_velocity: Vector3 = Basis(Vector3.UP, yaw).inverse() * velocity_seen
		var tilt: Vector3 = _held_tilt.step(Vector3(clampf(local_velocity.z * 0.03, -0.2, 0.2), 0.0, clampf(-local_velocity.x * 0.04, -0.2, 0.2)), delta)
		global_transform = Transform3D(Basis(Vector3.UP, _held_yaw) * Basis.from_euler(tilt), target_pos)
	elif _reset_pending:
		global_transform = _start_transform
		linear_velocity = Vector3.ZERO
		angular_velocity = Vector3.ZERO
		freeze = false
		sleeping = false
		_reset_pending = false
	elif socket_id != &"":
		global_transform = _snap_target
	elif mirror:
		if _has_net_target:
			global_transform = global_transform.interpolate_with(_net_target, 1.0 - exp(-MIRROR_SMOOTHING * delta))
	elif holder_id == &"" and global_position.y < FALL_LIMIT:
		reset_to_start()


## Where this prop should be, as other peers must see it.
func net_pose() -> Transform3D:
	if _reset_pending:
		return _start_transform
	if socket_id != &"":
		return _snap_target
	return global_transform


func set_mirror(on: bool) -> void:
	mirror = on
	_has_net_target = false
	_hold_target = null
	if on:
		freeze_mode = RigidBody3D.FREEZE_MODE_KINEMATIC
		freeze = true


func begin_hold(target: Node3D, holder: StringName) -> void:
	_hold_target = target
	_begin_follow()
	_reset_pending = false
	holder_id = holder
	socket_id = &""
	freeze_mode = RigidBody3D.FREEZE_MODE_KINEMATIC
	freeze = true
	collision_layer = 0
	collision_mask = 0
	revision += 1
	state_changed.emit(self)


func place_at(target: Transform3D, socket: StringName) -> void:
	_hold_target = null
	_reset_pending = false
	holder_id = &""
	socket_id = socket
	_snap_target = target
	collision_layer = _layer
	collision_mask = _mask
	freeze = true
	global_transform = target
	linear_velocity = Vector3.ZERO
	angular_velocity = Vector3.ZERO
	revision += 1
	state_changed.emit(self)


func release(velocity: Vector3) -> void:
	_hold_target = null
	_reset_pending = false
	holder_id = &""
	collision_layer = _layer
	collision_mask = _mask
	freeze = false
	linear_velocity = velocity
	angular_velocity = Vector3.ZERO
	revision += 1
	state_changed.emit(self)


func reset_to_start() -> void:
	_hold_target = null
	holder_id = &""
	socket_id = &""
	collision_layer = _layer
	collision_mask = _mask
	# Freeze now, move on the next physics step (see _physics_process).
	freeze_mode = RigidBody3D.FREEZE_MODE_KINEMATIC
	freeze = true
	_reset_pending = true
	revision += 1
	state_changed.emit(self)


## Client side: adopt the host's ownership state.
func apply_net_state(holder: StringName, socket: StringName, rev: int, pose: Transform3D, hold_target: Node3D) -> void:
	holder_id = holder
	socket_id = socket
	revision = rev
	_reset_pending = false
	freeze_mode = RigidBody3D.FREEZE_MODE_KINEMATIC
	freeze = true
	if holder != &"":
		_hold_target = hold_target
		_begin_follow()
		collision_layer = 0
		collision_mask = 0
		return
	_hold_target = null
	collision_layer = _layer
	collision_mask = _mask
	if socket != &"":
		_snap_target = pose
	else:
		_net_target = pose
		_has_net_target = true


## Client side: a position update for a free prop. Ignored if it is older than
## the ownership state already applied.
func apply_net_snapshot(rev: int, pose: Transform3D) -> void:
	if holder_id != &"" or socket_id != &"" or rev < revision:
		return
	_net_target = pose
	_has_net_target = true


## Start of a carry: no lag left over from a previous one.
func _begin_follow() -> void:
	if _hold_target == null:
		return
	_held_yaw = _hold_target.global_rotation.y
	_last_hold_position = _hold_target.global_position
	_held_tilt.reset()
