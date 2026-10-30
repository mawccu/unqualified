class_name Hud
extends CanvasLayer
## Crosshair, interaction prompt, short messages and the pause menu. It keeps
## running while the tree is paused so the menu and Esc stay responsive.

signal pause_toggled
signal resume_requested
signal restart_requested
signal quit_requested
signal host_requested
signal join_requested(address: String)
signal leave_requested

const TOAST_SECONDS: float = 2.0

var _prompt: Label
var _toast: Label
var _menu: Control
var _resume_button: Button
var _restart_button: Button
var _status_label: Label
var _address_edit: LineEdit
var _net_row: HBoxContainer
var _leave_button: Button
var _toast_left: float = 0.0
var _task_box: VBoxContainer
var _task_signature: String = ""


func _init() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	layer = 10


func _ready() -> void:
	_build_crosshair()
	_build_labels()
	_build_task_panel()
	_build_menu()


func _input(event: InputEvent) -> void:
	if event.is_action_pressed(&"pause"):
		pause_toggled.emit()
		get_viewport().set_input_as_handled()
	elif event.is_action_pressed(&"restart"):
		restart_requested.emit()
		get_viewport().set_input_as_handled()


func _process(delta: float) -> void:
	if _toast_left > 0.0:
		_toast_left -= delta
		if _toast_left <= 0.0:
			_toast.text = ""


func set_prompt(text: String) -> void:
	_prompt.text = text


func show_toast(text: String) -> void:
	_toast.text = text
	_toast_left = TOAST_SECONDS


## Rebuilds the shift task board. results come from TaskBoard.evaluate.
func set_tasks(results: Array[Dictionary]) -> void:
	var signature: String = ""
	for result: Dictionary in results:
		signature += "%s:%s;" % [result["id"], result["text"]]
	if signature == _task_signature:
		return
	_task_signature = signature
	for child: Node in _task_box.get_children():
		child.queue_free()
	var heading := Label.new()
	heading.text = "SHIFT TASKS"
	heading.add_theme_font_size_override(&"font_size", 15)
	heading.modulate = Color(1.0, 0.85, 0.4)
	_task_box.add_child(heading)
	for result: Dictionary in results:
		var status: int = int(result["status"])
		var mark: String = "[ ]"
		var color: Color = Color(0.92, 0.92, 0.92)
		match status:
			TaskBoard.Status.IN_PROGRESS:
				mark = "[~]"
				color = Color(1.0, 0.88, 0.4)
			TaskBoard.Status.NEEDS_FIXING:
				mark = "[!]"
				color = Color(1.0, 0.55, 0.35)
			TaskBoard.Status.DONE:
				mark = "[x]"
				color = Color(0.5, 0.95, 0.55)
			TaskBoard.Status.NOT_BUILT:
				mark = "[-]"
				color = Color(0.6, 0.6, 0.65)
		var title := Label.new()
		title.text = "%s %s" % [mark, result["name"]]
		title.add_theme_font_size_override(&"font_size", 18)
		title.modulate = color
		_task_box.add_child(title)
		var detail := Label.new()
		detail.text = str(result["text"])
		detail.add_theme_font_size_override(&"font_size", 14)
		detail.modulate = color
		_task_box.add_child(detail)
		var goal := Label.new()
		goal.text = str(result["goal"])
		goal.add_theme_font_size_override(&"font_size", 12)
		goal.modulate = Color(0.75, 0.75, 0.8, 0.85)
		goal.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		goal.custom_minimum_size = Vector2(290.0, 0.0)
		_task_box.add_child(goal)


## mode_id is a NetSession.Mode value.
func set_net_ui(mode_id: int, status: String) -> void:
	_status_label.text = status
	var offline: bool = mode_id == NetSession.Mode.OFFLINE
	_address_edit.visible = offline
	_net_row.visible = offline
	_leave_button.visible = not offline
	_restart_button.visible = mode_id != NetSession.Mode.CLIENT


func set_menu_visible(visible_now: bool) -> void:
	_menu.visible = visible_now
	if visible_now:
		_resume_button.grab_focus()


func _build_crosshair() -> void:
	var dot := ColorRect.new()
	dot.color = Color(1.0, 1.0, 1.0, 0.85)
	dot.size = Vector2(6.0, 6.0)
	dot.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(dot)
	dot.set_anchors_and_offsets_preset(Control.PRESET_CENTER, Control.PRESET_MODE_KEEP_SIZE)


func _build_labels() -> void:
	_prompt = _make_label(Control.PRESET_BOTTOM_WIDE, -150.0, -80.0, 22)
	_toast = _make_label(Control.PRESET_BOTTOM_WIDE, -200.0, -160.0, 20)
	_toast.add_theme_color_override(&"font_color", Color(1.0, 0.8, 0.5))

	var help := Label.new()
	help.text = "WASD move | Mouse look | E pick up | Left click place | Q drop | Shift sprint | Esc menu | F5 restart"
	help.position = Vector2(12.0, 8.0)
	help.add_theme_font_size_override(&"font_size", 14)
	help.modulate = Color(1.0, 1.0, 1.0, 0.7)
	help.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(help)


func _build_task_panel() -> void:
	var panel := PanelContainer.new()
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.05, 0.05, 0.08, 0.72)
	style.set_corner_radius_all(8)
	style.set_content_margin_all(12)
	panel.add_theme_stylebox_override(&"panel", style)
	add_child(panel)
	panel.anchor_left = 1.0
	panel.anchor_right = 1.0
	panel.anchor_top = 0.0
	panel.anchor_bottom = 0.0
	panel.offset_left = -340.0
	panel.offset_right = -14.0
	panel.offset_top = 40.0
	panel.offset_bottom = 40.0
	panel.grow_vertical = Control.GROW_DIRECTION_END
	_task_box = VBoxContainer.new()
	_task_box.add_theme_constant_override(&"separation", 3)
	_task_box.mouse_filter = Control.MOUSE_FILTER_IGNORE
	panel.add_child(_task_box)


func _make_label(preset: Control.LayoutPreset, top: float, bottom: float, font_size: int) -> Label:
	var label := Label.new()
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_BOTTOM
	label.add_theme_font_size_override(&"font_size", font_size)
	label.add_theme_color_override(&"font_outline_color", Color.BLACK)
	label.add_theme_constant_override(&"outline_size", 6)
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(label)
	label.set_anchors_and_offsets_preset(preset)
	label.offset_top = top
	label.offset_bottom = bottom
	return label


func _build_menu() -> void:
	_menu = Control.new()
	_menu.visible = false
	add_child(_menu)
	_menu.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)

	var dim := ColorRect.new()
	dim.color = Color(0.0, 0.0, 0.0, 0.55)
	_menu.add_child(dim)
	dim.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)

	var center := CenterContainer.new()
	_menu.add_child(center)
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)

	var panel := PanelContainer.new()
	center.add_child(panel)
	var margin := MarginContainer.new()
	for side: StringName in [&"margin_left", &"margin_right", &"margin_top", &"margin_bottom"]:
		margin.add_theme_constant_override(side, 24)
	panel.add_child(margin)

	var box := VBoxContainer.new()
	box.custom_minimum_size = Vector2(360.0, 0.0)
	box.add_theme_constant_override(&"separation", 10)
	margin.add_child(box)

	var title := Label.new()
	title.text = "Paused"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override(&"font_size", 28)
	box.add_child(title)

	_add_slider(box, "Field of view", Settings.FOV_MIN, Settings.FOV_MAX, 1.0, Settings.fov,
		func(value: float) -> void: Settings.fov = value)
	_add_slider(box, "Mouse sensitivity", Settings.SENSITIVITY_MIN, Settings.SENSITIVITY_MAX, 0.05,
		Settings.mouse_sensitivity, func(value: float) -> void: Settings.mouse_sensitivity = value)

	_status_label = Label.new()
	_status_label.text = "Offline"
	_status_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	box.add_child(_status_label)
	_address_edit = LineEdit.new()
	_address_edit.text = "127.0.0.1"
	_address_edit.placeholder_text = "Host address"
	box.add_child(_address_edit)
	_net_row = HBoxContainer.new()
	_net_row.add_theme_constant_override(&"separation", 10)
	box.add_child(_net_row)
	var host_button := Button.new()
	host_button.text = "Host a game"
	host_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	host_button.pressed.connect(func() -> void: host_requested.emit())
	_net_row.add_child(host_button)
	var join_button := Button.new()
	join_button.text = "Join"
	join_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	join_button.pressed.connect(func() -> void: join_requested.emit(_address_edit.text.strip_edges()))
	_net_row.add_child(join_button)
	_leave_button = _add_button(box, "Leave game", leave_requested)

	_resume_button = _add_button(box, "Resume", resume_requested)
	_restart_button = _add_button(box, "Restart", restart_requested)
	_add_button(box, "Quit", quit_requested)


func _add_slider(box: VBoxContainer, caption: String, low: float, high: float, step: float,
		value: float, on_change: Callable) -> void:
	var label := Label.new()
	label.text = caption
	box.add_child(label)
	var slider := HSlider.new()
	slider.min_value = low
	slider.max_value = high
	slider.step = step
	slider.value = value
	slider.value_changed.connect(on_change)
	box.add_child(slider)


func _add_button(box: VBoxContainer, caption: String, pressed_signal: Signal) -> Button:
	var button := Button.new()
	button.text = caption
	button.pressed.connect(func() -> void: pressed_signal.emit())
	box.add_child(button)
	return button
