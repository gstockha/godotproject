extends Control
var allocateMode = false
onready var player = get_node("..")
var barMap = {0: "weight", 1: "traction", 2: "bounce", 3: "size", 4: "speed", 5: "energy"}
onready var barNodes = [$gravityBar, $tractionBar, $bounceBar, $girthBar, $speedBar, $energyBar]
onready var preSpendNodes = [$gravityBar/preSpendBar, $tractionBar/preSpendBar, $bounceBar/preSpendBar,
$girthBar/preSpendBar, $speedBar/preSpendBar, $energyBar/preSpendBar]
onready var targetBar = $targetBar
onready var spendNote = $bpSpendNote
onready var targetInputNote = $targetBar/inputNote
onready var alertBar = get_node('../bpSpendAlert')
onready var bp5Node = preload("res://items/5bp.tscn")
onready var bp1Node = preload("res://items/bp.tscn")
var target = 0
var init = true
var bpUnspent = 0
var bpSpent = 0
var bpPreset = 0
var bpTotal = 0
var presetList = []
var targetInput = 'D :  + 1    E :  + 5    Space :  fill'
var targetInputAlt = 'A :  clear preset'
var controls = {"allocate_stats": "", "ui_up": "", "ui_down": "", "ui_right": "", "pan_right": "", "ui_left": "", "jump": ""}
var buttonName = "Alt"
var spendRecord = []
var recordPointer = 0

func _ready():
	if globals.player_count > 1:
		rect_position.y = 930
		if (globals.player_count == 2 || (globals.player_count == 3 && player.playerId == 0)):
			rect_scale = Vector2(.8,.8)
			rect_position.x = 1830
		else:
			rect_scale = Vector2(.7,.7)
			rect_position.x = 1610
			alertBar.add_font_override("font", load("res://fonts/tipSmallOutline.tres"))
	for i in range(90):
		presetList.append(null)
		spendRecord.append(null)
	if Input.is_joy_known(0):
		targetInput = "-> :  + 1    R trigger :  + 5    "
		var name = Input.get_joy_name(0).to_lower()
		if name.begins_with("d"):
			buttonName = "Triangle"
			targetInput += "Cross :  fill"
		else:
			buttonName = "Y"
			targetInput += "A :  fill"
		targetInputAlt =  '<- :  clear preset'
	alertBar.text = "Press " + buttonName + " to spend points!"
	$exitNote.text = "Press " + buttonName + " to close"
	targetInputNote.text = targetInput
	#get controls
	var id = str(player.playerId)
	controls["allocate_stats"] = globals.allocate_stats + id
	controls["pan_right"] = globals.pan_right + id
	controls["jump"] = globals.jump + id
	if id == "0": id = ""
	controls["ui_up"] = globals.ui_up + id
	controls["ui_down"] = globals.ui_down + id
	controls["ui_left"] = globals.ui_left + id
	controls["ui_right"] = globals.ui_right + id

func _input(event: InputEvent) -> void:
	if event.is_action_pressed(controls["allocate_stats"]):
		allocateMode = !allocateMode
		visible = allocateMode
		if allocateMode:
			if init: #target bar fix
				targetBar.rect_position.x = barNodes[0].rect_position.x - 2.167
				targetBar.rect_position.y = barNodes[0].rect_position.y - 1.788
				init = false
			player.idle = 2
			bpUnspent = player.bpUnspent
			bpSpent = player.bpSpent
			targetBar.visible = bpSpent < 90# && bpUnspent > 0
			alertBar.visible = false
			_draw_InputNote(1)
		else: player.idle = 0
	if (allocateMode == false): return
	if event.is_action_pressed(controls["ui_up"]) || event.is_action_pressed(controls["ui_down"]):
		if event.is_action_pressed(controls["ui_up"]): target = target - 1 if (target > 0) else 5
		elif event.is_action_pressed(controls["ui_down"]): target = target + 1 if (target < 5) else 0
		targetBar.rect_position.x = barNodes[target].rect_position.x - 2.167
		targetBar.rect_position.y = barNodes[target].rect_position.y - 1.788
		_draw_InputNote(1)
	elif event.is_action_pressed(controls["ui_right"]) || event.is_action_pressed(controls["pan_right"]) || event.is_action_pressed(controls["jump"]):
		if bpSpent >= 90: return
		var points = 1
		if (event.is_action_pressed(controls["pan_right"])): points = 5
		elif (event.is_action_pressed(controls["jump"])): points = 30
		var oldbpSpent = bpSpent
		if bpUnspent > 0: player._setStat(points, barMap[target], true)
		elif bpPreset + bpSpent < 90: _add_Preset(points)
		else: return
		bpUnspent = player.bpUnspent
		bpSpent = player.bpSpent
		if bpSpent != oldbpSpent && bpSpent - oldbpSpent < points: _add_Preset(points - (bpSpent - oldbpSpent))
		targetBar.visible = bpSpent < 90# && bpUnspent > 0
		if (bpSpent < 90):
			if (bpUnspent > 0): spendNote.text = str(bpUnspent) + ' points to spend'
			else: spendNote.text = str(bpSpent) + ' / 90  set'
		else: spendNote.text = 'max points spent'
		_draw_InputNote(0)
	elif event.is_action_pressed(controls["ui_left"]):
		_clear_PresetList(target)

func _add_Preset(points) -> void:
	if barNodes[target].value >= 30: return
	while points > 0 && bpPreset + bpSpent < 90:
		if preSpendNodes[target].value >= 30: return
		presetList[bpPreset + bpSpent] = target
		bpPreset += 1
		var diff = preSpendNodes[target].value - barNodes[target].value
		if diff < 0: diff = 0
		preSpendNodes[target].value = barNodes[target].value + diff + 1
		points -= 1
	_draw_InputNote(0)

func _check_PresetList(bpsent, unspent) -> void:
	var targetIndex = presetList[bpsent]
	bpUnspent = unspent
	bpSpent = bpsent
	if targetIndex == null || bpPreset < 1 || bpSpent >= 90 || bpsent < 0 || bpsent > 89:
		bpPreset = 0
#		print('preallocation allocation failed')
	else:
		player._setStat(1, barMap[targetIndex], true)
#		print('set!')
		presetList[bpsent] = null
		bpPreset -= 1
		bpUnspent = player.bpUnspent
		bpSpent = player.bpSpent
	if (bpPreset < 1 || bpUnspent < 1):
		alertBar.visible = !allocateMode && bpUnspent > 0
		targetBar.visible = bpSpent < 90
		if (bpSpent < 90):
			if (bpUnspent > 0): spendNote.text = str(bpUnspent) + ' points to spend'
			else: spendNote.text = str(bpSpent) + ' / 90  set'
		else: spendNote.text = 'max points spent'
		if targetIndex != null && barNodes[targetIndex].value >= preSpendNodes[targetIndex].value:
			preSpendNodes[targetIndex].value = 0

func _clear_PresetList(target) -> void:
	if bpPreset < 1: return
	bpPreset = 0
	for i in range(6): preSpendNodes[i].value = barNodes[i].value
	if target == null: #clear all
		presetList = []
		for i in range(90): presetList.append(null)
	else: #clear selected
		var newList = []
		var val
		var pointer = 0
		for i in range(90):
			val = presetList[i]
			if val == target: continue
			if val == null: newList.append(null)
			else:
				newList.append(val)
				bpPreset += 1
				preSpendNodes[val].value += 1
			pointer += 1
		for i in range(pointer, 90): newList.append(null)
		presetList = newList
	targetInputNote.text = targetInput
	if (bpSpent < 90):
		if (bpUnspent > 0): spendNote.text = str(bpUnspent) + ' points to spend'
		else: spendNote.text = str(bpSpent) + ' / 90  set'
	else: spendNote.text = 'max points spent'

func _drop_BP() -> void:
	bpTotal = player.bp
	bpUnspent = player.bpUnspent
	bpSpent = player.bpSpent
	if bpTotal < 1: return
	var drop = 2 + round(bpTotal * .1)
	while drop > bpTotal: drop -= 1
	print("drop: " + str(drop))
	var subBP = drop
	if (drop <= 0 || player.deathPlace == Vector3.ZERO): return
	var bpParent = get_node("../../../../../../../bps")
	if (drop - 5 > 0):
		var bp5 = bp5Node.instance()
		bpParent.add_child(bp5)
		bp5.global_transform.origin = player.deathPlace
		drop -= 5
		bpTotal -= 5
	for i in range(drop):
		var bp1 = bp1Node.instance()
		bpParent.add_child(bp1)
		bp1.global_transform.origin = Vector3(player.deathPlace.x + (rand_range(-1,1) * 4),
		player.deathPlace.y, player.deathPlace.z + (rand_range(-1,1) * 4))
		bpTotal -= 1
	player.deathPlace = Vector3.ZERO
	while bpUnspent > 0 && subBP > 0:
		bpUnspent -= 1
		subBP -= 1
	var presetBuffer = []
	while bpSpent > 0 && subBP > 0:
		recordPointer -= 1
		var targ = spendRecord[recordPointer]
		for i in range(recordPointer, -1, -1):
			if spendRecord[i] != targ || subBP < 1 || bpSpent < 1: break
			if targ == spendRecord[i]:
				presetBuffer.push_front(spendRecord[i])
				spendRecord[i] = null
				subBP -= 1
				recordPointer -= 1
				bpSpent -= 1
		recordPointer += 1
	if player.bpSpent != bpSpent:
		_hardset_PlayerPoints(presetBuffer)
		player.bpSpent = bpSpent
	player.bpUnspent = bpUnspent
	player.bp = bpTotal
	if (bpUnspent > 0): spendNote.text = str(bpUnspent) + ' points to spend'
	else: spendNote.text = str(bpSpent) + ' / 90  set'

func _record_spend(points: int) -> void:
	var spentPoints = 0
	for i in range(recordPointer, 90):
		spendRecord[i] = target
		spentPoints += 1
		recordPointer += 1
		if spentPoints == points: break
	print(spendRecord)

func _hardset_PlayerPoints(presetBuffer: Array) -> void: #set it without using the player function
	player.traction = 0
	player.speedPoints = 0
	player.weightPoints = 0
	player.sizePoints = 0
	player.bouncePoints = 0
	player.energyPoints = 0
	player.bpUnspent = 0
	var allocate = []
	for i in range(6): allocate.append(0)
	for i in range(90):
		if spendRecord[i] == null: break
		allocate[spendRecord[i]] += 1
	for i in range(6):
		if allocate[i] < 1:
			barNodes[i].value = 0
			continue
		player.bpUnspent += allocate[i]
		player._setStat(allocate[i], barMap[i], false)
	var presetBufferLength = len(presetBuffer)
	if presetBufferLength > 0:
		var oldPresetList = []
		var presetPoint = -1
		var recordPoint = -1
		for i in range(6): preSpendNodes[i].value = barNodes[i].value
		for i in range(90):
			oldPresetList.append(presetList[i])
			if presetList[i] != null:
				presetList[i] = null
				if presetPoint == -1: presetPoint = i
			if recordPoint == -1 && spendRecord[i] == null: recordPoint = i
		var bufferPointer = 0
		for i in range(recordPoint, 90):
			presetList[i] = presetBuffer[bufferPointer]
			bufferPointer += 1
			bpPreset += 1
			preSpendNodes[presetList[i]].value += 1
			if bufferPointer == presetBufferLength: break
		for i in range(recordPoint + bufferPointer, 90):
			if oldPresetList[presetPoint] == null: break
			presetList[i] = oldPresetList[presetPoint]
			presetPoint += 1
			preSpendNodes[presetList[i]].value += 1

func _draw_InputNote(mode: int) -> void:
	if mode == 0:
		if preSpendNodes[target].value >= 30: targetInputNote.text = targetInputAlt
		else: targetInputNote.text = targetInputAlt + '    ' + targetInput
	else:
		if preSpendNodes[target].value < 1: targetInputNote.text = targetInput
		elif preSpendNodes[target].value >= 30: targetInputNote.text = targetInputAlt
		else: targetInputNote.text = targetInputAlt + '    ' + targetInput
