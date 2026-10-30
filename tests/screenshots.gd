extends Node
## Renders a tour of the venue to PNG files so the look, the size and the layout
## can be reviewed without playing. Needs a real window (not --headless):
##   godot --path . res://tests/screenshots.tscn -- <output folder>

var _out_dir: String = "user://shots"
var _session: Session
var _player: Player


func _ready() -> void:
	var args: PackedStringArray = OS.get_cmdline_user_args()
	if args.size() > 0:
		_out_dir = args[0]
	DirAccess.make_dir_recursive_absolute(_out_dir)
	get_tree().create_timer(120.0).timeout.connect(func() -> void: get_tree().quit(2))
	_run()


func _run() -> void:
	var packed := load("res://scenes/bootstrap/main.tscn") as PackedScene
	_session = packed.instantiate() as Session
	add_child(_session)
	await _frames(12)
	_player = _session.player

	await _view("40_hero_view", VenueLayout.HERO_POSITION, VenueLayout.HERO_YAW, VenueLayout.HERO_PITCH)
	await _view("41_ballroom_from_the_back", Vector3(0.0, 0.05, 0.8), 0.0, 0.02)
	await _view("42_stage_from_the_floor", Vector3(0.0, 0.05, -8.5), 0.0, 0.12)
	await _view("43_guest_tables_and_chairs", Vector3(-5.6, 0.05, -6.5), 1.5708, -0.2)
	await _view("44_dessert_station", Vector3(9.4, 0.05, -5.0), -1.5708, -0.28)
	await _view("45_photo_corner", Vector3(-8.0, 0.05, -7.0), 1.5708, 0.0)
	await _view("46_back_of_house", Vector3(-2.5, 0.05, 6.6), 0.0, -0.1)
	await _view("47_prep_table_with_cake_layers", Vector3(-0.8, 0.05, 4.9), 0.0, -0.4)

	# Walk up the steps onto the stage with real physics, as a player would.
	_player.global_position = Vector3(0.0, 0.05, -10.5)
	_player.set_look(0.0, 0.0)
	Input.action_press(&"move_forward")
	await _frames(100)
	Input.action_release(&"move_forward")
	print("stage walk: y = %.2f m, z = %.2f m (the stage is %.2f m high and starts at z %.2f)" % [_player.global_position.y, _player.global_position.z, VenueLayout.STAGE_HEIGHT, VenueLayout.STAGE_FRONT_Z])
	await _shot("48_standing_on_the_stage")

	# The whole floor from above, ceiling removed, to judge the size.
	var ceiling: Node = _session.find_child("Ceiling", true, false)
	if ceiling != null:
		(ceiling as Node3D).visible = false
	_session.coordinator.visible = true
	var camera := Camera3D.new()
	camera.position = Vector3(0.0, 34.0, -4.0)
	camera.rotation_degrees = Vector3(-90.0, 0.0, 0.0)
	camera.fov = 52.0
	_session.add_child(camera)
	camera.make_current()
	_session.hud.visible = false
	await _frames(10)
	await _shot("49_top_down_whole_venue")

	get_tree().quit(0)


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
	var err: Error = image.save_png("%s/%s.png" % [_out_dir, shot_name])
	print("%s %s" % ["saved" if err == OK else "FAILED", shot_name])
