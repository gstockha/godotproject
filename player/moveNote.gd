extends Label
var alpha = 2

func _ready():
	if globals.player_count > 1:
		 add_font_override("font", load("res://fonts/tipSmall.tres"))

func _process(delta):
	if (text != ""):
		alpha -= .01
		if alpha < 1:
			if alpha < 0:
				alpha = 0
				text = ""
			add_color_override("font_color", Color(1,1,1,alpha))
