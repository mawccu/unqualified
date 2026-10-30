class_name VenueLayout
extends RefCounted
## Where everything is in the hotel. Shared by the builder, the session, the
## network clamp and the tests so nothing hard-codes a coordinate twice.
##
## Plan (top view, +z is south, units are metres):
##   z -7 .. 2   public ballroom: stage and sound desk (north), dance floor,
##               three guest tables (west and north-east), dessert station (east),
##               photo display (west wall)
##   z  2        dividing wall with two archways at x = -6 and x = +6
##   z  2 .. 7   back of house: prep table with the cake layers, sign table,
##               storage shelves (west), wash-up nook (east), service notes

const BOUNDS_MIN: Vector2 = Vector2(-9.6, -6.6)
const BOUNDS_MAX: Vector2 = Vector2(9.6, 6.6)
const CEILING: float = 3.5
const WALL_HEIGHT: float = 3.4
const DIVIDER_Z: float = 2.0
const DOOR_X: float = 6.0
const DOOR_WIDTH: float = 1.6

const SPAWN_ORIGIN: Vector3 = Vector3(-1.2, 0.05, 5.0)
const SPAWN_SPACING: float = 0.8

const TABLE_TOP: float = 0.84
const GUEST_TABLES: Array[Vector3] = [
	Vector3(-6.2, 0.0, -3.6),
	Vector3(-6.2, 0.0, -0.4),
	Vector3(5.6, 0.0, -3.6),
]

const SOUND_DESK: Vector3 = Vector3(3.2, 0.0, -3.0)
const PHOTO_DISPLAY: Vector3 = Vector3(-9.2, 0.0, -2.0)

## Centre of the cake stand plate and the height of its top surface.
const DESSERT_STAND: Vector3 = Vector3(8.9, 0.88, -0.8)

const PREP_TABLE: Vector3 = Vector3(0.0, 0.0, 3.4)
const PREP_TOP: float = 0.9
const SIGN_TABLE: Vector3 = Vector3(-5.5, 0.0, 3.6)
const SIGN_TABLE_TOP: float = 0.8

## Cake layers, bottom to top: id, radius, height.
const CAKE_LAYERS: Array[Dictionary] = [
	{"id": &"cake_base", "name": "bottom cake layer", "radius": 0.32, "height": 0.22, "color": Color(0.98, 0.95, 0.9)},
	{"id": &"cake_middle", "name": "middle cake layer", "radius": 0.23, "height": 0.20, "color": Color(0.98, 0.78, 0.84)},
	{"id": &"cake_top", "name": "top cake layer", "radius": 0.15, "height": 0.18, "color": Color(0.84, 0.78, 0.96)},
]

const CAKE_SOCKETS: Array[StringName] = [&"cake_socket_base", &"cake_socket_middle", &"cake_socket_top"]
const TABLE_SOCKETS: Array[StringName] = [&"table_socket_1", &"table_socket_2", &"table_socket_3"]
const SIGN_IDS: Array[StringName] = [&"sign_1", &"sign_2", &"sign_3"]


static func spawn_position(slot: int) -> Vector3:
	return SPAWN_ORIGIN + Vector3(SPAWN_SPACING * slot, 0.0, 0.0)


static func cake_layer_position(layer: int) -> Vector3:
	var origin: Vector3 = DESSERT_STAND
	var y: float = origin.y
	for i: int in layer:
		y += float(CAKE_LAYERS[i]["height"])
	return Vector3(origin.x, y, origin.z)


static func inside_bounds(pos: Vector3) -> bool:
	return pos.x >= BOUNDS_MIN.x and pos.x <= BOUNDS_MAX.x and pos.z >= BOUNDS_MIN.y and pos.z <= BOUNDS_MAX.y
