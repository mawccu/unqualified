extends Node
## Renders a few views of the M0 room to PNG files so the look and the prompts
## can be reviewed without playing. Needs a real window (not --headless):
##   godot --path . res://tests/screenshots.tscn -- <output folder>

var _out_dir: String = "user://shots"


func _ready() -> void:
	var args: PackedStringArray = OS.get_cmdline_user_args()
	if args.size() > 0:
		_out_dir = args[0]
	DirAccess.make_dir_recursive_absolute(_out_dir)
	get_tree().create_timer(60.0).timeout.connect(func() -> void: get_tree().quit(2))
	_run()


func _run() -> void:
	var packed := load("res://scenes/bootstrap/main.tscn") as PackedScene
	var session := packed.instantiate() as Session
	add_child(session)
	await _frames(5)
	var player: Player = session.player
	var service: InteractionService = session.service


	# 1. Wide view from the spawn point
	_aim(session, player, Vector3(-5.2, 0.05, 3.9), -0.97, -0.12)
	await _frames(8)
	await _shot("1_room_wide")

	# 2. Looking at the cake section on the side table
	_aim(session, player, Vector3(-3.0, 0.05, 3.4), 0.0, -0.46)
	await _frames(8)
	await _shot("2_prompt_pick_up")

	# 3. Holding it, aiming at the stand with the ghost preview
	service.request({"request_id": 1, "actor_id": &"peer_1", "action": InteractionService.PICKUP,
		"target_id": &"cake_section_01", "expected_revision": 0})
	_aim(session, player, Vector3(3.0, 0.05, -1.6), 0.0, -0.45)
	await _frames(8)
	await _shot("3_prompt_place_ghost")

	# 4. Placed on the stand
	print("place result: ", service.request({"request_id": 2, "actor_id": &"peer_1", "action": InteractionService.PLACE,
		"target_id": &"dessert_socket", "expected_revision": -1}))
	await _frames(8)
	var prop: PickupProp = session.get_node("CakeSection01") as PickupProp
	print("prop pos=", prop.global_position, " holder=", prop.holder_id, " socket=", prop.socket_id, " frozen=", prop.freeze, " visible=", prop.is_visible_in_tree())
	await _shot("4_placed")

	# Reset in the real window: the prop must return to its start, not stay put
	service.reset_all()
	await _frames(8)
	print("after reset: prop pos=", prop.global_position, " (start is (-3.0, 0.9, 2.0))")

	get_tree().quit(0)


func _aim(session: Session, player: Player, pos: Vector3, yaw: float, pitch: float) -> void:
	session._set_menu(false)
	player.global_position = pos
	player.set_look(yaw, pitch)


func _frames(count: int) -> void:
	for i: int in count:
		await get_tree().process_frame


func _shot(shot_name: String) -> void:
	await RenderingServer.frame_post_draw
	var image: Image = get_viewport().get_texture().get_image()
	var path: String = "%s/%s.png" % [_out_dir, shot_name]
	var err: Error = image.save_png(path)
	print("%s %s (%dx%d)" % ["saved" if err == OK else "FAILED", path, image.get_width(), image.get_height()])
