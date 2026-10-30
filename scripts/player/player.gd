class_name Player
extends CharacterBody3D
## One player in the room. The local player reads input, shows what the
## crosshair can do, and sends every world change through submit_request. A
## remote player is a body that smoothly follows poses sent over the network.

signal prompt_changed(text: String)
signal feedback(text: String)

const WALK_SPEED: float = 4.0
const SPRINT_SPEED: float = 6.0
const ACCELERATION: float = 12.0
const GRAVITY: float = 20.0
const EYE_HEIGHT: float = 1.55
const AIM_REACH: float = 3.2
const HOLD_DISTANCE: float = 1.2
const HOLD_MIN_DISTANCE: float = 0.8
const HOLD_OFFSET: Vector2 = Vector2(0.38, -0.4)
const BASE_LOOK: float = 0.0022
const MAX_PITCH: float = 1.45
const REMOTE_SMOOTHING: float = 16.0

const REASON_TEXT: Dictionary = {
	&"hands_full": "Your hands are full.",
	&"nothing_held": "You are not holding anything.",
	&"already_held": "Someone else has that.",
	&"out_of_range": "Too far away.",
	&"stale_revision": "It just changed. Try again.",
	&"socket_taken": "Something is already there.",
	&"socket_rejects": "That does not go there.",
	&"socket_locked": "Put the layer below it first.",
	&"no_target": "Nothing to use there.",
}

@export var actor_id: StringName = &"peer_1"

var peer_id: int = 1
var slot: int = 0
var is_local: bool = true
var held_prop_id: StringName = &""
var controls_enabled: bool = true
var service: InteractionService
## Set by Session on the local player: sends a request dictionary onward.
var submit_request: Callable
var hold_point: Node3D

var _head: Node3D
var _camera: Camera3D
var _ray: RayCast3D
var _pitch: float = 0.0
var _spawn: Transform3D
var _request_counter: int = 0
var _last_prompt: String = ""
var _previewed_socket: PlacementSocket
var _previewed_prop: PickupProp
var _net_pos: Vector3
var _net_yaw: float = 0.0
var _net_pitch: float = 0.0
var _avatar: WorkerPresenter
var _hands: FirstPersonHands
var _look_delta: Vector2 = Vector2.ZERO
var _last_held: StringName = &""
var _last_position: Vector3 = Vector3.ZERO


## Call before add_child.
func setup(actor: StringName, peer: int, slot_index: int, local: bool) -> void:
	actor_id = actor
	peer_id = peer
	slot = slot_index
	is_local = local


func _ready() -> void:
	collision_layer = Layers.PLAYER
	collision_mask = Layers.WORLD if is_local else 0

	_head = Node3D.new()
	_head.position = Vector3(0.0, EYE_HEIGHT, 0.0)
	add_child(_head)

	hold_point = Node3D.new()
	hold_point.position = Vector3(HOLD_OFFSET.x, HOLD_OFFSET.y, -HOLD_DISTANCE)
	_head.add_child(hold_point)

	# Everyone has a worker body. The local player does not see theirs (first
	# person) but it still throws a shadow on the floor.
	_avatar = WorkerFactory.create(slot)
	_avatar.name = "Avatar"
	add_child(_avatar)

	if is_local:
		_avatar.set_shadow_only(true)
		var shape := CollisionShape3D.new()
		var capsule := CapsuleShape3D.new()
		capsule.radius = 0.35
		capsule.height = 1.7
		shape.shape = capsule
		shape.position = Vector3(0.0, 0.85, 0.0)
		add_child(shape)

		_camera = Camera3D.new()
		_camera.keep_aspect = Camera3D.KEEP_WIDTH
		_head.add_child(_camera)

		_ray = RayCast3D.new()
		_ray.target_position = Vector3(0.0, 0.0, -AIM_REACH)
		_ray.collision_mask = Layers.WORLD | Layers.PROP | Layers.SOCKET
		_ray.collide_with_areas = false
		_camera.add_child(_ray)

		_hands = FirstPersonHands.new()
		_hands.name = "Hands"
		_hands.setup(WorkerCostumes.for_slot(slot))
		_camera.add_child(_hands)

		Settings.changed.connect(_apply_settings)
		_apply_settings()
	else:
		# Other players hold a prop close to the chest, where their arms reach.
		hold_point.position = Vector3(0.0, -0.5, -0.62)

	_spawn = global_transform
	_net_pos = global_position
	_net_yaw = rotation.y
	_last_position = global_position


## A recoverable slapstick slip. Cosmetic only: it never takes control away.
## Not sent over the network yet, because nothing triggers it yet.
func stumble() -> void:
	_avatar.trigger(WorkerAnimations.STUMBLE)
	if _hands != null:
		_hands.stumble()


func avatar() -> WorkerPresenter:
	return _avatar


func hands() -> FirstPersonHands:
	return _hands


func eye_position() -> Vector3:
	return _head.global_position


func drop_velocity() -> Vector3:
	return velocity * 0.5


func look_pitch() -> float:
	return _pitch if is_local else _net_pitch


## Position, yaw and pitch as the network should carry them.
func sync_pose() -> Array:
	if is_local:
		return [global_position, rotation.y, _pitch]
	return [_net_pos, _net_yaw, _net_pitch]


## Turns the player. Used by tests and by reset; mouse look sets the same values.
func set_look(yaw: float, pitch: float) -> void:
	rotation.y = yaw
	_pitch = clampf(pitch, -MAX_PITCH, MAX_PITCH)
	if _head != null:
		_head.rotation.x = _pitch


func net_position() -> Vector3:
	return _net_pos


## Remote players: where to move toward next.
func net_set_target(pos: Vector3, yaw: float, pitch: float) -> void:
	_net_pos = pos
	_net_yaw = yaw
	_net_pitch = pitch


func reset() -> void:
	global_transform = _spawn
	velocity = Vector3.ZERO
	held_prop_id = &""
	_pitch = 0.0
	_net_pos = global_position
	if _head != null:
		_head.rotation.x = 0.0
	_set_preview(null)


## Shows the outcome of a request that did not succeed.
func report_result(result: Dictionary) -> void:
	if not result["ok"]:
		feedback.emit(str(REASON_TEXT.get(result["reason"], "Cannot do that.")))


func _unhandled_input(event: InputEvent) -> void:
	if not is_local or not controls_enabled:
		return
	var motion := event as InputEventMouseMotion
	if motion != null and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		var look: float = BASE_LOOK * Settings.mouse_sensitivity
		_look_delta += motion.relative
		rotate_y(-motion.relative.x * look)
		_pitch = clampf(_pitch - motion.relative.y * look, -MAX_PITCH, MAX_PITCH)
		_head.rotation.x = _pitch
	elif event.is_action_pressed(&"interact"):
		_interact()
	elif event.is_action_pressed(&"use"):
		_use()
	elif event.is_action_pressed(&"drop"):
		_drop()


func _physics_process(delta: float) -> void:
	if not is_local:
		var blend: float = 1.0 - exp(-REMOTE_SMOOTHING * delta)
		global_position = global_position.lerp(_net_pos, blend)
		rotation.y = lerp_angle(rotation.y, _net_yaw, blend)
		_head.rotation.x = lerpf(_head.rotation.x, _net_pitch, blend)
		# Everything the body animation needs is derived here from what every
		# peer already has: how the position changes and what is held.
		var seen_velocity: Vector3 = (global_position - _last_position) / maxf(delta, 0.0001)
		_last_position = global_position
		_avatar.update_motion(seen_velocity, seen_velocity.length() > SPRINT_SPEED * 0.9, held_prop_id != &"", delta)
		if held_prop_id != _last_held:
			_last_held = held_prop_id
			_avatar.trigger(WorkerAnimations.INTERACT)
		return
	var input := Vector2.ZERO
	if controls_enabled:
		input = Input.get_vector(&"move_left", &"move_right", &"move_forward", &"move_back")
	var speed: float = SPRINT_SPEED if controls_enabled and Input.is_action_pressed(&"sprint") else WALK_SPEED
	var wish: Vector3 = (global_transform.basis * Vector3(input.x, 0.0, input.y)).normalized() * speed
	var accel: float = minf(1.0, ACCELERATION * delta)
	velocity.x = lerpf(velocity.x, wish.x, accel)
	velocity.z = lerpf(velocity.z, wish.z, accel)
	if is_on_floor():
		velocity.y = 0.0
	else:
		velocity.y -= GRAVITY * delta
	move_and_slide()
	_update_hold_distance()
	var sprinting: bool = controls_enabled and Input.is_action_pressed(&"sprint")
	var carrying: bool = held_prop_id != &""
	_avatar.update_motion(velocity, sprinting, carrying, delta)
	_hands.carry_anchor = hold_point.position
	_hands.update_state(Vector2(velocity.x, velocity.z).length(), sprinting, carrying, _look_delta, delta)
	_look_delta = Vector2.ZERO


func _process(_delta: float) -> void:
	if is_local:
		_update_aim()


## The local body language for a deliberate pick up, place or drop.
func _pulse_interact() -> void:
	_avatar.trigger(WorkerAnimations.INTERACT)
	_hands.jab()


func _update_hold_distance() -> void:
	var distance: float = HOLD_DISTANCE
	if _ray.is_colliding():
		var hit_distance: float = _camera.global_position.distance_to(_ray.get_collision_point())
		distance = clampf(hit_distance - 0.45, HOLD_MIN_DISTANCE, HOLD_DISTANCE)
	hold_point.position = Vector3(HOLD_OFFSET.x, HOLD_OFFSET.y, -distance)


func _update_aim() -> void:
	if service == null or not controls_enabled:
		_set_prompt("")
		_set_preview(null)
		return
	var collider: Object = _ray.get_collider() if _ray.is_colliding() else null
	var socket := collider as PlacementSocket
	var prop := collider as PickupProp
	var lines: PackedStringArray = []
	var preview: PlacementSocket = null
	if held_prop_id != &"":
		if socket != null and service.validate(_place_request(socket)) == InteractionService.OK:
			lines.append("[Left click] Place on %s" % socket.display_name)
			preview = socket
		lines.append("[Q] Drop")
	elif prop != null and service.validate(_pickup_request(prop)) == InteractionService.OK:
		lines.append("[E] Pick up %s" % prop.display_name)
	_set_preview(preview)
	_set_prompt("\n".join(lines))


func _interact() -> void:
	if held_prop_id != &"" or not _ray.is_colliding():
		return
	var prop := _ray.get_collider() as PickupProp
	if prop != null:
		_pulse_interact()
		_submit(_pickup_request(prop))


func _use() -> void:
	if held_prop_id == &"" or not _ray.is_colliding():
		return
	var socket := _ray.get_collider() as PlacementSocket
	if socket != null:
		_pulse_interact()
		_submit(_place_request(socket))


func _drop() -> void:
	if held_prop_id != &"":
		_pulse_interact()
		_submit(_request(InteractionService.DROP, &"", -1))


func _pickup_request(prop: PickupProp) -> Dictionary:
	return _request(InteractionService.PICKUP, prop.entity_id, prop.revision)


func _place_request(socket: PlacementSocket) -> Dictionary:
	return _request(InteractionService.PLACE, socket.entity_id, -1)


func _request(action: StringName, target_id: StringName, expected_revision: int) -> Dictionary:
	_request_counter += 1
	return {
		"request_id": _request_counter,
		"actor_id": actor_id,
		"action": action,
		"target_id": target_id,
		"expected_revision": expected_revision,
	}


func _submit(req: Dictionary) -> void:
	if submit_request.is_valid():
		submit_request.call(req)


func _set_prompt(text: String) -> void:
	if text != _last_prompt:
		_last_prompt = text
		prompt_changed.emit(text)


func _set_preview(socket: PlacementSocket) -> void:
	var held: PickupProp = service.get_prop(held_prop_id) if service != null else null
	if socket == _previewed_socket and (socket == null or _previewed_prop == held):
		return
	if _previewed_socket != null:
		_previewed_socket.set_preview(false)
	_previewed_socket = socket
	_previewed_prop = held
	if socket != null:
		socket.set_preview(true, held)


func _apply_settings() -> void:
	_camera.fov = Settings.fov
