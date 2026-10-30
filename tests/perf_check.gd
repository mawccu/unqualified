extends Node
## Measures real frame times in a real window while the player walks around the
## venue, so "it is not smooth" becomes numbers.
##   godot --path . res://tests/perf_check.tscn -- [--windowed-size=WxH]
## Prints average and worst frame times, the 1% worst frames, hitches (frames
## over 25 ms), and what the renderer is drawing.

const FRAMES: int = 600
const WARMUP: int = 90

var _session: Session
var _player: Player
var _times: Array[float] = []


func _ready() -> void:
	get_tree().create_timer(120.0).timeout.connect(func() -> void:
		print("FAIL  watchdog")
		get_tree().quit(2))
	_run()


func _run() -> void:
	var packed := load("res://scenes/bootstrap/main.tscn") as PackedScene
	_session = packed.instantiate() as Session
	add_child(_session)
	await _frames(WARMUP)
	_player = _session.player
	_session._set_menu(false)
	print("renderer: %s, window: %s, vsync: %s" % [RenderingServer.get_video_adapter_name(), DisplayServer.window_get_size(), DisplayServer.window_get_vsync_mode()])

	var scenarios: Array[Dictionary] = [
		{"name": "hero view (stands still)", "pos": VenueLayout.HERO_POSITION, "yaw": VenueLayout.HERO_YAW, "pitch": VenueLayout.HERO_PITCH, "walk": false},
		{"name": "looking at the ballroom from the middle", "pos": Vector3(0.0, 0.05, 0.6), "yaw": 0.0, "pitch": 0.0, "walk": false},
		{"name": "walking through the ballroom", "pos": Vector3(-8.0, 0.05, 0.8), "yaw": -1.5708, "pitch": 0.0, "walk": true},
	]
	print("##### graphics quality: %s (the default, measured once from a clean start)" % Quality.NAMES[Settings.quality])
	await _frames(60)
	await _measure(scenarios[0])
	await _tour()
	get_tree().quit(0)


func _measure(scenario: Dictionary) -> void:
	_player.global_position = scenario["pos"]
	_player.set_look(scenario["yaw"], scenario["pitch"])
	await _frames(30)
	_times.clear()
	var last: int = Time.get_ticks_usec()
	for i: int in FRAMES:
		if scenario["walk"]:
			_player.global_position.x += 0.012
		await get_tree().process_frame
		var now: int = Time.get_ticks_usec()
		_times.append(float(now - last) / 1000.0)
		last = now
	_report(str(scenario["name"]))


func _report(label: String) -> void:
	var sorted: Array[float] = _times.duplicate()
	sorted.sort()
	var total: float = 0.0
	var hitches: int = 0
	for t: float in _times:
		total += t
		if t > 25.0:
			hitches += 1
	var avg: float = total / float(_times.size())
	var p99: float = sorted[int(float(sorted.size()) * 0.99)]
	print("")
	print("== %s" % label)
	print("   average %.2f ms (%.0f fps), median %.2f ms, 99th percentile %.2f ms, worst %.2f ms, hitches over 25 ms: %d of %d" % [
		avg, 1000.0 / avg, sorted[sorted.size() / 2], p99, sorted[sorted.size() - 1], hitches, _times.size()])
	print("   draw calls %d, objects drawn %d, triangles %d, nodes %d, physics bodies active %d" % [
		int(Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME)),
		int(Performance.get_monitor(Performance.RENDER_TOTAL_OBJECTS_IN_FRAME)),
		int(Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME)),
		int(Performance.get_monitor(Performance.OBJECT_NODE_COUNT)),
		int(Performance.get_monitor(Performance.PHYSICS_3D_ACTIVE_OBJECTS))])
	print("   process time %.2f ms, physics time %.2f ms" % [
		Performance.get_monitor(Performance.TIME_PROCESS) * 1000.0, Performance.get_monitor(Performance.TIME_PHYSICS_PROCESS) * 1000.0])


func _frames(count: int) -> void:
	for i: int in count:
		await get_tree().process_frame


## Holds the move key and measures how evenly the camera advances from one drawn
## frame to the next. On a screen faster than the physics rate, an uneven step
## (many frames that do not move, then one that jumps) is what judder feels like.
func _judder() -> void:
	_session._set_menu(false)
	_player.controls_enabled = true
	_player.global_position = Vector3(-9.0, 0.05, 0.8)
	_player.velocity = Vector3.ZERO
	_player.set_look(-1.5708, 0.0)
	await _frames(20)
	Input.action_press(&"move_forward")
	await _frames(30)
	var steps: Array[float] = []
	var frame_ms: Array[float] = []
	var frame_clock: int = Time.get_ticks_usec()
	var last: float = _player.global_position.x
	for i: int in 240:
		await get_tree().process_frame
		var now: float = _player.global_position.x
		steps.append(now - last)
		var tick: int = Time.get_ticks_usec()
		frame_ms.append(float(tick - frame_clock) / 1000.0)
		frame_clock = tick
		last = now
	Input.action_release(&"move_forward")
	var zero: int = 0
	var total: float = 0.0
	for s: float in steps:
		if absf(s) < 0.00001:
			zero += 1
		total += s
	var mean: float = total / float(steps.size())
	var variance: float = 0.0
	for s: float in steps:
		variance += (s - mean) * (s - mean)
	var deviation: float = sqrt(variance / float(steps.size()))
	print("")
	print("== judder: walking forward for %d drawn frames (physics %d Hz, display %.0f Hz)" % [steps.size(), Engine.physics_ticks_per_second, DisplayServer.screen_get_refresh_rate()])
	print("   frames where the camera did not move: %d of %d (%.0f%%)" % [zero, steps.size(), 100.0 * float(zero) / float(steps.size())])
	var sum_ms: float = 0.0
	var worst_ms: float = 0.0
	for f: float in frame_ms:
		sum_ms += f
		worst_ms = maxf(worst_ms, f)
	print("   engine process time %.2f ms, physics time %.2f ms, draw calls %d, objects %d" % [Performance.get_monitor(Performance.TIME_PROCESS) * 1000.0, Performance.get_monitor(Performance.TIME_PHYSICS_PROCESS) * 1000.0, int(Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME)), int(Performance.get_monitor(Performance.RENDER_TOTAL_OBJECTS_IN_FRAME))])
	print("   frame time while walking: average %.2f ms (%.0f fps), worst %.2f ms" % [sum_ms / float(frame_ms.size()), 1000.0 / (sum_ms / float(frame_ms.size())), worst_ms])
	print("   step per frame: mean %.4f m, deviation %.4f m, unevenness %.0f%% (0%% is perfectly smooth)" % [mean, deviation, 100.0 * deviation / maxf(absf(mean), 0.00001)])


## Switches parts of the game off one at a time and times a walk each time.
func _bisect() -> void:
	var variants: Array[Dictionary] = [
		{"name": "everything on", "off": []},
		{"name": "Session._process off (task refresh)", "off": [_session]},
		{"name": "Player._process off (aiming)", "off": [_player]},
		{"name": "HUD off", "off": [_session.hud]},
		{"name": "net node off", "off": [_session.net]},
		{"name": "coordinator off", "off": [_session.coordinator]},
	]
	for variant: Dictionary in variants:
		for node: Node in variant["off"]:
			node.set_process(false)
		_session._set_menu(false)
		_player.global_position = Vector3(-9.0, 0.05, 0.8)
		_player.velocity = Vector3.ZERO
		_player.set_look(-1.5708, 0.0)
		await _frames(10)
		Input.action_press(&"move_forward")
		await _frames(20)
		var start: int = Time.get_ticks_usec()
		for i: int in 120:
			await get_tree().process_frame
		var avg: float = float(Time.get_ticks_usec() - start) / 1000.0 / 120.0
		Input.action_release(&"move_forward")
		for node: Node in variant["off"]:
			node.set_process(true)
		print("   walk with %-40s %.2f ms per frame (%.0f fps)" % [variant["name"], avg, 1000.0 / avg])


func _still_vs_moving() -> void:
	_session._set_menu(false)
	_player.global_position = Vector3(-9.0, 0.05, 0.8)
	_player.set_look(-1.5708, 0.0)
	await _frames(30)
	var start: int = Time.get_ticks_usec()
	for i: int in 120:
		await get_tree().process_frame
	var still: float = float(Time.get_ticks_usec() - start) / 1000.0 / 120.0
	print("   STILL, no input:                  %.2f ms per frame (%.0f fps)" % [still, 1000.0 / still])
	Input.action_press(&"move_forward")
	await _frames(20)
	start = Time.get_ticks_usec()
	for i: int in 120:
		await get_tree().process_frame
	var moving: float = float(Time.get_ticks_usec() - start) / 1000.0 / 120.0
	Input.action_release(&"move_forward")
	print("   MOVING (move key held):           %.2f ms per frame (%.0f fps)" % [moving, 1000.0 / moving])
	_player.global_position = Vector3(-9.0, 0.05, 0.8)
	_player.velocity = Vector3.ZERO
	await _frames(30)
	start = Time.get_ticks_usec()
	for i: int in 120:
		_player.global_position.x += 0.01
		await get_tree().process_frame
	var scripted: float = float(Time.get_ticks_usec() - start) / 1000.0 / 120.0
	print("   MOVED BY SCRIPT (no physics):     %.2f ms per frame (%.0f fps)" % [scripted, 1000.0 / scripted])


## Visits every part of the venue in turn and reports the worst frame in each.
## Run it after deleting the shader cache to see the first-run stutter.
func _tour() -> void:
	var stops: Array[Dictionary] = [
		{"name": "back of house start", "pos": Vector3(0.0, 0.05, 5.4), "yaw": 0.0, "pitch": -0.1},
		{"name": "prep table", "pos": Vector3(-0.8, 0.05, 4.4), "yaw": 0.0, "pitch": -0.3},
		{"name": "archway looking into the ballroom", "pos": Vector3(-6.0, 0.05, 1.8), "yaw": -0.5, "pitch": 0.0},
		{"name": "stage and sound desk", "pos": Vector3(0.0, 0.05, 0.4), "yaw": 0.0, "pitch": 0.1},
		{"name": "dessert station", "pos": Vector3(6.6, 0.05, -0.8), "yaw": -1.5708, "pitch": -0.25},
		{"name": "guest tables", "pos": Vector3(-3.5, 0.05, -0.3), "yaw": 1.3, "pitch": -0.2},
		{"name": "photo corner", "pos": Vector3(-6.5, 0.05, -2.0), "yaw": 1.5708, "pitch": 0.0},
		{"name": "wash-up and storage", "pos": Vector3(5.0, 0.05, 4.0), "yaw": 0.6, "pitch": 0.0},
	]
	print("")
	print("== first-visit tour (worst frame at each stop)")
	var total_hitches: int = 0
	for stop: Dictionary in stops:
		_player.global_position = stop["pos"]
		_player.set_look(stop["yaw"], stop["pitch"])
		var worst: float = 0.0
		var last: int = Time.get_ticks_usec()
		for i: int in 40:
			await get_tree().process_frame
			var now: int = Time.get_ticks_usec()
			worst = maxf(worst, float(now - last) / 1000.0)
			last = now
		if worst > 33.0:
			total_hitches += 1
		print("   %-36s worst frame %7.1f ms %s" % [stop["name"], worst, "  <-- stutter" if worst > 33.0 else ""])
	print("   stops with a visible stutter (over 33 ms): %d of %d" % [total_hitches, stops.size()])
