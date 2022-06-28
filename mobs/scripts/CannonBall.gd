extends Area
onready var deleteTimer = $DeleteTimer
var speed = 30
var duration = 10
var damage = 40
var trajectory = Vector3.ZERO
var tajectorySet = false
var yvelocity = 0
onready var shakeBox = $Shakebox/CollisionShape
onready var hurtBox = $Hurtbox/CollisionShape
var shaken = false
var bltSpeed = 20
var bltVel = 4

func _ready():
	deleteTimer.start(duration)

func _physics_process(delta):
	if !tajectorySet:
		global_transform.origin = trajectory
		tajectorySet = true
		speed = 20 + (bltSpeed * randf())
		yvelocity = (2 + (bltVel * randf())) * .1
		visible = true
	translation -= get_transform().basis.z*speed*delta
	yvelocity -= delta
	translation.y += yvelocity

func _on_Shakebox_area_entered(area):
	var groups = area.get_groups()
	for i in range(len(groups)):
		if (groups[i] == "players"):
			var cam = area.owner.get("camera");
			var player = area.owner;
			cam.call("_shakeMove", 12, damage * .15, global_transform.origin.distance_to(player.global_transform.origin));
			break;


func _on_Bullet_body_entered(body):
	var bodyClass = body.get_class()
	if bodyClass == "StaticBody" || bodyClass.begins_with("CSG"): _on_DeleteTimer_timeout()

func _on_DeleteTimer_timeout():
	if shaken: queue_free()
	else:
		shakeBox.disabled = false
		hurtBox.disabled = false
		deleteTimer.start(.1)
		shaken = true

func _on_CollisionTimer_timeout():
	$CollisionShape.disabled = false;
