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

## Each stop: where to stand, how long to stay, and what to look at there.
const ROUTE: Array[Dictionary] = [
	{"pos": Vector3(0.0, 0.0, 0.6), "pause": 1.5, "look": Vector3(0.0, 1.5, -6.0), "state": &"checking the stage"},
	{"pos": Vector3(3.2, 0.0, -1.4), "pause": 2.5, "look": Vector3(3.2, 1.0, -3.0), "state": &"checking the sound desk"},
	{"pos": Vector3(6.4, 0.0, -0.8), "pause": 3.5, "look": Vector3(8.9, 1.1, -0.8), "state": &"inspecting the desserts"},
	{"pos": Vector3(1.0, 0.0, -4.0), "pause": 1.2, "look": Vector3(-6.2, 1.0, -3.6), "state": &"watching the floor"},
	{"pos": Vector3(-3.8, 0.0, -2.0), "pause": 3.0, "look": Vector3(-6.2, 1.0, -2.0), "state": &"checking the seating"},
	{"pos": Vector3(-3.0, 0.0, 0.4), "pause": 1.5, "look": Vector3(-9.2, 1.4, -2.0), "state": &"admiring the photo corner"},
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
	_label = VenueBuilder.label(self, "", Vector3(0.0, 2.35, 0.0), 0.0, 38, Color(1.0, 0.95, 0.7), 8)
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


func _build_body() -> void:
	var suit := VenueBuilder.material(Color(0.12, 0.16, 0.32), 0.7)
	var skin := VenueBuilder.material(Color(0.93, 0.76, 0.62), 0.6)
	var body := MeshInstance3D.new()
	var capsule := CapsuleMesh.new()
	capsule.radius = 0.3
	capsule.height = 1.4
	capsule.material = suit
	body.mesh = capsule
	body.position = Vector3(0.0, 0.7, 0.0)
	add_child(body)
	var legs := MeshInstance3D.new()
	var leg_mesh := BoxMesh.new()
	leg_mesh.size = Vector3(0.38, 0.45, 0.2)
	leg_mesh.material = VenueBuilder.material(Color(0.08, 0.1, 0.2), 0.7)
	legs.mesh = leg_mesh
	legs.position = Vector3(0.0, 0.22, 0.0)
	add_child(legs)
	var head := MeshInstance3D.new()
	var sphere := SphereMesh.new()
	sphere.radius = 0.17
	sphere.height = 0.34
	sphere.material = skin
	head.mesh = sphere
	head.position = Vector3(0.0, 1.62, 0.0)
	add_child(head)
	var hair := MeshInstance3D.new()
	var hair_mesh := SphereMesh.new()
	hair_mesh.radius = 0.18
	hair_mesh.height = 0.2
	hair_mesh.material = VenueBuilder.material(Color(0.18, 0.1, 0.07), 0.9)
	hair.mesh = hair_mesh
	hair.position = Vector3(0.0, 1.72, 0.03)
	add_child(hair)
	var nose := MeshInstance3D.new()
	var nose_mesh := BoxMesh.new()
	nose_mesh.size = Vector3(0.05, 0.05, 0.1)
	nose_mesh.material = skin
	nose.mesh = nose_mesh
	nose.position = Vector3(0.0, 1.6, -0.18)
	add_child(nose)
	var tie := MeshInstance3D.new()
	var tie_mesh := BoxMesh.new()
	tie_mesh.size = Vector3(0.07, 0.4, 0.03)
	tie_mesh.material = VenueBuilder.material(Color(0.85, 0.1, 0.15), 0.6)
	tie.mesh = tie_mesh
	tie.position = Vector3(0.0, 1.15, -0.29)
	add_child(tie)
	var clipboard := MeshInstance3D.new()
	var board_mesh := BoxMesh.new()
	board_mesh.size = Vector3(0.3, 0.4, 0.03)
	board_mesh.material = VenueBuilder.material(Color(0.95, 0.93, 0.8), 0.8)
	clipboard.mesh = board_mesh
	clipboard.position = Vector3(0.22, 1.05, -0.32)
	clipboard.rotation.z = -0.2
	add_child(clipboard)
