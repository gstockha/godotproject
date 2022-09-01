extends OptionButton


# Declare member variables here. Examples:
# var a = 2
# var b = "text"


# Called when the node enters the scene tree for the first time.
func _ready():
	_addItems()


func _addItems():
	for i in range(4): add_item(str(i+1))
	text = "1 Player"

func _on_players_item_selected(index):
	var pCount = index + 1
	globals.player_count = pCount
	text = " Players" if (pCount > 1) else " Player"
	text = str(pCount) + text
