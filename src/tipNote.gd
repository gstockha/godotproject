extends Label
onready var textTimer = $Timer

func _on_Timer_timeout():
	text = ''
