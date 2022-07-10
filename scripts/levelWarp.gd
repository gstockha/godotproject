extends Area
export var levelName = ""

func _on_levelWarp_area_entered(area):
	globals.currentScene = "res://levels/" + levelName + ".tscn"
	area.owner._setStat(0, "reset")
	get_tree().change_scene("res://menus/loadScene.tscn")
