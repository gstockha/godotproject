extends Camera
onready var player = get_node("../../")
onready var mesh = get_node('../../CollisionShape/BallSkin')
onready var lockScanner = get_node('../../lockOnScanner')
onready var setDelay = get_node("setDelay")
var cam = 0 #rotate mode
var camsets = [135,45,-45,-135]
var camsetarray = 1
onready var lastAng = camsets[camsetarray]
var turnRate = 8
#var stickMove = false
var turnDir = 'right'
var autoBuffer = false #make sure you're going the right direction to trigger auto cam
var customset = 0 #double camsets after a first one
var lockOn = null
var baseY = 5 #translation Y
var baseRotX = -10 #rotation_degrees X
var targetY = baseY
var targetRotX = baseRotX
var heightMove = false

func _ready():
	player.rotation_degrees.y = 45
	mesh.rotation_degrees.y = 45

func _input(event: InputEvent) -> void:
	if (event is InputEventMouseButton): #quick camera
		if event.is_pressed():
			cam = 1
			if (lockOn == null): lastAng = -1 * player.rotation.y
		elif (lockOn == null):
			cam = 0
			camsetarray = findClosestCamSet(player.rotation_degrees.y)
			#stickMove = false
			player.angTarget = -1 * player.rotation.y
	elif (event is InputEventMouseMotion and cam == 1) or event.is_action_pressed("pan_right") or event.is_action_pressed("pan_left"):
		_move_camera(event)
	elif (event.is_action_pressed("lock_on")): _findLockOn(lockOn)

func _move_camera(evn) -> void:
	turnRate = 8
	player.angDelayFriction = true
	if ((evn is InputEventMouseMotion) and (cam == 1)): #free cam
		player.rotate_y(-lerp(0, 1.0, evn.relative.x/300)) #needs to eventually just rotate camera not player
		if evn.relative.x < 0: turnDir = 'right'
		elif evn.relative.x: turnDir = 'left'
		player.angTarget = -1 * player.rotation.y
		player.cameraFriction = .5 + (player.traction * .001)
		camsetarray = findClosestCamSet(player.rotation_degrees.y)
		setDelay.stop()
		setDelay.start(6)
	elif (cam != 1):
		var rot = round(player.rotation_degrees.y)
		if player.rotation_degrees.y == camsets[camsetarray]:
			if evn.is_action("pan_left"):
				turnDir = 'left'
				if camsetarray < 3:
					camsetarray += 1
					cam = 2
				else:
					camsetarray = 0
					cam = 4
			elif evn.is_action("pan_right"):
				turnDir = 'right'
				if camsetarray > 0:
					camsetarray -= 1
					cam = 2
				else:
					camsetarray = 3
					cam = 3
		else: #find closest 90 degree angle
			var set = false
			if evn.is_action("pan_left"):
				turnDir = 'left'
				while(!set):
					if rot > -179: rot -= 1
					else: rot = 180
					for i in range(4):
						if (rot == camsets[i]):
							set = true
							camsetarray = i
							if (i == 0): cam = 4
							else: cam = 2
			elif evn.is_action("pan_right"):
				turnDir = 'right'
				while(!set):
					if rot < 179: rot += 1
					else: rot = -180
					for i in range(4):
						if (rot == camsets[i]):
							set = true
							camsetarray = i
							if (i == 3): cam = 3
							else: cam = 2
		lastAng = -1 * player.rotation.y
		#stickMove = false

func _process(delta: float) -> void:
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
			if customset != 0:
				cam = customset
				customset = 0
				return
#			print(camsetarray)
#			if player.cameraFriction == 1: player.cameraFriction = (1-(findDegreeDistance(lastAng,player.angTarget)/3.14))*.8
#			else:
#				player.cameraFriction -= (1 - (findDegreeDistance(lastAng,player.angTarget)/3.14))
#				if player.cameraFriction < 0: player.cameraFriction = 0
#			if player.cameraFriction > 1: player.cameraFriction = 1
	elif Input.get_action_strength("move_camera_right") > 0 or Input.get_action_strength("move_camera_left") > 0:
#		if stickMove == false:
#			lastAng = -1 * player.rotation.y
#			stickMove = true
		var panStrength = Input.get_action_strength("move_camera_right") - Input.get_action_strength("move_camera_left")
		if panStrength > 0: turnDir = 'right'
		else: turnDir = 'left'
		player.rotate_y(lerp(0, .11, panStrength*abs(panStrength)*.4)) #needs to eventually just rotate camera not player
#		if player.cameraFriction > 1: player.cameraFriction = 1
		player.cameraFriction = .5 + (player.traction * .0015)
		player.angTarget = -1 * player.rotation.y
		setDelay.stop()
		setDelay.start(6)
		camsetarray = findClosestCamSet(player.rotation_degrees.y)
#	elif stickMove == true:
#		stickMove = false
#		player.angTarget = -1 * player.rotation.y
#		camsetarray = findClosestCamSet(player.rotation_degrees.y)
#		player.cameraFriction = (1-(findDegreeDistance(lastAng,player.angTarget)/3.14))*1.1
#		if player.cameraFriction > 1: player.cameraFriction = 1
	if heightMove:
		print('moving')
		translation.y = lerp(translation.y, targetY, .05)
		rotation_degrees.x = lerp(rotation_degrees.x, targetRotX, .05)
		print(translation.y)
		print(rotation_degrees.x)
		if (translation.y > targetY - .1 && translation.y < targetY + .1):
			if (rotation_degrees.x > targetRotX - .1 && rotation_degrees.x < targetRotX + .1):
				heightMove = false

func _findLockOn(lockOnMode) -> void:
	lockOn = null
	if (lockOnMode != null): #revert to null
		player.lockOn = null
		camsetarray = findClosestCamSet(player.rotation_degrees.y)
		#player.rotation_degrees.y = camsets[camsetarray]
		#player.ang = player.rotation.y * -1
		player.angTarget = 0
		return
	var areas = []
	for area in lockScanner.get_overlapping_areas():
		if (area.get_parent().name == player.name): continue
		areas.append(area.get_parent())
	if len(areas) == 0: return
	var myPoint = global_transform.origin
	var distance = 9999
	var checkDistance
	var spaceState = get_world().direct_space_state
	var los
	for area in areas:
		los = spaceState.intersect_ray(player.translation, area.translation)
		if (los.size() > 0):
			if los["collider"].is_in_group("walls"): continue
		checkDistance = myPoint.distance_to(area.global_transform.origin)
		if checkDistance < distance:
			distance = checkDistance
			lockOn = area
	if (lockOn == null): return
	player.lockOn = lockOn
	player.look_at(Vector3(lockOn.translation.x, player.translation.y, lockOn.translation.z), Vector3.UP)
	player.ang = player.rotation.y * -1

func findClosestCamSet(rotation: float): #in degrees
	var targ = 0
	var dist = 1000
	for i in range(len(camsets)):
		if abs(camsets[i] - rotation) < dist:
			dist = abs(camsets[i] - rotation)
			targ = i
	player.squishSet = false
	camsetarray = targ
	return targ

func findDegreeDistance(from,to):
	var max_angle = 6.28 #approx 2*PI
	var difference = fmod(to - from, max_angle)
	return abs(fmod(2 * difference, max_angle) - difference)

func _auto_move_camera(target: int, direction: String) -> void:
	camsetarray = findClosestCamSet(player.rotation_degrees.y)
	if target == camsetarray && direction != "H" && direction != "R": return
	if direction != "H" && direction != "R":
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
	elif direction == "H":
		targetY = baseY + target
		targetRotX = baseRotX - target
		heightMove = true
	elif direction == "R":
		if (target == 0): #no animation reset
			translation.y = baseY
			rotation_degrees.x = baseRotX
		else: #not 0, animation
			targetY = baseY
			targetRotX = baseRotX
			heightMove = true
	autoBuffer = false
