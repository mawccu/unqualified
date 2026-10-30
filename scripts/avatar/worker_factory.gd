class_name WorkerFactory
extends RefCounted
## Chooses the worker body: the authored model when one exists and follows the
## contract, otherwise the minimal code-built mannequin. Nothing else in the
## game needs to know which it got.
##
## Command line switches (after "--"): --fallback-workers forces the mannequin,
## --detailed-workers makes the mannequin the older, busier debug version.

static var force_fallback: bool = false


static func apply_command_line() -> void:
	for arg: String in OS.get_cmdline_user_args():
		if arg == "--fallback-workers":
			force_fallback = true
		elif arg == "--detailed-workers":
			WorkerAvatar.default_style = WorkerAvatar.Style.DETAILED


static func using_authored_model() -> bool:
	return not force_fallback and GlbWorker.usable()


static func create(slot: int) -> WorkerPresenter:
	var worker: WorkerPresenter
	if using_authored_model():
		worker = GlbWorker.new()
	else:
		worker = WorkerAvatar.new()
	worker.setup(slot)
	return worker
