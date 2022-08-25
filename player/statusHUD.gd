extends Control

func _ready():
	var player = get_node('..')
	if globals.player_count > 1:
		if globals.player_count == 2 || (globals.player_count == 3 && player.playerId == 0):
			margin_left = -158
			margin_right = 1442
			margin_bottom = -398
			margin_top = -1220
		elif globals.player_count > 2:
			margin_left = -90
			margin_right = 950
			margin_bottom = 38
			margin_top = -700
