extends Area
onready var deleteTimer = $DeleteTimer
export var speed = 30
export var duration = 10
export var damage = 40
var trajectory = Vector3.ZERO
var tajectorySet = false
var yvelocity = 0
onready var shakeBox = $Shakebox/CollisionShape
var shaken = false

func _ready():
	deleteTimer.start(duration)

func _physics_process(delta):
	if !tajectorySet:
		global_transform.origin = trajectory
		tajectorySet = true
		yvelocity = (2 + (7 * randf())) * .1
	translation -= get_transform().basis.z*speed*delta
	yvelocity -= delta
	translation.y += yvelocity

func _on_Shakebox_area_entered(area):
	var groups = area.get_groups()
	for i in range(len(groups)):
		if (groups[i] == "players"):
			var cam = area.owner.get("camera");
			var player = area.owner;
			cam.call("_shakeMove", 10, damage * .1, global_transform.origin.distance_to(player.global_transform.origin));
			break;


func _on_Bullet_body_entered(body):
	var bodyClass = body.get_class()
	if bodyClass == "StaticBody" || bodyClass.begins_with("CSG"): _on_DeleteTimer_timeout()

func _on_DeleteTimer_timeout():
	if shaken: queue_free()
	else:
		shakeBox.disabled = false
		deleteTimer.start(.1)
