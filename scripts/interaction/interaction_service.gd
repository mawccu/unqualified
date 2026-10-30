class_name InteractionService
extends Node
## The single path every world mutation takes. Offline play and, later, remote
## peers send the same request dictionary, so the host can validate intents
## instead of trusting clients. In M1 the actor id must come from the peer
## mapping, never from the request body.

signal request_resolved(request_id: int, ok: bool, reason: StringName)

const MAX_REACH: float = 3.6

const PICKUP: StringName = &"pickup"
const PLACE: StringName = &"place"
const DROP: StringName = &"drop"

const OK: StringName = &"ok"
const ERR_UNKNOWN_ACTOR: StringName = &"unknown_actor"
const ERR_UNKNOWN_ACTION: StringName = &"unknown_action"
const ERR_NO_TARGET: StringName = &"no_target"
const ERR_HANDS_FULL: StringName = &"hands_full"
const ERR_NOTHING_HELD: StringName = &"nothing_held"
const ERR_ALREADY_HELD: StringName = &"already_held"
const ERR_OUT_OF_RANGE: StringName = &"out_of_range"
const ERR_STALE: StringName = &"stale_revision"
const ERR_SOCKET_TAKEN: StringName = &"socket_taken"
const ERR_SOCKET_REJECTS: StringName = &"socket_rejects"

## False on a client: the host owns the truth, so this copy never drops or
## releases props by itself. It only validates, for prompts.
var authoritative: bool = true

var _actors: Dictionary[StringName, Player] = {}
var _props: Dictionary[StringName, PickupProp] = {}
var _sockets: Dictionary[StringName, PlacementSocket] = {}


func register_actor(actor: Player) -> void:
	_actors[actor.actor_id] = actor


## A player left. On the authority, whatever they held is dropped where it is,
## so it can be picked up again.
func unregister_actor(actor_id: StringName) -> void:
	var actor: Player = _actors.get(actor_id, null)
	if actor == null:
		return
	if authoritative:
		var held: PickupProp = _props.get(actor.held_prop_id, null)
		if held != null:
			held.release(Vector3.ZERO)
	actor.held_prop_id = &""
	_actors.erase(actor_id)


func props() -> Array[PickupProp]:
	var out: Array[PickupProp] = []
	out.assign(_props.values())
	return out


func get_prop(prop_id: StringName) -> PickupProp:
	return _props.get(prop_id, null)


func get_socket(socket_id: StringName) -> PlacementSocket:
	return _sockets.get(socket_id, null)


func register_prop(prop: PickupProp) -> void:
	_props[prop.entity_id] = prop


func register_socket(socket: PlacementSocket) -> void:
	_sockets[socket.entity_id] = socket


## Checks a request without changing anything. Returns OK or an ERR_ code.
func validate(req: Dictionary) -> StringName:
	var actor: Player = _actors.get(req.get("actor_id", &""), null)
	if actor == null:
		return ERR_UNKNOWN_ACTOR
	match req.get("action", &""):
		PICKUP:
			return _validate_pickup(actor, req)
		PLACE:
			return _validate_place(actor, req)
		DROP:
			return _validate_drop(actor)
	return ERR_UNKNOWN_ACTION


## Validates, then applies. Returns {"ok": bool, "reason": StringName}.
func request(req: Dictionary) -> Dictionary:
	var reason: StringName = validate(req)
	if reason == OK:
		_apply(req)
	request_resolved.emit(int(req.get("request_id", 0)), reason == OK, reason)
	return {"ok": reason == OK, "reason": reason}


## Puts every prop and socket back to its starting state and empties every
## actor's hands. It does not move players: each peer moves its own player.
func reset_all() -> void:
	for actor: Player in _actors.values():
		actor.held_prop_id = &""
	for socket: PlacementSocket in _sockets.values():
		socket.occupant_id = &""
		socket.set_preview(false)
	for prop: PickupProp in _props.values():
		prop.reset_to_start()


func _validate_pickup(actor: Player, req: Dictionary) -> StringName:
	var prop: PickupProp = _props.get(req.get("target_id", &""), null)
	if prop == null:
		return ERR_NO_TARGET
	if actor.held_prop_id != &"":
		return ERR_HANDS_FULL
	if prop.holder_id != &"":
		return ERR_ALREADY_HELD
	if not _in_reach(actor, prop.global_position):
		return ERR_OUT_OF_RANGE
	if int(req.get("expected_revision", -1)) != prop.revision:
		return ERR_STALE
	return OK


func _validate_place(actor: Player, req: Dictionary) -> StringName:
	var socket: PlacementSocket = _sockets.get(req.get("target_id", &""), null)
	if socket == null:
		return ERR_NO_TARGET
	var held: PickupProp = _props.get(actor.held_prop_id, null)
	if held == null:
		return ERR_NOTHING_HELD
	if socket.occupant_id != &"":
		return ERR_SOCKET_TAKEN
	if not socket.accepts(held):
		return ERR_SOCKET_REJECTS
	if not _in_reach(actor, socket.global_position):
		return ERR_OUT_OF_RANGE
	return OK


func _validate_drop(actor: Player) -> StringName:
	if not _props.has(actor.held_prop_id):
		return ERR_NOTHING_HELD
	return OK


func _apply(req: Dictionary) -> void:
	var actor: Player = _actors[req["actor_id"]]
	match req["action"]:
		PICKUP:
			var prop: PickupProp = _props[req["target_id"]]
			if prop.socket_id != &"":
				_sockets[prop.socket_id].occupant_id = &""
			prop.begin_hold(actor.hold_point, actor.actor_id)
			actor.held_prop_id = prop.entity_id
		PLACE:
			var held: PickupProp = _props[actor.held_prop_id]
			var socket: PlacementSocket = _sockets[req["target_id"]]
			socket.occupant_id = held.entity_id
			held.place_at(socket.snap_transform(), socket.entity_id)
			actor.held_prop_id = &""
		DROP:
			var dropped: PickupProp = _props[actor.held_prop_id]
			dropped.release(actor.drop_velocity())
			actor.held_prop_id = &""


func _in_reach(actor: Player, point: Vector3) -> bool:
	return actor.eye_position().distance_to(point) <= MAX_REACH
