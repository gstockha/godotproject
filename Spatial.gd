extends Spatial

onready var ball := $CSGSphere
# Declare member variables here. Examples:
# var a = 2
# var b = "text"
var time = rand_range(0.0,1.0)
var amp = 0.0;
var gravity = -1;
var velocity = 0;
# Called when the node enters the scene tree for the first time.
func _ready():
	translation.x = rand_range(-2.5,2.5)
	translation.y = rand_range(0,1)
	translation.z = rand_range(-2.5,2.5)
	amp = translation.y/2
	ball.radius = rand_range(.05, .1)
	ball.material.albedo_color = Color(rand_range(0.0,1.0),rand_range(0.0,1.0),rand_range(0.0,1.0),1.0)
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	velocity += gravity*delta
	translation.y += gravity*delta*delta + velocity*delta
	if (translation.y < 0):
		velocity += rand_range(1,2)
		translation.y = 0
	
	ball.scale.y = 1.0 + .5*(abs(velocity))
	ball.scale.z = 1.0 - .25*(abs(velocity))
	ball.scale.x = 1.0 - .25*(abs(velocity))
