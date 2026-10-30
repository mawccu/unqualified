extends Node
## Acceptance checks for the visual reset, measured on real rendered frames.
## Needs a real window (not --headless):
##   godot --path . res://tests/visual_check.tscn -- <output folder>
##
## It renders the same view twice, with and without the thing being judged, and
## counts how many pixels differ in a screen region. That measures what the thing
## actually covers, not what its geometry claims.

const CENTER_LOWER_THIRD: Rect2 = Rect2(0.33, 0.67, 0.34, 0.33)
const CENTER_BLOCK: Rect2 = Rect2(0.25, 0.25, 0.5, 0.5)
const CROSSHAIR_ZONE: Rect2 = Rect2(0.46, 0.44, 0.08, 0.12)
const DIFF_THRESHOLD: float = 0.06

var _out_dir: String = "user://shots"
var _session: Session
var _player: Player
var _checks: int = 0
var _failures: int = 0


func _ready() -> void:
	var args: PackedStringArray = OS.get_cmdline_user_args()
	if args.size() > 0:
		_out_dir = args[0]
	DirAccess.make_dir_recursive_absolute(_out_dir)
	get_tree().create_timer(120.0).timeout.connect(func() -> void:
		print("FAIL  watchdog")
		get_tree().quit(2))
	_run()


func _run() -> void:
	var packed := load("res://scenes/bootstrap/main.tscn") as PackedScene
	_session = packed.instantiate() as Session
	add_child(_session)
	await _frames(12)
	_player = _session.player
	_session.coordinator.visible = false

	print("worker body in use: %s" % ("authored glb" if WorkerFactory.using_authored_model() else "code-built fallback"))

	# 1. The standard hero view, and the HUD against every sign in it.
	await _view("20_hero_view", VenueLayout.HERO_POSITION, VenueLayout.HERO_YAW, VenueLayout.HERO_PITCH)
	_check_hud_layout()

	# 2. Hands at rest must not be drawn at all.
	await _view("21_idle_view", Vector3(-0.8, 0.05, 4.4), 0.0, -0.25)
	var with_hands: Image = await _grab()
	_player.hands().visible = false
	await _frames(2)
	var without_hands: Image = await _grab()
	_player.hands().visible = true
	var covered_idle: float = _changed_fraction(with_hands, without_hands, CENTER_LOWER_THIRD)
	_check("idle: the hands are not drawn (presence %.2f)" % _player.hands().presence, _player.hands().presence <= 0.03)
	_check("idle: hands cover %.2f%% of the centre lower third (limit 0.5%%)" % (covered_idle * 100.0), covered_idle <= 0.005)

	# 3. A carried prop must not take over the middle of the screen.
	_ask(_pickup(&"cake_base"))
	await _view("22_carrying_view", Vector3(-0.8, 0.05, 4.4), 0.0, -0.2, 40)
	var carrying: Image = await _grab()
	var prop: PickupProp = _session.service.get_prop(&"cake_base")
	prop.visible = false
	await _frames(2)
	var no_prop: Image = await _grab()
	prop.visible = true
	var block: float = _changed_fraction(carrying, no_prop, CENTER_BLOCK)
	var cross: float = _changed_fraction(carrying, no_prop, CROSSHAIR_ZONE)
	var whole: float = _changed_fraction(carrying, no_prop, Rect2(0, 0, 1, 1))
	_check("carrying: the prop covers %.1f%% of the central block (limit 20%%)" % (block * 100.0), block <= 0.20)
	_check("carrying: the crosshair zone stays clear (%.1f%% covered, limit 3%%)" % (cross * 100.0), cross <= 0.03)
	print("      the prop covers %.1f%% of the whole screen" % (whole * 100.0))
	_check("carrying: no hands are drawn while carrying", _player.hands().presence <= 0.03)

	# 4. One brief hand for an action, low in the corner.
	_player.hands().jab()
	await _frames(10)
	var acting: Image = await _grab()
	await _shot_image("23_action_hand", acting)
	_check("action: one hand is drawn for the action", _player.hands().presence > 0.5)
	var hand_pos: Vector3 = _player.hands().hand_position(true)
	_check("action: the hand is in the lower right corner (x %.2f, y %.2f)" % [hand_pos.x, hand_pos.y], hand_pos.x > 0.15 and hand_pos.y < -0.1)
	await _frames(60)
	_check("after the action the hand withdraws", _player.hands().presence <= 0.03)

	print("")
	print("%d checks, %d failed" % [_checks, _failures])
	get_tree().quit(1 if _failures > 0 else 0)


# HUD layout --------------------------------------------------------------------

func _check_hud_layout() -> void:
	var panel: Rect2 = _session.hud.task_panel_rect()
	var viewport: Vector2 = get_viewport().get_visible_rect().size
	_check("HUD: the task board is %.0f%% of the screen width (limit 25%%)" % (panel.size.x / viewport.x * 100.0), panel.size.x / viewport.x <= 0.25)
	_check("HUD: the task board background is see-through (alpha %.2f, limit 0.6)" % _session.hud.task_panel_background_alpha(), _session.hud.task_panel_background_alpha() <= 0.6)
	var camera: Camera3D = get_viewport().get_camera_3d()
	var overlaps: PackedStringArray = []
	var visible_signs: int = 0
	for node: Node in _session.find_children("*", "Label3D", true, false):
		var sign_label := node as Label3D
		if sign_label.text.strip_edges() == "":
			continue
		var rect: Rect2 = _screen_rect(camera, sign_label)
		if rect.size == Vector2.ZERO:
			continue
		if Rect2(Vector2.ZERO, viewport).intersects(rect):
			visible_signs += 1
		if rect.intersects(panel):
			overlaps.append(sign_label.text.replace("\n", " "))
	print("      signs on screen in the hero view: %d" % visible_signs)
	_check("HUD: the task board covers no sign in the hero view %s" % str(overlaps), overlaps.is_empty())


## The screen rectangle a Label3D covers, or an empty rect when it is behind the camera.
func _screen_rect(camera: Camera3D, label: Label3D) -> Rect2:
	var aabb: AABB = label.get_aabb()
	var low := Vector2(INF, INF)
	var high := Vector2(-INF, -INF)
	for i: int in 8:
		var corner: Vector3 = label.global_transform * aabb.get_endpoint(i)
		if camera.is_position_behind(corner):
			return Rect2()
		var p: Vector2 = camera.unproject_position(corner)
		low = low.min(p)
		high = high.max(p)
	return Rect2(low, high - low)


# Pixel measuring ---------------------------------------------------------------

func _changed_fraction(a: Image, b: Image, region: Rect2) -> float:
	var x0: int = int(region.position.x * a.get_width())
	var y0: int = int(region.position.y * a.get_height())
	var x1: int = int((region.position.x + region.size.x) * a.get_width())
	var y1: int = int((region.position.y + region.size.y) * a.get_height())
	var changed: int = 0
	var total: int = 0
	for y: int in range(y0, y1, 2):
		for x: int in range(x0, x1, 2):
			total += 1
			var ca: Color = a.get_pixel(x, y)
			var cb: Color = b.get_pixel(x, y)
			if absf(ca.r - cb.r) + absf(ca.g - cb.g) + absf(ca.b - cb.b) > DIFF_THRESHOLD:
				changed += 1
	return float(changed) / float(maxi(total, 1))


func _grab() -> Image:
	await RenderingServer.frame_post_draw
	return get_viewport().get_texture().get_image()


# Helpers -----------------------------------------------------------------------

func _pickup(prop_id: StringName) -> Dictionary:
	return {"request_id": 1, "actor_id": _player.actor_id, "action": InteractionService.PICKUP,
		"target_id": prop_id, "expected_revision": _session.service.get_prop(prop_id).revision}


func _ask(req: Dictionary) -> void:
	var result: Dictionary = _session.service.request(req)
	if not result["ok"]:
		print("request refused: ", result["reason"])


func _view(shot_name: String, pos: Vector3, yaw: float, pitch: float, settle: int = 16) -> void:
	_session._set_menu(false)
	_player.global_position = pos
	_player.set_look(yaw, pitch)
	await _frames(settle)
	await _shot_image(shot_name, await _grab())


func _shot_image(shot_name: String, image: Image) -> void:
	var err: Error = image.save_png("%s/%s.png" % [_out_dir, shot_name])
	print("%s %s" % ["saved" if err == OK else "FAILED", shot_name])


func _frames(count: int) -> void:
	for i: int in count:
		await get_tree().process_frame


func _check(label: String, condition: bool) -> void:
	_checks += 1
	if condition:
		print("PASS  ", label)
	else:
		_failures += 1
		print("FAIL  ", label)
