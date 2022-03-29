extends KinematicBody

export var speed = 5

var path = []
var currentPathIndex = 0
var velocity = Vector3.ZERO
var threshold = .1

onready var nav = get_node("../../Navigation")

func _physics_process(delta):
	if path.size() > 0:
		_moveToTarget()
		
func _moveToTarget():
	if global_transform.origin.distance_to(path[currentPathIndex]) < threshold:
		path.remove(0)
	else:
		var direction = path[currentPathIndex] - global_transform.origin
		velocity = direction.normalized() * speed
		move_and_slide(velocity, Vector3.UP)
		
func _getTargetPath(target_pos):
	path = nav.get_simple_path(global_transform.origin, target_pos)
