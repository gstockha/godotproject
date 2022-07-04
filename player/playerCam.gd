extends Camera
onready var player = get_node("../../")
onready var mesh = get_node('../../CollisionShape/BallSkin')
onready var lockScanner = get_node('../../lockOnScanner')
onready var setDelay = get_node("setDelay") #so camsets wont change angle if we've set it manually via panning
onready var bufferTimer = get_node("bufferTimer") #for camset node
var cam = 0 #rotate mode
var camsets = [135,45,-45,-135]
var camsetarray = 1
onready var lastAng = camsets[camsetarray]
var turnRate = 8
var stickMove = false
var turnDir = 'right'
var autoBuffer = false #make sure you're going the right direction to trigger auto cam
var customset = 0 #double camsets after a first one
var lockOn = null
var baseY = 5 #translation Y
var baseRotX = -10 #rotation_degrees X
var targetY = baseY
var targetRotX = baseRotX
var fixedCamLock = false #keep lock cam on?
var lerpMove = false
var heightMove = false
var angMove = false
var angMoveTarget = 0
var shakeMove = false
var shakeMoveTimer = 0
var shakeIntensity = 0
var shakeAlternate = false #smoother shake

func _ready():
	player.rotation_degrees.y = 45
	mesh.rotation_degrees.y = 45

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("pan_right") or event.is_action_pressed("pan_left"):
		if fixedCamLock: return
		if (lockOn == null): _move_camera(event)
		elif event.is_action_pressed("pan_right"): _directionalLockOn("R", true)
		elif event.is_action_pressed("pan_left"): _directionalLockOn("L", true)
	elif (event.is_action_pressed("lock_on")): _findLockOn(lockOn)

func _move_camera(evn) -> void:
	turnRate = 8
	player.angDelayFriction = true
	var ones = 0
	var fives = 0
#	for bp in get_tree().get_nodes_in_group("boingPoints"):
#		print(bp.name)
#		if bp.name.begins_with("5"): fives += 1
#		else: ones += 1
#	print("ones: " + str(ones))
#	print("fives: " + str(fives))
	if (cam != 1):
		if player.rotation_degrees.y == camsets[camsetarray]:
			if evn.get_action_strength("pan_left") > 0:
				turnDir = 'left'
				if camsetarray < 3:
					camsetarray += 1
					cam = 2
				else:
					camsetarray = 0
					cam = 4
			elif evn.get_action_strength("pan_right") > 0:
				turnDir = 'right'
				if camsetarray > 0:
					camsetarray -= 1
					cam = 2
				else:
					camsetarray = 3
					cam = 3
		else: #find closest 90 degree angle
			var playerRot = round(player.rotation_degrees.y)
			var rot = playerRot
			playerRot = deg2rad(playerRot)
			var set = false
			var dist
			var threshold = deg2rad(30)
			if evn.get_action_strength("pan_left") > 0:
				turnDir = 'left'
				while(!set):
					if rot > -179: rot -= 1
					else: rot = 180
					for i in range(4):
						if (rot == camsets[i]):
							if (findDegreeDistance(deg2rad(camsets[i]), playerRot) > threshold):
								set = true
								camsetarray = i
								if (i == 0): cam = 4
								else: cam = 2
			elif evn.get_action_strength("pan_right") > 0:
				turnDir = 'right'
				while(!set):
					if rot < 179: rot += 1
					else: rot = -180
					for i in range(4):
						if (rot == camsets[i]):
							if (findDegreeDistance(deg2rad(camsets[i]), playerRot) > threshold):
								set = true
								camsetarray = i
								if (i == 3): cam = 3
								else: cam = 2
		lastAng = -1 * player.rotation.y
		#stickMove = false

func _process(delta: float) -> void:
	if lerpMove:
		if heightMove:
			translation.y = lerp(translation.y, targetY, .05)
			rotation_degrees.x = lerp(rotation_degrees.x, targetRotX, .05)
			if (translation.y > targetY - .1 && translation.y < targetY + .1):
				translation.y = targetY
				if (rotation_degrees.x > targetRotX - .1 && rotation_degrees.x < targetRotX + .1):
					rotation_degrees.x = targetRotX
					heightMove = false
		if angMove:
			if sign(player.ang) != sign(angMoveTarget):
				var add = findDegreeDistance(player.ang, angMoveTarget);
				if (turnDir == "left"): add *= -1;
				player.ang = angMoveTarget + add
			player.ang = lerp_angle(player.ang, angMoveTarget, .1)
			if (player.ang > angMoveTarget - .005 && player.ang < angMoveTarget + .005):
				player.ang = angMoveTarget
				angMove = false
		lerpMove = heightMove || angMove
	elif shakeMove:
		var nextY = baseY + shakeIntensity if (randf() < .5) else baseY - shakeIntensity
		var nextX = shakeIntensity if (randf() < .5) else -shakeIntensity
		if shakeAlternate:
			translation.x = nextX
			if translation.y != nextY: translation.y = nextY
		elif !shakeAlternate:
			translation.y = nextY
			if translation.x != nextX: translation.x = nextX
		shakeAlternate = !shakeAlternate
		shakeMoveTimer -= 60 * delta
		if shakeMoveTimer < 0:
			shakeMove = false
			shakeMoveTimer = 0
			translation.y = baseY
			translation.x = 0
	elif fixedCamLock:
		if lockOn != null || player.angTarget != 0:
			fixedCamLock = false
			player.camLock = false
			near = 10
		else: player.camLock = true
	if (lockOn != null): return
	if cam > 1 and (player.rotation_degrees.y != camsets[camsetarray]): #q and e rotate
		var proty = player.rotation_degrees.y
		if cam == 2:
			if proty < camsets[camsetarray] - turnRate: player.rotation_degrees.y += turnRate * delta * 60
			elif proty > camsets[camsetarray] + turnRate: player.rotation_degrees.y -= turnRate * delta * 60
			else:
				player.rotation_degrees.y = camsets[camsetarray]
				cam = 0
		elif cam == 3: #over (135 to -135)
			if proty >= 0:#134:
				if proty < 180: player.rotation_degrees.y += turnRate * delta * 60
				else: player.rotation_degrees.y = -179
			else: #elif proty < 1:
				if proty < -135 - (turnRate + 1): player.rotation_degrees.y += turnRate * delta * 60
				else:
					player.rotation_degrees.y = -135
					cam = 0
			#else: player.rotation_degrees.y = 134
		elif cam == 4: #under (-135 to 135)
			if proty <= 0: #-134
				if proty > -180: player.rotation_degrees.y -= turnRate * delta * 60
				else: player.rotation_degrees.y = 179
			else: #elif proty > 1:
				if proty > 135 + (turnRate + 1): player.rotation_degrees.y -= turnRate * delta * 60
				else:
					player.rotation_degrees.y = 135
					cam = 0
			#else: player.rotation_degrees.y = -134
		player.angTarget = -1 * player.rotation.y
		if cam == 0:
			turnDir = 'left' if (player.ang + 3 > player.angTarget + 3) else 'right'
			player.call("_applyFriction", 0, .49 + (player.traction * .017))
			if customset != 0:
				cam = customset
				customset = 0
				return
	elif Input.get_action_strength("move_camera_right") > 0 or Input.get_action_strength("move_camera_left") > 0:
		var panStrength = Input.get_action_strength("move_camera_right") - Input.get_action_strength("move_camera_left")
		player.rotate_y(lerp(0, .1, panStrength*abs(panStrength)*.25)) #needs to eventually just rotate camera not player
		if !stickMove:
			lastAng = player.rotation.y * -1
			player.camLock = true
			stickMove = true
		setDelay.stop()
		setDelay.start(6)
	elif stickMove == true:
		stickMove = false
		player.camLock = false
		player.rotation.y = lastAng * -1

func _findLockOn(lockOnMode) -> void:
	if (lockOnMode != null): #revert to null
		if player.lockOn != null:
			player.lockOn = null
			player.angTarget = 0
		player.ang = player.rotation.y * -1
		player.camLock = false
		if lockOn != null && is_instance_valid(lockOn): lockOn.arrow.visible = false
		lockOn = null
		return
	if lockOn != null: lockOn.arrow.visible = false
	lockOn = null
	var areas = []
	for area in lockScanner.get_overlapping_areas():
		if (area.get_parent().name == player.name): continue
		areas.append(area.get_parent())
	if len(areas) == 0:
		_findLockOn(0)
		return
	var myPoint = player.global_transform.origin
	var spaceState = get_world().direct_space_state
	var los
	var distance = 9999
	var angDist = .5
	var checkDistance
	var distFloor = 17
	var moveAng = Vector2(player.moveDir[1] * -1, player.moveDir[0]).rotated(player.ang).angle()
	var lastRot = player.rotation.y
	for enemy in areas:
		print(enemy.name)
		if enemy.lockable == false: continue
		var eLocation = enemy.global_transform.origin
		los = spaceState.intersect_ray(player.translation, eLocation)
		if (los.size() > 0):
			if los["collider"].is_in_group("walls"):
				continue
		checkDistance = myPoint.distance_to(eLocation)
		# adjacent priority (distFloor) > angle priority (angDist) > distance priority (distance)
		if (checkDistance < distance && angDist == .5) || checkDistance < distFloor:
			#if closer than last and haven't set angle priority and haven't set adjacent priority
			distance = checkDistance
			lockOn = enemy
			if checkDistance < distFloor: distFloor = checkDistance #trigger adjacent target priority
		elif distFloor == 15: #if haven't triggered close target yet
			player.look_at(Vector3(eLocation.x,eLocation.y,eLocation.z),Vector3.UP)
			var enemyAngDist = findDegreeDistance(lastRot, player.rotation.y)
			if enemyAngDist < angDist:
				angDist = enemyAngDist #trigger angle priorty
				lockOn = enemy
			player.rotation.y = lastRot
	if (lockOn == null):
		_findLockOn(0)
		return
	player.lockOn = lockOn
	player.look_at(Vector3(lockOn.translation.x, player.translation.y, lockOn.translation.z), Vector3.UP)
	if player.angTarget != 0: player.ang = player.angTarget
	player.angTarget = player.rotation.y * -1
	player.rotation.y = lastRot
	lockOn.arrow.visible = true
	player.camLock = false

func _directionalLockOn(direction: String, closest: bool) -> void:
	var areas = []
	var tempLockOn = null
	var mobParent
	for area in lockScanner.get_overlapping_areas():
		mobParent = area.get_parent()
		if (mobParent.name == player.name) || (lockOn != null && mobParent == lockOn): continue
		areas.append(mobParent)
	if len(areas) == 0: return
	var spaceState = get_world().direct_space_state
	var los
	var angDist = 99 if closest else 0
	var enemyAngDist = 0
	var lastRot = player.rotation.y
	player.look_at(Vector3(lockOn.translation.x,lockOn.translation.y,lockOn.translation.z),Vector3.UP)
	var targAngle = player.rotation.y
	player.rotation.y = lastRot
	for enemy in areas:
		if enemy.lockable == false: continue
		var eLocation = enemy.global_transform.origin
		los = spaceState.intersect_ray(player.translation, eLocation)
		if (los.size() > 0):
			if los["collider"].is_in_group("walls"): continue
		player.look_at(Vector3(eLocation.x,eLocation.y,eLocation.z),Vector3.UP)
		if (direction=="L"&&player.rotation.y+3>targAngle+3)||(direction=="R"&&player.rotation.y+3<targAngle+3):
			enemyAngDist = findDegreeDistance(player.rotation.y, targAngle)
			if (closest && enemyAngDist < angDist) || (!closest && enemyAngDist > angDist):
				angDist = enemyAngDist
				tempLockOn = enemy
		player.rotation.y = lastRot
	if tempLockOn == null:
		if closest && direction == "R": _directionalLockOn("L", false)
		elif closest && direction == "L": _directionalLockOn("R", false)
		else:
			lockOn = 0
			_findLockOn(lockOn)
		return
	if lockOn != null: lockOn.arrow.visible = false
	lockOn = tempLockOn
	player.lockOn = lockOn
	player.look_at(Vector3(lockOn.translation.x, player.translation.y, lockOn.translation.z), Vector3.UP)
	player.angTarget = player.rotation.y * -1
	player.rotation.y = lastRot
	lockOn.arrow.visible = true
	player.camLock = false

func findClosestCamSet(rotation: float): #in degrees
	if player.camLock: rotation = rad2deg(lastAng * -1)
	var targ = 0
	var dist = 1000
	for i in range(len(camsets)):
		if abs(camsets[i] - rotation) < dist:
			dist = abs(camsets[i] - rotation)
			targ = i
	player.squishSet = false
	camsetarray = targ
	return targ

func findDegreeDistance(from: float, to: float):
	var max_angle = 6.28 #approx 2*PI
	var difference = fmod(to - from, max_angle)
	return abs(fmod(2 * difference, max_angle) - difference)

func _auto_move_camera(target: int, direction: String, neg: bool) -> void:
	camsetarray = findClosestCamSet(player.rotation_degrees.y)
	if fixedCamLock:
		fixedCamLock = false
		player.camLock = false
		near = 10
	if target == camsetarray && (direction == "R" || direction == "L"): return
	if direction == "R" || direction == "L":
		if direction == "L":
			turnDir = 'left'
			if (target == 0): cam = 4 if (camsetarray != 1) else 2
			elif (target == 2): cam = 2# if (camsetarray != 0) else 2
			elif (target == 3): cam = 3 if (camsetarray != 2) else 2
		elif direction == "R":
			if (target == 2): 
				if (camsetarray != 0): cam = 2
				else:
					cam = 3
					customset = 2
			elif (target == 1): cam = 2
		player.angDelayFriction = false
		turnRate = 2
		camsetarray = target
		setDelay.stop()
		setDelay.start(3)
	else:
		lerpMove = true
		shakeMove = false
		if direction == "H":
			if (target <= 3): target = 3 #some fuck shit
			elif (target <= 6): target = 6
			if neg: target *= -1
			if (translation.y == baseY + target): return
			targetY = baseY + target
			targetRotX = baseRotX - target
			heightMove = true
		elif direction == "HA": #height angle hybrid (for ramps and stuff)
			var htarg = 6
			if neg: htarg *= -1
			if (translation.y != baseY + htarg):
				targetY = baseY + htarg
				targetRotX = baseRotX - 6
				heightMove = true
			angMoveTarget = deg2rad(target - 180) #target goes to the angMove
			angMove = true
			player.angTarget = 0
			player.camLock = false
			turnDir = 'left' if (player.ang + 3 > angMoveTarget + 3) else 'right'
		elif direction == "O": #height animation reset
			targetY = baseY
			targetRotX = baseRotX
			heightMove = true
			angMove = false
			player.camLock = false
		elif direction == "A" || direction == "AF":
			angMoveTarget = deg2rad(target - 180)
			angMove = true
			player.angTarget = 0
			player.camLock = false
			turnDir = 'left' if (player.ang + 3 > angMoveTarget + 3) else 'right'
			if direction == "AF":
				fixedCamLock = true
				near = 12
	autoBuffer = false #tigger the buffer off

func _setToDefaults() -> void:
	player.lockOn = null
	player.camLock = false
	player.angTarget = 0
	_findLockOn(0)
	translation.y = baseY
	rotation_degrees.x = baseRotX
	heightMove = false
	angMove = false
	lerpMove = false
	shakeMove = false
	fixedCamLock = false
	near = 10
	
func _shakeMove(setTimer: float, intensity: float, distance: float) -> void:
	if distance != 0:
		var distMod = (intensity * 1.5) / distance
		if distMod < .15: return
		if distMod > 1: distMod = 1
		intensity *= distMod
	shakeMove = true
	shakeMoveTimer = setTimer
	shakeIntensity = intensity * .1

func _on_bufferTimer_timeout():
	bufferTimer.stop()
	autoBuffer = false
