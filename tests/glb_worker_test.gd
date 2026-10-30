extends Node
## Checks the authored worker model against docs/assets/WORKER_GLB_CONTRACT.md,
## and that the game really uses it behind the presenter boundary.
##   godot --headless --path . res://tests/glb_worker_test.tscn

const REQUIRED_BONES: Array[String] = [
	"Hips", "Torso", "Head", "ArmL", "ElbowL", "HandL", "ArmR", "ElbowR", "HandR", "LegL", "FootL", "LegR", "FootR",
]
const HAIR: Array[String] = ["short", "pompadour", "ponytail", "beanie"]
const ACCESSORIES: Array[String] = ["bow_tie", "lanyard", "apron", "headset", "radio", "belt", "one_glove"]

var _checks: int = 0
var _failures: int = 0


func _ready() -> void:
	get_tree().create_timer(60.0).timeout.connect(func() -> void:
		print("FAIL  watchdog")
		get_tree().quit(2))
	_run()


func _run() -> void:
	_check("the model file exists at %s" % GlbWorker.MODEL_PATH, GlbWorker.model_available())
	_check("the model follows the contract (problems: %s)" % str(GlbWorker.problems()), GlbWorker.usable())
	_check("the factory hands out the authored model", WorkerFactory.using_authored_model())
	if not GlbWorker.usable():
		_finish()
		return

	var raw := (load(GlbWorker.MODEL_PATH) as PackedScene).instantiate() as Node3D
	add_child(raw)
	var skeleton := raw.find_children("*", "Skeleton3D", true, false)[0] as Skeleton3D
	for bone: String in REQUIRED_BONES:
		_check("skeleton has bone %s" % bone, skeleton.find_bone(bone) >= 0)

	var anim_player := raw.find_children("*", "AnimationPlayer", true, false)[0] as AnimationPlayer
	for clip: StringName in WorkerAnimations.clip_names():
		_check("clip '%s' is in the model" % clip, anim_player.has_animation(clip))
		if anim_player.has_animation(clip):
			var anim: Animation = anim_player.get_animation(clip)
			var source: Animation = WorkerAnimations.build_library().get_animation(clip)
			_check("clip '%s' keeps its length (%.2f vs %.2f s)" % [clip, anim.length, source.length], absf(anim.length - source.length) < 0.08)
			_check("clip '%s' animates the skeleton (%d tracks)" % [clip, anim.get_track_count()], anim.get_track_count() >= 9)

	var materials: Dictionary = {}
	var names: Dictionary = {}
	for node: Node in raw.find_children("*", "MeshInstance3D", true, false):
		var instance := node as MeshInstance3D
		names[String(instance.name)] = true
		for surface: int in instance.mesh.get_surface_count():
			var material := instance.get_active_material(surface)
			if material != null:
				materials[material.resource_name] = true
	for role: String in ["Shirt", "Vest", "Trousers", "Shoes", "Skin", "Hair", "Accent"]:
		_check("material '%s' exists to be tinted" % role, materials.has(role))
	for style: String in HAIR:
		var found: bool = false
		for key: String in names:
			if key.to_lower().begins_with("hair_%s" % style):
				found = true
		_check("hair style '%s' has a mesh" % style, found)
	for acc: String in ACCESSORIES:
		var found: bool = false
		for key: String in names:
			if key.to_lower().begins_with("acc_%s" % acc):
				found = true
		_check("accessory '%s' has a mesh" % acc, found)
	raw.queue_free()

	# Behind the presenter boundary: four workers, four costumes.
	var workers: Array[WorkerPresenter] = []
	for slot: int in 4:
		var worker := WorkerFactory.create(slot)
		_check("slot %d is built from the authored model" % slot, worker is GlbWorker)
		add_child(worker)
		workers.append(worker)
	await get_tree().process_frame
	await get_tree().process_frame

	var body_meshes: Array[MeshInstance3D] = []
	for worker: WorkerPresenter in workers:
		var mesh := worker.find_child("Worker", true, false) as MeshInstance3D
		body_meshes.append(mesh)
	var aabb: AABB = body_meshes[0].get_aabb()
	var global_box: AABB = body_meshes[0].global_transform * aabb
	_check("the worker is about 1.7 m tall (%.2f)" % global_box.size.y, global_box.size.y > 1.55 and global_box.size.y < 1.9)
	_check("the model stands on the floor (lowest point %.3f m)" % global_box.position.y, absf(global_box.position.y) < 0.03)
	_check("the model faces -Z like the game expects (nose and shoes lean toward -Z: centre z %.3f)" % global_box.get_center().z, global_box.get_center().z < 0.0)

	var vests: Array[Color] = []
	var distinct: bool = true
	for i: int in 4:
		var vest: Color = _surface_color(body_meshes[i], "Vest")
		var expected: Color = WorkerCostumes.for_slot(i)["vest"]
		_check("slot %d vest is tinted to its costume" % i, vest.is_equal_approx(expected))
		for other: Color in vests:
			if other.is_equal_approx(vest):
				distinct = false
		vests.append(vest)
	_check("the four workers have four different vests", distinct)

	for i: int in 4:
		var style: String = str(WorkerCostumes.for_slot(i)["hair_style"])
		var shown: PackedStringArray = []
		for node: Node in workers[i].find_children("*", "MeshInstance3D", true, false):
			var inst := node as MeshInstance3D
			var key: String = String(inst.name).to_lower()
			if key.begins_with("hair_") and inst.visible:
				shown.append(key)
		var only_this: bool = not shown.is_empty()
		for key: String in shown:
			if not key.begins_with("hair_%s" % style):
				only_this = false
		_check("slot %d shows only its own hair (%s)" % [i, style], only_this)
		var acc_shown: PackedStringArray = []
		for node: Node in workers[i].find_children("*", "MeshInstance3D", true, false):
			var inst2 := node as MeshInstance3D
			var key2: String = String(inst2.name).to_lower()
			if key2.begins_with("acc_") and inst2.visible:
				acc_shown.append(key2)
		var wanted: Array = WorkerCostumes.for_slot(i)["accessories"]
		var ok: bool = true
		for key2: String in acc_shown:
			var matched: bool = false
			for acc_name: StringName in wanted:
				if key2.begins_with("acc_%s" % acc_name):
					matched = true
			if not matched:
				ok = false
		_check("slot %d shows only its own accessories" % i, ok and not acc_shown.is_empty())

	# The same state machine and clips as the fallback.
	var walker: WorkerPresenter = workers[0]
	_check("a new authored worker starts idle", walker.current_state() == WorkerAnimations.IDLE)
	_check("the authored worker walks", await _reach(walker, Vector3(0, 0, -2.0), false, false, WorkerAnimations.WALK))
	_check("the authored worker carries", await _reach(walker, Vector3(0, 0, -2.0), false, true, WorkerAnimations.CARRY_WALK))
	_check("the authored worker returns to idle", await _reach(walker, Vector3.ZERO, false, false, WorkerAnimations.IDLE))
	walker.trigger(WorkerAnimations.STUMBLE)
	_check("the authored worker stumbles", await _reach(walker, Vector3.ZERO, false, false, WorkerAnimations.STUMBLE))
	_check("and recovers on its own", await _reach(walker, Vector3.ZERO, false, false, WorkerAnimations.IDLE, 2.0))

	# The fallback is still available behind a switch.
	WorkerFactory.force_fallback = true
	var fallback := WorkerFactory.create(0)
	_check("--fallback-workers gives the code-built mannequin", fallback is WorkerAvatar)
	WorkerFactory.force_fallback = false
	fallback.free()
	_finish()


func _surface_color(instance: MeshInstance3D, role: String) -> Color:
	for surface: int in instance.mesh.get_surface_count():
		var material := instance.get_active_material(surface) as StandardMaterial3D
		if material != null and material.resource_name.to_lower() == role.to_lower():
			return material.albedo_color
	return Color(-1, -1, -1)


func _reach(worker: WorkerPresenter, velocity: Vector3, sprinting: bool, carrying: bool, state: StringName, seconds: float = 1.5) -> bool:
	var end: float = Time.get_ticks_msec() / 1000.0 + seconds
	while Time.get_ticks_msec() / 1000.0 < end:
		worker.update_motion(velocity, sprinting, carrying, 1.0 / 60.0)
		if worker.current_state() == state:
			return true
		await get_tree().process_frame
	return worker.current_state() == state


func _finish() -> void:
	print("")
	print("%d checks, %d failed" % [_checks, _failures])
	get_tree().quit(1 if _failures > 0 else 0)


func _check(label: String, condition: bool) -> void:
	_checks += 1
	if condition:
		print("PASS  ", label)
	else:
		_failures += 1
		print("FAIL  ", label)
