class_name WeddingAssets
extends RefCounted
## Loads the models from the wedding asset pack (assets/wedding/*.glb). The pack
## is static art: origins sit at the ground contact centre, fronts face +Z, and
## there is no collision or gameplay in them. Those are added by the code that
## places them. Provenance and licence are in docs/assets/ASSET_REGISTER.md.

const PATH: String = "res://assets/wedding/%s.glb"

static var _scenes: Dictionary = {}


static func scene(id: String) -> PackedScene:
	if not _scenes.has(id):
		_scenes[id] = load(PATH % id) as PackedScene
	return _scenes[id]


static func instance(id: String) -> Node3D:
	return scene(id).instantiate() as Node3D


## The first mesh instance of a model, with its transform inside the model.
static func first_mesh_instance(root: Node) -> MeshInstance3D:
	var found: Array[Node] = root.find_children("*", "MeshInstance3D", true, false)
	return found[0] as MeshInstance3D if not found.is_empty() else null


## The model's mesh and where it sits relative to the model root, so one mesh
## can be drawn many times with a MultiMesh and still land in the right place.
static func mesh_and_base(id: String) -> Array:
	var root: Node3D = instance(id)
	var holder := Node3D.new()
	holder.add_child(root)
	var mesh_instance: MeshInstance3D = first_mesh_instance(root)
	var mesh: Mesh = mesh_instance.mesh
	var base: Transform3D = root.transform * _relative_transform(mesh_instance, root)
	holder.free()
	return [mesh, base]


static func bounds(id: String) -> AABB:
	var root: Node3D = instance(id)
	var holder := Node3D.new()
	holder.add_child(root)
	var out := AABB()
	var first: bool = true
	for node: Node in root.find_children("*", "MeshInstance3D", true, false):
		var mi := node as MeshInstance3D
		var box: AABB = _relative_transform(mi, root) * mi.get_aabb()
		out = box if first else out.merge(box)
		first = false
	holder.free()
	return out


static func _relative_transform(node: Node3D, root: Node3D) -> Transform3D:
	var t: Transform3D = Transform3D.IDENTITY
	var current: Node = node
	while current != null and current != root:
		t = (current as Node3D).transform * t
		current = current.get_parent()
	return t
