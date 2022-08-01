extends VSlider

var music_bus = AudioServer.get_bus_index("Music")

# Called when the node enters the scene tree for the first time.
func _ready():
	value = AudioServer.get_bus_volume_db(music_bus)

func _on_music_slider_value_changed(value):
	AudioServer.set_bus_volume_db(music_bus, value)
