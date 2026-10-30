class_name VenueLayout
extends RefCounted
## Where everything is in the hotel. Shared by the builder, the session, the
## network clamp and the tests so nothing hard-codes a coordinate twice.
##
## Everything is laid out on the 2 m grid of the wedding asset pack's floor tiles
## and wall panels. Plan (top view, +z is south, metres):
##   z -16 .. 2   public ballroom, 28 x 18: stage and sound desk (north), six
##                guest tables, dessert station (east wall), photo corner (west wall)
##   z   2        dividing wall with two staff doorways at x = -7 and x = +7
##   z   2 .. 8   back of house, 28 x 6: prep table with the cake layers, sign
##                table, storage shelves (west), wash-up and cleaning kit (east)

const HALF_WIDTH: float = 14.0
const NORTH_Z: float = -16.0
const SOUTH_Z: float = 8.0
const DIVIDER_Z: float = 2.0
const BOUNDS_MIN: Vector2 = Vector2(-13.6, -15.6)
const BOUNDS_MAX: Vector2 = Vector2(13.6, 7.6)
const WALL_HEIGHT: float = 3.0
const CEILING: float = 3.0
const DOOR_X: float = 7.0
const DOOR_WIDTH: float = 2.0

const SPAWN_ORIGIN: Vector3 = Vector3(-1.2, 0.05, 5.6)
const SPAWN_SPACING: float = 0.8

const TABLE_TOP: float = 0.83
## The three tables that take part in the seating task, then the decorative ones.
const GUEST_TABLES: Array[Vector3] = [
	Vector3(-9.0, 0.0, -9.0),
	Vector3(-9.0, 0.0, -4.0),
	Vector3(9.0, 0.0, -9.0),
]
const DECOR_TABLES: Array[Vector3] = [
	Vector3(9.0, 0.0, -4.0),
	Vector3(-3.5, 0.0, -4.0),
	Vector3(3.5, 0.0, -4.0),
	Vector3(-3.5, 0.0, -9.5),
	Vector3(3.5, 0.0, -9.5),
]
const CHAIRS_PER_TABLE: int = 6
const CHAIR_RING: float = 1.12

const STAGE_FRONT_Z: float = -12.4
const STAGE_HEIGHT: float = 0.3
const SOUND_DESK: Vector3 = Vector3(7.5, 0.0, -11.2)
const PHOTO_DISPLAY: Vector3 = Vector3(-13.1, 0.0, -7.0)

const DESSERT_TABLE_TOP: float = 0.94
const DESSERT_STAND_HEIGHT: float = 0.29
## Centre of the cake stand and the height of the top of its plate.
const DESSERT_STAND: Vector3 = Vector3(12.7, 1.225, -5.0)

const PREP_TABLE: Vector3 = Vector3(0.0, 0.0, 3.9)
const PREP_TOP: float = 0.9
const SIGN_TABLE: Vector3 = Vector3(-5.5, 0.0, 3.9)
const SIGN_TABLE_TOP: float = 0.9

## Cake layers, bottom to top: id, display name, radius, height, asset.
const CAKE_LAYERS: Array[Dictionary] = [
	{"id": &"cake_base", "name": "bottom cake layer", "radius": 0.33, "height": 0.26},
	{"id": &"cake_middle", "name": "middle cake layer", "radius": 0.25, "height": 0.23},
	{"id": &"cake_top", "name": "top cake layer", "radius": 0.175, "height": 0.21},
]

const CAKE_SOCKETS: Array[StringName] = [&"cake_socket_base", &"cake_socket_middle", &"cake_socket_top"]
const TABLE_SOCKETS: Array[StringName] = [&"table_socket_1", &"table_socket_2", &"table_socket_3"]
const SIGN_IDS: Array[StringName] = [&"sign_1", &"sign_2", &"sign_3"]
const SIGN_HEIGHT: float = 0.43

## The "hero view": standing in the west staff doorway looking across the
## ballroom, so the stage, tables and dessert station read in layers.
const HERO_POSITION: Vector3 = Vector3(-7.0, 0.05, 1.3)
const HERO_YAW: float = -0.62
const HERO_PITCH: float = 0.0


static func all_tables() -> Array[Vector3]:
	var out: Array[Vector3] = []
	out.append_array(GUEST_TABLES)
	out.append_array(DECOR_TABLES)
	return out


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
