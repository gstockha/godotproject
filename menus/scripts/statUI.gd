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
var target = 0
var bpUnspent = 0
var bpSpent = 0
var buttonName = "Alt"
var bpPreset = 0
var presetList = []
var targetInput = 'D :  + 1    E :  + 5    Space :  fill'
var targetInputAlt = targetInput + '    Q :  clear preset'

func _ready():
	for i in range(90): presetList.append(null)
	if Input.is_joy_known(0):
		targetInput = "-> :  + 1    R trigger :  + 5    "
		var name = Input.get_joy_name(0).to_lower()
		if name.begins_with("d"):
			buttonName = "Triangle"
			targetInput += "Cross :  fill"
		else:
			buttonName = "Y"
			targetInput += "A :  fill"
	targetInputAlt = targetInput + '    L trigger :  clear preset'
	alertBar.text = "Press " + buttonName + " to spend points!";
	$exitNote.text = "Press " + buttonName + " to close"
	targetInputNote.text = targetInput

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("allocate_stats"):
		allocateMode = !allocateMode
		visible = allocateMode
		if allocateMode:
			player.idle = 2
			bpUnspent = player.bpUnspent
			bpSpent = player.bpSpent
			targetBar.visible = bpSpent < 90# && bpUnspent > 0
			alertBar.visible = false
			targetInputNote.text = targetInput if preSpendNodes[target].value < 1 else targetInputAlt
		else: player.idle = 0
	if (allocateMode == false): return
	if event.is_action_pressed("ui_up") || event.is_action_pressed("ui_down"):
		if event.is_action_pressed("ui_up"): target = target - 1 if (target > 0) else 4
		elif event.is_action_pressed("ui_down"): target = target + 1 if (target < 4) else 0
		targetBar.rect_position.x = barNodes[target].rect_position.x - 1.418
		targetBar.rect_position.y = barNodes[target].rect_position.y - 1.92
		targetInputNote.text = targetInput if preSpendNodes[target].value < 1 else targetInputAlt
	elif event.is_action_pressed("ui_right") || event.is_action_pressed("pan_right") || event.is_action_pressed("jump"):
		if bpSpent >= 90: return
		var points = 1
		if (event.is_action_pressed("pan_right")): points = 5
		elif (event.is_action_pressed("jump")): points = 30
		var oldbpSpent = bpSpent
		if bpUnspent > 0: player._setStat(points, barMap[target])
		elif bpPreset + bpSpent < 90: _add_Preset(points)
		else: return
		bpUnspent = player.bpUnspent
		bpSpent = player.bpSpent
		if bpSpent != oldbpSpent && bpSpent - oldbpSpent < points:
			_add_Preset(points - (bpSpent - oldbpSpent))
		targetBar.visible = bpSpent < 90# && bpUnspent > 0
		if (bpSpent < 90):
			if (bpUnspent > 0): spendNote.text = str(bpUnspent) + ' points to spend'
			else: spendNote.text = str(bpPreset + bpSpent) + ' / 90  set'
		else: spendNote.text = 'max points spent'
	elif event.is_action_pressed("pan_left"): _clear_PresetList(target)

func _add_Preset(points) -> void:
	if barNodes[target].value >= 30: return
	while points > 0:
		if preSpendNodes[target].value >= 30: return
		bpPreset += 1
		presetList[bpPreset + bpSpent - 1] = target
		var diff = preSpendNodes[target].value - barNodes[target].value
		if diff < 0: diff = 0
		preSpendNodes[target].value = barNodes[target].value + diff + 1
		points -= 1
	targetInputNote.text = targetInputAlt

func _check_PresetList(bpsent, unspent) -> void:
	var targetIndex = presetList[bpsent]
	bpUnspent = unspent
	bpSpent = bpsent
	if targetIndex == null || bpPreset < 1 || bpSpent >= 90 || bpsent < 0 || bpsent > 89:
		bpPreset = 0
#		print('failed')
	else:
		player._setStat(1, barMap[targetIndex])
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
			else: spendNote.text = str(bpPreset + bpSpent) + ' / 90  set'
		else: spendNote.text = 'max points spent'
		if targetIndex != null && barNodes[targetIndex].value >= preSpendNodes[targetIndex].value:
			preSpendNodes[targetIndex].value = 0

func _clear_PresetList(target) -> void:
	if bpPreset < 1: return
	bpPreset = 0
	if target == null: #clear all
		presetList = []
		for i in range(90): presetList.append(null)
		for bar in preSpendNodes: bar.value = 0
	else: #clear selected
		var newList = []
		var val
		for i in range(90):
			val = presetList[i]
			if val == null || val == target: newList.append(null)
			else:
				newList.append(val)
				bpPreset += 1
		presetList = newList
		preSpendNodes[target].value = 0
	targetInputNote.text = targetInput
	if (bpSpent < 90):
		if (bpUnspent > 0): spendNote.text = str(bpUnspent) + ' points to spend'
		else: spendNote.text = str(bpPreset + bpSpent) + ' / 90  set'
	else: spendNote.text = 'max points spent'

