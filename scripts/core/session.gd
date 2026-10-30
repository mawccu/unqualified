class_name Session
extends Node3D
## Session root. Builds the graybox room in code (no editor needed yet), owns
## the roster of players, wires the interaction service, networking and HUD,
## and owns pause and restart. Solo pauses the tree on the menu; once online
## the game must keep running for everyone else.

signal local_result(request_id: int, ok: bool, reason: StringName)

const OFFLINE_ACTOR: StringName = &"peer_1"
const SPAWN_ORIGIN: Vector3 = Vector3(-3.9, 0.05, 3.4)
const SPAWN_SPACING: float = 0.8

var service: InteractionService
var net: NetSession
var hud: Hud
## The player this peer controls. Null while a client waits for the host.
var player: Player
var players: Dictionary[StringName, Player] = {}
var menu_open: bool = false


func _ready() -> void:
	service = InteractionService.new()
	service.name = "InteractionService"
	add_child(service)

	_build_environment()
	_build_room()
	_build_props()

	net = NetSession.new()
	net.name = "Net"
	net.session = self
	add_child(net)

	hud = Hud.new()
	hud.name = "Hud"
	add_child(hud)
	hud.pause_toggled.connect(func() -> void: _set_menu(not menu_open))
	hud.resume_requested.connect(func() -> void: _set_menu(false))
	hud.restart_requested.connect(restart)
	hud.quit_requested.connect(func() -> void: get_tree().quit())
	hud.host_requested.connect(func() -> void: start_host())
	hud.join_requested.connect(func(address: String) -> void: start_join(address))
	hud.leave_requested.connect(func() -> void: net.leave())
	net.status_changed.connect(_on_net_status)
	net.connection_failed.connect(func() -> void: hud.show_toast("Could not connect."))
	net.server_lost.connect(func() -> void: hud.show_toast("The host left."))
	get_window().focus_exited.connect(_on_focus_lost)

	_offline_roster()
	_on_net_status(net.status_text())
	_set_menu(false)


func spawn_position(slot: int) -> Vector3:
	return SPAWN_ORIGIN + Vector3(SPAWN_SPACING * slot, 0.0, 0.0)


func start_host(port: int = NetSession.DEFAULT_PORT, bind_ip: String = "*") -> Error:
	return net.host(port, bind_ip)


func start_join(address: String, port: int = NetSession.DEFAULT_PORT) -> Error:
	return net.join(address, port)


## Local player intent. Offline and host apply it straight away; a client
## sends it to the host, which answers later.
func submit(req: Dictionary) -> void:
	if player == null:
		return
	if net.mode == NetSession.Mode.CLIENT:
		net.send_request(req)
		return
	var result: Dictionary = service.request(req)
	local_result.emit(int(req["request_id"]), result["ok"], result["reason"])
	player.report_result(result)


func on_remote_result(request_id: int, ok: bool, reason: StringName) -> void:
	local_result.emit(request_id, ok, reason)
	if player != null:
		player.report_result({"ok": ok, "reason": reason})


func restart() -> void:
	if net.mode == NetSession.Mode.CLIENT:
		return
	service.reset_all()
	if player != null:
		player.reset()
	net.broadcast_restart()
	_set_menu(false)


## Make the set of players match the host's list.
func apply_roster(entries: Array) -> void:
	var wanted: Dictionary[StringName, Dictionary] = {}
	for entry: Dictionary in entries:
		wanted[StringName(str(entry["actor_id"]))] = entry
	for actor: StringName in players.keys():
		if not wanted.has(actor):
			_remove_player(actor)
	for actor: StringName in wanted:
		if not players.has(actor):
			_add_player(actor, wanted[actor])


## A client is about to wait for the host's roster.
func prepare_client() -> void:
	service.authoritative = false
	for prop: PickupProp in service.props():
		prop.set_mirror(true)
	apply_roster([])


func return_offline() -> void:
	service.authoritative = true
	for prop: PickupProp in service.props():
		prop.set_mirror(false)
	apply_roster([])
	service.reset_all()
	_offline_roster()
	_refresh_pause()


func apply_prop_state(prop_id: StringName, holder: StringName, socket: StringName, rev: int, pose: Transform3D) -> void:
	var prop: PickupProp = service.get_prop(prop_id)
	if prop == null:
		return
	var old_holder: Player = players.get(prop.holder_id, null)
	if old_holder != null and old_holder.held_prop_id == prop_id:
		old_holder.held_prop_id = &""
	var old_socket: PlacementSocket = service.get_socket(prop.socket_id)
	if old_socket != null and old_socket.occupant_id == prop_id:
		old_socket.occupant_id = &""
	var hold_target: Node3D = null
	var effective_holder: StringName = holder
	if holder != &"":
		var holder_player: Player = players.get(holder, null)
		if holder_player == null:
			effective_holder = &""
		else:
			hold_target = holder_player.hold_point
			holder_player.held_prop_id = prop_id
	var new_socket: PlacementSocket = service.get_socket(socket)
	if new_socket != null:
		new_socket.occupant_id = prop_id
	prop.apply_net_state(effective_holder, socket, rev, pose, hold_target)


func apply_snapshot(props: Array, poses: Array) -> void:
	for item: Array in props:
		var prop: PickupProp = service.get_prop(StringName(str(item[0])))
		if prop != null:
			prop.apply_net_snapshot(int(item[1]), item[2])
	for item: Array in poses:
		var actor := StringName(str(item[0]))
		var other: Player = players.get(actor, null)
		if other != null and not other.is_local:
			other.net_set_target(item[1], float(item[2]), float(item[3]))


func _offline_roster() -> void:
	apply_roster([{"actor_id": OFFLINE_ACTOR, "peer_id": 1, "slot": 0}])


func _add_player(actor: StringName, entry: Dictionary) -> void:
	var peer_id: int = int(entry["peer_id"])
	var slot: int = int(entry["slot"])
	var is_local: bool = peer_id == multiplayer.get_unique_id()
	var created := Player.new()
	created.name = "Player_%s" % actor
	created.setup(actor, peer_id, slot, is_local)
	created.position = spawn_position(slot)
	created.service = service
	if is_local:
		created.submit_request = submit
		created.controls_enabled = not menu_open
		created.prompt_changed.connect(hud.set_prompt)
		created.feedback.connect(hud.show_toast)
		player = created
	add_child(created)
	players[actor] = created
	service.register_actor(created)


func _remove_player(actor: StringName) -> void:
	var gone: Player = players.get(actor, null)
	service.unregister_actor(actor)
	players.erase(actor)
	if gone != null:
		if gone == player:
			player = null
		gone.queue_free()


func _on_net_status(text: String) -> void:
	hud.set_net_ui(net.mode, text)
	_refresh_pause()


func _refresh_pause() -> void:
	get_tree().paused = menu_open and net.mode == NetSession.Mode.OFFLINE


func _set_menu(open: bool) -> void:
	if menu_open and not open:
		Settings.save()
	menu_open = open
	_refresh_pause()
	if player != null:
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
