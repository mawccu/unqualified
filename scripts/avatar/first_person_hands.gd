class_name FirstPersonHands
extends Node3D
## Interaction feedback hands for the local player. First-person readability
## beats showing off an avatar, so these are hidden by default:
##   at rest, walking and carrying   nothing is drawn (a carried prop is its own feedback)
##   a pick up, place or drop        the right hand rises briefly, low in the corner
##   cleaning                        the right hand stays and scrubs
##   a stumble                       both hands flick up and withdraw
## They are small (HAND_SCALE of the old size), sit in the lower corners, and
## fade out when the action ends. Presentation only: they never move the
## camera or the interaction origin.

const HAND_SCALE: float = 0.5
## Where the hands wait, out of view below the screen edge.
const HIDDEN_L: Vector3 = Vector3(-0.24, -0.40, -0.46)
const HIDDEN_R: Vector3 = Vector3(0.24, -0.40, -0.46)
## Where they appear: low in the bottom corners, nowhere near the centre.
const SHOWN_L: Vector3 = Vector3(-0.27, -0.20, -0.50)
const SHOWN_R: Vector3 = Vector3(0.27, -0.20, -0.50)
const JAB_SECONDS: float = 0.45
const STUMBLE_SECONDS: float = 0.45
const FADE_SPEED: float = 9.0
const MIN_VISIBLE: float = 0.03

var _hand_l: Node3D
var _hand_r: Node3D
var _spring_l := SecondarySpring.new(170.0, 18.0, 5.0)
var _spring_r := SecondarySpring.new(170.0, 18.0, 5.0)
var _jab_time: float = 99.0
var _stumble_time: float = 99.0
var _cleaning: bool = false
var _clean_phase: float = 0.0
## 0 means fully withdrawn and not drawn at all, 1 means fully shown.
var presence: float = 0.0
## Kept for the carry hold point; hands no longer follow it.
var carry_anchor: Vector3 = Vector3(0.38, -0.4, -1.2)


## Call before add_child.
func setup(costume: Dictionary) -> void:
	_hand_l = _build_hand(-1.0, costume)
	_hand_r = _build_hand(1.0, costume)
	_spring_l.reset(HIDDEN_L)
	_spring_r.reset(HIDDEN_R)
	_hand_l.position = HIDDEN_L
	_hand_r.position = HIDDEN_R
	visible = false


## Called every physics frame by the local player.
func update_state(_speed: float, _sprinting: bool, _carrying: bool, _look_delta: Vector2, delta: float) -> void:
	_jab_time += delta
	_stumble_time += delta
	if _cleaning:
		_clean_phase += delta * 12.0

	var acting_right: bool = _jab_time < JAB_SECONDS or _cleaning or _stumble_time < STUMBLE_SECONDS
	var acting_left: bool = _stumble_time < STUMBLE_SECONDS
	presence = move_toward(presence, 1.0 if acting_right else 0.0, FADE_SPEED * delta)
	visible = presence > MIN_VISIBLE
	if not visible:
		_spring_l.reset(HIDDEN_L)
		_spring_r.reset(HIDDEN_R)
		_hand_l.position = HIDDEN_L
		_hand_r.position = HIDDEN_R
		return

	var target_r: Vector3 = SHOWN_R if acting_right else HIDDEN_R
	var target_l: Vector3 = SHOWN_L if acting_left else HIDDEN_L
	if _jab_time < JAB_SECONDS:
		var envelope: float = sin(clampf(_jab_time / JAB_SECONDS, 0.0, 1.0) * PI)
		target_r += Vector3(-0.05, 0.06, -0.12) * envelope
	if _cleaning:
		target_r += Vector3(cos(_clean_phase) * 0.06, 0.04 + sin(_clean_phase) * 0.03, -0.08)
	if acting_left:
		var s: float = sin(clampf(_stumble_time / STUMBLE_SECONDS, 0.0, 1.0) * PI)
		target_l += Vector3(-0.06, 0.16, 0.0) * s
		target_r += Vector3(0.06, 0.16, 0.0) * s
	_hand_l.position = _spring_l.step(target_l, delta)
	_hand_r.position = _spring_r.step(target_r, delta)


func jab() -> void:
	_jab_time = 0.0


func stumble() -> void:
	_stumble_time = 0.0


func set_cleaning(on: bool) -> void:
	_cleaning = on


func hand_position(right: bool) -> Vector3:
	return (_hand_r if right else _hand_l).position


func _build_hand(side: float, costume: Dictionary) -> Node3D:
	var hand := Node3D.new()
	hand.name = "HandR" if side > 0.0 else "HandL"
	hand.scale = Vector3.ONE * HAND_SCALE
	add_child(hand)
	var skin: Color = costume["skin"]
	var sleeve: Color = costume["shirt"]
	var cuff: Color = costume["vest"]
	_mesh(hand, _sphere(0.085), skin, Vector3.ZERO, Vector3(1.0, 0.85, 1.15))
	_mesh(hand, _sphere(0.04), skin, Vector3(-side * 0.075, 0.02, -0.05), Vector3.ONE)
	# The forearm runs back and down toward where the shoulder would be.
	var back: Vector3 = Vector3(side * 0.12, -0.2, 0.7).normalized()
	var arm := _mesh(hand, _capsule(0.062, 0.62), sleeve, back * 0.36, Vector3.ONE)
	arm.basis = Basis(Quaternion(Vector3.UP, back))
	var cuff_mesh := _mesh(hand, _cylinder(0.07, 0.05), cuff, back * 0.1, Vector3.ONE)
	cuff_mesh.basis = Basis(Quaternion(Vector3.UP, back))
	return hand


func _mesh(parent: Node3D, mesh: PrimitiveMesh, color: Color, pos: Vector3, scl: Vector3) -> MeshInstance3D:
	var instance := MeshInstance3D.new()
	mesh.material = VenueBuilder.material(color, 0.75)
	instance.mesh = mesh
	instance.position = pos
	instance.scale = scl
	parent.add_child(instance)
	return instance


func _sphere(radius: float) -> SphereMesh:
	var m := SphereMesh.new()
	m.radial_segments = 14
	m.rings = 7
	m.radius = radius
	m.height = radius * 2.0
	return m


func _capsule(radius: float, height: float) -> CapsuleMesh:
	var m := CapsuleMesh.new()
	m.radial_segments = 12
	m.rings = 3
	m.radius = radius
	m.height = height
	return m


func _cylinder(radius: float, height: float) -> CylinderMesh:
	var m := CylinderMesh.new()
	m.radial_segments = 16
	m.rings = 1
	m.top_radius = radius
	m.bottom_radius = radius
	m.height = height
	return m
