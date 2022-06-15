extends Spatial

onready var player = get_node("../playerNode/PlayerBall")
var spawnTime = 60
var enemyNodes = {}
var enemyCount = {}
var enemies = ["goon", "mole", "spinner", "hopper", "cannon"]

func _ready():
	var childName
	spawnTime = (name[name.length()-3] + name[name.length()-2] + name[name.length()-1]) as int
	for enemy in enemies: enemyCount[enemy] = [0,0]
	for child in get_children():
		childName = child.name.to_lower()
		for enemy in enemies:
			if childName.begins_with(enemy):
				enemyCount[enemy][0] += 1
				enemyCount[enemy][1] += 1
				if (enemyCount[enemy][1] == 1): #add to load object if it exists
					enemyNodes[enemy] = load('res://mobs/' + enemy + '.tscn')
				break
#	for i in range(enemies.size()):
#		for x in range(enemyCount[enemies[i]][1]):
#			enemyPoints[enemies[i]].append(Vector3.ZERO)

func  _spawnMob(mobName: String, point: Vector3, spawnTimer: Timer) -> void:
	if (enemyCount[mobName][0] < enemyCount[mobName][1]):
		var spawnedEnemy = enemyNodes[mobName].instance()
		add_child(spawnedEnemy)
		spawnedEnemy.global_transform.origin = point
		spawnedEnemy.spawnPoint = point
		enemyCount[mobName][0] += 1
	spawnTimer.queue_free()

func _spawnTimerSet(mobName: String, point: Vector3) -> void:
#	enemyPoints[mobName][enemyCount[mobName][0]] = point
#	if (enemyCount[mobName][0] > 0): return
#	enemyCount[mobName][0] = enemyCount[mobName][1]
#	for i in range(enemyCount[mobName][1]):
	var spawnTimer = Timer.new()
	add_child(spawnTimer)
	spawnTimer.connect("timeout", self, "_spawnMob", [mobName, point, spawnTimer])
	spawnTimer.start(spawnTime)
	enemyCount[mobName][0] -= 1
