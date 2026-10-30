extends Node
## Player settings. Stored in user://settings.cfg; a missing or damaged file
## falls back to defaults instead of failing.

signal changed

const PATH: String = "user://settings.cfg"
const FOV_MIN: float = 60.0
const FOV_MAX: float = 110.0
const SENSITIVITY_MIN: float = 0.3
const SENSITIVITY_MAX: float = 3.0

var fov: float = 80.0:
	set(value):
		fov = clampf(value, FOV_MIN, FOV_MAX)
		changed.emit()

var mouse_sensitivity: float = 1.0:
	set(value):
		mouse_sensitivity = clampf(value, SENSITIVITY_MIN, SENSITIVITY_MAX)
		changed.emit()


func _ready() -> void:
	load_from_disk()


func load_from_disk() -> void:
	var cfg := ConfigFile.new()
	if cfg.load(PATH) != OK:
		return
	fov = _read_number(cfg, "view", "fov", fov)
	mouse_sensitivity = _read_number(cfg, "input", "mouse_sensitivity", mouse_sensitivity)


func save() -> void:
	var cfg := ConfigFile.new()
	cfg.set_value("view", "fov", fov)
	cfg.set_value("input", "mouse_sensitivity", mouse_sensitivity)
	var err: Error = cfg.save(PATH)
	if err != OK:
		push_warning("Could not save settings (error %d)." % err)


func _read_number(cfg: ConfigFile, section: String, key: String, fallback: float) -> float:
	var value: Variant = cfg.get_value(section, key, fallback)
	if typeof(value) == TYPE_FLOAT or typeof(value) == TYPE_INT:
		return float(value)
	return fallback
