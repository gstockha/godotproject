using Godot;

public class Thump : StaticBody
{
float yvelocity = 0;
int damage = 3;
enum states{
    ready,
    crush,
    grounded,
    ascend
}
states state = states.ready;
float originY;
Timer fallTimer;
MeshInstance mesh;
RayCast floorCast;
Area crushBox;

public override void _Ready(){
    fallTimer = GetNode<Timer>("FallTimer");
    mesh = GetNode<MeshInstance>("MeshInstance");
    floorCast = GetNode<RayCast>("FallCast");
    crushBox = GetNode<Area>("Crushbox");
    originY = GlobalTransform.origin.y;
    fallTimer.Start(2);
}

public override void _PhysicsProcess(float delta){
    if (state == states.ready || state == states.grounded) return;
    Vector3 floorCastPoint = floorCast.GetCollisionPoint();
    if (state == states.crush){
        yvelocity += 5 * delta;
        Translation = new Vector3(Translation.x, Translation.y - yvelocity, Translation.z);
        if (floorCastPoint.y > Translation.y - 1.5F){
            state = states.grounded;
            crushBox.Monitorable = true;
            fallTimer.Start(2);
            Translation = new Vector3(Translation.x, floorCastPoint.y + mesh.Scale.y, Translation.z);
        }
    }
    else if (state == states.ascend){
        Translation = new Vector3(Translation.x, Translation.y + (10 * delta), Translation.z);
        if (GlobalTransform.origin.y >= originY){
            state = states.ready;
            fallTimer.Start(2);
        }
    }
}

public void _on_FallTimer_timeout(){
    fallTimer.Stop();
    if (state == states.crush) return;
    yvelocity = 0;
    crushBox.Monitorable = false;
    if (state == states.grounded) state = states.ascend;
    else if (state == states.ready){
        yvelocity = 0;
        state = states.crush;
    }
}

}
