extends Control
var allocateMode = false
onready var player = get_node("..")
var barMap = {0: "weight", 1: "traction", 2: "bounce", 3: "size", 4: "speed", 5: "energy"}
onready var barNodes = [$gravityBar, $tractionBar, $bounceBar, $girthBar, $speedBar, $energyBar]
onready var targetBar = $targetBar
onready var spendNote = $bpSpendNote
onready var alertBar = get_node('../bpSpendAlert')
var target = 0
var bpUnspent = 0
var bpSpent = 0

func _ready():
	var buttonName = "Alt"
	if Input.is_joy_known(0):
		$targetBar/inputNote.text = "R direction :  + 1    R trigger :  + 5"
		var name = Input.get_joy_name(0).to_lower()
		buttonName = "triangle" if name.begins_with("d") else "Y"
	alertBar.text = "Press " + buttonName + " to spend points!";
	$exitNote.text = "Press  " + buttonName + " to close"

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("allocate_stats"):
		allocateMode = !allocateMode
		player.idle = allocateMode
		visible = allocateMode
		if allocateMode:
			bpUnspent = player.bpUnspent
			bpSpent = player.bpSpent
			targetBar.visible = bpSpent < 90 && bpUnspent > 0
			spendNote.visible = bpUnspent > 0 || bpSpent >= 90
			alertBar.visible = false
	if (allocateMode == false): return
	if event.is_action_pressed("ui_up"):
		target = target - 1 if (target > 0) else 4
		targetBar.rect_position.x = barNodes[target].rect_position.x - 3.418
		targetBar.rect_position.y = barNodes[target].rect_position.y - 2.92
	elif event.is_action_pressed("ui_down"):
		target = target + 1 if (target < 4) else 0
		targetBar.rect_position.x = barNodes[target].rect_position.x - 3.418
		targetBar.rect_position.y = barNodes[target].rect_position.y - 2.92
	elif event.is_action_pressed("ui_right") || event.is_action_pressed("pan_right"):
		if bpSpent >= 90: return
		var points = 1 if (event.is_action_pressed("move_right")) else 5
		if (bpUnspent > 0): player._setStat(points, barMap[target])
		else: player._setStat(0, barMap[target])
		bpUnspent = player.bpUnspent
		bpSpent = player.bpSpent
		targetBar.visible = bpSpent < 90 && bpUnspent > 0
		spendNote.visible = bpUnspent > 0 || bpSpent >= 90
		if (bpSpent < 90): spendNote.text = str(bpUnspent) + ' points to spend'
		else: spendNote.text = 'max points spent'

func _process(delta) -> void:
	if allocateMode: player.idle = true
