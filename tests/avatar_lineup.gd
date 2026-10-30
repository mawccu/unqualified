extends Node
## Renders the four coworkers in poses, the first-person hands, and a remote
## player carrying a prop, as PNG files. Needs a real window (not --headless):
##   godot --path . res://tests/avatar_lineup.tscn -- <output folder>

var _out_dir: String = "user://shots"
var _session: Session
var _player: Player


func _ready() -> void:
	var args: PackedStringArray = OS.get_cmdline_user_args()
	if args.size() > 0:
		_out_dir = args[0]
	DirAccess.make_dir_recursive_absolute(_out_dir)
	get_tree().create_timer(90.0).timeout.connect(func() -> void: get_tree().quit(2))
	_run()


func _run() -> void:
	var packed := load("res://scenes/bootstrap/main.tscn") as PackedScene
	_session = packed.instantiate() as Session
	add_child(_session)
	await _frames(10)
	_player = _session.player
	_session.hud.visible = false
	_session.coordinator.visible = false

	# Two rows of the four coworkers on the dance floor, posed with freeze_pose.
	var rows: Array[Array] = [
		[[WorkerAnimations.IDLE, 0.4], [WorkerAnimations.WALK, 0.2], [WorkerAnimations.CARRY_IDLE, 0.3], [WorkerAnimations.STUMBLE, 0.12]],
		[[WorkerAnimations.SPRINT, 0.1], [WorkerAnimations.INTERACT, 0.2], [WorkerAnimations.CLEAN, 0.25], [WorkerAnimations.CELEBRATE, 0.6]],
	]
	var names: Array[String] = ["10_lineup_idle_walk_carry_stumble", "11_lineup_sprint_interact_clean_celebrate"]
	for row: int in 2:
		var avatars: Array[WorkerPresenter] = []
		for i: int in 4:
			var avatar: WorkerPresenter = WorkerFactory.create(i)
			avatar.position = Vector3(-1.65 + 1.1 * float(i), 0.0, -2.4)
			avatar.rotation.y = PI + (0.25 if i % 2 == 0 else -0.25)
			_session.add_child(avatar)
			avatars.append(avatar)
		await _frames(3)
		for i: int in 4:
			avatars[i].freeze_pose(rows[row][i][0], rows[row][i][1])
		await _view(names[row], Vector3(0.0, 0.05, 1.0), 0.0, -0.05)
		for avatar: WorkerPresenter in avatars:
			avatar.queue_free()
		await _frames(2)

	# First person: hands empty, then carrying a cake layer, then a jab.
	await _view("12_hands_empty", Vector3(-0.8, 0.05, 4.4), 0.0, -0.3)
	_ask(_pickup(&"cake_base"))
	await _view("13_hands_carrying", Vector3(-0.8, 0.05, 4.4), 0.0, -0.1, 40)
	_player.hands().jab()
	await _view("14_hands_jab", Vector3(-0.8, 0.05, 4.4), 0.0, -0.1, 4)

	# A remote player, seen from another peer, carrying a layer.
	_ask({"request_id": 9, "actor_id": _player.actor_id, "action": InteractionService.DROP, "target_id": &"", "expected_revision": -1})
	var remote := Player.new()
	remote.setup(&"peer_2", 2, 1, false)
	remote.position = Vector3(0.4, 0.05, 3.2)
	remote.service = _session.service
	_session.add_child(remote)
	_session.service.register_actor(remote)
	await _frames(5)
	_ask({"request_id": 10, "actor_id": &"peer_2", "action": InteractionService.PICKUP, "target_id": &"cake_middle",
		"expected_revision": _session.service.get_prop(&"cake_middle").revision})
	await _frames(5)
	remote.net_set_target(Vector3(0.4, 0.05, 2.4), 0.5, 0.0)
	remote.avatar().update_motion(Vector3(0, 0, -1.3), false, true, 1.0 / 60.0)
	await _view("15_remote_player_carrying", Vector3(-1.0, 0.05, 4.5), -0.58, -0.02, 14)

	get_tree().quit(0)


func _pickup(prop_id: StringName) -> Dictionary:
	return {"request_id": 1, "actor_id": _player.actor_id, "action": InteractionService.PICKUP,
		"target_id": prop_id, "expected_revision": _session.service.get_prop(prop_id).revision}


func _ask(req: Dictionary) -> void:
	var result: Dictionary = _session.service.request(req)
	if not result["ok"]:
		print("request ", req["action"], " ", req["target_id"], " refused: ", result["reason"])


func _view(shot_name: String, pos: Vector3, yaw: float, pitch: float, settle: int = 14) -> void:
	_session._set_menu(false)
	_player.global_position = pos
	_player.set_look(yaw, pitch)
	await _frames(settle)
	await _shot(shot_name)


func _frames(count: int) -> void:
	for i: int in count:
		await get_tree().process_frame


func _shot(shot_name: String) -> void:
	await RenderingServer.frame_post_draw
	var image: Image = get_viewport().get_texture().get_image()
	var err: Error = image.save_png("%s/%s.png" % [_out_dir, shot_name])
	print("%s %s" % ["saved" if err == OK else "FAILED", shot_name])
