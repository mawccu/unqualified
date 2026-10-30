extends Node
## Registers the default input actions in code. Gameplay reads actions by name
## only, so a rebinding screen can replace these without touching other scripts.

const KEYS: Dictionary = {
	&"move_forward": KEY_W,
	&"move_back": KEY_S,
	&"move_left": KEY_A,
	&"move_right": KEY_D,
	&"sprint": KEY_SHIFT,
	&"interact": KEY_E,
	&"drop": KEY_Q,
	&"pause": KEY_ESCAPE,
	&"restart": KEY_F5,
}

const MOUSE_BUTTONS: Dictionary = {
	&"use": MOUSE_BUTTON_LEFT,
}


func _ready() -> void:
	for action: StringName in KEYS:
		var key_event := InputEventKey.new()
		key_event.physical_keycode = int(KEYS[action]) as Key
		_bind(action, key_event)
	for action: StringName in MOUSE_BUTTONS:
		var button_event := InputEventMouseButton.new()
		button_event.button_index = int(MOUSE_BUTTONS[action]) as MouseButton
		_bind(action, button_event)


func _bind(action: StringName, event: InputEvent) -> void:
	if not InputMap.has_action(action):
		InputMap.add_action(action)
	InputMap.action_add_event(action, event)
