extends Node
## Checks for the worker body: spring maths, costumes, every animation clip, the
## state machine, the wobble, and that none of it can move what interaction uses.
##   godot --headless --path . res://tests/avatar_test.tscn

var _checks: int = 0
var _failures: int = 0


func _ready() -> void:
	get_tree().create_timer(90.0).timeout.connect(func() -> void:
		print("FAIL  watchdog: avatar test did not finish in 90 s")
		get_tree().quit(2))
	_run()


func _run() -> void:
	_test_spring()
	_test_costumes()
	_test_clips()
	await _test_state_machine()
	await _test_wobble()
	await _test_controls_unaffected()
	print("")
	print("%d checks, %d failed" % [_checks, _failures])
	get_tree().quit(1 if _failures > 0 else 0)


# Springs -----------------------------------------------------------------------

func _test_spring() -> void:
	var spring := SecondarySpring.new(140.0, 7.0, 5.0)
	var peak: float = 0.0
	for i: int in 180:
		spring.step(Vector3(1.0, 0.0, 0.0), 1.0 / 60.0)
		peak = maxf(peak, spring.value.x)
	_check("a spring settles on its target within 3 s", absf(spring.value.x - 1.0) < 0.01)
	_check("it overshoots once on the way (that is the wobble)", peak > 1.05)

	var wild := SecondarySpring.new(140.0, 7.0, 0.5)
	for i: int in 300:
		wild.step(Vector3(100.0, -80.0, 40.0), 1.0 / 60.0)
	_check("a huge target never pushes it past its limit", wild.value.length() <= 0.5001)

	var huge := SecondarySpring.new(140.0, 7.0, 0.5)
	huge.step(Vector3(1.0, 1.0, 1.0), 5.0)
	_check("one enormous time step stays finite and bounded", huge.value.is_finite() and huge.value.length() <= 0.5001)


# Costumes ----------------------------------------------------------------------

func _test_costumes() -> void:
	_check("there are four costume variants", WorkerCostumes.VARIANTS.size() == 4)
	_check("a fifth slot wraps back to the first costume", WorkerCostumes.for_slot(4) == WorkerCostumes.for_slot(0))
	var vests: Array[Color] = []
	var sets: Array[String] = []
	for variant: Dictionary in WorkerCostumes.VARIANTS:
		vests.append(variant["vest"])
		sets.append(",".join(variant["accessories"].map(func(a: StringName) -> String: return String(a))))
	var distinct_vests: bool = true
	for i: int in vests.size():
		for j: int in range(i + 1, vests.size()):
			if vests[i].is_equal_approx(vests[j]):
				distinct_vests = false
	_check("every costume has its own colour block", distinct_vests)
	var unique_sets: Array[String] = []
	for s: String in sets:
		if not unique_sets.has(s):
			unique_sets.append(s)
	_check("every costume has its own accessory set", unique_sets.size() == 4)

	for slot: int in 4:
		var avatar := WorkerAvatar.new()
		avatar.setup(slot)
		add_child(avatar)
		var needed: Array[String] = [
			"Body/Hips/LegL", "Body/Hips/LegR", "Body/Hips/Torso", "Body/Hips/Torso/HeadPivot/HeadSway",
			"Body/Hips/Torso/ArmL/ElbowL/Hand", "Body/Hips/Torso/ArmR/ElbowR/Hand",
		]
		var complete: bool = true
		for path: String in needed:
			if avatar.get_node_or_null(NodePath(path)) == null:
				complete = false
		_check("costume %d (%s) builds every joint" % [slot, WorkerCostumes.for_slot(slot)["name"]], complete)
		avatar.queue_free()

	var tall := WorkerAvatar.new()
	tall.setup(0)
	add_child(tall)
	var head: Node3D = tall.get_node("Body/Hips/Torso/HeadPivot") as Node3D
	var head_top: float = (WorkerAnimations.REST_HIPS_Y + 0.02 + head.position.y + 0.49) * WorkerAvatar.BODY_SCALE
	_check("the worker is about 1.8 m tall (%.2f)" % head_top, head_top > 1.6 and head_top < 2.0)
	var head_share: float = (0.54 * WorkerAvatar.BODY_SCALE) / head_top
	_check("the head is about a third of the height (%.0f%%)" % (head_share * 100.0), head_share > 0.28 and head_share < 0.42)
	tall.queue_free()


# Clips -------------------------------------------------------------------------

func _test_clips() -> void:
	var lib: AnimationLibrary = WorkerAnimations.build_library()
	var joint_count: int = WorkerAnimations.joints().size()
	var avatar := WorkerAvatar.new()
	avatar.setup(0)
	add_child(avatar)

	for clip: StringName in WorkerAnimations.clip_names():
		_check("clip '%s' exists" % clip, lib.has_animation(clip))
		var anim: Animation = lib.get_animation(clip)
		var wants_loop: bool = WorkerAnimations.LOOPING.has(clip)
		_check("clip '%s' %s" % [clip, "loops" if wants_loop else "plays once"],
			(anim.loop_mode == Animation.LOOP_LINEAR) == wants_loop)
		_check("clip '%s' keys every joint (%d tracks)" % [clip, anim.get_track_count()], anim.get_track_count() == joint_count)

		var resolves: bool = true
		var seam_ok: bool = true
		var in_range: bool = true
		for t: int in anim.get_track_count():
			var path: String = str(anim.track_get_path(t))
			var node_path: String = path.get_slice(":", 0)
			var prop: String = path.get_slice(":", 1)
			if avatar.get_node_or_null(NodePath(node_path)) == null:
				resolves = false
			var key_count: int = anim.track_get_key_count(t)
			var first: Vector3 = anim.track_get_key_value(t, 0)
			var last: Vector3 = anim.track_get_key_value(t, key_count - 1)
			if wants_loop and not first.is_equal_approx(last):
				seam_ok = false
			for k: int in key_count:
				var v: Vector3 = anim.track_get_key_value(t, k)
				if not v.is_finite():
					in_range = false
				elif prop == "rotation" and maxf(maxf(absf(v.x), absf(v.y)), absf(v.z)) > 3.2:
					in_range = false
				elif prop == "position" and (v.y < 0.3 or v.y > 0.6 or absf(v.x) > 0.2 or absf(v.z) > 0.2):
					in_range = false
		_check("clip '%s': every track points at a real joint" % clip, resolves)
		_check("clip '%s': values finite and within believable ranges" % clip, in_range)
		if wants_loop:
			_check("clip '%s': loop seam is exact" % clip, seam_ok)

	var stumble_len: float = lib.get_animation(WorkerAnimations.STUMBLE).length
	_check("stumble is a short recoverable slip (%.2f s, brief says 0.25 to 0.45)" % stumble_len, stumble_len >= 0.25 and stumble_len <= 0.45)
	avatar.queue_free()


# State machine -----------------------------------------------------------------

func _test_state_machine() -> void:
	var avatar := WorkerAvatar.new()
	avatar.setup(1)
	add_child(avatar)
	await get_tree().process_frame
	await get_tree().process_frame
	_check("a new worker starts idle", avatar.current_state() == WorkerAnimations.IDLE)

	_check("walking speed plays walk", await _reach(avatar, Vector3(0, 0, -2.0), false, false, WorkerAnimations.WALK))
	_check("sprinting plays sprint", await _reach(avatar, Vector3(0, 0, -6.0), true, false, WorkerAnimations.SPRINT))
	_check("fast without sprint held plays walk", await _reach(avatar, Vector3(0, 0, -6.0), false, false, WorkerAnimations.WALK))
	_check("standing still plays idle", await _reach(avatar, Vector3.ZERO, false, false, WorkerAnimations.IDLE))
	_check("carrying while still plays carry_idle", await _reach(avatar, Vector3.ZERO, false, true, WorkerAnimations.CARRY_IDLE))
	_check("carrying while walking plays carry_walk", await _reach(avatar, Vector3(0, 0, -2.0), false, true, WorkerAnimations.CARRY_WALK))
	_check("sprinting with a prop still plays carry_walk", await _reach(avatar, Vector3(0, 0, -6.0), true, true, WorkerAnimations.CARRY_WALK))
	_check("putting it down returns to idle", await _reach(avatar, Vector3.ZERO, false, false, WorkerAnimations.IDLE))

	avatar.trigger(WorkerAnimations.INTERACT)
	_check("interact plays", await _reach(avatar, Vector3.ZERO, false, false, WorkerAnimations.INTERACT))
	_check("interact returns to idle on its own", await _reach(avatar, Vector3.ZERO, false, false, WorkerAnimations.IDLE, 2.0))

	await _reach(avatar, Vector3(0, 0, -2.0), false, false, WorkerAnimations.WALK)
	avatar.trigger(WorkerAnimations.STUMBLE)
	var saw_stumble: bool = await _reach(avatar, Vector3(0, 0, -2.0), false, false, WorkerAnimations.STUMBLE)
	_check("a stumble interrupts walking", saw_stumble)
	avatar.trigger(WorkerAnimations.INTERACT)
	await get_tree().process_frame
	_check("an interact cannot interrupt a stumble", avatar.current_state() == WorkerAnimations.STUMBLE)
	_check("after the stumble the worker is walking again", await _reach(avatar, Vector3(0, 0, -2.0), false, false, WorkerAnimations.WALK, 2.0))

	await _reach(avatar, Vector3.ZERO, false, false, WorkerAnimations.IDLE)
	avatar.trigger(WorkerAnimations.CLEAN)
	_check("clean plays", await _reach(avatar, Vector3.ZERO, false, false, WorkerAnimations.CLEAN))
	await _hold(avatar, Vector3.ZERO, 1.6)
	_check("clean keeps looping until stopped", avatar.current_state() == WorkerAnimations.CLEAN)
	_check("moving away ends cleaning", await _reach(avatar, Vector3(0, 0, -2.0), false, false, WorkerAnimations.WALK, 2.0))

	await _reach(avatar, Vector3.ZERO, false, false, WorkerAnimations.IDLE)
	avatar.trigger(WorkerAnimations.CELEBRATE)
	_check("celebrate plays", await _reach(avatar, Vector3.ZERO, false, false, WorkerAnimations.CELEBRATE))
	_check("celebrate returns to idle", await _reach(avatar, Vector3.ZERO, false, false, WorkerAnimations.IDLE, 3.0))

	avatar.trigger(&"not_an_action")
	await get_tree().process_frame
	_check("an unknown action is ignored", avatar.current_state() == WorkerAnimations.IDLE)
	avatar.queue_free()


## Feed the same motion each frame until the avatar reaches a state or time runs out.
func _reach(avatar: WorkerAvatar, velocity: Vector3, sprinting: bool, carrying: bool, state: StringName, seconds: float = 1.5) -> bool:
	var end: float = Time.get_ticks_msec() / 1000.0 + seconds
	while Time.get_ticks_msec() / 1000.0 < end:
		avatar.update_motion(velocity, sprinting, carrying, 1.0 / 60.0)
		if avatar.current_state() == state:
			return true
		await get_tree().process_frame
	return avatar.current_state() == state


func _hold(avatar: WorkerAvatar, velocity: Vector3, seconds: float) -> void:
	var end: float = Time.get_ticks_msec() / 1000.0 + seconds
	while Time.get_ticks_msec() / 1000.0 < end:
		avatar.update_motion(velocity, false, false, 1.0 / 60.0)
		await get_tree().process_frame


# Wobble ------------------------------------------------------------------------

func _test_wobble() -> void:
	var avatar := WorkerAvatar.new()
	avatar.setup(0)
	add_child(avatar)
	await _hold(avatar, Vector3.ZERO, 0.4)
	var rest_tie: float = (avatar.sway_nodes()[1] as Node3D).rotation.length()
	_check("loose parts hang still when the worker is still", rest_tie < 0.01)

	var max_tie: float = 0.0
	var max_head: float = 0.0
	var end: float = Time.get_ticks_msec() / 1000.0 + 0.4
	while Time.get_ticks_msec() / 1000.0 < end:
		avatar.update_motion(Vector3(0, 0, -5.0), false, false, 1.0 / 60.0)
		await get_tree().process_frame
		max_tie = maxf(max_tie, (avatar.sway_nodes()[1] as Node3D).rotation.length())
		max_head = maxf(max_head, (avatar.sway_nodes()[0] as Node3D).rotation.length())
	_check("starting to run swings the tie (peak %.2f rad)" % max_tie, max_tie > 0.03)
	_check("the tie never swings past its limit", max_tie <= 0.7001)
	_check("the head never sways past its limit (peak %.2f rad)" % max_head, max_head <= 0.3201)

	await _hold(avatar, Vector3(0, 0, -5.0), 2.5)
	var settled: float = (avatar.sway_nodes()[1] as Node3D).rotation.length()
	_check("at a steady pace the swing dies down (%.3f rad)" % settled, settled < 0.05)
	avatar.queue_free()


# Controls ----------------------------------------------------------------------

func _test_controls_unaffected() -> void:
	var floor_body := StaticBody3D.new()
	floor_body.collision_layer = Layers.WORLD
	var floor_shape := CollisionShape3D.new()
	var floor_box := BoxShape3D.new()
	floor_box.size = Vector3(30.0, 0.2, 30.0)
	floor_shape.shape = floor_box
	floor_body.add_child(floor_shape)
	floor_body.position = Vector3(2.0, -0.1, 2.0)
	add_child(floor_body)
	var player := Player.new()
	player.setup(&"test", 1, 0, true)
	player.position = Vector3(2.0, 0.05, 2.0)
	player.controls_enabled = false
	add_child(player)
	for settle: int in 40:
		await get_tree().physics_frame
	var eye_start: Vector3 = player.eye_position()
	var hold_start: Vector3 = player.hold_point.position
	var position_start: Vector3 = player.global_position
	_check("the local body throws a shadow but is not drawn", _all_shadow_only(player.avatar()))
	_check("the local player has first-person hands", player.hands() != null)

	player.stumble()
	player.held_prop_id = &"something"
	var eye_drift: float = 0.0
	var hold_drift: float = 0.0
	var position_drift: float = 0.0
	var saw_stumble: bool = false
	for i: int in 90:
		await get_tree().physics_frame
		eye_drift = maxf(eye_drift, player.eye_position().distance_to(eye_start))
		hold_drift = maxf(hold_drift, player.hold_point.position.distance_to(hold_start))
		position_drift = maxf(position_drift, player.global_position.distance_to(position_start))
		if player.avatar().current_state() == WorkerAnimations.STUMBLE:
			saw_stumble = true
	_check("the stumble and wobble really played", saw_stumble)
	_check("the eye (interaction origin) never moved (%.6f m)" % eye_drift, eye_drift < 0.00001)
	_check("the hold point never moved (%.6f m)" % hold_drift, hold_drift < 0.00001)
	_check("the player's position never moved (%.6f m)" % position_drift, position_drift < 0.00001)

	var remote := Player.new()
	remote.setup(&"remote", 2, 1, false)
	remote.position = Vector3(3.0, 0.05, 3.0)
	add_child(remote)
	await get_tree().physics_frame
	_check("a remote player's body is drawn", not _all_shadow_only(remote.avatar()))
	_check("a remote player has no first-person hands", remote.hands() == null)
	player.queue_free()
	remote.queue_free()


func _all_shadow_only(avatar: WorkerPresenter) -> bool:
	var meshes: Array[Node] = avatar.find_children("*", "MeshInstance3D", true, false)
	if meshes.is_empty():
		return false
	for node: Node in meshes:
		if (node as MeshInstance3D).cast_shadow != GeometryInstance3D.SHADOW_CASTING_SETTING_SHADOWS_ONLY:
			return false
	return true


func _check(label: String, condition: bool) -> void:
	_checks += 1
	if condition:
		print("PASS  ", label)
	else:
		_failures += 1
		print("FAIL  ", label)
