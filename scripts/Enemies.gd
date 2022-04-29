extends Spatial

onready var player = get_node("../playerNode/PlayerBall")
var enemyNodes = {
	"goon": load("res://scenes/mobs/Goon.tscn"),
	"mole": load("res://scenes/mobs/Mole.tscn")
}
var enemyCount = {
	"goon": [0,0],
	"mole": [0,0]
}
var enemyPoints = {
	"goon": [],
	"mole": []
}
var enemies = ["goon", "mole"]

func _ready():
	var childName
	for child in get_children():
		childName = child.name.to_lower()
		for enemy in enemies:
			if childName.begins_with(enemy):
				enemyCount[enemy][0] += 1
				enemyCount[enemy][1] += 1
				break
	for i in range(enemies.size()):
		for x in range(enemyCount[enemies[i]][1]):
			enemyPoints[enemies[i]].append(Vector3.ZERO)

func  _spawnMob(mobName: String, point: Vector3, spawnTimer: Timer) -> void:
	var spawnedEnemy = enemyNodes[mobName].instance()
	add_child(spawnedEnemy)
	spawnedEnemy.global_transform.origin = point
	spawnedEnemy.spawnPoint = point
	spawnTimer.queue_free()

func _spawnTimerSet(mobName: String, point: Vector3, time: float) -> void:
	enemyCount[mobName][0] -= 1
	print(enemyCount[mobName][0])
	enemyPoints[mobName][enemyCount[mobName][0]] = point
	if (enemyCount[mobName][0] > 0): return
	enemyCount[mobName][0] = enemyCount[mobName][1]
	for i in range(enemyCount[mobName][1]):
		var spawnTimer = Timer.new()
		add_child(spawnTimer)
		spawnTimer.connect("timeout", self, "_spawnMob", [mobName, enemyPoints[mobName][i], spawnTimer])
		spawnTimer.start(time)
