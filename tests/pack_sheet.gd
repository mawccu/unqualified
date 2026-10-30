extends Node3D
## Contact sheet of every model in assets/wedding, rendered in Godot, plus the
## facts needed to place them: size, lowest point, and which way chairs face.
##   godot --path . res://tests/pack_sheet.tscn -- <output folder>

var _out_dir: String = "user://shots"


func _ready() -> void:
	var args: PackedStringArray = OS.get_cmdline_user_args()
	if args.size() > 0:
		_out_dir = args[0]
	DirAccess.make_dir_recursive_absolute(_out_dir)
	get_tree().create_timer(90.0).timeout.connect(func() -> void: get_tree().quit(2))
	_run()


func _run() -> void:
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color(0.82, 0.84, 0.84)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(1, 1, 1)
	env.ambient_light_energy = 0.7
	var world_env := WorldEnvironment.new()
	world_env.environment = env
	add_child(world_env)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-50, 30, 0)
	sun.light_energy = 0.9
	add_child(sun)

	var files: PackedStringArray = DirAccess.get_files_at("res://assets/wedding")
	var names: Array[String] = []
	for f: String in files:
		if f.ends_with(".glb"):
			names.append(f.trim_suffix(".glb"))
	names.sort()
	var columns: int = 8
	var spacing: float = 2.6
	var index: int = 0
	for model_name: String in names:
		var scene := load("res://assets/wedding/%s.glb" % model_name) as PackedScene
		if scene == null:
			print("FAILED to load ", model_name)
			continue
		var holder := Node3D.new()
		holder.position = Vector3(float(index % columns) * spacing, 0.0, float(index / columns) * spacing)
		add_child(holder)
		var instance := scene.instantiate() as Node3D
		holder.add_child(instance)
		var box: AABB = _bounds(instance)
		var label := Label3D.new()
		label.text = model_name
		label.font_size = 36
		label.pixel_size = 0.004
		label.position = Vector3(0, -0.02, 1.1)
		label.rotation_degrees = Vector3(-60, 0, 0)
		label.modulate = Color(0.1, 0.1, 0.12)
		holder.add_child(label)
		print("%-22s size %.2f x %.2f x %.2f  lowest %.3f  centre z %.3f" % [model_name, box.size.x, box.size.y, box.size.z, box.position.y, box.get_center().z])
		index += 1

	var chair := load("res://assets/wedding/guest_chair.glb") as PackedScene
	var chair_node := chair.instantiate() as Node3D
	add_child(chair_node)
	var top := AABB()
	var seat := AABB()
	var first_top: bool = true
	var first_seat: bool = true
	for node: Node in chair_node.find_children("*", "MeshInstance3D", true, false):
		var inst := node as MeshInstance3D
		var arrays: Array = inst.mesh.surface_get_arrays(0)
		var verts: PackedVector3Array = arrays[Mesh.ARRAY_VERTEX]
		for v: Vector3 in verts:
			var p: Vector3 = inst.global_transform * v
			if p.y > 0.65:
				top = AABB(p, Vector3.ZERO) if first_top else top.expand(p)
				first_top = false
			elif p.y > 0.35 and p.y < 0.55:
				seat = AABB(p, Vector3.ZERO) if first_seat else seat.expand(p)
				first_seat = false
	print("chair: backrest centre z %.3f, seat centre z %.3f -> backrest is toward %s, so the chair faces %s" % [top.get_center().z, seat.get_center().z, "+Z" if top.get_center().z > seat.get_center().z else "-Z", "-Z" if top.get_center().z > seat.get_center().z else "+Z"])
	chair_node.queue_free()

	var cam := Camera3D.new()
	var rows: int = int(ceil(float(index) / float(columns)))
	cam.position = Vector3(float(columns - 1) * spacing * 0.5, 14.0, float(rows) * spacing + 6.0)
	cam.rotation_degrees = Vector3(-52, 0, 0)
	cam.fov = 60.0
	add_child(cam)
	cam.make_current()
	for i: int in 12:
		await get_tree().process_frame
	await RenderingServer.frame_post_draw
	var image: Image = get_viewport().get_texture().get_image()
	image.save_png("%s/30_pack_sheet.png" % _out_dir)
	print("saved 30_pack_sheet (%d models)" % index)
	get_tree().quit(0)


func _bounds(node: Node3D) -> AABB:
	var out := AABB()
	var first: bool = true
	for child: Node in node.find_children("*", "MeshInstance3D", true, false):
		var inst := child as MeshInstance3D
		var box: AABB = inst.global_transform * inst.get_aabb()
		out = box if first else out.merge(box)
		first = false
	return out
