extends Spatial


# Declare member variables here. Examples:
# var a = 2
# var b = "text"
var i = 0
var x = 0.0
var y = 0.0
# Called when the node enters the scene tree for the first time.
func _ready():
	var this_ball = load("res://menus/bouncy_menu_deco.tscn")
	for i in range(500):
		add_child(this_ball.instance())


# Called every frame. 'delta' is the elapsed time since the previous frame.
#func _process(delta):
#	pass
