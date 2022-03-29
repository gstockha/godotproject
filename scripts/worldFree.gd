extends Spatial

onready var player = get_node("playerNode/PlayerBall")

func _on_enemyPathTimer_timeout():
	get_tree().call_group("mobs", "_getTargetPath", player.global_transform.origin)
