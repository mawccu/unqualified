extends Node
## Renders a tour of the venue to PNG files so the look and the prompts can be
## reviewed without playing. Needs a real window (not --headless):
##   godot --path . res://tests/screenshots.tscn -- <output folder>

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

	await _view("01_back_of_house_start", Vector3(0.0, 0.05, 5.4), 0.0, -0.18)
	await _view("02_cake_prep_prompt", Vector3(-0.8, 0.05, 4.3), 0.0, -0.45)
	await _view("03_ballroom_from_archway", Vector3(-6.0, 0.05, 1.3), -0.35, 0.02)
	await _view("04_stage_and_sound_desk", Vector3(-1.0, 0.05, 0.9), -0.25, 0.08)
	await _view("05_dessert_station", Vector3(6.6, 0.05, -0.8), -1.5708, -0.28)
	await _view("06_guest_table", Vector3(-4.0, 0.05, -0.3), 1.2, -0.3)
	await _view("07_photo_display", Vector3(-6.5, 0.05, -2.0), 1.5708, 0.05)

	# Carry two cake layers to the stand, and a sign to the wrong table.
	var layers: Array[StringName] = [&"cake_base", &"cake_middle"]
	for i: int in layers.size():
		_teleport(Vector3(-0.8 + 0.8 * float(i), 0.05, 4.4))
		_ask(_pickup(layers[i]))
		_teleport(Vector3(7.5, 0.05, -0.8))
		_ask(_place(VenueLayout.CAKE_SOCKETS[i]))
		await _frames(3)
	_teleport(Vector3(-5.5, 0.05, 4.5))
	_ask(_pickup(&"sign_2"))
	_teleport(VenueLayout.GUEST_TABLES[0] + Vector3(0, 0.05, 1.4))
	_ask(_place(VenueLayout.TABLE_SOCKETS[0]))
	await _frames(20)
	await _view("08_progress_and_task_board", Vector3(6.6, 0.05, -0.8), -1.5708, -0.28)

	# Hold the top layer and aim at its socket: the ghost preview.
	_teleport(Vector3(0.8, 0.05, 4.4))
	_ask(_pickup(&"cake_top"))
	await _view("09_ghost_preview", Vector3(7.3, 0.05, -0.8), -1.5708, -0.12)

	get_tree().quit(0)


func _pickup(prop_id: StringName) -> Dictionary:
	return {"request_id": 1, "actor_id": _player.actor_id, "action": InteractionService.PICKUP,
		"target_id": prop_id, "expected_revision": _session.service.get_prop(prop_id).revision}


func _place(socket_id: StringName) -> Dictionary:
	return {"request_id": 2, "actor_id": _player.actor_id, "action": InteractionService.PLACE,
		"target_id": socket_id, "expected_revision": -1}


func _ask(req: Dictionary) -> void:
	var result: Dictionary = _session.service.request(req)
	if not result["ok"]:
		print("request ", req["action"], " ", req["target_id"], " refused: ", result["reason"])


func _teleport(pos: Vector3) -> void:
	_player.global_position = pos


func _view(shot_name: String, pos: Vector3, yaw: float, pitch: float) -> void:
	_session._set_menu(false)
	_player.global_position = pos
	_player.set_look(yaw, pitch)
	await _frames(14)
	await _shot(shot_name)


func _frames(count: int) -> void:
	for i: int in count:
		await get_tree().process_frame


func _shot(shot_name: String) -> void:
	await RenderingServer.frame_post_draw
	var image: Image = get_viewport().get_texture().get_image()
	var path: String = "%s/%s.png" % [_out_dir, shot_name]
	var err: Error = image.save_png(path)
	print("%s %s (%dx%d)" % ["saved" if err == OK else "FAILED", shot_name, image.get_width(), image.get_height()])
