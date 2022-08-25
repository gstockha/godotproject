extends RayCast
onready var shadow = get_node("shadowSkin");
onready var player = get_parent();
onready var deathPlaceTimer = $deathPlaceTimer
var shadowScale = .6

func _ready():
	deathPlaceTimer.start(.1)
	shadowScale = player.collisionBaseScale + .1
	shadow.scale.x = shadowScale
	shadow.scale.z = shadowScale

func _process(delta):
	if is_colliding():
		var point = get_collision_point()
		shadow.global_transform.origin = point
#		var heightScale = (point.y/player.global_transform.origin.y) + .07
#		if heightScale > 1: heightScale = 1
#		elif heightScale < .1: heightScale = .1
		shadow.scale.x = shadowScale# * heightScale
		shadow.scale.z = shadowScale# * heightScale


func _on_deathPlaceTimer_timeout():
	deathPlaceTimer.start(2)
	if get_collider():
		player.deathPlace = get_collision_point()
		player.deathPlace.y += .5
