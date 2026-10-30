extends Node
## Pure checks of the task logic. No game window, no nodes from the venue.
##   godot --headless --path . res://tests/task_board_test.tscn

var _checks: int = 0
var _failures: int = 0


func _ready() -> void:
	var tasks: Array[Dictionary] = TaskBoard.catalog()

	var empty: Array[Dictionary] = TaskBoard.evaluate(tasks, {})
	_check("an empty room: dessert not started", _status(empty, &"dessert") == TaskBoard.Status.NOT_STARTED)
	_check("an empty room: seating not started", _status(empty, &"seating") == TaskBoard.Status.NOT_STARTED)
	_check("audio is reported as not built yet", _status(empty, &"audio") == TaskBoard.Status.NOT_BUILT)
	_check("nothing is done in an empty room", not TaskBoard.all_done(empty))

	var one: Array[Dictionary] = TaskBoard.evaluate(tasks, {&"cake_socket_base": &"cake_base"})
	_check("one right layer is in progress", _status(one, &"dessert") == TaskBoard.Status.IN_PROGRESS)
	_check("one right layer reads '1 of 3 layers'", _text(one, &"dessert") == "1 of 3 layers")

	var full: Dictionary = {
		&"cake_socket_base": &"cake_base",
		&"cake_socket_middle": &"cake_middle",
		&"cake_socket_top": &"cake_top",
	}
	var cake_done: Array[Dictionary] = TaskBoard.evaluate(tasks, full)
	_check("all three layers in order is done", _status(cake_done, &"dessert") == TaskBoard.Status.DONE)
	_check("done reads 'Done'", _text(cake_done, &"dessert") == "Done")
	_check("cake alone does not finish the shift", not TaskBoard.all_done(cake_done))

	var wrong_order: Dictionary = {
		&"cake_socket_base": &"cake_middle",
		&"cake_socket_middle": &"cake_base",
	}
	var swapped: Array[Dictionary] = TaskBoard.evaluate(tasks, wrong_order)
	_check("layers in the wrong order need fixing", _status(swapped, &"dessert") == TaskBoard.Status.NEEDS_FIXING)
	_check("wrong order counts two wrong, none right", _field(swapped, &"dessert", "wrong") == 2 and _field(swapped, &"dessert", "correct") == 0)

	var mixed: Dictionary = {
		&"table_socket_1": &"sign_2",
		&"table_socket_2": &"sign_1",
		&"table_socket_3": &"sign_3",
	}
	var signs: Array[Dictionary] = TaskBoard.evaluate(tasks, mixed)
	_check("two swapped signs and one right: needs fixing", _status(signs, &"seating") == TaskBoard.Status.NEEDS_FIXING)
	_check("two swapped signs count 1 right, 2 wrong", _field(signs, &"seating", "correct") == 1 and _field(signs, &"seating", "wrong") == 2)
	_check("the text says what is wrong", _text(signs, &"seating") == "1 of 3 signs right, 2 in the wrong place")

	var fixed: Dictionary = {
		&"table_socket_1": &"sign_1",
		&"table_socket_2": &"sign_2",
		&"table_socket_3": &"sign_3",
	}
	var seating_done: Array[Dictionary] = TaskBoard.evaluate(tasks, fixed)
	_check("each sign on its own table is done", _status(seating_done, &"seating") == TaskBoard.Status.DONE)

	var both: Dictionary = full.duplicate()
	both.merge(fixed)
	var all: Array[Dictionary] = TaskBoard.evaluate(tasks, both)
	_check("cake and seating done, audio not built: all built tasks are done", TaskBoard.all_done(all))

	var stray: Array[Dictionary] = TaskBoard.evaluate(tasks, {&"no_such_socket": &"sign_1"})
	_check("a prop in an unknown socket changes nothing", _status(stray, &"seating") == TaskBoard.Status.NOT_STARTED)

	var repaired: Dictionary = wrong_order.duplicate()
	repaired[&"cake_socket_base"] = &"cake_base"
	repaired[&"cake_socket_middle"] = &"cake_middle"
	var after_fix: Array[Dictionary] = TaskBoard.evaluate(tasks, repaired)
	_check("putting the layers right removes the wrong count", _field(after_fix, &"dessert", "wrong") == 0 and _field(after_fix, &"dessert", "correct") == 2)

	_check("the catalog has exactly three outcomes", tasks.size() == 3)
	_check("every placement names a socket that the venue builds", _sockets_exist(tasks))

	print("")
	print("%d checks, %d failed" % [_checks, _failures])
	get_tree().quit(1 if _failures > 0 else 0)


func _sockets_exist(tasks: Array[Dictionary]) -> bool:
	var known: Array[StringName] = []
	known.append_array(VenueLayout.CAKE_SOCKETS)
	known.append_array(VenueLayout.TABLE_SOCKETS)
	var props: Array[StringName] = []
	for layer: Dictionary in VenueLayout.CAKE_LAYERS:
		props.append(StringName(layer["id"]))
	props.append_array(VenueLayout.SIGN_IDS)
	for task: Dictionary in tasks:
		var placements: Dictionary = task["placements"]
		for socket_id: StringName in placements:
			if not known.has(socket_id) or not props.has(placements[socket_id]):
				return false
	return true


func _find(results: Array[Dictionary], id: StringName) -> Dictionary:
	for result: Dictionary in results:
		if result["id"] == id:
			return result
	return {}


func _status(results: Array[Dictionary], id: StringName) -> int:
	return int(_find(results, id)["status"])


func _text(results: Array[Dictionary], id: StringName) -> String:
	return str(_find(results, id)["text"])


func _field(results: Array[Dictionary], id: StringName, key: String) -> int:
	return int(_find(results, id)[key])


func _check(label: String, condition: bool) -> void:
	_checks += 1
	if condition:
		print("PASS  ", label)
	else:
		_failures += 1
		print("FAIL  ", label)
