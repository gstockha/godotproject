extends Area
export var levelName = ""



func _on_levelWarp_area_entered(area):
	get_tree().change_scene("res://levels/" + levelName + ".tscn")
