extends Spatial

onready var player = get_node("playerNode/PlayerBall")
var goon = load("res://scenes/mobs/Goon.tscn")

func  _spawnMob(mobName: String, point: Vector3) -> void:
	var spawnedEnemy
	match mobName:
		"goon": spawnedEnemy = goon.instance()
	$enemies.add_child(spawnedEnemy)
	spawnedEnemy.global_transform.origin = point
