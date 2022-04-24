extends Spatial

onready var player = get_node("../playerNode/PlayerBall")
var enemyList = {
	"goon": load("res://scenes/mobs/Goon.tscn"),
	"mole": load("res://scenes/mobs/Mole.tscn")
}

func  _spawnMob(mobName: String, point: Vector3, spawnTimer: Timer) -> void:
	var spawnedEnemy = enemyList[mobName].instance()
	add_child(spawnedEnemy)
	spawnedEnemy.global_transform.origin = point
	spawnedEnemy.spawnPoint = point
	spawnTimer.queue_free()

func _spawnTimerSet(mobName: String, point: Vector3, time: float) -> void:
	var spawnTimer = Timer.new()
	add_child(spawnTimer)
	spawnTimer.connect("timeout", self, "_spawnMob", [mobName, point, spawnTimer])
	spawnTimer.start(time)
