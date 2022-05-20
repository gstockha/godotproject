using Godot;

public class Thump : StaticBody
{
float yvelocity = 0;
bool slow = false;
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
Timer shakeTimer;
MeshInstance mesh;
RayCast floorCast;
Position3D bottomPosition;
Area crushBox;
CollisionShape shakeBox;

public override void _Ready(){
    fallTimer = GetNode<Timer>("FallTimer");
    shakeTimer = GetNode<Timer>("ShakeTimer");
    mesh = GetNode<MeshInstance>("MeshInstance");
    floorCast = GetNode<RayCast>("FallCast");
    bottomPosition = GetNode<Position3D>("BottomPosition");
    crushBox = GetNode<Area>("Crushbox");
    shakeBox = GetNode<CollisionShape>("Shakebox/CollisionShape");
    originY = GlobalTransform.origin.y;
    fallTimer.Start(2);
    slow = Name.BeginsWith("slow");
    if (slow) floorCast.Scale = new Vector3(floorCast.Scale.x, Mathf.Floor(Scale.y * 2), floorCast.Scale.z);
}

public override void _PhysicsProcess(float delta){
    if (state == states.ready || state == states.grounded) return;
    if (state == states.crush){
        yvelocity = (!slow) ? yvelocity + 5 * delta : 5 * delta;
        Translation = new Vector3(Translation.x, Translation.y - yvelocity, Translation.z);
        if ((!slow && floorCast.GetCollisionPoint().y > Translation.y - 1.5F) || (slow && floorCast.IsColliding())){
            state = states.grounded;
            crushBox.Monitorable = true;
            if (!slow){
                Translation = new Vector3(Translation.x, floorCast.GetCollisionPoint().y + mesh.Scale.y, Translation.z);
                fallTimer.Start(2);
                shakeTimer.Start(.1F);
                shakeBox.Disabled = false;
            }
            else fallTimer.Start(3);
        }
    }
    else if (state == states.ascend){
        float fallRate = (slow == false) ? 10 * delta : 5 * delta;
        Translation = new Vector3(Translation.x, Translation.y + fallRate, Translation.z);
        if (GlobalTransform.origin.y >= originY){
            state = states.ready;
            if (!slow) fallTimer.Start(2);
            else fallTimer.Start(3);
        }
    }
}

public void _on_Shakebox_area_entered(Area area){
    Godot.Collections.Array groups = area.GetGroups();
    for (int i = 0; i < groups.Count; i++){
        if (groups[i].ToString() == "players"){
            Camera cam = (Camera)area.Owner.Get("camera");
            Spatial player = (Spatial)area.Owner;
            cam.Call("_shakeMove", 10, damage, Translation.DistanceTo(player.Translation));
            break;
        }
    }
}

public void _on_ShakeTimer_timeout(){
    shakeTimer.Stop();
    shakeBox.Disabled = true;
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
