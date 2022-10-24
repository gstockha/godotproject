using Godot;

public class Mole : KinematicBody
{
int aggroRange = 40;
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
Timer burrowTimer;
Timer shootTimer;
Timer springTimer;
Timer deathTimer;
Area hitbox;
Vector3 spawnPoint;
Vector3 launchVec;
float yvelocity;
Spatial target;
Godot.Collections.Array players = new Godot.Collections.Array {};
Spatial parent;
PackedScene bullet;
MeshInstance mesh;
Position3D shooter;
MeshInstance arrow;
bool active = false;
bool lockable = true;
bool wallb = false;

public override void _Ready(){
    burrowTimer = GetNode<Timer>("BurrowTimer");
    shootTimer = GetNode<Timer>("ShootTimer");
    springTimer = GetNode<Timer>("SpringTimer");
    deathTimer = GetNode<Timer>("DeathTimer");
    hitbox = GetNode<Area>("Hitbox");
    //target = GetNode<Spatial>("../../../playerNode/PlayerBall");
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
    if (power != 0) launchVec = new Vector3(cVec.x * power * 2.3F, 0, cVec.z * power * 2.3F);
    else launchVec = new Vector3(cVec.x, 0, cVec.z);
    yvelocity = power * 1.5F;
    burrowTimer.Stop();
    deathTimer.Start(2);
    vulnerableClass = 0;
    lockable = false;
    foreach (Node player in players) player.Call("_lockOn", this, 0);
    parent.Call("_dropBP", GlobalTransform.origin, .4);
}

public void _squish(float power){ //check power vs health and all that here?
    state = states.squished;
    burrowTimer.Stop();
    deathTimer.Start(1.5F);
    vulnerableClass = 0;
    lockable = false;
    mesh.Translation = new Vector3(mesh.Translation.x, mesh.Translation.y - 1.1F, mesh.Translation.z);
    foreach (Node player in players) player.Call("_lockOn", this, 0);
    parent.Call("_dropBP", GlobalTransform.origin, .4);
}

public void _on(){
    if (!deathTimer.IsStopped() || active) return;
    burrowTimer.Start(1);
    state = states.search;
    SetPhysicsProcess(true);
    active = true;
}

public void _off(){
    if (!deathTimer.IsStopped() || !active) return;
    burrowTimer.Stop();
    shootTimer.Stop();
    springTimer.Stop();
    vulnerableClass = 2;
    mesh.Scale = new Vector3(mesh.Scale.x, meshY, mesh.Scale.z);
    SetPhysicsProcess(false);
    active = false;
}

public void _on_BurrowTimer_timeout(){
    float timerSet = 2.5F;
    if (state == states.burrowed){
        if (springTimer.IsStopped()) springTimer.Start(.1F);
        hitbox.Monitorable = true;
        vulnerableClass = 2;
        mesh.Scale = new Vector3(mesh.Scale.x, meshY, mesh.Scale.z);
        mesh.Translation = new Vector3(mesh.Translation.x, mesh.Translation.y + 1, mesh.Translation.z);
    }
    float dist = 0;
    float targ= -1;
    Vector3 location = GlobalTransform.origin;
    foreach (Spatial player in players){
        dist = location.DistanceTo(player.GlobalTransform.origin);
        if (targ == -1 || dist < targ){
            targ = dist;
            target = player;
        }
    }
    if (GlobalTransform.origin.DistanceTo(target.GlobalTransform.origin) > aggroRange) state = states.search;
    else if (state == states.search || state == states.burrowed){
        state = states.attack;
        Vector3 oldRot = Rotation;
        float nuRot;
        LookAt(target.GlobalTransform.origin, Vector3.Up);
        nuRot = Rotation.y;
        Rotation = new Vector3(oldRot.x, nuRot, oldRot.z);
        shootTimer.Start(1);
        timerSet += 1;
    }
    else if (state == states.attack){
        state = states.burrowed;
        hitbox.Monitorable = false;
        vulnerableClass = 0;
        mesh.Scale = new Vector3(mesh.Scale.x, 0.1F, mesh.Scale.z);
        mesh.Translation = new Vector3(mesh.Translation.x, mesh.Translation.y - 1, mesh.Translation.z);
        timerSet -= .5F;
    }
    burrowTimer.Start(timerSet);
}

public void _on_ShootTimer_timeout(){
    shootTimer.Stop();
    if (state != states.attack) return;
    Area blt = (Area)bullet.Instance();
    parent.AddChild(blt);
    blt.Set("trajectory", shooter.GlobalTransform.origin);
    blt.RotateY(Rotation.y);
}

public void _on_DeathTimer_timeout(){
    deathTimer.Stop();
    QueueFree();
    if (lockable) foreach (Node player in players) player.Call("_lockOn", this, 0);
    parent.Call("_spawnTimerSet", this, "mole", spawnPoint);
}

}
