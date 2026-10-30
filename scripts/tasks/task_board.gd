class_name TaskBoard
extends RefCounted
## Pure task logic: given which prop sits in which socket, work out how each
## of the shift's essential outcomes is doing. No nodes, no network, no input,
## so it can be tested directly and every peer computes the same answer from
## the same replicated socket state.
##
## A task is "placement" based: each requirement says which prop belongs in
## which socket. Anything else in that socket counts as wrong, not missing, so
## swapped signs or a layer in the wrong order are visible mistakes the crew
## can notice and fix, not blocked actions.

enum Status { NOT_STARTED, IN_PROGRESS, NEEDS_FIXING, DONE, NOT_BUILT }

const DESSERT: StringName = &"dessert"
const AUDIO: StringName = &"audio"
const SEATING: StringName = &"seating"


## The three essential outcomes of the Wedding Crew shift.
static func catalog() -> Array[Dictionary]:
	return [
		{
			"id": DESSERT,
			"name": "Dessert display",
			"goal": "Stack the three cake layers on the stand, biggest at the bottom",
			"unit": "layers",
			"placements": {
				&"cake_socket_base": &"cake_base",
				&"cake_socket_middle": &"cake_middle",
				&"cake_socket_top": &"cake_top",
			},
		},
		{
			"id": AUDIO,
			"name": "Ceremony audio",
			"goal": "Set up the sound desk for the ceremony",
			"unit": "steps",
			"placements": {},
			"not_built": true,
		},
		{
			"id": SEATING,
			"name": "Guest seating",
			"goal": "Put each table sign on its own table",
			"unit": "signs",
			"placements": {
				&"table_socket_1": &"sign_1",
				&"table_socket_2": &"sign_2",
				&"table_socket_3": &"sign_3",
			},
		},
	]


## occupancy maps socket id to the id of the prop sitting in it (absent when empty).
## Returns one dictionary per task: id, name, goal, status, correct, wrong, total, text.
static func evaluate(tasks: Array[Dictionary], occupancy: Dictionary) -> Array[Dictionary]:
	var out: Array[Dictionary] = []
	for task: Dictionary in tasks:
		out.append(_evaluate_one(task, occupancy))
	return out


static func _evaluate_one(task: Dictionary, occupancy: Dictionary) -> Dictionary:
	var placements: Dictionary = task["placements"]
	var total: int = placements.size()
	var correct: int = 0
	var wrong: int = 0
	for socket_id: StringName in placements:
		var occupant: StringName = occupancy.get(socket_id, &"")
		if occupant == &"":
			continue
		if occupant == placements[socket_id]:
			correct += 1
		else:
			wrong += 1
	var status: Status = Status.NOT_STARTED
	if bool(task.get("not_built", false)):
		status = Status.NOT_BUILT
	elif correct == total and total > 0:
		status = Status.DONE
	elif wrong > 0:
		status = Status.NEEDS_FIXING
	elif correct > 0:
		status = Status.IN_PROGRESS
	return {
		"id": task["id"],
		"name": task["name"],
		"goal": task["goal"],
		"status": status,
		"correct": correct,
		"wrong": wrong,
		"total": total,
		"text": status_text(status, correct, wrong, total, str(task["unit"])),
	}


static func status_text(status: Status, correct: int, wrong: int, total: int, unit: String) -> String:
	match status:
		Status.DONE:
			return "Done"
		Status.NEEDS_FIXING:
			return "%d of %d %s right, %d in the wrong place" % [correct, total, unit, wrong]
		Status.IN_PROGRESS:
			return "%d of %d %s" % [correct, total, unit]
		Status.NOT_BUILT:
			return "Coming in the next build"
	return "Not started"


## True when every built task is done.
static func all_done(results: Array[Dictionary]) -> bool:
	for result: Dictionary in results:
		var status: int = int(result["status"])
		if status != Status.DONE and status != Status.NOT_BUILT:
			return false
	return true
