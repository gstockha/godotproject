extends Control

func _ready():
	if globals.player_count > 1:
		margin_left = -100
		margin_right = 1000
		if globals.player_count == 2:
			margin_bottom = 25
			margin_top = -550
		else:
			margin_bottom = 23
			margin_top = -500
