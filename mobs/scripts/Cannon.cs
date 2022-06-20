using Godot;

public class Cannon : KinematicBody
{
int vulnerableClass = 2; //0: none, 1: just crash, 2: killed by dash and crash, 3: just dash
enum states{
    attack,
    squished,
    launched
}
states state = states.attack;
float[] squishSet = new float[] {0,0,0};
float startAngle;
Timer shootTimer;
Timer springTimer;
Timer deathTimer;
Area hitbox;
Vector3 spawnPoint;
Vector3 launchVec;
float yvelocity;
float fireRate = 3.5F;
Spatial target;
Spatial parent;
PackedScene bullet;
MeshInstance mesh;
Position3D shooter;
MeshInstance arrow;
bool active = false;

public override void _Ready(){
    shootTimer = GetNode<Timer>("ShootTimer");
    springTimer = GetNode<Timer>("SpringTimer");
    deathTimer = GetNode<Timer>("DeathTimer");
    hitbox = GetNode<Area>("Hitbox");
    target = GetNode<Spatial>("../../../playerNode/PlayerBall");
    mesh = GetNode<MeshInstance>("MeshInstance");
    bullet = (PackedScene)GD.Load("res://mobs/scenes/CannonBall.tscn");
    parent = GetNode<Spatial>("../.");
    shooter = GetNode<Position3D>("Shooter");
    arrow = GetNode<MeshInstance>("Arrow");
    squishSet[0] = mesh.Scale.x * 1.7F;
    squishSet[1] = mesh.Scale.y * .3F;
    squishSet[2] = mesh.Scale.z * 1.7F;
    spawnPoint = GlobalTransform.origin;
    startAngle = Mathf.Rad2Deg(Rotation.y) + 180;
    SetPhysicsProcess(false);
}

public override void _PhysicsProcess(float delta){
    // if (state == states.search) return;
    if (state == states.squished) mesh.Scale = new Vector3(Mathf.Lerp(mesh.Scale.x,squishSet[0],.2F),Mathf.Lerp(mesh.Scale.y, squishSet[1],.2F),Mathf.Lerp(mesh.Scale.x,squishSet[2],.2F));
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
    deathTimer.Start(2);
    vulnerableClass = 0;
    if (target.Get("lockOn") == this) target.Call("_lockOn", true, 0);
}

public void _squish(float power){ //check power vs health and all that here?
    state = states.squished;
    deathTimer.Start(1.5F);
    vulnerableClass = 0;
    mesh.Translation = new Vector3(mesh.Translation.x, mesh.Translation.y - 1.1F, mesh.Translation.z);
    if (target.Get("lockOn") == this) target.Call("_lockOn", true, 0);
}

public void _on_ShootTimer_timeout(){
    if (state != states.attack) return;
    float randAngle = (GD.Randf() * 90) - 45;
    Rotation = new Vector3(Rotation.x, Mathf.Deg2Rad((startAngle - 180) + randAngle), Rotation.z);
    Area blt = (Area)bullet.Instance();
    parent.AddChild(blt);
    blt.Set("trajectory", shooter.GlobalTransform.origin);
    blt.RotateY(Rotation.y);
    shootTimer.Start(fireRate);
}

public void _on(){
    if (!deathTimer.IsStopped() || active) return;
    state = states.attack;
    shootTimer.Start(1);
    SetPhysicsProcess(true);
    active = true;
}

public void _off(){
    if (!deathTimer.IsStopped() || !active) return;
    shootTimer.Stop();
    SetPhysicsProcess(false);
    active = false;
}

public void _on_DeathTimer_timeout(){
    deathTimer.Stop();
    QueueFree();
    parent.Call("_spawnTimerSet", GetNode<Spatial>("."), "cannon", spawnPoint);
}

}
