using Godot;

public class Hopper : KinematicBody
{
int aggroRange = 27;
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
int damage = 17;
Area hitbox;
Vector3 spawnPoint;
Vector3 launchVec;
Vector3 velocity;
Vector3 velocityNormal;
float yvelocity;
Spatial target;
Spatial parent;
MeshInstance mesh;
MeshInstance arrow;
Timer deathTimer;
Timer aggroTimer;
RayCast bottom;
bool ascending = false;
float landingCooldown = 0;
float baseScale;
bool active = false;
bool lockable = true;
bool meshSquished = false;

public override void _Ready(){
    hitbox = GetNode<Area>("Hitbox");
    target = GetNode<Spatial>("../../../playerNode/PlayerBall");
    mesh = GetNode<MeshInstance>("MeshInstance");
    parent = GetNode<Spatial>("../.");
    arrow = GetNode<MeshInstance>("Arrow");
    deathTimer = GetNode<Timer>("DeathTimer");
    aggroTimer = GetNode<Timer>("AggroTimer");
    bottom = GetNode<RayCast>("RayCast");
    meshY = mesh.Scale.y;
    baseScale = mesh.Scale.x;
    squishSet[0] = mesh.Scale.x * 1.7F;
    squishSet[1] = mesh.Scale.y * .3F;
    squishSet[2] = mesh.Scale.z * 1.7F;
    spawnPoint = GlobalTransform.origin;
    SetPhysicsProcess(false);
}

public override void _PhysicsProcess(float delta){
    if (state == states.search) return;
    if (state == states.attack){
        if (!bottom.IsColliding()){
            if (deathTimer.IsStopped()) deathTimer.Start(3);
        }
        else if (!deathTimer.IsStopped()) deathTimer.Stop();
        if (!ascending){
            if (!IsOnFloor()){
                yvelocity -= 30 * delta;
                MoveAndSlide(new Vector3(velocityNormal.x * 10, yvelocity, velocityNormal.z * 10), Vector3.Up);
            }
            else{
                if (landingCooldown > 60){
                    if (GlobalTransform.origin.DistanceTo(target.GlobalTransform.origin) > aggroRange){
                        state = states.search;
                        aggroTimer.Start(2.5F);
                        mesh.Scale = new Vector3(baseScale,baseScale,baseScale);
                        if (meshSquished){
                            Translation = new Vector3(Translation.x, Translation.y + .3F, Translation.z);
                            meshSquished = false;
                        }
                    }
                    else{
                        velocityNormal = new Vector3(target.GlobalTransform.origin - GlobalTransform.origin).Normalized();
                        velocity = new Vector3(velocityNormal.x * 10, velocityNormal.y * 10, velocityNormal.z * 10);
                        ascending = true;
                        yvelocity = 5;
                        mesh.Scale = new Vector3(baseScale*.8F,baseScale*1.2F,baseScale*.8F);
                        if (meshSquished){
                            Translation = new Vector3(Translation.x, Translation.y + .3F, Translation.z);
                            meshSquished = false;
                        }
                    }
                    landingCooldown = 0;
                }
                else{
                    if (landingCooldown == 0){
                        mesh.Scale = new Vector3(baseScale*1.3F,baseScale*.7F,baseScale*1.3F);
                        if (!meshSquished){
                            Translation = new Vector3(Translation.x, Translation.y - .3F, Translation.z);
                            meshSquished = true;
                        }
                    }
                    landingCooldown += 60 * delta;
                }
            }
        }
        else{
            yvelocity += 30 * delta;
            MoveAndSlide(new Vector3(velocityNormal.x * 10, yvelocity, velocityNormal.z * 10), Vector3.Up);
            if (yvelocity > 15) ascending = false;
        }
    }
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
    if (power != 0) launchVec = new Vector3((cVec.x * power * 1.5F) + (Mathf.Sign(cVec.x) * 2), 0, (cVec.z * power * 1.5F) + (Mathf.Sign(cVec.z) * 2));
    else launchVec = new Vector3(cVec.x, 0, cVec.z);
    yvelocity = power * 2;
    deathTimer.Start(3);
    aggroTimer.Stop();
    vulnerableClass = 0;
    lockable = false;
    if (target.Get("lockOn") == this) target.Call("_lockOn", true, 0);
}

public void _squish(float power){ //check power vs health and all that here?
    state = states.squished;
    vulnerableClass = 0;
    lockable = false;
    deathTimer.Start(1.5F);
    aggroTimer.Stop();
    mesh.Translation = new Vector3(mesh.Translation.x, mesh.Translation.y - 1.1F, mesh.Translation.z);
    mesh.Scale = new Vector3(squishSet[0],squishSet[1],squishSet[2]);
    if (target.Get("lockOn") == this) target.Call("_lockOn", true, 0);
}

public void _on(){
    if ((!deathTimer.IsStopped() && state != states.attack) || active) return;
    aggroTimer.Start(1);
    state = states.search;
    SetPhysicsProcess(true);
    active = true;
}

public void _off(){
    if ((!deathTimer.IsStopped() && state != states.attack) || !active) return;
    aggroTimer.Stop();
    SetPhysicsProcess(false);
    active = false;
    ascending = false;
    yvelocity = 0;
    mesh.Scale = new Vector3(baseScale, baseScale, baseScale);
    if (meshSquished){
        Translation = new Vector3(Translation.x, Translation.y + .3F, Translation.z);
        meshSquished = false;
    }
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
    if (lockable && target.Get("lockOn") == this) target.Call("_lockOn", true, 0);
    parent.Call("_spawnTimerSet", GetNode<Spatial>("."), "hopper", spawnPoint);
}

}
