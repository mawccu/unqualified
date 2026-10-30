class_name VenueBuilder
extends RefCounted
## Builds the hotel from simple shapes: a polished public ballroom and a plain,
## cluttered back of house. Everything is made in code for now so the layout
## stays in one readable place; move to editor scenes when real art arrives.
## Positions come from VenueLayout.

const CREAM: Color = Color(0.86, 0.80, 0.70)
const WOOD_DARK: Color = Color(0.26, 0.17, 0.12)
const WOOD_LIGHT: Color = Color(0.46, 0.34, 0.24)
const GOLD: Color = Color(0.70, 0.55, 0.30)
const CARPET: Color = Color(0.30, 0.11, 0.13)
const CURTAIN: Color = Color(0.36, 0.11, 0.14)
const LINEN: Color = Color(0.93, 0.91, 0.86)
const SERVICE_WALL: Color = Color(0.56, 0.62, 0.60)
const SERVICE_FLOOR: Color = Color(0.40, 0.42, 0.42)
const STEEL: Color = Color(0.62, 0.66, 0.69)
const CHAIR: Color = Color(0.74, 0.66, 0.50)
const FLOWERS: Array[Color] = [
	Color(0.84, 0.62, 0.64), Color(0.93, 0.90, 0.82), Color(0.55, 0.65, 0.52), Color(0.66, 0.58, 0.72),
]


static func build(root: Node3D) -> void:
	_shell(root)
	_ballroom_dressing(root)
	_stage(root)
	_sound_desk(root)
	_guest_tables(root)
	_dessert_station(root)
	_photo_display(root)
	_lighting(root)
	_back_of_house(root)


# Primitives ------------------------------------------------------------------

static func material(color: Color, roughness: float = 0.85, glow: float = 0.0) -> StandardMaterial3D:
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color
	mat.roughness = roughness
	if glow > 0.0:
		mat.emission_enabled = true
		mat.emission = color
		mat.emission_energy_multiplier = glow * 0.35
	return mat


static func box(parent: Node3D, node_name: String, size: Vector3, pos: Vector3, color: Color,
		solid: bool = true, yaw: float = 0.0, glow: float = 0.0, roughness: float = 0.85) -> Node3D:
	var mesh := BoxMesh.new()
	mesh.size = size
	mesh.material = material(color, roughness, glow)
	var shape := BoxShape3D.new()
	shape.size = size
	return _body(parent, node_name, mesh, shape if solid else null, pos, yaw)


static func cylinder(parent: Node3D, node_name: String, radius: float, height: float, pos: Vector3,
		color: Color, solid: bool = true, glow: float = 0.0, roughness: float = 0.85) -> Node3D:
	var mesh := CylinderMesh.new()
	mesh.top_radius = radius
	mesh.bottom_radius = radius
	mesh.height = height
	mesh.material = material(color, roughness, glow)
	var shape := CylinderShape3D.new()
	shape.radius = radius
	shape.height = height
	return _body(parent, node_name, mesh, shape if solid else null, pos, 0.0)


static func ball(parent: Node3D, node_name: String, radius: float, pos: Vector3, color: Color,
		glow: float = 0.0) -> Node3D:
	var mesh := SphereMesh.new()
	mesh.radius = radius
	mesh.height = radius * 2.0
	mesh.material = material(color, 0.7, glow)
	return _body(parent, node_name, mesh, null, pos, 0.0)


static func label(parent: Node3D, text: String, pos: Vector3, yaw_degrees: float, size: int,
		color: Color = Color(1, 1, 1), outline: int = 12) -> Label3D:
	var l := Label3D.new()
	l.text = text
	l.font_size = size
	l.pixel_size = 0.004
	l.modulate = color
	l.outline_size = outline
	l.outline_modulate = Color(0, 0, 0, 0.9)
	l.position = pos
	l.rotation_degrees = Vector3(0, yaw_degrees, 0)
	l.double_sided = false
	parent.add_child(l)
	return l


static func _body(parent: Node3D, node_name: String, mesh: Mesh, shape: Shape3D, pos: Vector3, yaw: float) -> Node3D:
	var visual := MeshInstance3D.new()
	visual.mesh = mesh
	if shape == null:
		visual.name = node_name
		visual.position = pos
		visual.rotation.y = yaw
		parent.add_child(visual)
		return visual
	var body := StaticBody3D.new()
	body.name = node_name
	body.position = pos
	body.rotation.y = yaw
	body.collision_layer = Layers.WORLD
	body.collision_mask = 0
	body.add_child(visual)
	var collider := CollisionShape3D.new()
	collider.shape = shape
	body.add_child(collider)
	parent.add_child(body)
	return body


# Shell -----------------------------------------------------------------------

static func _shell(root: Node3D) -> void:
	var h: float = VenueLayout.WALL_HEIGHT
	var mid: float = h * 0.5
	# Floors: carpet in the ballroom, plain concrete behind the scenes.
	box(root, "BallroomFloor", Vector3(20.0, 0.2, 9.0), Vector3(0, -0.1, -2.5), CARPET, true, 0.0, 0.0, 0.95)
	box(root, "ServiceFloor", Vector3(20.0, 0.2, 5.0), Vector3(0, -0.1, 4.5), SERVICE_FLOOR, true, 0.0, 0.0, 0.7)
	# A dance floor inlay of polished wood.
	box(root, "DanceFloor", Vector3(7.0, 0.02, 4.0), Vector3(0, 0.011, -2.4), WOOD_LIGHT, false, 0.0, 0.0, 0.35)
	box(root, "DanceFloorTrim", Vector3(7.3, 0.015, 4.3), Vector3(0, 0.005, -2.4), GOLD, false, 0.0, 0.15, 0.4)
	# Outer walls: cream in front of house, grey-green behind.
	box(root, "WallNorth", Vector3(20.4, h, 0.2), Vector3(0, mid, -7.1), CREAM)
	box(root, "WallSouth", Vector3(20.4, h, 0.2), Vector3(0, mid, 7.1), SERVICE_WALL)
	box(root, "WallWestBallroom", Vector3(0.2, h, 9.2), Vector3(-10.1, mid, -2.4), CREAM)
	box(root, "WallEastBallroom", Vector3(0.2, h, 9.2), Vector3(10.1, mid, -2.4), CREAM)
	box(root, "WallWestService", Vector3(0.2, h, 5.2), Vector3(-10.1, mid, 4.5), SERVICE_WALL)
	box(root, "WallEastService", Vector3(0.2, h, 5.2), Vector3(10.1, mid, 4.5), SERVICE_WALL)
	# Dividing wall with two archways.
	var dz: float = VenueLayout.DIVIDER_Z
	var gap: float = VenueLayout.DOOR_WIDTH * 0.5
	var door_x: float = VenueLayout.DOOR_X
	var west_len: float = 10.0 - (door_x + gap)
	box(root, "DividerWest", Vector3(west_len, h, 0.2), Vector3(-10.0 + west_len * 0.5, mid, dz), SERVICE_WALL)
	box(root, "DividerEast", Vector3(west_len, h, 0.2), Vector3(10.0 - west_len * 0.5, mid, dz), SERVICE_WALL)
	var centre_len: float = (door_x - gap) * 2.0
	box(root, "DividerCentre", Vector3(centre_len, h, 0.2), Vector3(0, mid, dz), SERVICE_WALL)
	for sign_x: float in [-1.0, 1.0]:
		box(root, "Lintel", Vector3(VenueLayout.DOOR_WIDTH, h - 2.3, 0.2),
			Vector3(sign_x * door_x, 2.3 + (h - 2.3) * 0.5, dz), SERVICE_WALL)
		box(root, "ArchTrim", Vector3(VenueLayout.DOOR_WIDTH + 0.3, 0.12, 0.3),
			Vector3(sign_x * door_x, 2.3, dz), GOLD, false, 0.0, 0.2, 0.4)
	var ceiling := box(root, "Ceiling", Vector3(20.4, 0.2, 14.4), Vector3(0, VenueLayout.CEILING + 0.1, 0), Color(0.88, 0.85, 0.78), false) as MeshInstance3D
	# The soft key light below comes in "through" the ceiling.
	ceiling.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	# Signs over the archways, one for each side.
	for sign_x: float in [-1.0, 1.0]:
		label(root, "STAFF ONLY", Vector3(sign_x * door_x, 2.65, dz + 0.12), 0.0, 52, Color(0.86, 0.74, 0.40))
		label(root, "BALLROOM", Vector3(sign_x * door_x, 2.65, dz - 0.12), 180.0, 52, Color(0.98, 0.92, 0.75))


static func _ballroom_dressing(root: Node3D) -> void:
	var h: float = VenueLayout.WALL_HEIGHT
	# Dark wood panelling with a gold rail along the three outer ballroom walls.
	box(root, "PanelNorth", Vector3(19.8, 1.0, 0.08), Vector3(0, 0.5, -6.96), WOOD_DARK, false)
	box(root, "PanelWest", Vector3(0.08, 1.0, 8.8), Vector3(-9.96, 0.5, -2.5), WOOD_DARK, false)
	box(root, "PanelEast", Vector3(0.08, 1.0, 8.8), Vector3(9.96, 0.5, -2.5), WOOD_DARK, false)
	box(root, "RailNorth", Vector3(19.8, 0.05, 0.1), Vector3(0, 1.03, -6.95), GOLD, false, 0.0, 0.2, 0.4)
	box(root, "RailWest", Vector3(0.1, 0.05, 8.8), Vector3(-9.95, 1.03, -2.5), GOLD, false, 0.0, 0.2, 0.4)
	box(root, "RailEast", Vector3(0.1, 0.05, 8.8), Vector3(9.95, 1.03, -2.5), GOLD, false, 0.0, 0.2, 0.4)
	# Columns.
	for cx: float in [-9.5, 9.5]:
		for cz: float in [-6.5, 1.4]:
			cylinder(root, "Column", 0.3, h, Vector3(cx, h * 0.5, cz), CREAM)
			cylinder(root, "ColumnBase", 0.38, 0.25, Vector3(cx, 0.125, cz), WOOD_DARK, false)
			cylinder(root, "ColumnCap", 0.38, 0.2, Vector3(cx, h - 0.1, cz), GOLD, false, 0.1)
	# Fairy lights along the north wall.
	for i: int in 17:
		var lx: float = -8.0 + float(i)
		ball(root, "FairyLight", 0.05, Vector3(lx, 3.15 - 0.12 * sin(float(i) * 1.3), -6.85), Color(1.0, 0.85, 0.5), 3.0)
	# Welcome sign near the entrance archway.
	label(root, "WELCOME, GUESTS", Vector3(-6.0, 1.7, 1.88), 180.0, 44, Color(0.98, 0.92, 0.75))


static func _stage(root: Node3D) -> void:
	box(root, "StagePlatform", Vector3(8.0, 0.5, 2.2), Vector3(0, 0.25, -5.9), WOOD_DARK, true, 0.0, 0.0, 0.5)
	box(root, "StageEdge", Vector3(8.0, 0.06, 0.08), Vector3(0, 0.5, -4.8), GOLD, false, 0.0, 0.3, 0.4)
	box(root, "BackCurtain", Vector3(8.6, 3.0, 0.12), Vector3(0, 2.0, -6.9), CURTAIN, false)
	for sx: float in [-1.0, 1.0]:
		box(root, "SideCurtain", Vector3(1.1, 2.95, 0.5), Vector3(sx * 4.4, 1.98, -6.5), CURTAIN, false)
		box(root, "Speaker", Vector3(0.55, 1.2, 0.5), Vector3(sx * 3.3, 1.1, -5.6), Color(0.21, 0.21, 0.24), true, 0.0, 0.0, 0.5)
		ball(root, "SpeakerCone", 0.16, Vector3(sx * 3.3, 0.95, -5.34), Color(0.25, 0.25, 0.3))
	# A flower arch on the stage for the ceremony.
	for sx: float in [-1.0, 1.0]:
		cylinder(root, "ArchPost", 0.07, 2.1, Vector3(sx * 1.2, 1.55, -6.0), LINEN, false)
	box(root, "ArchTop", Vector3(2.6, 0.12, 0.14), Vector3(0, 2.6, -6.0), LINEN, false)
	for i: int in 9:
		ball(root, "ArchFlower", 0.15, Vector3(-1.2 + float(i) * 0.3, 2.66, -6.0), FLOWERS[i % FLOWERS.size()])
	label(root, "AMARA & JAMAL", Vector3(0, 3.22, -6.82), 0.0, 120, Color(0.88, 0.80, 0.56), 16)
	label(root, "TODAY", Vector3(0, 2.92, -6.82), 0.0, 40, Color(0.98, 0.85, 0.85))


static func _sound_desk(root: Node3D) -> void:
	var p: Vector3 = VenueLayout.SOUND_DESK
	box(root, "SoundDesk", Vector3(1.9, 0.82, 0.8), p + Vector3(0, 0.41, 0), Color(0.21, 0.22, 0.25), true, 0.0, 0.0, 0.5)
	box(root, "Mixer", Vector3(1.4, 0.07, 0.5), p + Vector3(0, 0.86, 0.02), Color(0.2, 0.2, 0.24), false, 0.0, 0.0, 0.4)
	for i: int in 8:
		var cx: float = -0.56 + float(i) * 0.16
		ball(root, "Knob", 0.035, p + Vector3(cx, 0.92, -0.12), FLOWERS[i % FLOWERS.size()])
		box(root, "Fader", Vector3(0.03, 0.02, 0.2), p + Vector3(cx, 0.9, 0.08), Color(0.9, 0.9, 0.95), false)
	label(root, "SOUND", p + Vector3(0, 1.45, 0.45), 0.0, 60, Color(0.9, 0.95, 1.0))


static func _guest_tables(root: Node3D) -> void:
	var index: int = 0
	for center: Vector3 in VenueLayout.GUEST_TABLES:
		index += 1
		var top: float = VenueLayout.TABLE_TOP
		cylinder(root, "GuestTable%d" % index, 0.9, top - 0.02, center + Vector3(0, (top - 0.02) * 0.5, 0), LINEN)
		cylinder(root, "TableTop%d" % index, 0.94, 0.03, center + Vector3(0, top - 0.015, 0), LINEN, false)
		for c: int in 6:
			var angle: float = float(c) / 6.0 * TAU
			var cpos: Vector3 = center + Vector3(cos(angle) * 1.35, 0.0, sin(angle) * 1.35)
			var chair := Node3D.new()
			chair.name = "Chair"
			chair.position = cpos
			chair.rotation.y = -angle + PI * 0.5
			root.add_child(chair)
			box(chair, "Seat", Vector3(0.46, 0.06, 0.46), Vector3(0, 0.46, 0), CHAIR, false)
			box(chair, "Back", Vector3(0.46, 0.55, 0.05), Vector3(0, 0.76, -0.2), CHAIR, false)
			for lx: float in [-0.19, 0.19]:
				for lz: float in [-0.19, 0.19]:
					cylinder(chair, "Leg", 0.02, 0.43, Vector3(lx, 0.215, lz), WOOD_DARK, false)
		# Centrepiece off to the side so the table sign has the middle.
		var vase_pos: Vector3 = center + Vector3(0.45, top, 0.3)
		cylinder(root, "Vase", 0.07, 0.22, vase_pos + Vector3(0, 0.11, 0), Color(0.75, 0.9, 0.95), false, 0.0, 0.2)
		for f: int in 5:
			var fa: float = float(f) * 1.25
			ball(root, "Flower", 0.09, vase_pos + Vector3(cos(fa) * 0.07, 0.3 + 0.03 * float(f % 2), sin(fa) * 0.07), FLOWERS[f % FLOWERS.size()])
		for s: int in 4:
			var sa: float = float(s) / 4.0 * TAU + 0.4
			cylinder(root, "Plate", 0.11, 0.015, center + Vector3(cos(sa) * 0.62, top + 0.01, sin(sa) * 0.62), Color(0.99, 0.99, 1.0), false, 0.0, 0.3)


static func _dessert_station(root: Node3D) -> void:
	var stand: Vector3 = VenueLayout.DESSERT_STAND
	var table_pos: Vector3 = Vector3(stand.x, 0.0, stand.z)
	box(root, "DessertTable", Vector3(1.0, 0.82, 2.5), table_pos + Vector3(0, 0.41, 0), LINEN)
	box(root, "DessertTableTop", Vector3(1.06, 0.03, 2.56), table_pos + Vector3(0, 0.835, 0), LINEN, false)
	# The stand: a gold pedestal and plate. The plate top is where layer one sits.
	cylinder(root, "StandPedestal", 0.07, 0.06, table_pos + Vector3(0, 0.85, 0), GOLD, false, 0.2, 0.3)
	cylinder(root, "StandPlate", 0.4, 0.04, Vector3(stand.x, stand.y - 0.02, stand.z), GOLD, false, 0.15, 0.3)
	# Dessert-table trimmings.
	for dz: float in [-0.95, 0.95]:
		cylinder(root, "TrimVase", 0.06, 0.2, table_pos + Vector3(0.0, 0.94, dz), Color(0.75, 0.9, 0.95), false, 0.0, 0.2)
		for f: int in 4:
			ball(root, "TrimFlower", 0.08, table_pos + Vector3(cos(float(f)) * 0.05, 1.1 + 0.02 * float(f), dz + sin(float(f)) * 0.05), FLOWERS[f % FLOWERS.size()])
	for i: int in 5:
		cylinder(root, "Cupcake", 0.05, 0.07, table_pos + Vector3(-0.28, 0.88, -0.6 + float(i) * 0.3), Color(0.98, 0.8, 0.5), false)
		ball(root, "Icing", 0.05, table_pos + Vector3(-0.28, 0.95, -0.6 + float(i) * 0.3), FLOWERS[i % FLOWERS.size()])
	label(root, "DESSERTS", Vector3(9.86, 2.1, stand.z), -90.0, 80, Color(1.0, 0.85, 0.9), 14)


static func _photo_display(root: Node3D) -> void:
	var p: Vector3 = VenueLayout.PHOTO_DISPLAY
	box(root, "PhotoBackdrop", Vector3(0.12, 2.3, 2.2), p + Vector3(-0.5, 1.15, 0), Color(0.98, 0.84, 0.88), false)
	for dz: float in [-1.1, 1.1]:
		cylinder(root, "PhotoPost", 0.09, 2.6, p + Vector3(0.0, 1.3, dz), LINEN, false)
	box(root, "PhotoArch", Vector3(0.18, 0.18, 2.4), p + Vector3(0.0, 2.55, 0), LINEN, false)
	for i: int in 11:
		var a: float = float(i)
		ball(root, "PhotoFlower", 0.17, p + Vector3(0.0, 2.6 + 0.05 * float(i % 2), -1.1 + a * 0.22), FLOWERS[i % FLOWERS.size()])
	# A big gold frame that people pose behind.
	box(root, "FrameTop", Vector3(0.06, 0.07, 1.3), p + Vector3(-0.38, 1.9, 0), GOLD, false, 0.0, 0.2, 0.4)
	box(root, "FrameBottom", Vector3(0.06, 0.07, 1.3), p + Vector3(-0.38, 0.95, 0), GOLD, false, 0.0, 0.2, 0.4)
	for dz: float in [-0.65, 0.65]:
		box(root, "FrameSide", Vector3(0.06, 1.0, 0.07), p + Vector3(-0.38, 1.42, dz), GOLD, false, 0.0, 0.2, 0.4)
	label(root, "SAY CHEESE", p + Vector3(-0.3, 1.42, 0.0), 90.0, 64, Color(0.45, 0.1, 0.2), 0)


static func _lighting(root: Node3D) -> void:
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color(0.05, 0.04, 0.04)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(1.0, 0.9, 0.78)
	env.ambient_light_energy = 0.28
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.tonemap_exposure = 0.9
	env.ssao_enabled = true
	env.ssao_radius = 1.2
	env.ssao_intensity = 2.0
	env.glow_enabled = true
	env.glow_intensity = 0.15
	env.glow_bloom = 0.02
	var world_env := WorldEnvironment.new()
	world_env.environment = env
	root.add_child(world_env)
	# Chandeliers over the ballroom, plain strip lights behind the scenes.
	for pos: Vector3 in [Vector3(-5.0, 3.0, -3.0), Vector3(5.0, 3.0, -3.0), Vector3(0.0, 3.0, 0.2)]:
		_chandelier(root, pos, true)
	for pos: Vector3 in [Vector3(-6.0, 3.2, 4.5), Vector3(0.0, 3.2, 4.5), Vector3(6.0, 3.2, 4.5)]:
		_strip_light(root, pos)
	# One soft warm key light so shadows all fall the same way: it comes from
	# the entrance side (south-west) and high. Fill comes from the ambient light.
	var key := DirectionalLight3D.new()
	key.name = "KeyLight"
	key.rotation_degrees = Vector3(-58.0, 35.0, 0.0)
	key.light_color = Color(1.0, 0.88, 0.72)
	key.light_energy = 0.42
	key.shadow_enabled = true
	key.shadow_blur = 1.6
	root.add_child(key)
	# A pool of light on the stage.
	var spot := SpotLight3D.new()
	spot.position = Vector3(0, 3.2, -3.4)
	spot.rotation_degrees = Vector3(-72, 0, 0)
	spot.spot_range = 7.0
	spot.spot_angle = 42.0
	spot.light_energy = 1.2
	spot.light_color = Color(1.0, 0.9, 0.75)
	root.add_child(spot)


static func _chandelier(root: Node3D, pos: Vector3, shadows: bool) -> void:
	var node := Node3D.new()
	node.name = "Chandelier"
	node.position = pos
	root.add_child(node)
	cylinder(node, "Chain", 0.015, 0.45, Vector3(0, 0.3, 0), GOLD, false)
	cylinder(node, "Ring", 0.38, 0.05, Vector3.ZERO, GOLD, false, 0.4, 0.3)
	for i: int in 8:
		var a: float = float(i) / 8.0 * TAU
		ball(node, "Bulb", 0.06, Vector3(cos(a) * 0.38, 0.06, sin(a) * 0.38), Color(1.0, 0.9, 0.65), 6.0)
	var light := OmniLight3D.new()
	light.position = Vector3(0, -0.2, 0)
	light.omni_range = 11.0
	light.light_energy = 0.95
	light.light_color = Color(1.0, 0.86, 0.66)
	light.shadow_enabled = shadows
	node.add_child(light)


static func _strip_light(root: Node3D, pos: Vector3) -> void:
	box(root, "StripLight", Vector3(2.4, 0.06, 0.25), pos, Color(0.95, 0.97, 1.0), false, 0.0, 4.0)
	var light := OmniLight3D.new()
	light.position = pos + Vector3(0, -0.3, 0)
	light.omni_range = 8.0
	light.light_energy = 0.7
	light.light_color = Color(0.92, 0.96, 1.0)
	root.add_child(light)


# Back of house ---------------------------------------------------------------

static func _back_of_house(root: Node3D) -> void:
	# Safety stripe along the service walls.
	var stripe: Color = Color(0.78, 0.66, 0.22)
	box(root, "StripeSouth", Vector3(19.8, 0.14, 0.04), Vector3(0, 1.1, 6.98), stripe, false)
	box(root, "StripeWest", Vector3(0.04, 0.14, 4.8), Vector3(-9.98, 1.1, 4.5), stripe, false)
	box(root, "StripeEast", Vector3(0.04, 0.14, 4.8), Vector3(9.98, 1.1, 4.5), stripe, false)
	# Prep table with the cake layers on top.
	var prep: Vector3 = VenueLayout.PREP_TABLE
	box(root, "PrepTable", Vector3(2.8, VenueLayout.PREP_TOP, 0.9), prep + Vector3(0, VenueLayout.PREP_TOP * 0.5, 0), STEEL, true, 0.0, 0.0, 0.4)
	label(root, "CAKE PREP", prep + Vector3(0, 1.75, -0.55), 180.0, 60, Color(0.86, 0.80, 0.58))
	# A table for the table signs.
	var st: Vector3 = VenueLayout.SIGN_TABLE
	box(root, "SignTable", Vector3(1.5, VenueLayout.SIGN_TABLE_TOP, 0.7), st + Vector3(0, VenueLayout.SIGN_TABLE_TOP * 0.5, 0), WOOD_LIGHT)
	label(root, "TABLE SIGNS", st + Vector3(0, 1.55, 0.0), 0.0, 50, Color(0.86, 0.80, 0.58))
	# Storage shelving along the west half of the south wall.
	var shelf_colors: Array[Color] = [Color(0.36, 0.46, 0.56), Color(0.50, 0.56, 0.58), Color(0.40, 0.50, 0.46), Color(0.58, 0.60, 0.62)]
	for i: int in 4:
		_shelf(root, Vector3(-9.0 + float(i) * 1.85, 0.0, 6.6), shelf_colors, i)
	label(root, "STORAGE", Vector3(-7.2, 2.7, 6.95), 180.0, 60, Color(0.86, 0.80, 0.58))
	# Wash-up nook on the east side.
	box(root, "WashCounter", Vector3(3.4, 0.9, 0.75), Vector3(7.6, 0.45, 6.55), STEEL, true, 0.0, 0.0, 0.35)
	box(root, "SinkBasin", Vector3(0.9, 0.12, 0.5), Vector3(7.6, 0.92, 6.55), Color(0.3, 0.33, 0.38), false, 0.0, 0.0, 0.3)
	cylinder(root, "Tap", 0.025, 0.3, Vector3(7.6, 1.08, 6.82), Color(0.85, 0.87, 0.9), false, 0.0, 0.2)
	box(root, "TapSpout", Vector3(0.04, 0.04, 0.2), Vector3(7.6, 1.22, 6.72), Color(0.85, 0.87, 0.9), false)
	label(root, "WASH-UP", Vector3(7.6, 2.3, 6.95), 180.0, 60, Color(0.7, 0.95, 1.0))
	cylinder(root, "MopBucket", 0.22, 0.4, Vector3(5.2, 0.2, 6.3), Color(0.80, 0.66, 0.20), true)
	cylinder(root, "MopHandle", 0.02, 1.4, Vector3(5.2, 0.8, 6.3), Color(0.7, 0.5, 0.3), false)
	cylinder(root, "Extinguisher", 0.09, 0.45, Vector3(9.5, 0.23, 6.5), Color(0.70, 0.18, 0.16), false)
	# The notice board: staff-only notes.
	box(root, "NoticeBoard", Vector3(2.0, 1.2, 0.05), Vector3(1.0, 1.85, 6.97), Color(0.68, 0.5, 0.32), false)
	for i: int in 5:
		box(root, "Note", Vector3(0.32, 0.4, 0.01), Vector3(0.3 + float(i) * 0.35, 1.85 + 0.12 * sin(float(i) * 2.0), 6.935), Color(0.98, 0.97, 0.85), false, 0.15 * float(i - 2))
	label(root, "SERVICE NOTES", Vector3(1.0, 2.7, 6.95), 180.0, 56, Color(0.86, 0.80, 0.58))
	# Spare stock to make it feel lived in.
	for i: int in 3:
		box(root, "Crate", Vector3(0.7, 0.5, 0.6), Vector3(9.0 - float(i) * 0.1, 0.25 + float(i) * 0.5, 4.2), Color(0.36, 0.48, 0.58), true, 0.1 * float(i))


static func _shelf(root: Node3D, pos: Vector3, colors: Array[Color], seed_index: int) -> void:
	for px: float in [-0.8, 0.8]:
		for pz: float in [-0.22, 0.22]:
			cylinder(root, "ShelfPost", 0.025, 2.2, pos + Vector3(px, 1.1, pz), Color(0.4, 0.42, 0.46), false)
	for level: int in 3:
		var y: float = 0.45 + float(level) * 0.7
		box(root, "ShelfBoard", Vector3(1.7, 0.05, 0.55), pos + Vector3(0, y, 0), Color(0.5, 0.52, 0.56), true)
		for b: int in 3:
			var color: Color = colors[(seed_index + level + b) % colors.size()]
			var bw: float = 0.32 + 0.06 * float((b + level) % 3)
			box(root, "Stock", Vector3(bw, 0.3, 0.38), pos + Vector3(-0.55 + float(b) * 0.55, y + 0.175, 0.0), color, false, 0.05 * float(b))
