class_name NetSession
extends Node
## Host-authoritative networking over ENet. The host runs InteractionService
## and owns props, ownership and the clock. A client sends intents (request,
## its own pose) and displays what the host sends back (roster, prop state,
## snapshots). Peer identity comes from the connection, never from a payload.
##
## M1 compromise: player movement is reported by each client and only
## sanity-checked by the host (speed and room bounds). The host does not
## correct a client that lies about where it is; it just refuses to act on a
## position it has not accepted. Full host-authoritative movement is later.

signal status_changed(text: String)
signal connection_failed
signal server_lost

enum Mode { OFFLINE, HOST, CLIENT }

const DEFAULT_PORT: int = 24680
const MAX_PLAYERS: int = 4
const SEND_INTERVAL: float = 0.05
const SPEED_SLACK: float = 1.5


const MAX_PITCH: float = 1.45
const LOG_LIMIT: int = 300

var mode: Mode = Mode.OFFLINE
var session: Session
var event_log: PackedStringArray = []

var _roster: Array = []
var _peer_actor: Dictionary[int, StringName] = {}
var _last_pose_msec: Dictionary[int, int] = {}
var _send_accum: float = 0.0


func _ready() -> void:
	multiplayer.peer_connected.connect(_on_peer_connected)
	multiplayer.peer_disconnected.connect(_on_peer_disconnected)
	multiplayer.connected_to_server.connect(_on_connected_to_server)
	multiplayer.connection_failed.connect(_on_connection_failed)
	multiplayer.server_disconnected.connect(_on_server_disconnected)


func host(port: int = DEFAULT_PORT, bind_ip: String = "*") -> Error:
	if mode != Mode.OFFLINE:
		return ERR_ALREADY_IN_USE
	var peer := ENetMultiplayerPeer.new()
	peer.set_bind_ip(bind_ip)
	var err: Error = peer.create_server(port, MAX_PLAYERS - 1)
	if err != OK:
		log_event("could not host on port %d (error %d)" % [port, err])
		return err
	multiplayer.multiplayer_peer = peer
	mode = Mode.HOST
	_peer_actor.clear()
	_last_pose_msec.clear()
	_peer_actor[1] = session.player.actor_id
	_roster = [_entry(session.player.actor_id, 1, 0)]
	for prop: PickupProp in session.service.props():
		prop.state_changed.connect(_on_prop_state_changed)
	log_event("hosting on port %d (bind %s)" % [port, bind_ip])
	_emit_status()
	return OK


func join(address: String, port: int = DEFAULT_PORT) -> Error:
	if mode != Mode.OFFLINE:
		return ERR_ALREADY_IN_USE
	var peer := ENetMultiplayerPeer.new()
	var err: Error = peer.create_client(address, port)
	if err != OK:
		log_event("could not start connecting to %s:%d (error %d)" % [address, port, err])
		return err
	multiplayer.multiplayer_peer = peer
	mode = Mode.CLIENT
	session.prepare_client()
	log_event("connecting to %s:%d" % [address, port])
	_emit_status()
	return OK


## Back to offline, from either role.
func leave() -> void:
	if mode == Mode.OFFLINE:
		return
	if mode == Mode.HOST:
		for prop: PickupProp in session.service.props():
			if prop.state_changed.is_connected(_on_prop_state_changed):
				prop.state_changed.disconnect(_on_prop_state_changed)
	var peer := multiplayer.multiplayer_peer as ENetMultiplayerPeer
	if peer != null:
		peer.close()
	multiplayer.multiplayer_peer = OfflineMultiplayerPeer.new()
	mode = Mode.OFFLINE
	_peer_actor.clear()
	_last_pose_msec.clear()
	_roster = []
	log_event("left the session")
	session.return_offline()
	_emit_status()


func send_request(req: Dictionary) -> void:
	if not _is_connected():
		return
	rpc_request.rpc_id(1, int(req["request_id"]), req["action"], req["target_id"], int(req["expected_revision"]))


func broadcast_restart() -> void:
	if mode != Mode.HOST:
		return
	for id: int in _client_ids():
		rpc_restart.rpc_id(id)


func log_event(text: String) -> void:
	var line: String = "NET %.2f %s" % [Time.get_ticks_msec() / 1000.0, text]
	print(line)
	event_log.append(line)
	if event_log.size() > LOG_LIMIT:
		event_log.remove_at(0)


func status_text() -> String:
	match mode:
		Mode.HOST:
			return "Hosting: %d of %d players" % [_roster.size(), MAX_PLAYERS]
		Mode.CLIENT:
			if _is_connected():
				return "Connected: %d players" % session.players.size()
			return "Connecting..."
	return "Offline"


func _physics_process(delta: float) -> void:
	if mode == Mode.OFFLINE:
		return
	_send_accum += delta
	if _send_accum < SEND_INTERVAL:
		return
	_send_accum = 0.0
	if mode == Mode.HOST:
		_broadcast_snapshot()
	elif _is_connected() and session.player != null:
		var pose: Array = session.player.sync_pose()
		rpc_pose.rpc_id(1, pose[0], pose[1], pose[2])


# Host side ------------------------------------------------------------------

func _on_peer_connected(id: int) -> void:
	if mode != Mode.HOST:
		return
	var slot: int = _free_slot()
	var actor := StringName("peer_%d" % id)
	_peer_actor[id] = actor
	# Notice a vanished client in about 3 to 8 seconds instead of ENet's default 5 to 30.
	var enet := multiplayer.multiplayer_peer as ENetMultiplayerPeer
	if enet != null and enet.get_peer(id) != null:
		enet.get_peer(id).set_timeout(32, 3000, 8000)
	_roster.append(_entry(actor, id, slot))
	session.apply_roster(_roster)
	_send_roster()
	for prop: PickupProp in session.service.props():
		rpc_prop_state.rpc_id(id, prop.entity_id, prop.holder_id, prop.socket_id, prop.revision, prop.net_pose())
	log_event("peer %d joined as %s in slot %d (players=%d)" % [id, actor, slot, _roster.size()])
	_emit_status()


func _on_peer_disconnected(id: int) -> void:
	if mode != Mode.HOST:
		return
	var actor: StringName = _peer_actor.get(id, &"")
	_peer_actor.erase(id)
	_last_pose_msec.erase(id)
	if actor == &"":
		return
	var holding: String = ""
	var player: Player = session.players.get(actor, null)
	if player != null and player.held_prop_id != &"":
		holding = String(player.held_prop_id)
	_roster = _roster.filter(func(e: Dictionary) -> bool: return e["actor_id"] != actor)
	session.apply_roster(_roster)
	_send_roster()
	log_event("peer %d (%s) left; was holding '%s' (players=%d)" % [id, actor, holding, _roster.size()])
	for prop: PickupProp in session.service.props():
		log_event("prop %s holder='%s' socket='%s' rev=%d" % [prop.entity_id, prop.holder_id, prop.socket_id, prop.revision])
	_emit_status()


func _on_prop_state_changed(prop: PickupProp) -> void:
	if mode != Mode.HOST:
		return
	for id: int in _client_ids():
		rpc_prop_state.rpc_id(id, prop.entity_id, prop.holder_id, prop.socket_id, prop.revision, prop.net_pose())


func _broadcast_snapshot() -> void:
	var ids: Array[int] = _client_ids()
	if ids.is_empty():
		return
	var props: Array = []
	for prop: PickupProp in session.service.props():
		if prop.holder_id == &"" and prop.socket_id == &"":
			props.append([prop.entity_id, prop.revision, prop.net_pose()])
	var poses: Array = []
	for actor: StringName in session.players:
		var pose: Array = session.players[actor].sync_pose()
		poses.append([actor, pose[0], pose[1], pose[2]])
	var npcs: Array = []
	var npc_pose: Array = session.coordinator.sync_pose()
	npcs.append([session.coordinator.entity_id, npc_pose[0], npc_pose[1], npc_pose[2]])
	for id: int in ids:
		rpc_snapshot.rpc_id(id, props, poses, npcs)


## Peers that are fully connected right now. Sending to one that is mid-disconnect logs an engine error.
func _client_ids() -> Array[int]:
	var ids: Array[int] = []
	var enet := multiplayer.multiplayer_peer as ENetMultiplayerPeer
	if enet == null:
		return ids
	for id: int in multiplayer.get_peers():
		var packet_peer: ENetPacketPeer = enet.get_peer(id)
		if packet_peer != null and packet_peer.get_state() == ENetPacketPeer.STATE_CONNECTED:
			ids.append(id)
	return ids


func _free_slot() -> int:
	var used: Array = _roster.map(func(e: Dictionary) -> int: return int(e["slot"]))
	for slot: int in MAX_PLAYERS:
		if not used.has(slot):
			return slot
	return MAX_PLAYERS - 1


func _send_roster() -> void:
	for id: int in _client_ids():
		rpc_roster.rpc_id(id, _roster)


func _entry(actor: StringName, peer: int, slot: int) -> Dictionary:
	return {"actor_id": actor, "peer_id": peer, "slot": slot}


# Client side ----------------------------------------------------------------

func _on_connected_to_server() -> void:
	log_event("connected to host as peer %d" % multiplayer.get_unique_id())
	_emit_status()


func _on_connection_failed() -> void:
	log_event("connection failed")
	leave()
	connection_failed.emit()


func _on_server_disconnected() -> void:
	log_event("host disconnected")
	leave()
	server_lost.emit()


func _is_connected() -> bool:
	var peer: MultiplayerPeer = multiplayer.multiplayer_peer
	return peer != null and peer.get_connection_status() == MultiplayerPeer.CONNECTION_CONNECTED


func _emit_status() -> void:
	status_changed.emit(status_text())


# Messages -------------------------------------------------------------------

@rpc("authority", "call_remote", "reliable")
func rpc_roster(entries: Array) -> void:
	session.apply_roster(entries)
	log_event("roster received: %d players" % entries.size())
	_emit_status()


@rpc("authority", "call_remote", "reliable")
func rpc_prop_state(prop_id: StringName, holder: StringName, socket: StringName, rev: int, pose: Transform3D) -> void:
	session.apply_prop_state(prop_id, holder, socket, rev, pose)


@rpc("authority", "call_remote", "unreliable_ordered")
func rpc_snapshot(props: Array, poses: Array, npcs: Array) -> void:
	session.apply_snapshot(props, poses, npcs)


@rpc("authority", "call_remote", "reliable")
func rpc_result(request_id: int, ok: bool, reason: StringName) -> void:
	session.on_remote_result(request_id, ok, reason)


@rpc("authority", "call_remote", "reliable")
func rpc_restart() -> void:
	if session.player != null:
		session.player.reset()


## Client intent. The actor is the connection that sent it.
@rpc("any_peer", "call_remote", "reliable")
func rpc_request(request_id: int, action: StringName, target_id: StringName, expected_revision: int) -> void:
	if mode != Mode.HOST:
		return
	var sender: int = multiplayer.get_remote_sender_id()
	var actor: StringName = _peer_actor.get(sender, &"")
	if actor == &"":
		return
	var req: Dictionary = {
		"request_id": request_id,
		"actor_id": actor,
		"action": action,
		"target_id": target_id,
		"expected_revision": expected_revision,
	}
	var result: Dictionary = session.service.request(req)
	log_event("request %d from %s: %s %s -> %s" % [request_id, actor, action, target_id, result["reason"]])
	rpc_result.rpc_id(sender, request_id, result["ok"], result["reason"])


## Client pose report. Clamped to a believable speed and to the room.
@rpc("any_peer", "call_remote", "unreliable_ordered")
func rpc_pose(pos: Vector3, yaw: float, pitch: float) -> void:
	if mode != Mode.HOST:
		return
	var sender: int = multiplayer.get_remote_sender_id()
	var player: Player = session.players.get(_peer_actor.get(sender, &""), null)
	if player == null or not pos.is_finite() or not is_finite(yaw) or not is_finite(pitch):
		return
	var now: int = Time.get_ticks_msec()
	var elapsed: float = clampf((now - int(_last_pose_msec.get(sender, now - 50))) / 1000.0, 0.02, 0.5)
	_last_pose_msec[sender] = now
	var from: Vector3 = player.net_position()
	var step: Vector3 = pos - from
	var max_step: float = Player.SPRINT_SPEED * SPEED_SLACK * elapsed
	if step.length() > max_step:
		pos = from + step.normalized() * max_step
	pos.x = clampf(pos.x, VenueLayout.BOUNDS_MIN.x, VenueLayout.BOUNDS_MAX.x)
	pos.z = clampf(pos.z, VenueLayout.BOUNDS_MIN.y, VenueLayout.BOUNDS_MAX.y)
	pos.y = clampf(pos.y, -0.5, 2.0)
	player.net_set_target(pos, yaw, clampf(pitch, -MAX_PITCH, MAX_PITCH))
