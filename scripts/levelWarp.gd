extends Area
export var levelName = ""

func _ready():
	if levelName == "demo" && globals.pyramided == false: levelName = "pyramid"

func _on_levelWarp_area_entered(area):
	if !area.get_parent().name.begins_with("PlayerBall"): return
	globals.currentScene = levelName
	get_tree().change_scene("res://menus/loadScene.tscn")
	globals.pyramided = true
