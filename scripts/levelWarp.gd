extends Area
export var levelName = ""

func _on_levelWarp_area_entered(area):
	globals.currentScene = "res://levels/" + levelName + ".tscn"
	get_tree().change_scene("res://menus/loadScene.tscn")
