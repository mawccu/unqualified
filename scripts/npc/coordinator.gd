class_name Coordinator
extends Node3D
## The wedding coordinator: the client's eyes in the ballroom. For now she only
## patrols a fixed loop and pauses to look at each station. Attention, line of
## sight and suspicion come next; this is the body and the route they will use.
##
## Only the authority (offline player or host) walks the route. A client copy
## just follows the pose it is sent.

const SPEED: float = 1.25
const TURN_SPEED: float = 4.0
const SMOOTHING: float = 12.0
const MODEL_SCALE: float = 0.77

## Each stop: where to stand, how long to stay, and what to look at there.
const ROUTE: Array[Dictionary] = [
	{"pos": Vector3(0.0, 0.0, -1.0), "pause": 1.5, "look": Vector3(0.0, 1.5, -14.0), "state": &"checking the stage"},
	{"pos": Vector3(0.0, 0.0, -9.0), "pause": 2.5, "look": Vector3(7.5, 1.0, -11.2), "state": &"checking the sound desk"},
	{"pos": Vector3(6.0, 0.0, -6.5), "pause": 0.1, "look": Vector3(10.8, 1.0, -6.6), "state": &"walking"},
	{"pos": Vector3(10.8, 0.0, -6.6), "pause": 3.5, "look": Vector3(12.7, 1.1, -5.0), "state": &"inspecting the desserts"},
	{"pos": Vector3(0.0, 0.0, -6.0), "pause": 1.2, "look": Vector3(-9.0, 1.0, -9.0), "state": &"watching the floor"},
	{"pos": Vector3(-6.0, 0.0, -6.5), "pause": 3.0, "look": Vector3(-9.0, 1.0, -6.5), "state": &"checking the seating"},
	{"pos": Vector3(-11.0, 0.0, -7.0), "pause": 2.0, "look": Vector3(-12.5, 1.4, -7.0), "state": &"admiring the photo corner"},
	{"pos": Vector3(-6.5, 0.0, -6.0), "pause": 0.1, "look": Vector3(-5.0, 1.0, -1.0), "state": &"walking"},
	{"pos": Vector3(-5.0, 0.0, -1.0), "pause": 0.5, "look": Vector3(0.0, 1.0, -1.0), "state": &"walking"},
]

@export var entity_id: StringName = &"coordinator"

## False on a client: the host's pose is followed instead of walking the route.
var authoritative: bool = true
var state: StringName = &"patrolling"

var _stop: int = 0
var _pause_left: float = 0.0
var _arrived: bool = false
var _net_pos: Vector3
var _net_yaw: float = 0.0
var _label: Label3D


func _ready() -> void:
	position = ROUTE[0]["pos"]
	_net_pos = position
	_build_body()
	_label = VenueBuilder.label(self, "", Vector3(0.0, 2.1, 0.0), 0.0, 38, Color(1.0, 0.95, 0.7), 8)
	_label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	_label.double_sided = true


func _physics_process(delta: float) -> void:
	if not authoritative:
		var blend: float = 1.0 - exp(-SMOOTHING * delta)
		position = position.lerp(_net_pos, blend)
		rotation.y = lerp_angle(rotation.y, _net_yaw, blend)
		_update_label()
		return
	var stop: Dictionary = ROUTE[_stop]
	var target: Vector3 = stop["pos"]
	if not _arrived:
		var to_target: Vector3 = target - position
		to_target.y = 0.0
		if to_target.length() < 0.08:
			_arrived = true
			_pause_left = float(stop["pause"])
			state = stop["state"]
		else:
			position += to_target.normalized() * minf(SPEED * delta, to_target.length())
			_face(position + to_target, delta)
			state = &"walking"
	else:
		_face(stop["look"], delta)
		_pause_left -= delta
		if _pause_left <= 0.0:
			_stop = (_stop + 1) % ROUTE.size()
			_arrived = false
	_update_label()


## Pose as the network should carry it: position, yaw, and what she is doing.
func sync_pose() -> Array:
	return [position, rotation.y, String(state)]


func net_set_target(pos: Vector3, yaw: float, new_state: String) -> void:
	_net_pos = pos
	_net_yaw = yaw
	state = StringName(new_state)


func _face(point: Vector3, delta: float) -> void:
	var flat: Vector3 = point - global_position
	flat.y = 0.0
	if flat.length() < 0.05:
		return
	var yaw: float = atan2(-flat.x, -flat.z)
	rotation.y = lerp_angle(rotation.y, yaw, minf(1.0, TURN_SPEED * delta))


func _update_label() -> void:
	if _label != null:
		_label.text = "COORDINATOR\n%s" % String(state)


## The pack's coordinator model. It is a static mesh (no skeleton yet), so she
## glides along her route instead of walking. The pack's models are about a third
## bigger than the player, so she is scaled to a believable height.
func _build_body() -> void:
	var model: Node3D = WeddingAssets.instance("coordinator")
	model.name = "Model"
	model.scale = Vector3.ONE * MODEL_SCALE
	# The model faces +Z; the route logic treats -Z as forward.
	model.rotation.y = PI
	add_child(model)
