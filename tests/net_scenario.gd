extends Node
## One player of a multi-process network test. The launcher starts one Godot
## process per player and passes options after "--":
##   --role=host|client --name=A --port=24700 --expect=3 --scenario=contend
##   --t0=<unix seconds> --rounds=24 --period=1.5 --host_contends=0|1
##   --how=leave|kill --timeout=45
## Every line it prints starts with "T <name> " so the launcher can parse it.

var _a: Dictionary = {}
var _name: String = "x"
var _is_host: bool = false
var _session: Session
var _prop: PickupProp
var _results: Dictionary = {}


func _ready() -> void:
	for arg: String in OS.get_cmdline_user_args():
		if arg.begins_with("--") and arg.contains("="):
			var pair: PackedStringArray = arg.substr(2).split("=", true, 1)
			_a[pair[0]] = pair[1]
	_name = str(_a.get("name", "x"))
	_is_host = str(_a.get("role", "")) == "host"
	get_tree().create_timer(float(_a.get("timeout", "45"))).timeout.connect(_on_timeout)
	_run()


func _on_timeout() -> void:
	_log("TIMEOUT")
	get_tree().quit(2)


func _log(text: String) -> void:
	print("T %s %s" % [_name, text])


func _run() -> void:
	var packed := load("res://scenes/bootstrap/main.tscn") as PackedScene
	_session = packed.instantiate() as Session
	add_child(_session)
	await get_tree().process_frame
	await get_tree().process_frame
	_prop = _session.service.get_prop(&"cake_section_01")
	_session.local_result.connect(func(id: int, ok: bool, reason: StringName) -> void: _results[id] = [ok, reason])

	var port: int = int(_a.get("port", "24700"))
	var err: Error = OK
	if _is_host:
		err = _session.start_host(port, "127.0.0.1")
	else:
		err = _session.start_join("127.0.0.1", port)
	if err != OK:
		_log("FATAL could not start, error %d" % err)
		get_tree().quit(1)
		return

	var expect: int = int(_a.get("expect", "2"))
	var joined: bool = await _wait_until(
		func() -> bool: return _session.player != null and _session.players.size() >= expect, 20.0)
	_log("JOINED ok=%s players=%d me=%s" % [joined, _session.players.size(), _my_id()])

	match str(_a.get("scenario", "lobby")):
		"lobby":
			await _lobby()
		"contend":
			await _contend()
		"disconnect":
			await _disconnect()
		"visual":
			await _visual()
		"far":
			await _far()
	await _sleep(1.0)
	_log("DONE")
	get_tree().quit(0)


func _lobby() -> void:
	var parts: PackedStringArray = []
	for actor: StringName in _session.players:
		var other: Player = _session.players[actor]
		parts.append("%s:slot%d:local=%s" % [actor, other.slot, other.is_local])
	_log("ROSTER %s" % ",".join(parts))
	# Clients leave one at a time: three peers vanishing in the same instant makes
	# the engine log errors about sending to peers that are already gone.
	var stagger: float = float("ABC".find(_name)) * 0.8 if not _is_host else 0.0
	await _sleep(5.0 if _is_host else 2.5 + stagger)


func _contend() -> void:
	var t0: float = float(_a["t0"])
	var period: float = float(_a.get("period", "1.5"))
	var rounds: int = int(_a.get("rounds", "24"))
	var attempts: bool = (not _is_host) or str(_a.get("host_contends", "0")) == "1"
	for r: int in rounds:
		var boundary: float = t0 + r * period
		if Time.get_unix_time_from_system() > boundary + 0.1:
			_log("ROUND %d SKIPPED" % r)
			continue
		await _sleep_until(boundary)
		if attempts:
			_session.submit(_pickup(1000 + r))
		await _sleep_until(boundary + 0.8)
		if attempts:
			var res: Array = _results.get(1000 + r, [false, &"no_answer"])
			_log("ROUND %d ATTEMPT me=%s ok=%s reason=%s" % [r, _my_id(), res[0], res[1]])
		if _is_host:
			_log("ROUND %d HOLDER %s rev=%d" % [r, _prop.holder_id, _prop.revision])
		await _sleep_until(boundary + 1.0)
		if _is_host:
			_session.service.reset_all()


func _disconnect() -> void:
	var t0: float = float(_a["t0"])
	var kill: bool = str(_a.get("how", "leave")) == "kill"
	if _is_host:
		await _sleep_until(t0 + (16.0 if kill else 9.0))
		_log("HOST final prop holder='%s' rev=%d" % [_prop.holder_id, _prop.revision])
	elif _name == "B":
		await _sleep_until(t0)
		_session.submit(_pickup(1))
		await _sleep(0.6)
		var res: Array = _results.get(1, [false, &"no_answer"])
		_log("PICKUP ok=%s reason=%s holding=%s" % [res[0], res[1], _session.player.held_prop_id])
		await _sleep_until(t0 + 1.5)
		if kill:
			_log("KILLED while holding")
			OS.delay_msec(300)
			OS.kill(OS.get_process_id())
		else:
			_log("LEAVING while holding")
			_session.net.leave()
	else:
		var saw: bool = await _wait_until(func() -> bool: return _prop.holder_id != &"", 10.0)
		_log("SAW holder='%s' ok=%s" % [_prop.holder_id, saw])
		var started: float = Time.get_unix_time_from_system()
		var freed: bool = await _wait_until(func() -> bool: return _prop.holder_id == &"", 25.0)
		_log("FREED ok=%s after=%.1fs" % [freed, Time.get_unix_time_from_system() - started])
		_session.submit(_pickup(2))
		await _sleep(1.0)
		var res2: Array = _results.get(2, [false, &"no_answer"])
		_log("PICKUP_AFTER ok=%s reason=%s holder=%s" % [res2[0], res2[1], _prop.holder_id])


func _far() -> void:
	var t0: float = float(_a["t0"])
	if _name == "B":
		await _sleep_until(t0)
		var player: Player = _session.player
		while player.global_position.x < 5.5:
			player.global_position.x += 5.0 * get_physics_process_delta_time()
			await get_tree().physics_frame
		await _sleep(0.5)
		_log("WALKED to x=%.2f" % player.global_position.x)
		_session.submit(_pickup(1))
		await _sleep(0.6)
		var r1: Array = _results.get(1, [false, &"no_answer"])
		_log("FAR_PICKUP ok=%s reason=%s" % [r1[0], r1[1]])
		# Teleport next to the prop and ask at once: the host has not accepted the move.
		player.global_position = Vector3(-3.0, 0.05, 3.2)
		_session.submit(_pickup(2))
		await _sleep(0.6)
		var r2: Array = _results.get(2, [false, &"no_answer"])
		_log("TELEPORT_PICKUP ok=%s reason=%s" % [r2[0], r2[1]])
		# Standing there, the host catches up at a believable speed; then it works.
		await _sleep(2.5)
		_session.submit(_pickup(3))
		await _sleep(0.6)
		var r3: Array = _results.get(3, [false, &"no_answer"])
		_log("ARRIVED_PICKUP ok=%s reason=%s" % [r3[0], r3[1]])
	else:
		var max_x: Dictionary = {}
		var end: float = t0 + 9.0
		while Time.get_unix_time_from_system() < end:
			for actor: StringName in _session.players:
				var other: Player = _session.players[actor]
				if not other.is_local:
					max_x[actor] = maxf(float(max_x.get(actor, -99.0)), other.global_position.x)
			await get_tree().process_frame
		for actor: StringName in max_x:
			_log("SEEN %s maxx=%.2f" % [actor, max_x[actor]])


func _pickup(request_id: int) -> Dictionary:
	return {
		"request_id": request_id,
		"actor_id": _session.player.actor_id,
		"action": InteractionService.PICKUP,
		"target_id": &"cake_section_01",
		"expected_revision": _prop.revision,
	}


func _my_id() -> String:
	return String(_session.player.actor_id) if _session.player != null else "none"


func _wait_until(condition: Callable, seconds: float) -> bool:
	var end: float = Time.get_unix_time_from_system() + seconds
	while Time.get_unix_time_from_system() < end:
		if condition.call():
			return true
		await get_tree().process_frame
	return bool(condition.call())


func _sleep(seconds: float) -> void:
	await get_tree().create_timer(seconds).timeout


func _sleep_until(unix_time: float) -> void:
	var wait: float = unix_time - Time.get_unix_time_from_system()
	if wait > 0.0:
		await get_tree().create_timer(wait).timeout


## A client picks the prop up and walks forward; the host (run in a real
## window, not headless) takes a screenshot of what it sees.
func _visual() -> void:
	var t0: float = float(_a["t0"])
	if _name == "B":
		await _sleep_until(t0)
		_session.submit(_pickup(1))
		await _sleep(0.6)
		var end: float = t0 + 3.5
		while Time.get_unix_time_from_system() < end:
			_session.player.global_position.z -= 0.6 * get_physics_process_delta_time()
			await get_tree().physics_frame
		await _sleep(3.0)
	elif _is_host:
		await _sleep_until(t0 + 2.0)
		_session.player.set_look(-0.52, -0.08)
		await _sleep_until(t0 + 4.2)
		_session._set_menu(false)
		await get_tree().process_frame
		await RenderingServer.frame_post_draw
		var dir: String = str(_a.get("shots", "user://shots"))
		DirAccess.make_dir_recursive_absolute(dir)
		var image: Image = get_viewport().get_texture().get_image()
		var err: Error = image.save_png("%s/5_remote_player_holding_cake.png" % dir)
		_log("SHOT saved=%s B holder='%s'" % [err == OK, _prop.holder_id])
	else:
		await _sleep(6.0)
