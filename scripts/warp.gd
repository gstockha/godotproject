extends Area

onready var target = $warpTarget

func _on_warp_body_entered(body):
	if body.name.begins_with("PlayerBall"):
		body.global_transform.origin = target.global_transform.origin
