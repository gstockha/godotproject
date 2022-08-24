extends Label

var time = 0

func _ready():
	if globals.player_count > 1:
		 add_font_override("font", load("res://fonts/tipSmall.tres"))
