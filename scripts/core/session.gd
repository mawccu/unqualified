class_name Session
extends Node3D
## M0 session root. Builds the graybox room in code (no editor needed yet),
## wires the interaction service, the local player and the HUD, and owns
## pause and restart. Solo pauses the tree on the menu; multiplayer must not.

var service: InteractionService
var player: Player
var hud: Hud
var menu_open: bool = false


func _ready() -> void:
	service = InteractionService.new()
	service.name = "InteractionService"
	add_child(service)

	_build_environment()
	_build_room()
	_build_props()
	_spawn_player()

	hud = Hud.new()
	hud.name = "Hud"
	add_child(hud)
	hud.pause_toggled.connect(func() -> void: _set_menu(not menu_open))
	hud.resume_requested.connect(func() -> void: _set_menu(false))
	hud.restart_requested.connect(restart)
	hud.quit_requested.connect(func() -> void: get_tree().quit())
	player.prompt_changed.connect(hud.set_prompt)
	player.feedback.connect(hud.show_toast)
	get_window().focus_exited.connect(_on_focus_lost)

	_set_menu(false)


func restart() -> void:
	service.reset_all()
	_set_menu(false)


func _set_menu(open: bool) -> void:
	if menu_open and not open:
		Settings.save()
	menu_open = open
	get_tree().paused = open
	player.controls_enabled = not open
	hud.set_menu_visible(open)
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE if open else Input.MOUSE_MODE_CAPTURED


func _on_focus_lost() -> void:
	if not menu_open:
		_set_menu(true)


func _build_environment() -> void:
	var sky := Sky.new()
	sky.sky_material = ProceduralSkyMaterial.new()
	var env := Environment.new()
	env.background_mode = Environment.BG_SKY
	env.sky = sky
	env.ambient_light_source = Environment.AMBIENT_SOURCE_SKY
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	var world_env := WorldEnvironment.new()
	world_env.environment = env
	add_child(world_env)

	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-55.0, -30.0, 0.0)
	sun.shadow_enabled = true
	add_child(sun)


func _build_room() -> void:
	var wall_color := Color(0.82, 0.78, 0.7)
	_add_block("Floor", Vector3(12.0, 0.2, 9.0), Vector3(0.0, -0.1, 0.0), Color(0.45, 0.4, 0.35))
	_add_block("WallNorth", Vector3(12.4, 3.2, 0.2), Vector3(0.0, 1.6, -4.6), wall_color)
	_add_block("WallSouth", Vector3(12.4, 3.2, 0.2), Vector3(0.0, 1.6, 4.6), wall_color)
	_add_block("WallEast", Vector3(0.2, 3.2, 9.0), Vector3(6.1, 1.6, 0.0), wall_color)
	_add_block("WallWest", Vector3(0.2, 3.2, 9.0), Vector3(-6.1, 1.6, 0.0), wall_color)
	_add_block("SideTable", Vector3(1.2, 0.8, 0.7), Vector3(-3.0, 0.4, 2.0), Color(0.55, 0.38, 0.25))
	_add_block("StandTable", Vector3(1.8, 0.9, 0.9), Vector3(3.0, 0.45, -3.0), Color(0.9, 0.9, 0.92))


func _build_props() -> void:
	var cake_mesh := CylinderMesh.new()
	cake_mesh.top_radius = 0.2
	cake_mesh.bottom_radius = 0.2
	cake_mesh.height = 0.18
	var cake_material := StandardMaterial3D.new()
	cake_material.albedo_color = Color(0.95, 0.7, 0.8)
	cake_mesh.material = cake_material

	var prop := PickupProp.new()
	prop.name = "CakeSection01"
	prop.entity_id = &"cake_section_01"
	prop.kind = &"cake_section"
	prop.display_name = "cake section"
	prop.collision_layer = Layers.PROP
	prop.collision_mask = Layers.WORLD | Layers.PROP
	prop.position = Vector3(-3.0, 0.9, 2.0)
	var prop_visual := MeshInstance3D.new()
	prop_visual.mesh = cake_mesh
	prop.add_child(prop_visual)
	var prop_shape := CollisionShape3D.new()
	var cylinder := CylinderShape3D.new()
	cylinder.radius = 0.2
	cylinder.height = 0.18
	prop_shape.shape = cylinder
	prop.add_child(prop_shape)
	add_child(prop)
	service.register_prop(prop)

	var socket := PlacementSocket.new()
	socket.name = "DessertSocket"
	socket.entity_id = &"dessert_socket"
	socket.display_name = "the cake stand"
	socket.accepts_kind = &"cake_section"
	socket.snap_offset = Vector3(0.0, 0.11, 0.0)
	socket.ghost_mesh = cake_mesh
	socket.position = Vector3(3.0, 0.9, -3.0)
	add_child(socket)
	service.register_socket(socket)


func _spawn_player() -> void:
	player = Player.new()
	player.name = "Player"
	player.actor_id = &"local"
	player.position = Vector3(0.0, 0.05, 3.5)
	player.service = service
	add_child(player)
	service.register_actor(player)


func _add_block(block_name: String, size: Vector3, pos: Vector3, color: Color) -> void:
	var body := StaticBody3D.new()
	body.name = block_name
	body.position = pos
	body.collision_layer = Layers.WORLD
	body.collision_mask = 0
	var mesh := BoxMesh.new()
	mesh.size = size
	var material := StandardMaterial3D.new()
	material.albedo_color = color
	mesh.material = material
	var visual := MeshInstance3D.new()
	visual.mesh = mesh
	body.add_child(visual)
	var shape := CollisionShape3D.new()
	var box := BoxShape3D.new()
	box.size = size
	shape.shape = box
	body.add_child(shape)
	add_child(body)
