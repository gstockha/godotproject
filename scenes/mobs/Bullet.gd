extends Area
onready var deleteTimer = $DeleteTimer
export var speed = 40
export var duration = 5
export var power = 10
var invincible = true
var trajectory = 0

func _ready():
	invincible = false
	deleteTimer.start(duration)

func _physics_process(delta):
	translation -= get_transform().basis.z*speed*delta

func _on_Bullet_body_entered(body):
	if body.get_class() == "StaticBody": _on_DeleteTimer_timeout()

func _on_DeleteTimer_timeout():
	queue_free()
