using Godot;

public class Hopper : KinematicBody
{
int aggroRange = 30;
int vulnerableClass = 2; //0: none, 1: just crash, 2: killed by dash and crash, 3: just dash
enum states{
    search,
    attack,
    squished,
    launched
}
states state = states.search;
float[] squishSet = new float[] {0,0,0};
float meshY;
int damage = 5;
Area hitbox;
Vector3 spawnPoint;
Vector3 launchVec;
Vector3 velocity;
float yvelocity;
Spatial target;
Spatial parent;
MeshInstance mesh;
MeshInstance arrow;
Timer deathTimer;
Timer aggroTimer;
Timer shakeTimer;
RayCast bottom;
CollisionShape shakeBox;
bool ascending = false;
float landingCooldown = 0;

public override void _Ready(){
    hitbox = GetNode<Area>("Hitbox");
    target = GetNode<Spatial>("../../../playerNode/PlayerBall");
    mesh = GetNode<MeshInstance>("MeshInstance");
    parent = GetNode<Spatial>("../.");
    arrow = GetNode<MeshInstance>("Arrow");
    deathTimer = GetNode<Timer>("DeathTimer");
    aggroTimer = GetNode<Timer>("AggroTimer");
    shakeTimer = GetNode<Timer>("ShakeTimer");
    shakeBox = GetNode<CollisionShape>("Shakebox/CollisionShape");
    bottom = GetNode<RayCast>("RayCast");
    aggroTimer.Start(2.5F);
    meshY = mesh.Scale.y;
    squishSet[0] = mesh.Scale.x * 1.7F;
    squishSet[1] = mesh.Scale.y * .3F;
    squishSet[2] = mesh.Scale.z * 1.7F;
    spawnPoint = GlobalTransform.origin;
}

public override void _PhysicsProcess(float delta){
    if (state == states.search) return;
    if (state == states.attack){
        if (deathTimer.IsStopped()){
            if (!bottom.IsColliding()) deathTimer.Start(3);
            else deathTimer.Stop();
        }
        if (!ascending){
            if (!IsOnFloor()){
                yvelocity -= 30 * delta;
                MoveAndSlide(new Vector3(velocity.x * 10, yvelocity, velocity.z * 10), Vector3.Up);
            }
            else{
                if (landingCooldown > 30){
                    if (GlobalTransform.origin.DistanceTo(target.GlobalTransform.origin) > aggroRange){
                        state = states.search;
                        aggroTimer.Start(2.5F);
                    }
                    else{
                        velocity = new Vector3(target.GlobalTransform.origin - GlobalTransform.origin).Normalized();
                        ascending = true;
                        yvelocity = 0;
                    }
                    landingCooldown = 0;
                }
                else{
                    if (landingCooldown == 0){
                        shakeBox.Disabled = false;
                        shakeTimer.Start(.1F);
                    }   
                    landingCooldown += 60 * delta;
                }
            }
        }
        else{
            yvelocity += 30 * delta;
            MoveAndSlide(new Vector3(velocity.x * 10, yvelocity, velocity.z * 10), Vector3.Up);
            if (yvelocity > 15) ascending = false;
        }
    }
    else if (state == states.squished) mesh.Scale = new Vector3(Mathf.Lerp(mesh.Scale.x,squishSet[0],.2F),Mathf.Lerp(mesh.Scale.y, squishSet[1],.2F),Mathf.Lerp(mesh.Scale.x,squishSet[2],.2F));
    else if (state == states.launched){
        MoveAndSlide(new Vector3(launchVec.x, yvelocity, launchVec.z), Vector3.Up);
        yvelocity -= 27 * delta;
        if (IsOnFloor()) _on_DeathTimer_timeout();
        else if (IsOnWall()){
            Node collider = (Node)GetSlideCollision(0).Collider;
            if (collider.IsInGroup("walls")) launchVec = launchVec.Bounce(GetSlideCollision(0).Normal);
        }
    }
}

public void _launch(float power, Vector3 cVec){
    state = states.launched;
    if (power != 0) launchVec = new Vector3(cVec.x * power, 0, cVec.z * power);
    else launchVec = new Vector3(cVec.x, 0, cVec.z);
    yvelocity = power;
    deathTimer.Start(3);
    vulnerableClass = 0;
    if (target.Get("lockOn") == this) target.Call("_lockOn", true, 0);
}

public void _squish(float power){ //check power vs health and all that here?
    state = states.squished;
    vulnerableClass = 0;
    deathTimer.Start(1.5F);
    mesh.Translation = new Vector3(mesh.Translation.x, mesh.Translation.y - 1.1F, mesh.Translation.z);
    if (target.Get("lockOn") == this) target.Call("_lockOn", true, 0);
}

public void _on_Shakebox_area_entered(Area area){
    Godot.Collections.Array groups = area.GetGroups();
    for (int i = 0; i < groups.Count; i++){
        if (groups[i].ToString() == "players"){
            Camera cam = (Camera)area.Owner.Get("camera");
            Spatial player = (Spatial)area.Owner;
            cam.Call("_shakeMove", 10, 2, Translation.DistanceTo(player.GlobalTransform.origin));
            break;
        }
    }
}

public void _on_ShakeTimer_timeout(){
    shakeTimer.Stop();
    shakeBox.Disabled = true;
}

public void _on_AggroTimer_timeout(){
    if (GlobalTransform.origin.DistanceTo(target.GlobalTransform.origin) > aggroRange) aggroTimer.Start(2.5F);
    else{
        state = states.attack;
        ascending = false;
    }
}

public void _on_DeathTimer_timeout(){
    deathTimer.Stop();
    QueueFree();
    parent.Call("_spawnTimerSet", "hopper", spawnPoint);
}

}
