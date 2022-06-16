extends Spatial

onready var player = get_node("../../playerNode/PlayerBall")
var spawnTime = 60
var enemyNodes = {}
var enemyCount = {}
var enemies = ["goon", "mole", "spinner", "hopper", "cannon"]
var enemyChildren = []
var checkFrequencies = [5, 20]
var checkerThreshold = checkFrequencies[0]
var distanceChecker = checkerThreshold
var active = false

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
					var eCap = enemy[0].to_upper() + enemy.substr(1,-1)
					enemyNodes[enemy] = load('res://mobs/scenes/' + eCap + '.tscn')
				break
		enemyChildren.append(child)

func _process(_delta):
	distanceChecker += _delta
	if distanceChecker > checkerThreshold:
		distanceChecker = 0
		var pLocation = player.global_transform.origin
		var myLocation = global_transform.origin
		if !active && myLocation.distance_to(pLocation) < 90 && pLocation.y < myLocation.y + 15 && pLocation.y > myLocation.y - 15:
			active = true
			checkerThreshold = checkFrequencies[1]
			for enemy in enemyChildren: enemy.call("_on")
		elif active && (myLocation.distance_to(pLocation) >= 90 || pLocation.y > myLocation.y + 15 || pLocation.y < myLocation.y - 15):
			active = false
			checkerThreshold = checkFrequencies[0]
			for enemy in enemyChildren: enemy.call("_off")

func  _spawnMob(mobName: String, point: Vector3, spawnTimer: Timer) -> void:
	if (enemyCount[mobName][0] < enemyCount[mobName][1]):
		var spawnedEnemy = enemyNodes[mobName].instance()
		add_child(spawnedEnemy)
		spawnedEnemy.global_transform.origin = point
		spawnedEnemy.spawnPoint = point
		enemyCount[mobName][0] += 1
		enemyChildren.append(spawnedEnemy)
		if active: spawnedEnemy.call("_on")
	spawnTimer.queue_free()

func _spawnTimerSet(mobNode: Spatial, mobName: String, point: Vector3) -> void:
	enemyChildren.erase(mobNode)
	var spawnTimer = Timer.new()
	add_child(spawnTimer)
	spawnTimer.connect("timeout", self, "_spawnMob", [mobName, point, spawnTimer])
	spawnTimer.start(spawnTime)
	enemyCount[mobName][0] -= 1
