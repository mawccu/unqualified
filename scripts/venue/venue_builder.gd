class_name VenueBuilder
extends RefCounted
## Builds the hotel. The look comes from the wedding asset pack (floor tiles, wall
## panels, furniture and props from assets/wedding); this class places them and
## adds what the art does not have: collision, lights, signs and the layout.
## Positions come from VenueLayout. Repeated parts (floor tiles, wall panels,
## chairs) are drawn with MultiMesh so a large room costs a few draw calls.

const TEAL: Color = Color(0.14, 0.45, 0.45)
const TEAL_DARK: Color = Color(0.10, 0.32, 0.34)
const WOOD: Color = Color(0.55, 0.38, 0.26)
const CREAM: Color = Color(0.90, 0.88, 0.82)
const BRASS: Color = Color(0.78, 0.62, 0.30)
const SERVICE_FLOOR: Color = Color(0.46, 0.50, 0.50)
const FAIRY: Color = Color(1.0, 0.86, 0.55)


static func build(root: Node3D) -> void:
	_shell(root)
	_stage(root)
	_sound_desk(root)
	_tables_and_chairs(root)
	_dessert_station(root)
	_photo_corner(root)
	_lighting(root)
	_back_of_house(root)


# Primitives (also used by the NPC, the hands and the fallback worker) ------------

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
		solid_body: bool = true, yaw: float = 0.0, glow: float = 0.0, roughness: float = 0.85) -> Node3D:
	var mesh := BoxMesh.new()
	mesh.size = size
	mesh.material = material(color, roughness, glow)
	var shape := BoxShape3D.new()
	shape.size = size
	return _body(parent, node_name, mesh, shape if solid_body else null, pos, yaw)


static func cylinder(parent: Node3D, node_name: String, radius: float, height: float, pos: Vector3,
		color: Color, solid_body: bool = true, glow: float = 0.0, roughness: float = 0.85) -> Node3D:
	var mesh := CylinderMesh.new()
	mesh.radial_segments = 24
	mesh.rings = 1
	mesh.top_radius = radius
	mesh.bottom_radius = radius
	mesh.height = height
	mesh.material = material(color, roughness, glow)
	var shape := CylinderShape3D.new()
	shape.radius = radius
	shape.height = height
	return _body(parent, node_name, mesh, shape if solid_body else null, pos, 0.0)


static func ball(parent: Node3D, node_name: String, radius: float, pos: Vector3, color: Color,
		glow: float = 0.0) -> Node3D:
	var mesh := SphereMesh.new()
	mesh.radial_segments = 16
	mesh.rings = 8
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


## An invisible solid box, for collision under art that has none.
static func solid(parent: Node3D, node_name: String, size: Vector3, pos: Vector3, yaw: float = 0.0, pitch: float = 0.0) -> StaticBody3D:
	var body := StaticBody3D.new()
	body.name = node_name
	body.position = pos
	body.rotation = Vector3(pitch, yaw, 0.0)
	body.collision_layer = Layers.WORLD
	body.collision_mask = 0
	var collider := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = size
	collider.shape = shape
	body.add_child(collider)
	parent.add_child(body)
	return body


## A pack model placed in the world, with optional box collision (size) centred
## above the model's ground point.
static func place(parent: Node3D, id: String, pos: Vector3, yaw: float = 0.0, collision: Vector3 = Vector3.ZERO,
		model_scale: float = 1.0) -> Node3D:
	var holder := Node3D.new()
	holder.name = id.capitalize().replace(" ", "")
	holder.position = pos
	holder.rotation.y = yaw
	holder.scale = Vector3.ONE * model_scale
	var model: Node3D = WeddingAssets.instance(id)
	holder.add_child(model)
	parent.add_child(holder)
	if collision != Vector3.ZERO:
		solid(holder, "Collision", collision, Vector3(0.0, collision.y * 0.5, 0.0))
	return holder


## Draw one pack mesh at many places with a single MultiMesh.
static func scatter(parent: Node3D, id: String, transforms: Array[Transform3D], shadows: bool = true) -> MultiMeshInstance3D:
	var parts: Array = WeddingAssets.mesh_and_base(id)
	var mm := MultiMesh.new()
	mm.transform_format = MultiMesh.TRANSFORM_3D
	mm.mesh = parts[0]
	mm.instance_count = transforms.size()
	var base: Transform3D = parts[1]
	for i: int in transforms.size():
		mm.set_instance_transform(i, transforms[i] * base)
	var node := MultiMeshInstance3D.new()
	node.name = "%s_x%d" % [id, transforms.size()]
	node.multimesh = mm
	if not shadows:
		node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	parent.add_child(node)
	return node


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


# Shell: floor, walls, ceiling, doorways ---------------------------------------------

static func _shell(root: Node3D) -> void:
	var w: float = VenueLayout.HALF_WIDTH
	var north: float = VenueLayout.NORTH_Z
	var south: float = VenueLayout.SOUTH_Z
	var dz: float = VenueLayout.DIVIDER_Z
	var h: float = VenueLayout.WALL_HEIGHT

	# Floors. The ballroom is wood planks (pack tiles); behind the scenes is plain.
	var tiles: Array[Transform3D] = []
	for ix: int in range(-13, 14, 2):
		for iz: int in range(-15, 2, 2):
			tiles.append(Transform3D(Basis.IDENTITY, Vector3(float(ix), 0.0, float(iz))))
	scatter(root, "floor_tile_2m", tiles, false)
	solid(root, "BallroomFloor", Vector3(w * 2.0 + 0.4, 0.3, dz - north + 0.2), Vector3(0.0, -0.15, (north + dz) * 0.5))
	box(root, "ServiceFloor", Vector3(w * 2.0 + 0.4, 0.3, south - dz + 0.2), Vector3(0.0, -0.15, (dz + south) * 0.5), SERVICE_FLOOR, true, 0.0, 0.0, 0.7)

	# Walls: pack panels facing inward, plus invisible colliders.
	var panels: Array[Transform3D] = []
	for ix: int in range(-13, 14, 2):
		panels.append(Transform3D(Basis(Vector3.UP, 0.0), Vector3(float(ix), 0.0, north)))
		panels.append(Transform3D(Basis(Vector3.UP, PI), Vector3(float(ix), 0.0, south)))
	for iz: int in range(-15, 8, 2):
		panels.append(Transform3D(Basis(Vector3.UP, PI * 0.5), Vector3(-w, 0.0, float(iz))))
		panels.append(Transform3D(Basis(Vector3.UP, -PI * 0.5), Vector3(w, 0.0, float(iz))))
	var door: float = VenueLayout.DOOR_X
	var gap: float = VenueLayout.DOOR_WIDTH * 0.5
	var runs: Array[Vector2] = [Vector2(-w, -door - gap), Vector2(-door + gap, door - gap), Vector2(door + gap, w)]
	for run: Vector2 in runs:
		var x: float = run.x + 1.0
		while x < run.y:
			panels.append(Transform3D(Basis(Vector3.UP, PI), Vector3(x, 0.0, dz)))
			x += 2.0
		var length: float = run.y - run.x
		solid(root, "DividerSolid", Vector3(length, h, 0.3), Vector3((run.x + run.y) * 0.5, h * 0.5, dz))
	scatter(root, "wall_panel_2m", panels, false)
	solid(root, "WallNorthSolid", Vector3(w * 2.0 + 0.6, h, 0.3), Vector3(0.0, h * 0.5, north))
	solid(root, "WallSouthSolid", Vector3(w * 2.0 + 0.6, h, 0.3), Vector3(0.0, h * 0.5, south))
	solid(root, "WallWestSolid", Vector3(0.3, h, south - north + 0.6), Vector3(-w, h * 0.5, (north + south) * 0.5))
	solid(root, "WallEastSolid", Vector3(0.3, h, south - north + 0.6), Vector3(w, h * 0.5, (north + south) * 0.5))

	var ceiling := box(root, "Ceiling", Vector3(w * 2.0 + 0.6, 0.2, south - north + 0.6),
		Vector3(0.0, VenueLayout.CEILING + 0.1, (north + south) * 0.5), Color(0.88, 0.87, 0.82), false) as MeshInstance3D
	# The soft key light below comes in "through" the ceiling.
	ceiling.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	# The two staff doorways between the ballroom and the back of house.
	for sx: float in [-1.0, 1.0]:
		place(root, "staff_doorway", Vector3(sx * door, 0.0, dz), PI)


# Stage and sound ---------------------------------------------------------------------

static func _stage(root: Node3D) -> void:
	var north: float = VenueLayout.NORTH_Z
	var depth_a: float = north + 0.915
	var depth_b: float = north + 2.745
	for ix: int in 3:
		for z: float in [depth_a, depth_b]:
			place(root, "stage_platform", Vector3((float(ix) - 1.0) * 3.8, 0.0, z))
	# One walkable block under the platforms and a ramp under the steps, so a
	# player can walk up onto the stage.
	var stage_depth: float = 3.66
	var stage_center_z: float = north + stage_depth * 0.5
	solid(root, "StageSolid", Vector3(11.4, VenueLayout.STAGE_HEIGHT, stage_depth), Vector3(0.0, VenueLayout.STAGE_HEIGHT * 0.5, stage_center_z))
	var front: float = north + stage_depth
	place(root, "stage_steps", Vector3(0.0, 0.0, front + 0.35))
	var slope: float = atan(VenueLayout.STAGE_HEIGHT / 0.7)
	solid(root, "StageRamp", Vector3(1.0, 0.08, 0.76), Vector3(0.0, VenueLayout.STAGE_HEIGHT * 0.5 - 0.02, front + 0.35), 0.0, slope)

	# Backdrop, banner, arch and speakers.
	box(root, "Backdrop", Vector3(11.8, 2.6, 0.12), Vector3(0.0, 1.5, north + 0.12), TEAL_DARK, false)
	for sx: float in [-1.0, 1.0]:
		box(root, "Drape", Vector3(0.9, 2.55, 0.35), Vector3(sx * 6.3, 1.5, north + 0.35), TEAL, false)
	place(root, "flower_arch", Vector3(0.0, VenueLayout.STAGE_HEIGHT, north + 2.0), 0.0, Vector3.ZERO, 0.68)
	for sx: float in [-1.0, 1.0]:
		place(root, "speaker", Vector3(sx * 4.9, VenueLayout.STAGE_HEIGHT, north + 2.4), 0.0, Vector3(0.45, 0.98, 0.41))
	label(root, "AMARA & JAMAL", Vector3(0.0, 2.62, north + 0.22), 0.0, 110, Color(0.95, 0.86, 0.6), 14)
	label(root, "TODAY", Vector3(0.0, 2.3, north + 0.22), 0.0, 40, Color(0.88, 0.92, 0.9))
	# Fairy lights along the north wall.
	for i: int in 21:
		var lx: float = -10.0 + float(i)
		ball(root, "FairyLight", 0.045, Vector3(lx, 2.78 - 0.1 * sin(float(i) * 1.3), north + 0.2), FAIRY, 3.0)


static func _sound_desk(root: Node3D) -> void:
	var p: Vector3 = VenueLayout.SOUND_DESK
	place(root, "sound_desk", p, 0.0, Vector3(1.35, 0.94, 0.65))
	place(root, "audio_mixer", p + Vector3(0.0, 0.94, 0.0))
	place(root, "cable_red", p + Vector3(-1.1, 0.0, 0.8), 0.3)
	place(root, "cable_blue", p + Vector3(-0.5, 0.0, 1.1), 1.2)
	place(root, "cable_yellow", p + Vector3(0.3, 0.0, 1.0), 2.1)
	label(root, "SOUND", p + Vector3(0.0, 1.9, 0.0), 0.0, 60, Color(0.9, 0.95, 0.95))


# Tables, chairs, flowers ---------------------------------------------------------------

static func _tables_and_chairs(root: Node3D) -> void:
	var chairs: Array[Transform3D] = []
	var count: int = VenueLayout.CHAIRS_PER_TABLE
	for center: Vector3 in VenueLayout.all_tables():
		place(root, "guest_table", center, 0.0, Vector3(1.46, VenueLayout.TABLE_TOP, 1.46))
		for c: int in count:
			var angle: float = float(c) / float(count) * TAU + 0.3
			var spot: Vector3 = center + Vector3(cos(angle), 0.0, sin(angle)) * VenueLayout.CHAIR_RING
			# The pack's chairs face +Z, so turn each one so its front looks at the table.
			var toward: Vector3 = center - spot
			var yaw: float = atan2(toward.x, toward.z)
			chairs.append(Transform3D(Basis(Vector3.UP, yaw), spot))
	scatter(root, "guest_chair", chairs)
	# The three seating-task tables keep their middle free for the table sign, so
	# their flowers sit off to the side; the decorative tables carry them in the middle.
	for center: Vector3 in VenueLayout.GUEST_TABLES:
		place(root, "flower_centerpiece", center + Vector3(0.42, VenueLayout.TABLE_TOP, 0.25))
	for center: Vector3 in VenueLayout.DECOR_TABLES:
		place(root, "flower_centerpiece", center + Vector3(0.0, VenueLayout.TABLE_TOP, 0.0))


# Dessert station and photo corner ---------------------------------------------------------

static func _dessert_station(root: Node3D) -> void:
	var stand: Vector3 = VenueLayout.DESSERT_STAND
	for dz: float in [-1.0, 1.0]:
		place(root, "dessert_table", Vector3(stand.x, 0.0, stand.z + dz), PI * 0.5, Vector3(0.85, VenueLayout.DESSERT_TABLE_TOP, 2.0))
	place(root, "cake_stand", Vector3(stand.x, VenueLayout.DESSERT_TABLE_TOP, stand.z))
	label(root, "DESSERTS", Vector3(VenueLayout.HALF_WIDTH - 0.2, 2.2, stand.z), -90.0, 80, Color(0.95, 0.85, 0.85), 14)


static func _photo_corner(root: Node3D) -> void:
	var p: Vector3 = VenueLayout.PHOTO_DISPLAY
	place(root, "flower_arch", p + Vector3(0.5, 0.0, 0.0), PI * 0.5)
	place(root, "photo_display", p + Vector3(1.6, 0.0, 0.0), PI * 0.5, Vector3(0.78, 1.75, 0.45))


# Lighting ------------------------------------------------------------------------------------

static func _lighting(root: Node3D) -> void:
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color(0.05, 0.05, 0.05)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(1.0, 0.95, 0.88)
	env.ambient_light_energy = 0.34
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.tonemap_exposure = 0.95
	env.ssao_enabled = true
	env.ssao_radius = 1.2
	env.ssao_intensity = 2.0
	env.glow_enabled = true
	env.glow_intensity = 0.15
	env.glow_bloom = 0.02
	var world_env := WorldEnvironment.new()
	world_env.environment = env
	root.add_child(world_env)
	# Six chandeliers over the ballroom, strip lights behind the scenes.
	for pos: Vector3 in [Vector3(-7.0, 2.65, -7.0), Vector3(7.0, 2.65, -7.0), Vector3(0.0, 2.65, -9.0),
			Vector3(0.0, 2.65, -3.0), Vector3(-7.0, 2.65, -1.0), Vector3(7.0, 2.65, -1.0)]:
		_chandelier(root, pos)
	for x: float in [-9.0, -3.0, 3.0, 9.0]:
		_strip_light(root, Vector3(x, 2.9, 5.0))
	# One soft warm key light so shadows all fall the same way.
	var key := DirectionalLight3D.new()
	key.name = "KeyLight"
	key.rotation_degrees = Vector3(-58.0, 35.0, 0.0)
	key.light_color = Color(1.0, 0.9, 0.76)
	key.light_energy = 0.42
	key.shadow_enabled = true
	key.shadow_blur = 1.6
	root.add_child(key)
	# A pool of light on the stage.
	var spot := SpotLight3D.new()
	spot.position = Vector3(0, 2.9, VenueLayout.NORTH_Z + 6.0)
	spot.rotation_degrees = Vector3(-72, 0, 0)
	spot.spot_range = 8.0
	spot.spot_angle = 46.0
	spot.light_energy = 1.2
	spot.light_color = Color(1.0, 0.92, 0.8)
	root.add_child(spot)


static func _chandelier(root: Node3D, pos: Vector3) -> void:
	var node := Node3D.new()
	node.name = "Chandelier"
	node.position = pos
	root.add_child(node)
	cylinder(node, "Chain", 0.015, 0.4, Vector3(0, 0.25, 0), BRASS, false)
	cylinder(node, "Ring", 0.34, 0.05, Vector3.ZERO, BRASS, false, 0.4, 0.3)
	for i: int in 8:
		var a: float = float(i) / 8.0 * TAU
		ball(node, "Bulb", 0.05, Vector3(cos(a) * 0.34, 0.05, sin(a) * 0.34), Color(1.0, 0.9, 0.7), 6.0)
	var light := OmniLight3D.new()
	light.position = Vector3(0, -0.2, 0)
	light.omni_range = 10.0
	light.light_energy = 0.95
	light.light_color = Color(1.0, 0.88, 0.7)
	light.shadow_enabled = true
	node.add_child(light)


static func _strip_light(root: Node3D, pos: Vector3) -> void:
	box(root, "StripLight", Vector3(2.2, 0.06, 0.25), pos, Color(0.95, 0.97, 1.0), false, 0.0, 4.0)
	var light := OmniLight3D.new()
	light.position = pos + Vector3(0, -0.3, 0)
	light.omni_range = 8.0
	light.light_energy = 0.7
	light.light_color = Color(0.92, 0.96, 1.0)
	root.add_child(light)


# Back of house ---------------------------------------------------------------------------------

static func _back_of_house(root: Node3D) -> void:
	var prep: Vector3 = VenueLayout.PREP_TABLE
	place(root, "prep_table", prep, 0.0, Vector3(1.8, VenueLayout.PREP_TOP, 0.75))
	label(root, "CAKE PREP", Vector3(prep.x, 2.3, VenueLayout.DIVIDER_Z + 0.25), 0.0, 60, Color(0.95, 0.88, 0.6))
	var sign_table: Vector3 = VenueLayout.SIGN_TABLE
	place(root, "prep_table", sign_table, 0.0, Vector3(1.8, VenueLayout.SIGN_TABLE_TOP, 0.75))
	place(root, "clipboard", sign_table + Vector3(0.7, VenueLayout.SIGN_TABLE_TOP, 0.0), 0.4)
	label(root, "TABLE SIGNS", Vector3(sign_table.x, 2.3, VenueLayout.DIVIDER_Z + 0.25), 0.0, 52, Color(0.95, 0.88, 0.6))

	# Storage shelving along the west half of the south wall.
	for i: int in 6:
		place(root, "storage_shelf", Vector3(-12.6 + float(i) * 1.3, 0.0, VenueLayout.SOUTH_Z - 0.4), PI, Vector3(1.2, 1.73, 0.51))
	label(root, "STORAGE", Vector3(-9.0, 2.4, VenueLayout.SOUTH_Z - 0.15), 180.0, 60, Color(0.95, 0.88, 0.6))
	for i: int in 3:
		place(root, "storage_crate", Vector3(-11.4 + float(i) * 0.7, 0.0, 5.2), float(i) * 0.2, Vector3(0.61, 0.46, 0.45))
	place(root, "storage_crate", Vector3(-11.0, 0.46, 5.2), 0.1)

	# Wash-up and the cleaning kit.
	place(root, "mop_bucket", Vector3(9.5, 0.0, VenueLayout.SOUTH_Z - 0.6))
	place(root, "mop", Vector3(10.2, 0.0, VenueLayout.SOUTH_Z - 0.6), 0.2)
	place(root, "cleaning_cloth", Vector3(8.6, 0.0, VenueLayout.SOUTH_Z - 0.6), 0.5)
	label(root, "WASH-UP", Vector3(9.5, 2.4, VenueLayout.SOUTH_Z - 0.15), 180.0, 60, Color(0.7, 0.92, 0.95))
	for i: int in 2:
		place(root, "storage_crate", Vector3(12.2, float(i) * 0.46, 5.0), 0.15 * float(i), Vector3(0.61, 0.46, 0.45))
