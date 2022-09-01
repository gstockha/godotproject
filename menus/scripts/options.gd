extends Control

signal goToRoom(room)

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.

func _on_backButton_goToRoom(room):
	emit_signal("goToRoom",room)
