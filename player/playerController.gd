extends Spatial
onready var vbox = $VBoxContainer
onready var hbox = preload("res://player/HSplitScreen.tscn")
onready var playerPort = preload("res://player/ViewportContainer.tscn")
onready var player = preload("res://player/PlayerBall.tscn")
onready var checkpointSpawn = get_node("../checkpoints/checkpointSpawn")
var spawnPoints = {0: Vector3(0,2,0)}

func _ready():
	if globals.player_count > 1:
		match globals.currentScene:
			"hub":
				spawnPoints = {0: Vector3(-2,2,2), 1: Vector3(2,2,-2), 2: Vector3(-2,2,-2), 3: Vector3(2,2,2)}
			"pyramid":
				spawnPoints = {0: Vector3(-2,2,2), 1: Vector3(2,2,-2), 2: Vector3(-2,2,-2), 3: Vector3(2,2,2)}
			"demo":
				spawnPoints = {0: Vector3(-2,2,2), 1: Vector3(2,2,-2), 2: Vector3(-2,2,-2), 3: Vector3(2,2,2)}
	var spawn = checkpointSpawn.global_transform.origin
	if (globals.player_count < 3):
		for i in range(globals.player_count):
			var hport = hbox.instance()
			var port = playerPort.instance()
			var plr = player.instance()
			plr.playerId = i
			port.get_child(0).add_child(plr)
			hport.add_child(port)
			vbox.add_child(hport)
	else:
		var hport = [hbox.instance(), hbox.instance()]
		for i in range(globals.player_count):
			var port = playerPort.instance()
			var plr = player.instance()
			plr.playerId = i
			port.get_child(0).add_child(plr)
			if (globals.player_count == 3):
				if (i == 0): hport[0].add_child(port)
				else: hport[1].add_child(port)
			else:
				if (i < 2): hport[0].add_child(port)
				else: hport[1].add_child(port)
		vbox.add_child(hport[0])
		vbox.add_child(hport[1])
	var offset
	for player in get_tree().get_nodes_in_group("players"):
		offset = spawnPoints[player.playerId]
		player.global_transform.origin = Vector3(spawn.x + offset.x,
		spawn.y + offset.y, spawn.z + offset.z)
