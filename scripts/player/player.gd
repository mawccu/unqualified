class_name Player
extends CharacterBody3D
## First-person controller. Reads input, shows what the crosshair can do, and
## sends every world change to InteractionService as a request.

signal prompt_changed(text: String)
signal feedback(text: String)

const WALK_SPEED: float = 4.0
const SPRINT_SPEED: float = 6.0
const ACCELERATION: float = 12.0
const GRAVITY: float = 20.0
const EYE_HEIGHT: float = 1.55
const AIM_REACH: float = 3.2
const HOLD_DISTANCE: float = 0.95
const BASE_LOOK: float = 0.0022
const MAX_PITCH: float = 1.45

const REASON_TEXT: Dictionary = {
	&"hands_full": "Your hands are full.",
	&"nothing_held": "You are not holding anything.",
	&"already_held": "Someone else has that.",
	&"out_of_range": "Too far away.",
	&"stale_revision": "It just changed. Try again.",
	&"socket_taken": "Something is already there.",
	&"socket_rejects": "That does not go there.",
	&"no_target": "Nothing to use there.",
}

@export var actor_id: StringName = &"local"

var held_prop_id: StringName = &""
var controls_enabled: bool = true
var service: InteractionService
var hold_point: Node3D

var _head: Node3D
var _camera: Camera3D
var _ray: RayCast3D
var _pitch: float = 0.0
var _spawn: Transform3D
var _request_counter: int = 0
var _last_prompt: String = ""
var _previewed_socket: PlacementSocket


func _ready() -> void:
	collision_layer = Layers.PLAYER
	collision_mask = Layers.WORLD

	var shape := CollisionShape3D.new()
	var capsule := CapsuleShape3D.new()
	capsule.radius = 0.35
	capsule.height = 1.7
	shape.shape = capsule
	shape.position = Vector3(0.0, 0.85, 0.0)
	add_child(shape)

	_head = Node3D.new()
	_head.position = Vector3(0.0, EYE_HEIGHT, 0.0)
	add_child(_head)

	_camera = Camera3D.new()
	_camera.keep_aspect = Camera3D.KEEP_WIDTH
	_head.add_child(_camera)

	hold_point = Node3D.new()
	hold_point.position = Vector3(0.25, -0.25, -HOLD_DISTANCE)
	_camera.add_child(hold_point)

	_ray = RayCast3D.new()
	_ray.target_position = Vector3(0.0, 0.0, -AIM_REACH)
	_ray.collision_mask = Layers.WORLD | Layers.PROP | Layers.SOCKET
	_ray.collide_with_areas = false
	_camera.add_child(_ray)

	_spawn = global_transform
	Settings.changed.connect(_apply_settings)
	_apply_settings()


func eye_position() -> Vector3:
	return _head.global_position


func drop_velocity() -> Vector3:
	return velocity * 0.5


func reset() -> void:
	global_transform = _spawn
	velocity = Vector3.ZERO
	held_prop_id = &""
	_pitch = 0.0
	_head.rotation.x = 0.0
	_set_preview(null)


func _unhandled_input(event: InputEvent) -> void:
	if not controls_enabled:
		return
	var motion := event as InputEventMouseMotion
	if motion != null and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		var look: float = BASE_LOOK * Settings.mouse_sensitivity
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
	var input := Vector2.ZERO
	if controls_enabled:
		input = Input.get_vector(&"move_left", &"move_right", &"move_forward", &"move_back")
	var speed: float = SPRINT_SPEED if controls_enabled and Input.is_action_pressed(&"sprint") else WALK_SPEED
	var wish: Vector3 = (global_transform.basis * Vector3(input.x, 0.0, input.y)).normalized() * speed
	var blend: float = minf(1.0, ACCELERATION * delta)
	velocity.x = lerpf(velocity.x, wish.x, blend)
	velocity.z = lerpf(velocity.z, wish.z, blend)
	if is_on_floor():
		velocity.y = 0.0
	else:
		velocity.y -= GRAVITY * delta
	move_and_slide()
	_update_hold_distance()


func _process(_delta: float) -> void:
	_update_aim()


func _update_hold_distance() -> void:
	var distance: float = HOLD_DISTANCE
	if _ray.is_colliding():
		var hit_distance: float = _camera.global_position.distance_to(_ray.get_collision_point())
		distance = clampf(hit_distance - 0.45, 0.5, HOLD_DISTANCE)
	hold_point.position = Vector3(0.25, -0.25, -distance)


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
		_submit(_pickup_request(prop))


func _use() -> void:
	if held_prop_id == &"" or not _ray.is_colliding():
		return
	var socket := _ray.get_collider() as PlacementSocket
	if socket != null:
		_submit(_place_request(socket))


func _drop() -> void:
	if held_prop_id != &"":
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
	var result: Dictionary = service.request(req)
	if not result["ok"]:
		feedback.emit(str(REASON_TEXT.get(result["reason"], "Cannot do that.")))


func _set_prompt(text: String) -> void:
	if text != _last_prompt:
		_last_prompt = text
		prompt_changed.emit(text)


func _set_preview(socket: PlacementSocket) -> void:
	if socket == _previewed_socket:
		return
	if _previewed_socket != null:
		_previewed_socket.set_preview(false)
	_previewed_socket = socket
	if socket != null:
		socket.set_preview(true)


func _apply_settings() -> void:
	_camera.fov = Settings.fov
