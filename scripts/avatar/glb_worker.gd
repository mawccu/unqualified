class_name GlbWorker
extends WorkerPresenter
## A worker shown from an authored, rigged .glb model at MODEL_PATH. This is the
## real art path. The model must follow docs/assets/WORKER_GLB_CONTRACT.md:
## a Skeleton3D, an AnimationPlayer with the nine clips in WorkerAnimations, and
## materials / meshes named so the four costumes can be applied. A model that
## does not follow the contract is rejected with a warning and the fallback
## mannequin is used instead.
##
## The model is visual only. The player's collision shape, eye and hold point
## are separate and never depend on it.

const MODEL_PATH: String = "res://assets/characters/worker.glb"
## glTF models face +Z; this game faces -Z.
const FACING_FIX: float = PI
const ROLES: Array[String] = ["shirt", "vest", "trousers", "shoes", "skin", "hair", "accent"]

static var _usable: int = -1
static var _problems: PackedStringArray = []

var _model: Node3D
## Every tinted surface as [mesh instance, surface index, material], so the
## overrides can be released when the worker leaves the scene and restored if it returns.
var _tinted: Array = []


static func model_available() -> bool:
	return ResourceLoader.exists(MODEL_PATH)


## True when the file exists AND follows the contract. Checked once.
static func usable() -> bool:
	if _usable == -1:
		_usable = 0
		if model_available():
			var scene := load(MODEL_PATH) as PackedScene
			if scene != null:
				var probe: Node = scene.instantiate()
				_problems = validate_model(probe)
				probe.free()
				_usable = 1 if _problems.is_empty() else 0
				if _usable == 0:
					push_warning("worker.glb does not follow the contract: %s" % ", ".join(_problems))
	return _usable == 1


static func problems() -> PackedStringArray:
	usable()
	return _problems


## Returns what is wrong with an instanced model. Empty means usable.
static func validate_model(root: Node) -> PackedStringArray:
	var out: PackedStringArray = []
	if root.find_children("*", "Skeleton3D", true, false).is_empty():
		out.append("no Skeleton3D")
	if root.find_children("*", "MeshInstance3D", true, false).is_empty():
		out.append("no mesh")
	var players: Array[Node] = root.find_children("*", "AnimationPlayer", true, false)
	if players.is_empty():
		out.append("no AnimationPlayer")
	else:
		var anim_player := players[0] as AnimationPlayer
		for clip: StringName in WorkerAnimations.clip_names():
			if not anim_player.has_animation(clip):
				out.append("missing clip '%s'" % clip)
	return out


func _build_visuals() -> void:
	var scene := load(MODEL_PATH) as PackedScene
	_model = scene.instantiate() as Node3D
	_model.name = "Model"
	_model.rotation.y = FACING_FIX
	add_child(_model)
	_apply_costume()


func _make_player() -> AnimationPlayer:
	var anim_player := _model.find_children("*", "AnimationPlayer", true, false)[0] as AnimationPlayer
	# glTF does not carry loop flags reliably, so set them from the contract.
	for clip: StringName in anim_player.get_animation_list():
		var anim: Animation = anim_player.get_animation(clip)
		anim.loop_mode = Animation.LOOP_LINEAR if WorkerAnimations.LOOPING.has(clip) else Animation.LOOP_NONE
	return anim_player


## Tint the model's named materials and show only this costume's hair and accessories.
func _apply_costume() -> void:
	var style: String = str(costume["hair_style"])
	var accessories: Array = costume["accessories"]
	for node: Node in _model.find_children("*", "MeshInstance3D", true, false):
		var instance := node as MeshInstance3D
		var node_name: String = instance.name.to_lower()
		if node_name.begins_with("hair_"):
			instance.visible = node_name == "hair_%s" % style
		elif node_name.begins_with("acc_"):
			instance.visible = accessories.has(StringName(node_name.trim_prefix("acc_")))
		if instance.mesh == null:
			continue
		for surface: int in instance.mesh.get_surface_count():
			var source := instance.get_active_material(surface) as StandardMaterial3D
			if source == null:
				continue
			var role: String = source.resource_name.to_lower()
			if ROLES.has(role):
				# A fresh material, not a duplicate: the headless renderer logs errors for duplicates.
				var tinted := StandardMaterial3D.new()
				tinted.resource_name = source.resource_name
				tinted.roughness = source.roughness
				tinted.metallic = source.metallic
				tinted.albedo_color = costume[role]
				instance.set_surface_override_material(surface, tinted)
				_tinted.append([instance, surface, tinted])


func _enter_tree() -> void:
	# Coming back after leaving the tree (reparenting): put the tint back.
	for entry: Array in _tinted:
		if is_instance_valid(entry[0]):
			(entry[0] as MeshInstance3D).set_surface_override_material(int(entry[1]), entry[2])


func _exit_tree() -> void:
	# Release the overrides while the renderer is still alive. Leaving them to be
	# freed during engine shutdown makes the headless renderer log errors.
	for entry: Array in _tinted:
		if is_instance_valid(entry[0]):
			(entry[0] as MeshInstance3D).set_surface_override_material(int(entry[1]), null)
