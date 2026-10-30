class_name PlacementSocket
extends StaticBody3D
## A spot that accepts one kind of prop. It does not care WHICH prop of that
## kind: putting the wrong one in is a mistake the crew can notice and fix, and
## the task logic reports it. While the player aims at it with a matching prop
## in hand it shows a ghost of that prop. Its collider is switched off while
## occupied so the placed prop itself can be aimed at and picked up.
##
## requires_socket lets a layer wait for the one below it (cake tiers).

@export var entity_id: StringName = &"socket"
@export var display_name: String = "stand"
@export var accepts_kind: StringName = &"cake_tier"
@export var requires_socket: StringName = &""
@export var pad_radius: float = 0.3
@export var show_pad: bool = true

var occupant_id: StringName = &"":
	set(value):
		occupant_id = value
		_refresh_collider()

var _shape: CollisionShape3D
var _ghost: MeshInstance3D
var _pad: MeshInstance3D


func _ready() -> void:
	collision_layer = Layers.SOCKET
	collision_mask = 0

	if show_pad:
		_pad = MeshInstance3D.new()
		var pad_mesh := CylinderMesh.new()
		pad_mesh.top_radius = pad_radius
		pad_mesh.bottom_radius = pad_radius
		pad_mesh.height = 0.012
		var pad_material := StandardMaterial3D.new()
		pad_material.albedo_color = Color(0.2, 0.75, 0.65, 0.55)
		pad_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
		pad_material.emission_enabled = true
		pad_material.emission = Color(0.2, 0.75, 0.65)
		pad_material.emission_energy_multiplier = 0.6
		pad_mesh.material = pad_material
		_pad.mesh = pad_mesh
		_pad.position = Vector3(0.0, 0.012, 0.0)
		add_child(_pad)

	_shape = CollisionShape3D.new()
	var cyl := CylinderShape3D.new()
	cyl.radius = maxf(pad_radius, 0.2)
	cyl.height = 0.4
	_shape.shape = cyl
	_shape.position = Vector3(0.0, 0.2, 0.0)
	add_child(_shape)

	_ghost = MeshInstance3D.new()
	var ghost_material := StandardMaterial3D.new()
	ghost_material.albedo_color = Color(0.3, 1.0, 0.6, 0.45)
	ghost_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_ghost.material_override = ghost_material
	_ghost.visible = false
	add_child(_ghost)

	_refresh_collider()


func accepts(prop: PickupProp) -> bool:
	return prop.kind == accepts_kind


## Where a prop comes to rest on this socket: its own half height above the pad.
func snap_transform_for(prop: PickupProp) -> Transform3D:
	return global_transform * Transform3D(Basis.IDENTITY, Vector3(0.0, prop.half_height + 0.006, 0.0))


## Show or hide the ghost. prop is the one in the player's hand.
func set_preview(on: bool, prop: PickupProp = null) -> void:
	if _ghost == null:
		return
	if on and prop != null and occupant_id == &"":
		_ghost.mesh = prop.visual_mesh
		_ghost.position = Vector3(0.0, prop.half_height + 0.006, 0.0)
		_ghost.visible = true
	else:
		_ghost.visible = false


func _refresh_collider() -> void:
	if _shape != null:
		_shape.set_deferred("disabled", occupant_id != &"")
	if _pad != null:
		_pad.visible = occupant_id == &""
