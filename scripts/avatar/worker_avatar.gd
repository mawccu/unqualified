class_name WorkerAvatar
extends WorkerPresenter
## The code-built FALLBACK worker. It exists so the game runs and the tests have
## a body before an authored model is available, and as a debug fixture. It is
## not the shipped look: assemblies of spheres, capsules and boxes cannot be
## polished character art. See GlbWorker and docs/assets/WORKER_GLB_CONTRACT.md.
##
## Style.MINIMAL (the default) is a faceless mannequin in the uniform palette:
## charcoal trousers, an off-white shirt, a dark vest, one small accent, and a
## hair or hat silhouette. Style.DETAILED is the earlier busier version with a
## face and accessories, kept behind a flag (--detailed-workers) for debugging.
##
## Secondary motion is limited to a loose tie, lanyard or apron on the detailed
## style. The head does not wobble.

enum Style { MINIMAL, DETAILED }

const BODY_SCALE: float = 1.1

## Set from the command line switch --detailed-workers to get the busier version.
static var default_style: Style = Style.MINIMAL

var style: Style = default_style

var _tie_sway: Node3D
var _lanyard_sway: Node3D
var _apron_sway: Node3D
var _head_sway: Node3D
var _tie_spring := SecondarySpring.new(95.0, 4.5, 0.7)
var _lanyard_spring := SecondarySpring.new(85.0, 4.0, 0.7)
var _apron_spring := SecondarySpring.new(80.0, 5.0, 0.5)


func _process(delta: float) -> void:
	var local_accel: Vector3 = global_transform.basis.inverse() * _accel
	var loose := Vector3(clampf(local_accel.z * 0.05, -0.6, 0.6), 0.0, clampf(-local_accel.x * 0.05, -0.6, 0.6))
	_tie_spring.step(loose, delta)
	_lanyard_spring.step(loose, delta)
	_apron_spring.step(loose * 0.5, delta)
	_tie_sway.rotation = _tie_spring.value
	_lanyard_sway.rotation = _lanyard_spring.value
	_apron_sway.rotation = _apron_spring.value


func sway_nodes() -> Array[Node3D]:
	# The head is listed first but never moves; the loose parts follow.
	return [_head_sway, _tie_sway, _lanyard_sway, _apron_sway]


# Body --------------------------------------------------------------------------

func _build_visuals() -> void:
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
		_part(leg, _capsule(0.1, 0.36), costume["shoes"], Vector3(0.0, -0.45, -0.06), Vector3(90.0, 0.0, 0.0))

	var torso := Node3D.new()
	torso.name = "Torso"
	torso.position = Vector3(0.0, 0.02, 0.0)
	hips.add_child(torso)
	_part(torso, _capsule(0.2, 0.56), costume["shirt"], Vector3(0.0, 0.3, 0.0))
	_part(torso, _box(Vector3(0.38, 0.4, 0.05)), costume["vest"], Vector3(0.0, 0.34, -0.18))
	_part(torso, _box(Vector3(0.38, 0.4, 0.05)), costume["vest"], Vector3(0.0, 0.34, 0.18))

	for side: int in 2:
		_build_arm(torso, side, -1.0 if side == 0 else 1.0, skin)

	var head_pivot := Node3D.new()
	head_pivot.name = "HeadPivot"
	head_pivot.position = Vector3(0.0, 0.62, 0.0)
	torso.add_child(head_pivot)
	_head_sway = Node3D.new()
	_head_sway.name = "HeadSway"
	head_pivot.add_child(_head_sway)
	_build_head(_head_sway, skin)

	_tie_sway = _sway_node(torso, "TieSway", Vector3(0.0, 0.52, -0.22))
	_lanyard_sway = _sway_node(torso, "LanyardSway", Vector3(0.0, 0.56, -0.23))
	_apron_sway = _sway_node(torso, "ApronSway", Vector3(0.0, 0.34, -0.22))

	if style == Style.MINIMAL:
		# One small accent per player: a pocket square on the vest.
		_part(torso, _box(Vector3(0.07, 0.07, 0.02)), costume["accent"], Vector3(0.11, 0.46, -0.215))
	else:
		for accessory: StringName in costume["accessories"]:
			_add_accessory(accessory, hips, torso)


func _sway_node(parent: Node3D, node_name: String, pos: Vector3) -> Node3D:
	var node := Node3D.new()
	node.name = node_name
	node.position = pos
	parent.add_child(node)
	return node


func _build_arm(torso: Node3D, side: int, sx: float, skin: Color) -> void:
	var arm := Node3D.new()
	arm.name = "ArmL" if side == 0 else "ArmR"
	arm.position = Vector3(sx * 0.26, 0.5, 0.0)
	torso.add_child(arm)
	_part(arm, _capsule(0.07, 0.32), costume["shirt"], Vector3(0.0, -0.15, 0.0))
	var elbow := Node3D.new()
	elbow.name = "ElbowL" if side == 0 else "ElbowR"
	elbow.position = Vector3(0.0, -0.28, 0.0)
	arm.add_child(elbow)
	_part(elbow, _capsule(0.065, 0.3), costume["shirt"], Vector3(0.0, -0.13, 0.0))
	var hand := Node3D.new()
	hand.name = "Hand"
	hand.position = Vector3(0.0, -0.27, 0.0)
	elbow.add_child(hand)
	var hand_radius: float = 0.075 if style == Style.MINIMAL else 0.115
	_part(hand, _sphere(hand_radius), skin, Vector3(0.0, -0.05, 0.0), Vector3.ZERO, Vector3(1.0, 1.1, 0.85))
	if style == Style.DETAILED:
		_part(hand, _sphere(0.05), skin, Vector3(-sx * 0.1, -0.03, -0.05))


func _build_head(parent: Node3D, skin: Color) -> void:
	_part(parent, _sphere(0.27), skin, Vector3(0.0, 0.22, 0.0))
	if style == Style.DETAILED:
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
			_part(parent, _sphere(0.3), Color(0.22, 0.23, 0.25), Vector3(0.0, 0.3, 0.02), Vector3.ZERO, Vector3(1.0, 0.8, 1.0))


func _add_accessory(accessory: StringName, hips: Node3D, torso: Node3D) -> void:
	var accent: Color = costume["accent"]
	match accessory:
		&"bow_tie":
			_part(_tie_sway, _box(Vector3(0.09, 0.07, 0.04)), accent, Vector3(-0.07, 0.0, 0.0), Vector3(0, 0, -15))
			_part(_tie_sway, _box(Vector3(0.09, 0.07, 0.04)), accent, Vector3(0.07, 0.0, 0.0), Vector3(0, 0, 15))
			_part(_tie_sway, _box(Vector3(0.05, 0.05, 0.05)), accent.darkened(0.3), Vector3.ZERO)
		&"lanyard":
			_part(_lanyard_sway, _box(Vector3(0.035, 0.42, 0.015)), accent, Vector3(0.0, -0.2, 0.0))
			_part(_lanyard_sway, _box(Vector3(0.16, 0.2, 0.02)), Color(0.93, 0.92, 0.88), Vector3(0.0, -0.46, -0.01))
		&"apron":
			_part(_apron_sway, _box(Vector3(0.44, 0.4, 0.03)), costume["vest"], Vector3(0.0, -0.2, 0.0))
			_part(_apron_sway, _box(Vector3(0.16, 0.12, 0.03)), accent, Vector3(0.0, -0.22, -0.02))
		&"headset":
			_part(_head_sway, _box(Vector3(0.6, 0.04, 0.05)), Color(0.12, 0.12, 0.14), Vector3(0.0, 0.5, 0.0))
			for side: float in [-1.0, 1.0]:
				_part(_head_sway, _sphere(0.08), Color(0.14, 0.14, 0.16), Vector3(side * 0.28, 0.24, 0.0), Vector3.ZERO, Vector3(0.5, 1.0, 1.0))
		&"radio":
			_part(hips, _box(Vector3(0.1, 0.17, 0.05)), Color(0.14, 0.14, 0.16), Vector3(-0.27, 0.14, 0.05))
		&"belt":
			_part(hips, _box(Vector3(0.5, 0.07, 0.4)), Color(0.28, 0.2, 0.14), Vector3(0.0, 0.07, 0.0))
		&"one_glove":
			var glove: Node3D = torso.get_node_or_null("ArmL/ElbowL/Hand") as Node3D
			if glove != null:
				_part(glove, _sphere(0.125), Color(0.9, 0.9, 0.88), Vector3(0.0, -0.05, 0.0), Vector3.ZERO, Vector3(1.0, 1.1, 0.9))


# Primitive helpers -------------------------------------------------------------

func _part(parent: Node3D, mesh: PrimitiveMesh, color: Color, pos: Vector3, rot_degrees: Vector3 = Vector3.ZERO,
		scl: Vector3 = Vector3.ONE) -> MeshInstance3D:
	var instance := MeshInstance3D.new()
	mesh.material = VenueBuilder.material(color, 0.8)
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
