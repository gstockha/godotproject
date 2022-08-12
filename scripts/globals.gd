extends Node

var currentScene = "res://levels/hub.tscn"
var bpTotal = 160
var jump = "jump"
var move_up = "move_up"
var move_down = "move_down"
var move_right = "move_right"
var move_left = "move_left"
var dash = "dash"
var special = "special"
var pan_left = "pan_left"
var pan_right = "pan_right"
var lock_on = "lock_on"
var allocate_stats = "allocate_stats"
var ui_left = "ui_left"
var ui_right = "ui_right"
var ui_up = "ui_up"
var ui_down = "ui_down"
var player_count = 2

#func _detectPlayers() -> void:
#	player_count = len(get_tree().get_nodes_in_group("players"))
#	player_count /= 2
#	print(str(player_count) + ' players detected!')
