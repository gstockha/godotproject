extends Node2D

signal goToRoom(room)

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.

func _on_menu_goToRoom(room):
	emit_signal("goToRoom",room)
