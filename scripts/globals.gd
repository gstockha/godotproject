extends Node

var currentScene = "hub"
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
var player_count = 1
var p1hasController = false
var pyramided = false

func _processJoyCount() -> void:
	if (player_count == 1): p1hasController = true
	else:
		var joyCount = 0
		for i in range(4): if Input.is_joy_known(i): joyCount += 1
		p1hasController = joyCount >= player_count
		
func _getControlId(id: int) -> String:
	var controllerId = ''
	if id == 0: controllerId = str(id) if !p1hasController else ""
	else: controllerId = str(id) if !globals.p1hasController else str(id + 1)
	return controllerId
