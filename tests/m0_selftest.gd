extends Node
## Headless check of the M0 interaction rules. Run:
##   godot --headless --path . res://tests/m0_selftest.tscn
## Exit code 0 means every check passed. It cannot judge how the game feels.

const NEAR_PROP: Vector3 = Vector3(-3.0, 0.05, 3.2)
const NEAR_SOCKET: Vector3 = Vector3(3.0, 0.05, -1.8)
const FAR_AWAY: Vector3 = Vector3(3.0, 0.05, 3.2)

var _checks: int = 0
var _failures: int = 0
var _session: Session
var _player: Player
var _prop: PickupProp
var _socket: PlacementSocket


func _ready() -> void:
	get_tree().create_timer(60.0).timeout.connect(_on_watchdog)
	_run()


func _run() -> void:
	var packed := load("res://scenes/bootstrap/main.tscn") as PackedScene
	if packed == null:
		print("FAIL  main scene did not load")
		get_tree().quit(1)
		return
	_session = packed.instantiate() as Session
	add_child(_session)
	await get_tree().process_frame
	await get_tree().physics_frame
	_player = _session.player
	_prop = _session.get_node("CakeSection01") as PickupProp
	_socket = _session.get_node("DessertSocket") as PlacementSocket
	var service: InteractionService = _session.service

	_check("scene builds one prop, one socket, one player", _prop != null and _socket != null and _player != null)
	_check("prop starts free at revision 0", _prop.holder_id == &"" and _prop.revision == 0)

	# Rejections
	_player.global_position = FAR_AWAY
	_check("pickup from far away is rejected as out_of_range",
		_reason(service, _req(&"peer_1", InteractionService.PICKUP, &"cake_section_01", 0)) == InteractionService.ERR_OUT_OF_RANGE)
	_player.global_position = NEAR_PROP
	_check("pickup with a stale revision is rejected",
		_reason(service, _req(&"peer_1", InteractionService.PICKUP, &"cake_section_01", 99)) == InteractionService.ERR_STALE)
	_check("pickup of an unknown id is rejected",
		_reason(service, _req(&"peer_1", InteractionService.PICKUP, &"nope", 0)) == InteractionService.ERR_NO_TARGET)
	_check("pickup by an unknown actor is rejected",
		_reason(service, _req(&"ghost", InteractionService.PICKUP, &"cake_section_01", 0)) == InteractionService.ERR_UNKNOWN_ACTOR)
	_check("place with empty hands is rejected",
		_reason(service, _req(&"peer_1", InteractionService.PLACE, &"dessert_socket", -1)) == InteractionService.ERR_NOTHING_HELD)
	_check("validate() alone changes nothing", _prop.holder_id == &"" and _prop.revision == 0)

	# Pickup and follow
	var result: Dictionary = service.request(_req(&"peer_1", InteractionService.PICKUP, &"cake_section_01", 0))
	_check("pickup in reach succeeds", result["ok"])
	_check("prop and player agree on who holds it", _prop.holder_id == &"peer_1" and _player.held_prop_id == &"cake_section_01")
	await get_tree().physics_frame
	await get_tree().physics_frame
	_check("held prop follows the hold point",
		_prop.global_position.distance_to(_player.hold_point.global_position) < 0.05)
	_check("second pickup with full hands is rejected",
		_reason(service, _req(&"peer_1", InteractionService.PICKUP, &"cake_section_01", _prop.revision)) == InteractionService.ERR_HANDS_FULL)

	# Contention: a second actor cannot take a held prop
	var other := Player.new()
	other.setup(&"other", 2, 1, false)
	other.position = NEAR_PROP
	other.service = service
	_session.add_child(other)
	service.register_actor(other)
	var contested: Dictionary = service.request(_req(&"other", InteractionService.PICKUP, &"cake_section_01", _prop.revision))
	_check("a second actor cannot take a prop that is held", not contested["ok"] and _prop.holder_id == &"peer_1")

	# Placement
	_player.global_position = FAR_AWAY
	_check("place out of reach is rejected",
		_reason(service, _req(&"peer_1", InteractionService.PLACE, &"dessert_socket", -1)) == InteractionService.ERR_OUT_OF_RANGE)
	_player.global_position = NEAR_SOCKET
	result = service.request(_req(&"peer_1", InteractionService.PLACE, &"dessert_socket", -1))
	_check("place in reach succeeds", result["ok"])
	_check("placed prop sits on the socket",
		_prop.global_position.distance_to(_socket.snap_transform().origin) < 0.01 and _prop.socket_id == &"dessert_socket")
	_check("socket records its occupant", _socket.occupant_id == &"cake_section_01")
	for i: int in 5:
		await get_tree().physics_frame
	_check("placed prop is still on the socket 5 frames later",
		_prop.global_position.distance_to(_socket.snap_transform().origin) < 0.01)
	print("      prop at ", _prop.global_position, " socket snap at ", _socket.snap_transform().origin)
	_check("player hands are empty after placing", _player.held_prop_id == &"")

	# Pickup from the socket frees it; then drop
	result = service.request(_req(&"peer_1", InteractionService.PICKUP, &"cake_section_01", _prop.revision))
	_check("a placed prop can be picked up again", result["ok"] and _socket.occupant_id == &"")
	result = service.request(_req(&"peer_1", InteractionService.DROP, &"", -1))
	_check("drop succeeds and the prop becomes physical", result["ok"] and not _prop.freeze and _prop.holder_id == &"")
	for i: int in 120:
		await get_tree().physics_frame
	_check("dropped prop settles above the floor", _prop.global_position.y > -0.5)

	# Reset
	service.reset_all()
	for i: int in 3:
		await get_tree().physics_frame
	_check("reset returns the prop to its start", _prop.global_position.distance_to(Vector3(-3.0, 0.9, 2.0)) < 0.05)
	_check("reset clears hands and socket", _player.held_prop_id == &"" and _socket.occupant_id == &"")

	# Repeat the whole cycle 20 times with no duplication
	var cycles_ok: bool = true
	for i: int in 20:
		service.reset_all()
		for j: int in 3:
			await get_tree().physics_frame
		_check("cycle %d: reset puts the prop back at its start" % i,
			_prop.global_position.distance_to(Vector3(-3.0, 0.9, 2.0)) < 0.05)
		_player.global_position = NEAR_PROP
		cycles_ok = cycles_ok and bool(service.request(_req(&"peer_1", InteractionService.PICKUP, &"cake_section_01", _prop.revision))["ok"])
		_player.global_position = NEAR_SOCKET
		cycles_ok = cycles_ok and bool(service.request(_req(&"peer_1", InteractionService.PLACE, &"dessert_socket", -1))["ok"])
		cycles_ok = cycles_ok and bool(service.request(_req(&"peer_1", InteractionService.PICKUP, &"cake_section_01", _prop.revision))["ok"])
		cycles_ok = cycles_ok and bool(service.request(_req(&"peer_1", InteractionService.DROP, &"", -1))["ok"])
		await get_tree().physics_frame
	_check("20 pickup/place/pickup/drop cycles all succeed", cycles_ok)
	_check("still exactly one prop in the world", _session.find_children("*", "PickupProp", true, false).size() == 1)

	print("")
	print("%d checks, %d failed" % [_checks, _failures])
	get_tree().quit(1 if _failures > 0 else 0)


func _req(actor: StringName, action: StringName, target: StringName, revision: int) -> Dictionary:
	return {"request_id": 0, "actor_id": actor, "action": action, "target_id": target, "expected_revision": revision}


func _reason(service: InteractionService, req: Dictionary) -> StringName:
	return service.validate(req)


func _check(label: String, condition: bool) -> void:
	_checks += 1
	if condition:
		print("PASS  ", label)
	else:
		_failures += 1
		print("FAIL  ", label)


func _on_watchdog() -> void:
	print("FAIL  watchdog: self-test did not finish in 60 s")
	get_tree().quit(2)
