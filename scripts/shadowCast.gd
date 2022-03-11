extends RayCast
onready var shadow = get_node("shadowSkin");
onready var player = get_parent();
var shadowScale = .6

func _ready():
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
