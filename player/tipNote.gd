extends Label
onready var textTimer = $Timer

func _ready():
	if globals.player_count > 1:
		 add_font_override("font", load("res://fonts/tipHalf.tres"))

func _on_Timer_timeout():
	text = ''
