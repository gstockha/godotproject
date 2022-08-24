extends Label

var time = 0
var prtime = 0
var timerOn = false
var math = preload("res://scripts/math.gd")

func _ready():
	if globals.player_count > 1:
		 add_font_override("font", load("res://fonts/tipSmall.tres"))

func _process(delta):
	if timerOn:
		time += delta * 60
		var secs = floor(time/100)
		var extended = (math.roundto(time,2)) - (secs * 100)
		text = str(secs) + '.' + str(extended) + ' seconds'
		
