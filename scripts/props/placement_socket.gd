class_name PlacementSocket
extends StaticBody3D
## A spot that accepts one kind of prop. Shows a ghost of the prop while the
## player aims at it with a matching prop in hand. Its collider is switched off
## while occupied so the placed prop itself can be aimed at and picked up.

@export var entity_id: StringName = &"socket"
@export var display_name: String = "stand"
@export var accepts_kind: StringName = &"cake_section"
@export var snap_offset: Vector3 = Vector3(0.0, 0.11, 0.0)
@export var ghost_mesh: Mesh

var occupant_id: StringName = &"":
	set(value):
		occupant_id = value
		_refresh_collider()

var _shape: CollisionShape3D
var _ghost: MeshInstance3D


func _ready() -> void:
	collision_layer = Layers.SOCKET
	collision_mask = 0

	var pad := MeshInstance3D.new()
	var pad_mesh := BoxMesh.new()
	pad_mesh.size = Vector3(0.55, 0.04, 0.55)
	var pad_material := StandardMaterial3D.new()
	pad_material.albedo_color = Color(0.15, 0.45, 0.42)
	pad_mesh.material = pad_material
	pad.mesh = pad_mesh
	add_child(pad)

	_shape = CollisionShape3D.new()
	var box := BoxShape3D.new()
	box.size = Vector3(0.7, 0.4, 0.7)
	_shape.shape = box
	_shape.position = Vector3(0.0, 0.18, 0.0)
	add_child(_shape)

	_ghost = MeshInstance3D.new()
	_ghost.mesh = ghost_mesh
	_ghost.position = snap_offset
	var ghost_material := StandardMaterial3D.new()
	ghost_material.albedo_color = Color(0.3, 1.0, 0.6, 0.45)
	ghost_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_ghost.material_override = ghost_material
	_ghost.visible = false
	add_child(_ghost)

	_refresh_collider()


func accepts(prop: PickupProp) -> bool:
	return prop.kind == accepts_kind


func snap_transform() -> Transform3D:
	return global_transform * Transform3D(Basis.IDENTITY, snap_offset)


func set_preview(on: bool) -> void:
	if _ghost != null:
		_ghost.visible = on and occupant_id == &""


func _refresh_collider() -> void:
	if _shape != null:
		_shape.set_deferred("disabled", occupant_id != &"")
