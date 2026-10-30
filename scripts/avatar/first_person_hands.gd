class_name FirstPersonHands
extends Node3D
## The two chunky mitten hands the local player sees, with sleeves in the
## coworker's costume colours. They rest low in view, bob while moving, swing
## up to hold a carried prop (following where the prop really is), jab on an
## interaction, scrub while cleaning, and flail on a stumble. They lag behind
## looking around, then settle.
##
## Presentation only: they never move the camera or the interaction origin.

const REST_L: Vector3 = Vector3(-0.27, -0.17, -0.5)
const REST_R: Vector3 = Vector3(0.27, -0.17, -0.5)
const JAB_SECONDS: float = 0.3
const STUMBLE_SECONDS: float = 0.4

var _hand_l: Node3D
var _hand_r: Node3D
var _spring_l := SecondarySpring.new(170.0, 18.0, 5.0)
var _spring_r := SecondarySpring.new(170.0, 18.0, 5.0)
var _sway: Vector2 = Vector2.ZERO
var _bob_phase: float = 0.0
var _jab_time: float = 99.0
var _stumble_time: float = 99.0
var _cleaning: bool = false
var _clean_phase: float = 0.0
var _carrying: bool = false
var _moving_amount: float = 0.0
## Where the carried prop is, in camera space. Set by the player every frame.
var carry_anchor: Vector3 = Vector3(0.12, -0.25, -0.95)


## Call before add_child.
func setup(costume: Dictionary) -> void:
	_hand_l = _build_hand(-1.0, costume)
	_hand_r = _build_hand(1.0, costume)
	_spring_l.reset(REST_L)
	_spring_r.reset(REST_R)
	_hand_l.position = REST_L
	_hand_r.position = REST_R


## Called every physics frame by the local player.
func update_state(speed: float, sprinting: bool, carrying: bool, look_delta: Vector2, delta: float) -> void:
	_carrying = carrying
	var moving: float = clampf(speed / 4.0, 0.0, 1.5)
	_moving_amount = lerpf(_moving_amount, moving, 1.0 - exp(-8.0 * delta))
	_bob_phase += delta * (6.0 + (3.0 if sprinting else 0.0)) * _moving_amount
	_jab_time += delta
	_stumble_time += delta
	if _cleaning:
		_clean_phase += delta * 12.0
	# Looking around drags the hands the other way for a moment.
	_sway = _sway.lerp(look_delta * -0.0009, 1.0 - exp(-14.0 * delta))

	var bob := Vector3(sin(_bob_phase) * 0.012, absf(cos(_bob_phase)) * 0.016, 0.0) * _moving_amount
	var base_l: Vector3 = REST_L
	var base_r: Vector3 = REST_R
	if _carrying:
		base_l = carry_anchor + Vector3(-0.31, 0.02, 0.1)
		base_r = carry_anchor + Vector3(0.31, 0.02, 0.1)
	var offset_l: Vector3 = bob + Vector3(_sway.x, _sway.y, 0.0)
	var offset_r: Vector3 = offset_l

	if _jab_time < JAB_SECONDS:
		var t: float = _jab_time / JAB_SECONDS
		var envelope: float = sin(t * PI)
		offset_r += Vector3(-0.04, 0.07, -0.3) * envelope
	if _cleaning:
		offset_r += Vector3(cos(_clean_phase) * 0.09, 0.08 + sin(_clean_phase) * 0.04, -0.15)
	if _stumble_time < STUMBLE_SECONDS:
		var s: float = sin(_stumble_time / STUMBLE_SECONDS * PI)
		offset_l += Vector3(-0.12, 0.32, 0.0) * s
		offset_r += Vector3(0.12, 0.32, 0.0) * s

	_hand_l.position = _spring_l.step(base_l + offset_l, delta)
	_hand_r.position = _spring_r.step(base_r + offset_r, delta)


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
	add_child(hand)
	var skin: Color = costume["skin"]
	var sleeve: Color = costume["shirt"]
	var cuff: Color = costume["vest"]
	# The mitten and its thumb wedge.
	_mesh(hand, _sphere(0.085), skin, Vector3.ZERO, Vector3(1.0, 0.85, 1.15))
	_mesh(hand, _sphere(0.04), skin, Vector3(-side * 0.075, 0.02, -0.05), Vector3.ONE)
	if costume["accessories"].has(&"one_glove") and side < 0.0:
		_mesh(hand, _sphere(0.095), Color(0.97, 0.97, 0.97), Vector3.ZERO, Vector3(1.0, 0.85, 1.15))
	# The forearm runs back and down toward where the shoulder would be.
	var back: Vector3 = Vector3(side * 0.12, -0.2, 0.7).normalized()
	var arm := _mesh(hand, _capsule(0.062, 0.62), sleeve, back * 0.36, Vector3.ONE)
	arm.basis = Basis(Quaternion(Vector3.UP, back))
	var cuff_mesh := _mesh(hand, _cylinder(0.07, 0.05), cuff, back * 0.1, Vector3.ONE)
	cuff_mesh.basis = Basis(Quaternion(Vector3.UP, back))
	return hand


func _mesh(parent: Node3D, mesh: PrimitiveMesh, color: Color, pos: Vector3, scl: Vector3) -> MeshInstance3D:
	var instance := MeshInstance3D.new()
	mesh.material = VenueBuilder.material(color, 0.65)
	instance.mesh = mesh
	instance.position = pos
	instance.scale = scl
	parent.add_child(instance)
	return instance


func _sphere(radius: float) -> SphereMesh:
	var m := SphereMesh.new()
	m.radius = radius
	m.height = radius * 2.0
	return m


func _capsule(radius: float, height: float) -> CapsuleMesh:
	var m := CapsuleMesh.new()
	m.radius = radius
	m.height = height
	return m


func _cylinder(radius: float, height: float) -> CylinderMesh:
	var m := CylinderMesh.new()
	m.top_radius = radius
	m.bottom_radius = radius
	m.height = height
	return m
