class_name Session
extends Node3D
## Session root. Builds the hotel (see VenueBuilder and VenueLayout), owns the
## roster of players, wires the interaction service, networking and HUD, and
## owns pause and restart. Solo pauses the tree on the menu; once online the
## game must keep running for everyone else.

signal local_result(request_id: int, ok: bool, reason: StringName)

const OFFLINE_ACTOR: StringName = &"peer_1"
const TASK_REFRESH: float = 0.2

var service: InteractionService
var net: NetSession
var hud: Hud
var coordinator: Coordinator
## The player this peer controls. Null while a client waits for the host.
var player: Player
var players: Dictionary[StringName, Player] = {}
var menu_open: bool = false

var _tasks: Array[Dictionary] = TaskBoard.catalog()
var _task_results: Array[Dictionary] = []
var _task_timer: float = 0.0


func _ready() -> void:
	service = InteractionService.new()
	service.name = "InteractionService"
	add_child(service)

	VenueBuilder.build(self)
	_build_cake()
	_build_table_signs()
	_build_coordinator()

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
	_refresh_tasks()
	_set_menu(false)


func _process(delta: float) -> void:
	_task_timer += delta
	if _task_timer >= TASK_REFRESH:
		_task_timer = 0.0
		_refresh_tasks()


func spawn_position(slot: int) -> Vector3:
	return VenueLayout.spawn_position(slot)


func task_results() -> Array[Dictionary]:
	return _task_results


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
	coordinator.authoritative = false
	for prop: PickupProp in service.props():
		prop.set_mirror(true)
	apply_roster([])


func return_offline() -> void:
	service.authoritative = true
	coordinator.authoritative = true
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


func apply_snapshot(props: Array, poses: Array, npcs: Array) -> void:
	for item: Array in props:
		var prop: PickupProp = service.get_prop(StringName(str(item[0])))
		if prop != null:
			prop.apply_net_snapshot(int(item[1]), item[2])
	for item: Array in poses:
		var actor := StringName(str(item[0]))
		var other: Player = players.get(actor, null)
		if other != null and not other.is_local:
			other.net_set_target(item[1], float(item[2]), float(item[3]))
	for item: Array in npcs:
		if StringName(str(item[0])) == coordinator.entity_id and not coordinator.authoritative:
			coordinator.net_set_target(item[1], float(item[2]), str(item[3]))


func _refresh_tasks() -> void:
	_task_results = TaskBoard.evaluate(_tasks, service.occupancy())
	hud.set_tasks(_task_results)


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


# Props and sockets -------------------------------------------------------------

func _build_cake() -> void:
	var previous: StringName = &""
	for i: int in VenueLayout.CAKE_LAYERS.size():
		var layer: Dictionary = VenueLayout.CAKE_LAYERS[i]
		var radius: float = float(layer["radius"])
		var height: float = float(layer["height"])
		var mesh := CylinderMesh.new()
		mesh.top_radius = radius
		mesh.bottom_radius = radius
		mesh.height = height
		mesh.material = VenueBuilder.material(layer["color"], 0.45)
		var start := Vector3(-0.8 + 0.8 * float(i), VenueLayout.PREP_TOP + height * 0.5 + 0.005, VenueLayout.PREP_TABLE.z)
		var prop: PickupProp = _make_prop(StringName(layer["id"]), &"cake_tier", str(layer["name"]), mesh, height * 0.5, start)
		var shape := CylinderShape3D.new()
		shape.radius = radius
		shape.height = height
		_add_prop_shape(prop, shape)
		# A ring of piped icing around the top of each layer.
		var ring := MeshInstance3D.new()
		var torus := TorusMesh.new()
		torus.inner_radius = radius - 0.025
		torus.outer_radius = radius + 0.005
		torus.material = VenueBuilder.material(Color(1.0, 1.0, 1.0), 0.3)
		ring.mesh = torus
		ring.position = Vector3(0.0, height * 0.5, 0.0)
		prop.add_child(ring)

		var socket := PlacementSocket.new()
		socket.name = "CakeSocket%d" % i
		socket.entity_id = VenueLayout.CAKE_SOCKETS[i]
		socket.display_name = "the cake stand" if i == 0 else "the cake"
		socket.accepts_kind = &"cake_tier"
		socket.requires_socket = previous
		socket.pad_radius = radius
		socket.position = VenueLayout.cake_layer_position(i)
		add_child(socket)
		service.register_socket(socket)
		previous = socket.entity_id


func _build_table_signs() -> void:
	for i: int in 3:
		var mesh := BoxMesh.new()
		mesh.size = Vector3(0.3, 0.2, 0.04)
		mesh.material = VenueBuilder.material(Color(0.95, 0.78, 0.3), 0.35, 0.15)
		var start := Vector3(-5.9 + 0.4 * float(i), VenueLayout.SIGN_TABLE_TOP + 0.1 + 0.005, VenueLayout.SIGN_TABLE.z)
		var prop: PickupProp = _make_prop(VenueLayout.SIGN_IDS[i], &"table_sign", "sign for table %d" % (i + 1), mesh, 0.1, start)
		var shape := BoxShape3D.new()
		shape.size = mesh.size
		_add_prop_shape(prop, shape)
		for face: float in [1.0, -1.0]:
			var text := VenueBuilder.label(prop, "TABLE %d" % (i + 1), Vector3(0.0, 0.0, 0.022 * face), 0.0 if face > 0.0 else 180.0, 40, Color(0.25, 0.08, 0.05), 0)
			text.pixel_size = 0.0045

		var socket := PlacementSocket.new()
		socket.name = "TableSocket%d" % (i + 1)
		socket.entity_id = VenueLayout.TABLE_SOCKETS[i]
		socket.display_name = "table %d" % (i + 1)
		socket.accepts_kind = &"table_sign"
		socket.pad_radius = 0.16
		socket.position = VenueLayout.GUEST_TABLES[i] + Vector3(0.0, VenueLayout.TABLE_TOP, 0.0)
		add_child(socket)
		service.register_socket(socket)


func _build_coordinator() -> void:
	coordinator = Coordinator.new()
	coordinator.name = "Coordinator"
	add_child(coordinator)


func _make_prop(id: StringName, kind: StringName, display: String, mesh: Mesh, half_height: float, start: Vector3) -> PickupProp:
	var prop := PickupProp.new()
	prop.name = String(id).capitalize().replace(" ", "")
	prop.entity_id = id
	prop.kind = kind
	prop.display_name = display
	prop.half_height = half_height
	prop.visual_mesh = mesh
	prop.collision_layer = Layers.PROP
	prop.collision_mask = Layers.WORLD | Layers.PROP
	prop.position = start
	var visual := MeshInstance3D.new()
	visual.mesh = mesh
	prop.add_child(visual)
	add_child(prop)
	service.register_prop(prop)
	return prop


func _add_prop_shape(prop: PickupProp, shape: Shape3D) -> void:
	var collider := CollisionShape3D.new()
	collider.shape = shape
	prop.add_child(collider)
