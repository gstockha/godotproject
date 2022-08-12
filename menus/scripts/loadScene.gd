extends Node

export var maxLoadTime = 10000

func _ready():
	goto_scene("res://levels/" + globals.currentScene + ".tscn")

func goto_scene(path):
	var loader = ResourceLoader.load_interactive(path)
	if loader == null:
		print("Resource loader unable to find the resource path!")
		return
	
	var loadBar = load("res://menus/loadBar.tscn").instance()
	get_tree().get_root().call_deferred('add_child', loadBar)
	var t = OS.get_ticks_msec()
	
	while true:
		var err = loader.poll()
		if err == ERR_FILE_EOF: #road comprete
			var resource = loader.get_resource()
			get_tree().change_scene_to(resource)
			print('Loaded ' + path + ' in ' + str((OS.get_ticks_msec() - t) * .001) + ' seconds!')
			loadBar.queue_free()
			queue_free()
			break
		elif err == OK: #stirr roading
			var progress = float(loader.get_stage())/loader.get_stage_count()
			loadBar.value = progress * 100
		else:
			print("Error while loading file!")
			break
		yield(get_tree(), "idle_frame")
		
