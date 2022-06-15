extends Area
onready var deleteTimer = $DeleteTimer
export var speed = 45
export var duration = 5
export var damage = 18
var trajectory = Vector3.ZERO
var tajectorySet = false

func _ready():
	deleteTimer.start(duration)

func _physics_process(delta):
	if !tajectorySet:
		global_transform.origin = trajectory
		tajectorySet = true
	translation -= get_transform().basis.z*speed*delta

func _on_Bullet_body_entered(body):
	var bodyClass = body.get_class()
	if bodyClass == "StaticBody" || bodyClass.begins_with("CSG"): _on_DeleteTimer_timeout()

func _on_DeleteTimer_timeout():
	queue_free()
