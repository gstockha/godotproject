extends KinematicBody

export var speed = 5

var path = []
var currentPathIndex = 0
var velocity = Vector3.ZERO
var yvelocity = 0
var aggroRange = 20
var damage = 20
var launched = false

onready var pathTimer = $PathTimer
onready var target = get_node("../../playerNode/PlayerBall")
onready var nav = get_node("../../Navigation")

func _ready():
	pathTimer.start(2)

func _physics_process(delta):
	if launched:
		move_and_slide(Vector3(velocity.x, yvelocity, velocity.z), Vector3.UP)
		yvelocity -= 20 * delta #gravity
	elif path.size() > 0:
		_moveToTarget()
		
func _moveToTarget():
	if global_transform.origin.distance_to(path[currentPathIndex]) > .1:
		var direction = path[currentPathIndex] - global_transform.origin
		velocity = direction.normalized() * speed
		move_and_slide(velocity, Vector3.UP)
	else: path.remove(0)
	
func _launch(power: float, cVec: Vector3):
	launched = true
	velocity = Vector3(cVec.x*power, 0, cVec.z*power)
	yvelocity = power
	print(power)

func _on_PathTimer_timeout():
	path = nav.get_simple_path(global_transform.origin, target.global_transform.origin)
