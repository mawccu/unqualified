class_name Quality
extends RefCounted
## Graphics quality presets. The costly parts of the venue are real-time shadows
## from several lights and screen-space ambient occlusion, so those are what the
## presets switch. Low draws no shadows at all; Medium keeps one soft shadow from
## the key light; High adds shadows from the chandeliers and ambient occlusion.

const NAMES: Array[String] = ["Low", "Medium", "High"]
const DEFAULT_LEVEL: int = 1


static func apply(root: Node, level: int) -> void:
	level = clampi(level, 0, NAMES.size() - 1)
	for node: Node in root.find_children("*", "WorldEnvironment", true, false):
		var env: Environment = (node as WorldEnvironment).environment
		if env != null:
			env.ssao_enabled = level >= 2
			env.glow_enabled = level >= 2
	for node: Node in root.find_children("*", "OmniLight3D", true, false):
		(node as OmniLight3D).shadow_enabled = level >= 2
	for node: Node in root.find_children("*", "SpotLight3D", true, false):
		(node as SpotLight3D).shadow_enabled = level >= 2
	for node: Node in root.find_children("*", "DirectionalLight3D", true, false):
		var sun := node as DirectionalLight3D
		sun.shadow_enabled = level >= 1
		sun.directional_shadow_mode = DirectionalLight3D.SHADOW_PARALLEL_4_SPLITS if level >= 2 else DirectionalLight3D.SHADOW_ORTHOGONAL
		sun.shadow_blur = 1.6 if level >= 2 else 0.8
