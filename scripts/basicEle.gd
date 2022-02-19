extends KinematicBody
var math = preload("res://scripts/math.gd")

#var floordir = Vector3(0.0, 1.0, 0.0)
export var gravity := 23.0
export var jumpforce := 12.0 #base level jumping height
var yvelocity = -1
export var bouncebase := .7
var bounce = bouncebase #how big your bounce is (can't be above 1)
var bounceCombo = 0
var bounceComboCap = 3
export var bouncethreshold := 3 #how much yvelocity you need to bounce
var basejumpwindow = 0 #your frame window for bounce jumps
var jumpwindow = 0
var ang = 0
var angTarget = 0
var cameraFriction = 1 #apply to friction after moving camera
#var angDelay = null #delay changing the ang from an auto camera change
var wallb = false #dribbled off wall
var wallbx = 0 #wall dribble direction
var wallby = 0
var idle = true
var dashing = false
var canCrash = false #set to true in jump function
var bouncedashing = 0
var walldashing = false #for speed boost after dashing into a wall
var rolling = true #rolling on the ground (event if not pressing keys)
var moving = false #keyboard press
var dir = []
export var dirsize = 13 #list size
var stickdir = [0,0]
var dragdir = [0,0]
var friction = 0
export var speedCap := 10.0 #max ground (roll) speed
var speed = speedCap
export var traction = 50
var tractionlist = [] #array 100 long
export var baseweight := 1.2
var weight = baseweight
var dashspeed = speedCap * 1.5
export var dashcost := .4
var boing = 0 #boing state
var boingCharge = false #holding space
var boingDash = false #use dashspeed in boing slide (turned on in isRolling() and turned off in boing jump and boing timer)
var squishSet = false #only run the mesh squish settings once in _squishNScale
var squishGrow = true #tells the _squishNScale script to keep growing
var squishReverb = [0,1,false] #0 is the boOoIiNng effect after bouncing, 1 is the last setting, 2 is wallb switch

var lastTranslationY = 0 #for shifted
var shiftedLastYNorm = 0 #downward slope boost
var shiftedLingerVec = Vector3.ZERO #shifted force movement for lingering momentum
var shiftedDir = 0 #going up a hill or down it
var shiftedSticky = -1 #-1 means is sticky, 0 means unsticky (activated)
var shiftedBoost = [0,0] #keep applying shift even after off shift (shiftedDir is 0), 1 is used and denominator
var rampSlope = 0 #0 gets good y normal of ramp
var shiftedLinger = false

var slowMo = false
var speedRun = false
var controlNames = {'roll': '', 'jump': '', 'dash': '', 'camera': '', 'restart': '', 'speedrun': ''}

onready var boingTimer = get_node("boingTimer")
onready var preBoingTimer = get_node("preBoingTimer")
onready var dashtimer = get_node("DashTimer")
onready var deathtimer = get_node("hitBox/deathTimer")
onready var mesh = $CollisionShape/BallSkin
onready var collisionShape = $CollisionShape
onready var collisionBaseScale = .6
onready var collisionScales = [collisionBaseScale,collisionBaseScale,collisionBaseScale]
onready var bottom = $RayCast
onready var checkpoint = get_node("../checkpoints/checkpoint1")
onready var moveNote = get_node("../../moveNote")
onready var tipNote = get_node("../../tipNote")
onready var speedrunNote = get_node("../../speedrunNote")
onready var camera = get_node("Position3D/playerCam")
onready var prNote = get_node("../../prNote")
var skrrt = [0,0]

var direction_ground = Vector2.ZERO
var velocity = Vector3.ZERO

func _ready():
	ang = (-1 * rotation.y)
	collisionShape.rotation_degrees.y = ang
	math.create2d(dir,dirsize,2,0) #create 2d array, 2 high 10 wide with 0 values
	yvelocity = -1
	checkpoint = checkpoint.get_translation()
	for x in range(101): #create stat lists
		#tractionlist.append(((i * (i * .2)) * .015))
		tractionlist.append((pow(1.0475,x)-1)*((pow(0.01*x,25)*.29)+.7))
		#if (x == 50) or (x == 100) or (x == 0): print(tractionlist[x])
	var controllerStr = ['','','','']
	if Input.is_joy_known(0) == false: controllerStr = ['WASD or Arrow Keys', 'Space', 'Shift or C', 'Q & E or Z & X', 'R', 'T']
	else:
		var name = Input.get_joy_name(0)
		if name.begins_with('x') or name.begins_with('X'): controllerStr = ['Left Joystick', 'A', 'X', 'L & R', 'Start' , 'Back']
		else: controllerStr = ['Left Joystick', 'Bottom Face Button', 'Left Face Button', 'L & R', 'Start', 'Select']
	controlNames['roll'] = controllerStr[0]
	controlNames['jump'] = controllerStr[1]
	controlNames['dash'] = controllerStr[2]
	controlNames['camera'] = controllerStr[3]
	controlNames['restart'] = controllerStr[4]
	controlNames['speedrun'] = controllerStr[5]

func _physics_process(delta: float) -> void: #where everything is actually ran
	#camera delay
	if boing == 0:
		_controller(delta) #general movement
		var isGrounded = is_on_floor() or yvelocity == -1
		if isGrounded: _isRolling(delta) #standing vs in-air
		elif not is_on_ceiling() and not is_on_wall(): _isAirborne(delta) #in air
		elif is_on_wall(): _isWall(delta) #wall physics
		elif yvelocity > 0: yvelocity *= -1 #if going up to ceiling
		_applyShift(delta,isGrounded)
		if squishGrow: _squishNScale(delta,null)
	else: _isBoinging(delta) #boinging
	if angTarget != 0:
		#print('turning ang ' + str(ang) +', to ' + str(angTarget))
		if (sign(ang) != sign(angTarget)):
			var add = math.findDegreeDistance(ang,angTarget)
			if camera.turnDir == 'left': add *= -1
			ang = angTarget + add
		ang = lerp(ang,angTarget,.015+(tractionlist[traction] * .0007))
		if math.roundto(ang,10) == math.roundto(angTarget,10):
			ang = angTarget
			angTarget = 0

func _controller(delta: float) -> void: #general movement (move_and_slide)
	#direction
	if not idle:
		stickdir[0] = Input.get_action_strength("move_right") - Input.get_action_strength("move_left")
		stickdir[1] = Input.get_action_strength("move_down") - Input.get_action_strength("move_up")
	if (stickdir[0] != 0 or stickdir[1] != 0): moving = true #if pressing movekeys
	else: moving = false
	_applyFriction(delta)
	direction_ground = Vector2(dragdir[0],dragdir[1]).rotated(ang).normalized()
	#move
	var xvel = 0
	var yvel = 0
	if rolling and !dashing: #rolling and moving
		xvel = direction_ground.x * (speed * friction)
		yvel = direction_ground.y * (speed * friction)
	elif !wallb and !dashing: #in air or idle (not rolling or moving)
		xvel = direction_ground.x * (.9 * speed * friction)
		yvel = direction_ground.y * (.9 * speed * friction)
	elif wallb and !dashing: #wallbounced
		xvel = wallbx
		yvel = wallby
	elif dashing: #dashed
		xvel = direction_ground.x * dashspeed
		yvel = direction_ground.y * dashspeed
	if (xvel != 0 or yvel != 0): _rotateMesh(xvel,yvel,delta)
	velocity = Vector3(xvel, yvelocity, yvel) #apply velocity
	move_and_slide(velocity, Vector3.UP, true) #move with velocity

func _applyFriction(delta: float) -> void: #add 'drag' to movement
	var current = dirsize - 1
	#multi-directional movement delay
	for i in range(dirsize - 1):
		dir[0][i] = dir[0][i+1]
		dir[1][i] = dir[1][i+1]
	dir[0][current] = stickdir[0]
	dir[1][current] = stickdir[1]
	dir[0][current] = math.arrayMean(dir[0])
	dir[1][current] = math.arrayMean(dir[1])
	#camera compensation
	if cameraFriction != 1:
		dir[0][current]  *= cameraFriction
		dir[1][current]  *= cameraFriction
		cameraFriction += delta * (2+(tractionlist[traction] * .04))
		if cameraFriction > 1:
			cameraFriction = 1
#			print('skrrt ' + str(round(skrrt[0])))
#			skrrt[0] = 0
#		else: skrrt[0] += 60 * delta
	var signdir
	if moving: #diametric turn delay (SKRRRT)
		dragdir[0] = math.arrayMean(dir[0])
		dragdir[1] = math.arrayMean(dir[1])
		for i in range(2): #apply sharp turn friction (SKRRT!)
			signdir = sign(stickdir[i])
			if (signdir != 0) and (sign(dragdir[i]) != signdir):
				dir[i][current] += ((tractionlist[traction]) * signdir) * delta
#				skrrt[i] += 60 * delta
#			elif (skrrt[i] != 0): 
#				print(round(skrrt[i]))
#				skrrt[i] = 0
	elif (dragdir[0] != 0 or dragdir[1] != 0): #stop at .015 when not moving and apply drift
		for i in range(2):
			if (dragdir[i] == 0): continue
			elif (abs(dragdir[i]) > .015): 
				dragdir[i] = math.arrayMean(dir[i])
				#apply drift
				signdir = sign(dir[i][current])
				dir[i][current] -= ((tractionlist[traction]) * .08 * signdir) * baseweight * delta
				if ((signdir == 1) and (dir[i][current] < 0)) or ((signdir == -1) and (dir[i][current] > 0)): dir[i][current] = 0
			else: dragdir[i] = 0
	#apply friction
	var absx = abs(dragdir[0])
	var absy = abs(dragdir[1])
	if absx > absy: friction = absx
	else: friction = absy
	if friction > 1: friction = 1

func _applyShift(delta: float, isGrounded: bool) -> void: #apply shift (slopoes or otherwise)
	if shiftedDir != 0 and not shiftedLinger: #on shift
		var grav = (.05 + (baseweight * .01))
		var fric = friction
		var shift = bottom.get_collision_normal()
		if shiftedDir > 0: #going down
			if !dashing: shiftedBoost[0] += delta * (baseweight * 10) #charge up a linger vector
			else: shiftedBoost[0] += delta * (baseweight * 20)
			if shiftedBoost[0] > 30: shiftedBoost[0] = 30
			shiftedBoost[1] = shiftedBoost[0] #records the max shiftedBoost[0] cap
			if (shift.y != 1): #to make sure we're not passing a flat vector below on accident
				var record = true
				if (shiftedLastYNorm == 0): shiftedLastYNorm = shift.y
				elif (round(shift.y * 10) > round(shiftedLastYNorm * 10)): record = false
				else: shiftedLastYNorm = shift.y
				if record: #save the last rolling down vector
					fric *= (shiftedBoost[0] * (1 - shiftedLastYNorm))
					shiftedLingerVec = Vector3(shift.x * grav * fric, 0, shift.z * grav * fric)
				_rotateMesh(shiftedLingerVec.x*2*60, shiftedLingerVec.z*2*60, delta)
		elif shiftedDir < 0: shiftedBoost[0] = 0
		move_and_collide(Vector3(shift.x * fric * grav,shiftedSticky,shift.z * fric * grav),true)
	elif shiftedBoost[0] > 0: #shift linger
		shiftedLinger = true
		shiftedBoost[0] -= delta * (baseweight * 10)
		if shiftedBoost[0] < 0: shiftedBoost[0] = 0
		var momentum = (shiftedBoost[0] / shiftedBoost[1])
		if rampSlope != 0: #decrease the Y slope vector over time
			rampSlope -= (delta * (1 - (shiftedBoost[1] * .01)) * (baseweight * .1))
			if rampSlope < 0: rampSlope = 0
		move_and_collide(Vector3((shiftedLingerVec.x*momentum)*friction,rampSlope,(shiftedLingerVec.z*momentum)*friction),true)
		_rotateMesh(shiftedLingerVec.x*60*momentum, shiftedLingerVec.z*60*momentum, delta)
		if (shiftedBoost[0] < 0 or momentum < .01 or friction == 0): #turn it off
			shiftedBoost[0] = 0
			shiftedBoost[1] = 0
			shiftedLinger = false
			rampSlope = 0
	if isGrounded: #if isRolling, check shift status
		if (get_slide_count() > 0): #shifted check
			shiftedSticky = 0
			if (not get_floor_normal().y < 1) or get_slide_collision(0).collider.is_in_group('flats'): #on normal ground
				shiftedDir = 0
				rampSlope = 0
				shiftedLastYNorm = 0
			else: #on shifted ground
				if shiftedDir != 0:
					shiftedDir = (lastTranslationY - translation.y) * delta * 60
					if (shiftedDir > 0): #going DOWN slope
						shiftedSticky = -1
						rampSlope = 0
					elif shiftedLinger and (get_slide_collision(0).collider.is_in_group('ramps')):
						if friction > .7 and (rampSlope < (1 - get_floor_normal().y)): #get relevant downward Y vector normal
							rampSlope = (1 - get_floor_normal().y) * friction
				else: shiftedDir = -.1
				lastTranslationY = translation.y
		elif !bottom.is_colliding(): #falling off of a shift
			yvelocity -= (gravity * weight) * delta #gravity
			shiftedSticky = 0

func _isRolling(delta: float) -> void: #on ground
	jumpwindow = 0
	wallb = false
	canCrash = false
	idle = false
	#if angDelay != null:
		#ang = angDelay
		#angDelay = null
	if !walldashing: #if landing, cancel dash
		if dashing and (shiftedDir == 0): #cancel dashing if not on shift
			bounce = bouncebase
			dashtimer.stop() #cancel
			boingDash = true
			dashing = false
			if (weight != baseweight): bouncedashing = 2 #so you can't crash out of a dash
			weight = baseweight
	else: walldashing = false
	if (yvelocity == -1): #not bouncing up
		#energy regen here
		if moving: rolling = true #if pressing movekeys
		else: #not pressing movekeys (rolling)
			if (friction > 0): rolling = true
			elif rolling:
				rolling = false
				friction = 0
				for i in range(dirsize):
					dir[0][i] = 0
					dir[1][i] = 0
		if is_on_wall() and rolling and not get_slide_collision(0).collider.is_in_group('obstacles'):
			_alterDirection(get_slide_collision(0).normal) #reverse direction if hit wall
			if dashing:
				dashtimer.stop()
				dashing = false
				speed = speedCap
	elif (yvelocity < -1): #falling (to bounce)
		if yvelocity < 0 and yvelocity > -1: yvelocity = -1
		if ((yvelocity * bounce) * -1 >= bouncethreshold) and yvelocity != -1: #change the number to a stat value (like bouncemod)
			if get_slide_collision(0).normal.y == 1 or boingCharge or bouncedashing == 2: #ground/on a shift and crashing/charging
				if bouncedashing != 2:
					boing = yvelocity * bounce
					bouncedashing = 0
					if bounce != bouncebase: bounceCombo = 0 #not full bounce
				else: #crashing
					boing = yvelocity * (bounce * (1 - (weight * .2)))
					bouncedashing = 1
				boing *= -1
				jumpwindow = 0
				basejumpwindow = round(boing * 1.2)
				if boingTimer.is_stopped():
					boingTimer.start(boing * .02)
				rolling = false
				bounce -= weight * .1
			elif shiftedDir != 0: yvelocity *= bounce * -1 #on a shift
		else: #don't bounce up
			yvelocity = -1
			bounce = bouncebase
			bounceCombo = 0
	elif shiftedDir > 1: yvelocity = -1 #(prevents slope jump cheese) if going up slope and yvelocity above -1

func _isAirborne(delta: float) -> void: #in air
	yvelocity -= (gravity * weight) * delta #gravity
	rolling = false
	_capSpeed(22,50)

func _isWall(delta: float) -> void: #on wall
	yvelocity -= (gravity * weight) * delta #gravity
	var isWall = (get_slide_count() > 0 and get_slide_collision(0).collider.is_in_group('walls')) == true
	if isWall or dashing:
		wallb = true
		var wallang = velocity.bounce(get_slide_collision(0).normal)
		wallang = Vector2(wallang.x,wallang.z)
		wallbx = wallang.x
		wallby = wallang.y
		_alterDirection(get_slide_collision(0).normal)
		if dashing and !walldashing:
			dashtimer.stop() #cancel
			dashing = false
			weight = baseweight
			speed = speedCap
			bouncedashing = 1
			walldashing = true
		if isWall:
			var timerSet = 0
			boing = speed * .7 * friction
			timerSet = boing * .07
			jumpwindow = 0
			basejumpwindow = round(boing * 4)
			if boingTimer.is_stopped(): boingTimer.start(timerSet)

func _isBoinging(delta: float) -> void: #boing physics
	if bottom.is_colliding() or (is_on_wall() and get_slide_collision(0).collider.is_in_group('walls')):
		if jumpwindow < basejumpwindow: jumpwindow += 60 * delta
		else: jumpwindow = basejumpwindow
		if !wallb and shiftedDir == 0:
			var jumpratio = jumpwindow / basejumpwindow
			var offset = (speed * bouncebase) / basejumpwindow #make it so you don't slow down as much
			if bottom.is_colliding() and bottom.get_collider().is_in_group('slides'):
				offset *= 2
				jumpratio *= (baseweight * .015)
			if offset > 1: offset = 1
			stickdir[0] *= (1 - jumpratio)
			stickdir[1] *= (1 - jumpratio)
			_applyFriction(delta)
			var spd = speed * friction * (bouncebase * offset)
			if boingDash:
				var dashSpd = (dashspeed * friction * (dashspeed / speedCap) * (bouncebase * offset))
				if dashSpd > spd: spd = dashSpd
				if boingCharge and (spd > 4) and (jumpwindow == 60 * delta): _drawMoveNote('slide')
			velocity = Vector3(direction_ground.x * spd, yvelocity, direction_ground.y * spd) #apply velocity
			move_and_slide(velocity, Vector3.UP, true) #move with velocity
		if (get_slide_count() > 0) and collisionScales[0] != collisionShape.scale.x:
			if !wallb: _squishNScale(delta,bottom.get_collision_normal())
			else: _squishNScale(delta,get_slide_collision(0).normal)
	else: #in air
		boingDash = false
		jumpwindow = 0
		_squishNScale((gravity * 0.017),null)
		squishSet = false
		boing = 0
		collisionShape.rotation_degrees.y = 0
		boingTimer.stop()

func _squishNScale(delta: float, squishNormal) -> void: #warp mesh around boinging
	var rate = delta * 60 * .1
	if squishNormal != null and squishSet == false:
		var squish = boing / 22
		if squish > .9: squish = .9
		squishReverb[0] = 0
		squishReverb[1] = 1
		squishGrow = true
		if is_on_floor() or shiftedDir != 0: #ground or shift
			collisionScales[0] = collisionBaseScale * (1 + (squish * .7)) #x
			collisionScales[1] = collisionBaseScale * (1 - (squish * .7)) #y
			collisionScales[2] = collisionScales[0] #z
		elif round(squishNormal.y) == 0: #wall
			collisionShape.rotation_degrees.y = 0
			squish *= 1.5 #increase squish effect by 50%
			var add = collisionBaseScale * (1 + (squish * .7))
			var sub = collisionBaseScale * (1 - (squish * .7))
			var camAng = camera.camsetarray
			collisionScales[1] = add #y
			var normx = round(squishNormal.x)
			var normz = round(squishNormal.z)
			var flip = false
			if normx == 0 or normz == 0: #45 degree offset wall
				collisionShape.rotation_degrees.y = 45
				flip = (sign(abs(normx)) == 1 and sign(abs(normz)) == 0)
			else: flip = (normx == 1 and normz == 1) or (normx == -1 and normz == -1)
			if flip:
				var temp = add
				add = sub
				sub = temp
			if (camAng == 1) or (camAng == 3):
				collisionScales[0] = sub #x
				collisionScales[2] = add #z
			elif (camAng == 0) or (camAng == 2):
				collisionScales[0] = add #x
				collisionScales[2] = sub #z
		squishSet = true
	elif squishNormal == null and squishReverb[0] != squishReverb[1]: #BbOoOiIinNg
		if squishReverb[0] > .75: squishReverb[0] = .75
		var mod = 1
		for i in range(3):
			if i == 0:
				if !squishReverb[2]:
					if (collisionShape.scale.x < collisionBaseScale): mod += squishReverb[0]
					elif (collisionShape.scale.x > collisionBaseScale): mod -= squishReverb[0]
				else: #if wallbounced, alter jiggle pattern
					squishReverb[2] = false
					mod += squishReverb[0]
			elif mod < 1: mod = 1 + squishReverb[0]
			else: mod = 1 - squishReverb[0]
			collisionScales[i] = collisionBaseScale * mod #reset
		squishReverb[1] = squishReverb[0]
		rate *= .9
	collisionShape.scale.x = lerp(collisionShape.scale.x,collisionScales[0],rate)
	collisionShape.scale.y = lerp(collisionShape.scale.y,collisionScales[1],rate)
	collisionShape.scale.z = lerp(collisionShape.scale.z,collisionScales[2],rate)
	if is_on_floor() and shiftedDir == 0:
		if mesh.translation.y > 0: mesh.translation.y = 0
		if collisionShape.scale.y < collisionBaseScale:
			var meshTarg = 0
			meshTarg -= ((collisionBaseScale*collisionBaseScale*10)*(collisionBaseScale-collisionShape.scale.y)/collisionBaseScale)
			meshTarg *= ((basejumpwindow*.5)/jumpforce) if ((basejumpwindow*.5)/jumpforce < 1) else 1
			if meshTarg < mesh.translation.y: mesh.translation.y = meshTarg
	elif mesh.translation.y != 0:
		if mesh.translation.y < -.5: mesh.translation.y = -.5
		elif mesh.translation.y < 0: mesh.translation.y += delta * abs(yvelocity)
		else: mesh.translation.y = 0
	if !is_on_floor() and !is_on_wall(): #airborne
		var check1 = collisionShape.scale.x>(collisionScales[0]*(1-(squishReverb[0])))
		if check1 and (collisionShape.scale.x<(collisionScales[0]*(1+(squishReverb[0])))):
			squishReverb[0] -= .02
			if squishReverb[0] < 0: squishReverb[0] = 0
			if squishReverb[0] == 0:
				collisionShape.scale.x = collisionScales[0]
				collisionShape.scale.y = collisionScales[1]
				collisionShape.scale.z = collisionScales[2]
	elif ((basejumpwindow != 0) and (jumpwindow/basejumpwindow >= 1)) or ((boing == 0) and (is_on_floor() or yvelocity == -1)):
		if boing == 0: #we are not boinging (just rollin)
			collisionShape.scale.x = collisionScales[0]
			collisionShape.scale.y = collisionScales[1]
			collisionShape.scale.z = collisionScales[2]
		else: #windowed
			collisionScales[0] = collisionShape.scale.x
			collisionScales[1] = collisionShape.scale.y
			collisionScales[2] = collisionShape.scale.z
	squishGrow = collisionShape.scale.x != collisionBaseScale

func _capSpeed(high,low) -> void: #speed bounce cap
	if yvelocity < -low: yvelocity = -low
	elif yvelocity > high: yvelocity = high

func _rotateMesh(xvel: float, yvel: float, delta: float) -> void:
	if !wallb: mesh.rotation.y = Vector2(dragdir[1],dragdir[0]).angle()
	var xv = abs(xvel)
	var yv = abs(yvel)
	var turn = (xv if (xv > yv) else yv)
	mesh.rotation.x += turn * 1.5 * delta #the float is a textured speed boost

func _alterDirection(alterNormal: Vector3) -> void: #change direction off of walls etc
	var wallbang = Vector3(dragdir[0],0,dragdir[1]).bounce(alterNormal)
	var camArray = camera.camsetarray
	if (camArray == 1) or (camArray == 3): wallbang.z *= -1
	elif (camArray == 0) or (camArray == 2): wallbang.x *= -1
	camArray = deg2rad(camera.camsets[camArray]) * -1
	if math.roundto(ang,100) != math.roundto(camArray, 100):
		angTarget = rotation.y * -1
		ang = camArray
	for i in range(dirsize):
		dir[0][i] = wallbang.z
		dir[1][i] = wallbang.x

func _jump() -> void: #jump logic
	boingCharge = true
	if boing != 0: #bounce jump
		yvelocity = boing
		boingDash = false
		_squishNScale((gravity * 0.017),null)
		squishSet = false
		boing = 0
		boingTimer.stop()
		collisionShape.rotation_degrees.y = 0
		if shiftedDir != 0: #bouncejump off of a slope
			var wallang = velocity.bounce(bottom.get_collision_normal())
			wallang = Vector2(wallang.x,wallang.z)
			_alterDirection(bottom.get_collision_normal())
			wallb = true
			wallbx = wallang.x * .5
			wallby = wallang.y * .5
		var lastyvel = yvelocity + (gravity * 0.017) #rough delta estimate
		var nuyvel = 0
		var combo = bounceCombo
		if combo > bounceComboCap: combo =  bounceComboCap
		if basejumpwindow < 1: basejumpwindow = 1
		var windowRatio = jumpwindow/basejumpwindow
		if jumpwindow > 0: #if it's a bounce jump, reward late bounce
			jumpwindow = ceil((jumpwindow + 1) * (bounce / (bouncebase)))
			if jumpwindow < 1: jumpwindow = 1
		var chargedNote = ''
		if windowRatio >= 1: chargedNote = 'charged '
		if bouncedashing != 1: #regular bouncejump
			jumpwindow = (jumpwindow/basejumpwindow * .75) + bouncebase
			nuyvel = math.roundto((jumpforce * (1 + combo * .035)) * jumpwindow,10)
			bounceCombo += 1
			if !wallb: _drawMoveNote(chargedNote + 'boingjump')
			else:
				_drawMoveNote(chargedNote + 'walljump')
				squishReverb[2] = true
		else: #crash / walldash bouncejump
			jumpwindow = (jumpwindow/basejumpwindow) + bouncebase
			bouncedashing = 0
			nuyvel = math.roundto((jumpforce * (1 + bounceComboCap * .1)) * jumpwindow,10) #crash bounce jump
			if wallb:
				_drawMoveNote(chargedNote + 'crash walljump')
				nuyvel *= (windowRatio * .65)
				lastyvel *= (windowRatio * .65)
				squishReverb[2] = true
			else: _drawMoveNote(chargedNote + 'crashjump')
		yvelocity = nuyvel
		if nuyvel > lastyvel: yvelocity = nuyvel
		else: yvelocity = lastyvel #never go below base bounce given your last yvel
		squishReverb[0] = yvelocity * .033
		_capSpeed(22,50)
		#print("velocity: " + str(yvelocity))
		jumpwindow = 0
		bounce = bouncebase #so you're still buoyant when landing
	elif (yvelocity == -1 or (is_on_floor() and shiftedDir == 0)) or (shiftedDir != 0 and bottom.is_colliding()):
		if preBoingTimer.is_stopped() and (shiftedDir == 0): preBoingTimer.start(.2)
		else: _normalJump() #on shift
	else: return
	canCrash = true
	if shiftedDir != 0: shiftedSticky = 0

func _normalJump() -> void:
	boingCharge = false
	_drawMoveNote('jump')
	yvelocity = jumpforce
	squishReverb[0] = yvelocity * .033
	preBoingTimer.stop()
	canCrash = true
	if shiftedDir != 0: shiftedSticky = 0

func _dash() -> void:
	if (moving or (dragdir[0] != 0 or dragdir[1] != 0)) and !dashing:
		if is_on_floor() and (yvelocity == -1) and (shiftedDir == 0): #on ground and not on shift
			yvelocity = jumpforce * .5 #half a jump thing
			_drawMoveNote('dash')
		elif canCrash: #in air and not on shift
			dashtimer.stop()
			weight = baseweight * 3
			shiftedDir = 0 #don't need to apply shifted gravity anymore if doing this
			_drawMoveNote('crash')
		elif (shiftedDir != 0): #is on shift
			dashtimer.start(.3) #start timer
			dashspeed = speedCap * 2
			_drawMoveNote('slope dash')
		else: return
		if moving: #dash changes direction
			var signy = sign(stickdir[1])
			for i in range(dirsize):
				dir[0][i] = stickdir[0] * friction
				dir[1][i] = stickdir[1] * friction
		dashing = true

func _input(event: InputEvent) -> void: #buttons
	if event.is_action_pressed("jump"): _jump()
	elif event.is_action_released("jump"):
		if boingCharge:
			if boing != 0 and (is_on_floor() or yvelocity == -1 or is_on_wall()):
				if !boingTimer.is_stopped(): boingTimer.stop()
				#if jumpwindow == basejumpwindow: print('boingcharge')
				_jump()
			elif !preBoingTimer.is_stopped() and (is_on_floor() or yvelocity == -1 or is_on_wall()): #normal jump
				_normalJump()
			boingCharge = false
	elif event.is_action_pressed("dash"): _dash()
	if event.is_action_pressed("game_restart"): _dieNRespawn()
	elif event.is_action_pressed("speedrun_reset"):
		self.set_translation(get_node("../checkpoints/checkpoint1").get_translation())
		speedrunNote.time = 0
		speedrunNote.timerOn = false
		if not speedRun:
			_drawTip('Speedrun mode activated!\nPress T to restart speedrun')
			tipNote.textTimer.start(2.5)
			speedRun = true
	elif event.is_action_pressed("add_traction"):
		if (traction < 100): traction += 10
		else: traction = 0
		print('traction ' + str(traction) + ' ' + str(tractionlist[traction]))
	elif event.is_action_pressed("sub_traction"):
		if (traction > 0): traction -= 10
		else: traction = 100
		print('traction ' + str(traction) + ' ' + str(tractionlist[traction]))
	elif event.is_action_pressed("slow-mo"):
		if !slowMo: Engine.time_scale = .3
		else: Engine.time_scale = 1
		slowMo = !slowMo
	elif event.is_action_pressed('debug_restart'): get_tree().reload_current_scene()
	elif event.is_action_pressed('end_game'): get_tree().quit()

func _on_DashTimer_timeout() -> void: #dash timer (on shifts)
	dashing = false
	walldashing = false
	dashspeed = speedCap * 1.5

func _on_boingTimer_timeout() -> void:
	if boingCharge: return
	_squishNScale((gravity * 0.017),null)
	squishSet = false
	if !wallb:
		yvelocity = boing
		squishReverb[0] = yvelocity * .033
	else:
		squishReverb[0] = boing * .12
		squishReverb[2] = true
	boingDash = false
	jumpwindow = 0
	boing = 0
	collisionShape.rotation_degrees.y = 0

func _on_preBoingTimer_timeout():
	boing = jumpforce
	jumpwindow = 0
	basejumpwindow = round(boing * 1.2)

func _dieNRespawn() -> void:
	idle = true
	yvelocity = 1
	stickdir[0] = 0
	stickdir[1] = 0
	weight = baseweight
	dashing = false
	dashtimer.stop()
	walldashing = false
	dashspeed = speedCap * 1.5
	wallb = false
	canCrash = false
	shiftedDir = 0
	shiftedLinger = false
	boingDash = false
	preBoingTimer.stop()
	_squishNScale((gravity * 0.017),null)
	squishSet = false
	boing = 0
	collisionShape.rotation_degrees.y = 0
	boingCharge = false
	boingTimer.stop()
	for i in range(dirsize):
		dir[0][i] = 0
		dir[1][i] = 0
	self.set_translation(checkpoint)

func _on_deathTimer_timeout():
	var areas = $hitBox.get_overlapping_areas()
	for index in areas:
		if index.is_in_group('killboxes'): return _dieNRespawn()

func _on_hitBox_area_entered(area):
	var groups = area.get_groups()
	for group in groups:
		match group:
			'checkpoints':
				checkpoint = area.get_translation()
				checkpoint.y
			'killboxes':
				if not area.name.begins_with('delay'): _dieNRespawn()
				elif deathtimer.is_stopped(): deathtimer.start(2)
			'warps':
				self.set_translation(get_node("../checkpoints/checkpoint1").get_translation())
				if speedRun:
					if prNote.text == "" or speedrunNote.time < speedrunNote.prtime:
						prNote.text = 'pr: ' + str(speedrunNote.text)
						speedrunNote.prtime = speedrunNote.time
				else:
					_drawTip('Speedrun mode activated!\nPress T to restart speedrun')
					tipNote.textTimer.start(2.5)
					speedRun = true
				speedrunNote.time = 0
				speedrunNote.timerOn = false
			'tips':
				var string = ''
				#{'roll': '', 'jump': '', 'dash': '', 'camera': '', 'restart': '', 'speedrun': ''}
				match area.name:
					'moveTip': string = str(controlNames['roll']) + ' to Roll'
					'jumpTip': string = str(controlNames['jump']) + ' to Jump'
					'bounceTip': string = str(controlNames['jump']) + ' after hitting ground\nto Boingjump'
					'camTip': string = str(controlNames['camera']) + '\nto rotate the camera'
					'restartTip':
						var rest = controlNames['restart']
						var speedr = controlNames['speedrun']
						string = str(rest) + ' to restart from checkpoint\n' + str(speedr) + ' to start speedrun mode'
					'boingTip': string = 'Hold ' + str(controlNames['jump']) + ' before landing\nto charge a Boingjump'
					'boingTip2': string = 'Get a bouncing start\nfor a big Boingjump!'
					'dashTip': string = str(controlNames['dash']) + ' to Dash'
					'slideTip': string = 'Hold ' + str(controlNames['jump']) + ' after dashing\nto Slide'
					'slideTip2': string = 'You can slide super far on glass!'
					'crashTip': string = 'Dash in mid-air\nto Crash'
					'crashTip2': string = 'Boingjump after crashing\nto Crashjump'
					'wallTip': string = 'Boingjump after hitting a wall\nto Walljump'
					'wallTip2': string = 'Chain a Crashjump into\na Walljump!'
					'shiftTip': string = 'Roll down slopes to go fast!'
					'shiftTip2': string = 'Jump off ramps at high speeds\nto get some air!'
					'part1Tip': string = 'Grats on making it this far. You got it!'
					'part3Tip': string = 'Take your time...'
					'part4Tip': string = 'Crash Walljump by Crashing or Dashing into\na wall followed by a Walljump'
					'endTip': string = 'Thats all for now. Good job!\nTravel down to restart in speedrun mode!'
				if string != '': _drawTip(string)
			'camerasets':
				if camera.setDelay.is_stopped():
					var tag = area.name.split("cameraset",true,1)
					camera._auto_move_camera(int(tag[1]))

func _on_hitBox_area_exited(area):
	var groups = area.get_groups()
	for group in groups:
		match group:
			'checkpoints': if area.name == 'checkpoint1' and speedRun:
				speedrunNote.timerOn = true
				speedrunNote.time = 0
			'killboxes': if area.name.begins_with('delay'): deathtimer.stop()
			'tips': tipNote.textTimer.start(2)

func _drawMoveNote(text) -> void:
	moveNote.text = text
	moveNote.alpha = 2.5
	moveNote.add_color_override("font_color", Color(1,1,1,1))

func _drawTip(text) -> void:
	if not tipNote.textTimer.is_stopped(): tipNote.textTimer.stop()
	tipNote.text = text
