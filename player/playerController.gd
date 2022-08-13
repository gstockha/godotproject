extends Spatial
onready var vbox = $VBoxContainer
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
	var offset
	for i in range(globals.player_count):
		var port = playerPort.instance()
		var plr = player.instance()
		plr.playerId = i
		port.get_child(0).add_child(plr)
		vbox.add_child(port)
		offset = spawnPoints[i]
		plr.global_transform.origin = Vector3(spawn.x + offset.x, spawn.y + offset.y, spawn.z + offset.z)
