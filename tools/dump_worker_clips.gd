extends SceneTree
## Writes the nine worker clips from WorkerAnimations to JSON so the Blender
## script bakes the SAME animation onto the authored rig (one source of truth).
##   godot --headless --path . -s tools/dump_worker_clips.gd -- <output.json>

const JOINT_NAMES: Dictionary = {
	"Hips": "Hips", "Torso": "Torso", "HeadPivot": "Head",
	"ArmL": "ArmL", "ElbowL": "ElbowL", "ArmR": "ArmR", "ElbowR": "ElbowR",
	"LegL": "LegL", "LegR": "LegR",
}


func _initialize() -> void:
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var out_path: String = args[0] if args.size() > 0 else "worker_clips.json"
	var library: AnimationLibrary = WorkerAnimations.build_library()
	var data: Dictionary = {}
	for clip: StringName in WorkerAnimations.clip_names():
		var anim: Animation = library.get_animation(clip)
		var tracks: Array = []
		for t: int in anim.get_track_count():
			var path: String = str(anim.track_get_path(t))
			var node_path: String = path.get_slice(":", 0)
			var prop: String = path.get_slice(":", 1)
			var joint: String = JOINT_NAMES[node_path.get_file()]
			var keys: Array = []
			for k: int in anim.track_get_key_count(t):
				var v: Vector3 = anim.track_get_key_value(t, k)
				keys.append([anim.track_get_key_time(t, k), [v.x, v.y, v.z]])
			tracks.append({"joint": joint, "prop": prop, "keys": keys})
		data[str(clip)] = {"length": anim.length, "loop": anim.loop_mode == Animation.LOOP_LINEAR, "tracks": tracks}
	var file := FileAccess.open(out_path, FileAccess.WRITE)
	file.store_string(JSON.stringify(data))
	file.close()
	print("wrote %d clips to %s" % [data.size(), out_path])
	quit(0)
