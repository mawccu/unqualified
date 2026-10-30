class_name SecondarySpring
extends RefCounted
## A damped spring for purely visual wobble: a head that lags a turn, a tie that
## swings, a held prop that tilts. Underdamped on purpose so it overshoots once
## and settles. It only ever drives presentation nodes, never anything the
## interaction or the host reads.

const MAX_STEP: float = 1.0 / 30.0
const MAX_SUBSTEPS: int = 4

var value: Vector3 = Vector3.ZERO
var velocity: Vector3 = Vector3.ZERO
var stiffness: float = 140.0
var damping: float = 7.0
## The value never leaves a sphere of this radius, whatever happens to the input.
var limit: float = 0.5


func _init(stiffness_: float = 140.0, damping_: float = 7.0, limit_: float = 0.5) -> void:
	stiffness = stiffness_
	damping = damping_
	limit = limit_


func step(target: Vector3, delta: float) -> Vector3:
	var remaining: float = minf(delta, MAX_STEP * MAX_SUBSTEPS)
	while remaining > 0.0:
		var dt: float = minf(remaining, MAX_STEP)
		remaining -= dt
		var accel: Vector3 = (target - value) * stiffness - velocity * damping
		velocity += accel * dt
		value += velocity * dt
		if value.length() > limit:
			value = value.normalized() * limit
			velocity *= 0.5
	return value


func reset(to: Vector3 = Vector3.ZERO) -> void:
	value = to
	velocity = Vector3.ZERO
