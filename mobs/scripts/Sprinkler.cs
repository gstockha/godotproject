using Godot;

public class Sprinkler : KinematicBody
{
int aggroRange = 50;
int vulnerableClass = 2; //0: none, 1: just crash, 2: killed by dash and crash, 3: just dash
enum states{
    search,
    attack,
    burrowed,
    squished,
    launched
}
states state = states.search;
float[] squishSet = new float[] {0,0,0};
float meshY;
Timer aggroTimer;
Timer shootTimer;
Timer springTimer;
Timer deathTimer;
Area hitbox;
Vector3 spawnPoint;
Vector3 launchVec;
float yvelocity;
bool wallb = false;
Spatial target;
Spatial parent;
PackedScene bullet;
MeshInstance mesh;
Position3D shooter;
MeshInstance arrow;
bool active = false;
bool lockable = true;
float startAngle = 0;
int oscillation = 0;
[Export] int oscillationRate = 4;
bool oscillationMode = true;


public override void _Ready(){
    shootTimer = GetNode<Timer>("ShootTimer");
    springTimer = GetNode<Timer>("SpringTimer");
    deathTimer = GetNode<Timer>("DeathTimer");
    aggroTimer = GetNode<Timer>("aggroTimer");
    hitbox = GetNode<Area>("Hitbox");
    target = GetNode<Spatial>("../../../playerNode/PlayerBall");
    mesh = GetNode<MeshInstance>("MeshInstance");
    bullet = (PackedScene)GD.Load("res://mobs/scenes/Bullet.tscn");
    parent = GetNode<Spatial>("../.");
    shooter = GetNode<Position3D>("Shooter");
    arrow = GetNode<MeshInstance>("Arrow");
    meshY = mesh.Scale.y;
    squishSet[0] = mesh.Scale.x * 1.7F;
    squishSet[1] = mesh.Scale.y * .3F;
    squishSet[2] = mesh.Scale.z * 1.7F;
    spawnPoint = GlobalTransform.origin;
    SetPhysicsProcess(false);
    startAngle = Rotation.y;// + 3.1416F;
    oscillation -= oscillationRate;
}

public override void _PhysicsProcess(float delta){
    if (state == states.search) return;
    if (state == states.squished) mesh.Scale = new Vector3(Mathf.Lerp(mesh.Scale.x,squishSet[0],.2F),Mathf.Lerp(mesh.Scale.y, squishSet[1],.2F),Mathf.Lerp(mesh.Scale.x,squishSet[2],.2F));
    else if (state == states.launched){
        MoveAndSlide(new Vector3(launchVec.x, yvelocity, launchVec.z), Vector3.Up);
        yvelocity -= 27 * delta;
        if (IsOnFloor()) _on_DeathTimer_timeout();
        else if (!wallb && IsOnWall()){
            Node collider = (Node)GetSlideCollision(0).Collider;
            if (collider.IsInGroup("walls")){
                launchVec = launchVec.Bounce(GetSlideCollision(0).Normal);
                wallb = true;
            }
        }
    }
}

public void _launch(float power, Vector3 cVec){
    state = states.launched;
    if (power != 0) launchVec = new Vector3(cVec.x * power, 0, cVec.z * power);
    else launchVec = new Vector3(cVec.x, 0, cVec.z);
    yvelocity = power;
    aggroTimer.Stop();
    deathTimer.Start(2);
    vulnerableClass = 0;
    lockable = false;
    if (target.Get("lockOn") == this) target.Call("_lockOn", true, 0);
    parent.Call("_dropBP", GlobalTransform.origin, .4);
}

public void _squish(float power){ //check power vs health and all that here?
    state = states.squished;
    aggroTimer.Stop();
    deathTimer.Start(1.5F);
    vulnerableClass = 0;
    lockable = false;
    mesh.Translation = new Vector3(mesh.Translation.x, mesh.Translation.y - 1.1F, mesh.Translation.z);
    if (target.Get("lockOn") == this) target.Call("_lockOn", true, 0);
    parent.Call("_dropBP", GlobalTransform.origin, .4);
}

public void _on(){
    if (!deathTimer.IsStopped() || active) return;
    state = states.search;
    SetPhysicsProcess(true);
    active = true;
    aggroTimer.Start(.1F);
}

public void _off(){
    if (!deathTimer.IsStopped() || !active) return;
    shootTimer.Stop();
    aggroTimer.Stop();
    SetPhysicsProcess(false);
    active = false;
    oscillation = 0;
}

public void _on_aggroTimer_timeout(){
    if (GlobalTransform.origin.DistanceTo(target.GlobalTransform.origin) > aggroRange) state = states.search;
    else if (state == states.search){
        state = states.attack;
        shootTimer.Start(.1F);
    }
    aggroTimer.Start(3);
}

public void _on_ShootTimer_timeout(){
    shootTimer.Stop();
    if (state != states.attack){
        oscillation = 0;
        return;
    }
    Area blt = (Area)bullet.Instance();
    if (oscillationMode){
        oscillation += oscillationRate;
        if (oscillation >= oscillationRate * 3) oscillationMode = false;
    }
    else{
        oscillation -= oscillationRate;
        if (oscillation <= -1 * (oscillationRate * 3)) oscillationMode = true;
    }
    float oscAng = (oscillation != 0) ? oscillation * .1F : 0;
    Rotation = new Vector3(Rotation.x, startAngle + oscAng, Rotation.z);
    parent.AddChild(blt);
    blt.Set("trajectory", shooter.GlobalTransform.origin);
    blt.RotateY(Rotation.y);
    shootTimer.Start(.5F);
}

public void _on_DeathTimer_timeout(){
    deathTimer.Stop();
    QueueFree();
    if (lockable && target.Get("lockOn") == this) target.Call("_lockOn", true, 0);
    parent.Call("_spawnTimerSet", GetNode<Spatial>("."), "sprinkler", spawnPoint, new float[] {startAngle, oscillationRate});
}

}
