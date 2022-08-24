extends Control

func _ready():
	if globals.player_count > 1:
		if globals.player_count == 2:
			margin_left = -158
			margin_right = 1442
		margin_bottom = -398
		margin_top = -1220
