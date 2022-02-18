extends Camera
onready var player = get_node("../../")
onready var mesh = get_node('../../CollisionShape/BallSkin')
onready var setDelay = get_node("setDelay")
var cam = 0 #rotate mode
var camsets = [135,45,-45,-135]
var camsetarray = 1
onready var lastAng = camsets[camsetarray]
var turnRate = 8
var stickMove = false
var turnDir = 'right'

func _ready():
	player.rotation_degrees.y = 45
	mesh.rotation_degrees.y = 45

func _input(event: InputEvent) -> void:
	if (event is InputEventMouseButton): #quick camera
		if event.is_pressed():
			cam = 1
			lastAng = -1 * player.rotation.y
		else:
			cam = 0
			camsetarray = findClosestCamSet(player.rotation_degrees.y)
			stickMove = false
			player.angTarget = -1 * player.rotation.y
			player.cameraFriction = (1-(findDegreeDistance(lastAng,player.angTarget)/3.14))
			if player.cameraFriction > 1: player.cameraFriction = 1
	elif (event is InputEventMouseMotion and cam == 1) or event.is_action_pressed("pan_right") or event.is_action_pressed("pan_left"):
		_move_camera(event)

func _move_camera(evn) -> void:
	turnRate = 8
	if ((evn is InputEventMouseMotion) and (cam == 1)): #free cam
		player.rotate_y(-lerp(0, 1.0, evn.relative.x/300)) #needs to eventually just rotate camera not player
		if evn.relative.x < 0: turnDir = 'right'
		elif evn.relative.x: turnDir = 'left'
		#new stuff line 1.27.22
		setDelay.stop()
		setDelay.start(6)
	elif (cam != 1):
		camsetarray = findClosestCamSet(player.rotation_degrees.y)
#		player.rotation_degrees.y = camsets[camsetarray]
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
		lastAng = -1 * player.rotation.y
		stickMove = false

func _process(delta: float) -> void:
	if cam > 1 and (player.rotation_degrees.y != camsets[camsetarray]): #q and e rotate
		var proty = player.rotation_degrees.y
		if cam == 2:
			if proty < camsets[camsetarray] - turnRate: player.rotation_degrees.y += turnRate * delta * 60
			elif proty > camsets[camsetarray] + turnRate: player.rotation_degrees.y -= turnRate * delta * 60
			else:
				player.rotation_degrees.y = camsets[camsetarray]
				cam = 0
		elif cam == 3: #over (135 to -135)
			if proty >= 134:
				if proty < 180: player.rotation_degrees.y += turnRate * delta * 60
				else: player.rotation_degrees.y = -179
			elif proty < 1:
				if proty < -144: player.rotation_degrees.y += turnRate * delta * 60
				else: 
					player.rotation_degrees.y = -135
					cam = 0
			else: player.rotation_degrees.y = 134
		else: #under (-135 to 135)
			if proty <= -134:
				if proty > -180: player.rotation_degrees.y -= turnRate * delta * 60
				else: player.rotation_degrees.y = 179
			elif proty > 1:
				if proty > 144: player.rotation_degrees.y -= turnRate * delta * 60
				else:
					player.rotation_degrees.y = 135
					cam = 0
			else: player.rotation_degrees.y = -134
		player.angTarget = -1 * player.rotation.y
		if cam == 0:
			if player.cameraFriction == 1: player.cameraFriction = (1-(findDegreeDistance(lastAng,player.angTarget)/3.14))*.8
			else:
				player.cameraFriction -= (1 - (findDegreeDistance(lastAng,player.angTarget)/3.14))
				if player.cameraFriction < 0: player.cameraFriction = 0
			if player.cameraFriction > 1: player.cameraFriction = 1
			#print(player.cameraFriction)
			#mesh.rotate_y(player.angTarget)
	elif Input.get_action_strength("move_camera_right") > 0 or Input.get_action_strength("move_camera_left") > 0:
		if stickMove == false:
			lastAng = -1 * player.rotation.y
			stickMove = true
		var panStrength = Input.get_action_strength("move_camera_right") - Input.get_action_strength("move_camera_left")
		if panStrength > 0: turnDir = 'right'
		else: turnDir = 'left'
		player.rotate_y(lerp(0, .055, panStrength*1.1)) #needs to eventually just rotate camera not player
		if player.cameraFriction > 1: player.cameraFriction = 1
		setDelay.stop()
		setDelay.start(6)
	elif stickMove == true:
		stickMove = false
		player.angTarget = -1 * player.rotation.y
		player.cameraFriction = (1-(findDegreeDistance(lastAng,player.angTarget)/3.14))*1.1
		if player.cameraFriction > 1: player.cameraFriction = 1

func findClosestCamSet(rotation: float):
	var targ = 0
	var dist = 1000
	for i in range(len(camsets)):
		if abs(camsets[i] - rotation) < dist:
			dist = abs(camsets[i] - rotation)
			targ = i
	player.squishSet = false
	return targ

func findDegreeDistance(from,to):
	var max_angle = 6.28 #approx 2*PI
	var difference = fmod(to - from, max_angle)
	return abs(fmod(2 * difference, max_angle) - difference)

func _auto_move_camera(target: int) -> void:
	if target == camsetarray: return
	player.rotation_degrees.y = camsets[target]
	setDelay.stop()
	setDelay.start(6)
