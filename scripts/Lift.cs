using Godot;

public class Lift : StaticBody
{
float yvelocity = 0;
int damage = 0;
enum states{
    ready,
    crush,
    grounded,
    ascend
}
states state = states.ready;
float originY;
Timer fallTimer;
RayCast floorCast;
public override void _Ready(){
    fallTimer = GetNode<Timer>("FallTimer");
    floorCast = GetNode<RayCast>("FallCast");
    originY = GlobalTransform.origin.y;
    fallTimer.Start(2);
}

public override void _PhysicsProcess(float delta){
    if (state == states.ready || state == states.grounded) return;
    if (state == states.crush){
        yvelocity = 5 * delta;
        Translation = new Vector3(Translation.x, Translation.y - yvelocity, Translation.z);
        if (floorCast.IsColliding()){
            state = states.grounded;
            fallTimer.Start(3);
        }
    }
    else if (state == states.ascend){
        float fallRate = 5 * delta;
        Translation = new Vector3(Translation.x, Translation.y + fallRate, Translation.z);
        if (GlobalTransform.origin.y >= originY){
            state = states.ready;
            fallTimer.Start(3);
        }
    }
}
public void _on_FallTimer_timeout(){
    fallTimer.Stop();
    if (state == states.crush) return;
    yvelocity = 0;
    if (state == states.grounded) state = states.ascend;
    else if (state == states.ready){
        yvelocity = 0;
        state = states.crush;
    }
}

}
