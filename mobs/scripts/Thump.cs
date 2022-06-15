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
bool fallMode = false;
int[] pushDir = new int[] {0,0};
float pushAng = 0;
Timer fallTimer;
Timer shakeTimer;
Position3D anchor;
Vector3 anchorOrigin;
Position3D bottomPosition;
Area crushBox;
CollisionShape shakeBox;

public override void _Ready(){
    fallTimer = GetNode<Timer>("FallTimer");
    shakeTimer = GetNode<Timer>("ShakeTimer");
    anchor = GetNode<Position3D>("Anchor");
    bottomPosition = GetNode<Position3D>("BottomPosition");
    crushBox = GetNode<Area>("Crushbox");
    shakeBox = GetNode<CollisionShape>("Shakebox/CollisionShape");
    fallTimer.Start(2);
    fallMode = !Name.BeginsWith("push");
    anchorOrigin = anchor.GlobalTransform.origin;
    originY = (fallMode) ? GlobalTransform.origin.y : GlobalTransform.origin.x;
    if (!fallMode){
        if (anchorOrigin.x != Translation.x) pushDir[0] = (anchorOrigin.x > Translation.x) ?  1 : -1;
        if (anchorOrigin.z != Translation.z) pushDir[1] = (anchorOrigin.z > Translation.z) ?  1 : -1;
    }
}

public override void _PhysicsProcess(float delta){
    if (state == states.ready || state == states.grounded) return;
    if (state == states.crush){
        if (fallMode){
            yvelocity = yvelocity + 5 * delta;
            Translation = new Vector3(Translation.x, Translation.y - yvelocity, Translation.z);
        }
        else{
            yvelocity = yvelocity + 2.5F * delta;
            Translation = new Vector3(Translation.x + (yvelocity * pushDir[0]), Translation.y, Translation.z + (yvelocity * pushDir[1]));
        }
        if (fallMode && Translation.y < anchorOrigin.y ||
        !fallMode && (pushDir[0] == 0 || Translation.x > anchorOrigin.x) && (pushDir[1] == 0 || Translation.z < anchorOrigin.z)){
            state = states.grounded;
            fallTimer.Start(2);
            if (fallMode){
                crushBox.Monitorable = true;
                Translation = new Vector3(Translation.x, anchorOrigin.y, Translation.z);
                shakeTimer.Start(.1F);
                shakeBox.Disabled = false;
            }
            else crushBox.Monitorable = false;
        }
    }
    else if (state == states.ascend){
        if (fallMode) Translation = new Vector3(Translation.x,Translation.y+(10*delta),Translation.z);
        else Translation = new Vector3(Translation.x-(5*delta*pushDir[0]),Translation.y,Translation.z-(5*delta*pushDir[1]));
        float originCheck = (fallMode) ? GlobalTransform.origin.y : GlobalTransform.origin.x;
        if (fallMode && originCheck >= originY || !fallMode && (pushDir[0] <= 0 && originCheck >= originY || pushDir[0] > 0 && originCheck <= originY)){
            state = states.ready;
            fallTimer.Start(2);
        }
    }
}

public void _on_Shakebox_area_entered(Area area){
    Godot.Collections.Array groups = area.GetGroups();
    for (int i = 0; i < groups.Count; i++){
        if (groups[i].ToString() == "players"){
            Camera cam = (Camera)area.Owner.Get("camera");
            Spatial player = (Spatial)area.Owner;
            cam.Call("_shakeMove", 10, damage, player.GlobalTransform.origin.DistanceTo(player.GlobalTransform.origin));
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
    if (fallMode) crushBox.Monitorable = false;
    if (state == states.grounded) state = states.ascend;
    else if (state == states.ready){
        yvelocity = 0;
        state = states.crush;
        if (!fallMode) crushBox.Monitorable = true;
    }
}

}