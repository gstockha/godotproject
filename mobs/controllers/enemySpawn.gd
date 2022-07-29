extends Spatial

onready var player = get_node("../../playerNode/PlayerBall")
onready var bp = preload("res://items/bp.tscn")
export var spawnTime = 60
var enemyNodes = {}
var enemyCount = {}
var enemies = ["goon", "mole", "spinner", "hopper", "cannon", "sprinkler"]
var enemyChildren = []
export var checkFrequencies = [3, 15]
var checkerThreshold = checkFrequencies[0]
var distanceChecker = checkerThreshold
var active = false
export var distanceTreshold = 90
export var distanceTresholdY = 15

func _ready():
	var childName
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

func _process(delta):
	distanceChecker += delta
	if distanceChecker > checkerThreshold:
		distanceChecker = 0
		var pLocation = player.global_transform.origin
		var myLocation = global_transform.origin
		var nuDist = distanceTreshold + 50
		if !active && myLocation.distance_to(pLocation) < nuDist && pLocation.y < myLocation.y + distanceTresholdY && pLocation.y > myLocation.y - distanceTresholdY:
			active = true
			checkerThreshold = checkFrequencies[1]
			for enemy in enemyChildren: enemy.call("_on")
		elif active && (myLocation.distance_to(pLocation) >= nuDist || pLocation.y > myLocation.y + distanceTresholdY || pLocation.y < myLocation.y - distanceTresholdY):
			active = false
			checkerThreshold = checkFrequencies[0]
			for enemy in enemyChildren:
				enemy.call("_off")
				enemy.global_transform.origin = enemy.spawnPoint

func  _spawnMob(mobName: String, point: Vector3, spawnTimer: Timer, variables: Array) -> void:
	if (enemyCount[mobName][0] < enemyCount[mobName][1]):
		var spawnedEnemy = enemyNodes[mobName].instance()
		add_child(spawnedEnemy)
		spawnedEnemy.global_transform.origin = point
		spawnedEnemy.spawnPoint = point
		enemyCount[mobName][0] += 1
		enemyChildren.append(spawnedEnemy)
		if active: spawnedEnemy.call("_on")
		if variables:
			match mobName:
				"spinner":
					spawnedEnemy.angMod = variables[0]
					spawnedEnemy.dirMod = variables[1]
				"cannon":
					spawnedEnemy.startAngle = variables[0]
					spawnedEnemy.bltSpeed = variables[1]
					spawnedEnemy.bltVel = variables[2]
				"sprinkler":
					spawnedEnemy.startAngle = variables[0]
					spawnedEnemy.oscillationRate = variables[1]
	spawnTimer.queue_free()

func _spawnTimerSet(mobNode: Spatial, mobName: String, point: Vector3, variables=[]) -> void:
	enemyChildren.erase(mobNode)
	var spawnTimer = Timer.new()
	add_child(spawnTimer)
	spawnTimer.connect("timeout", self, "_spawnMob", [mobName, point, spawnTimer, variables])
	spawnTimer.start(spawnTime)
	enemyCount[mobName][0] -= 1

func _dropBP(point: Vector3, chance: float) -> void:
	if(randf() < chance):
		var drop = bp.instance()
		add_child(drop)
		drop.global_transform.origin = point
