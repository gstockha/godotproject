using Godot;

public class Spinner : KinematicBody{
Vector3 velocity = Vector3.Zero;
Vector3 launchVec = Vector3.Zero;
float yvelocity = 0;
int damage = 23;
float speed = 9.7F;
float ang = 0;
[Export] float angMod = 1;
[Export] float dirMod = 1;

int vulnerableClass = 1; //0: none, 1: just crash, 2: killed by dash and crash, 3: just dash
enum states{
    spin,
    squished,
    launched
}
states state = states.spin;
float[] squishSet = new float[] {0,0,0};
Timer deathTimer;
Spatial target;
Godot.Collections.Array players = new Godot.Collections.Array {};
CSGCylinder mesh;
Vector3 spawnPoint;
Spatial parent;
MeshInstance arrow;
bool active = false;
bool lockable = true;

public override void _Ready(){
    deathTimer = GetNode<Timer>("DeathTimer");
    //target = GetNode<Spatial>("../../../playerNode/PlayerBall");
    mesh = GetNode<CSGCylinder>("CSGCylinder");
    parent = GetNode<Spatial>("../.");
    arrow = GetNode<MeshInstance>("Arrow");
    squishSet[0] = mesh.Scale.x * 1.3F;
    squishSet[1] = mesh.Scale.y * .7F;
    squishSet[2] = mesh.Scale.z * 1.3F;
    spawnPoint = GlobalTransform.origin;
    SetPhysicsProcess(false);
}

public override void _PhysicsProcess(float delta){
    if (state == states.spin){
        Vector2 spinVel = new Vector2(speed, speed).Rotated(ang*angMod*dirMod);
        velocity = new Vector3(spinVel.x, 0, spinVel.y);
        MoveAndSlide(new Vector3(velocity.x, 0, velocity.z), Vector3.Up);
        ang = Mathf.LerpAngle(ang, ang + .01F, 3);
        //mesh.Rotation = new Vector3(mesh.Rotation.x, mesh.Rotation.y + (.15F * rotDir), mesh.Rotation.z);
        mesh.RotateY(Rotation.y - (.15F * dirMod));
    }
    else if (state == states.squished){
        mesh.Scale = new Vector3(Mathf.Lerp(mesh.Scale.x, squishSet[0], .2F),Mathf.Lerp(mesh.Scale.y, squishSet[1], .2F),Mathf.Lerp(mesh.Scale.x, squishSet[2], .2F));
        mesh.Translation = new Vector3(mesh.Translation.x, Mathf.Lerp(mesh.Translation.y, 0, .2F), mesh.Translation.z);
    }
    else if (state == states.launched){
        MoveAndSlide(new Vector3(launchVec.x, yvelocity, launchVec.z), Vector3.Up);
        yvelocity -= 25 * delta;
        if (IsOnFloor() || IsOnWall()) _on_DeathTimer_timeout();
    }
}

public void _launch(float power, Vector3 cVec){
    state = states.launched;
    if (power != 0) launchVec = new Vector3(cVec.x * power * 3, 0, cVec.z * power * 3);
    else launchVec = new Vector3(velocity.x, 0, velocity.z);
    yvelocity = power * 1.5F;
    deathTimer.Start(2);
    vulnerableClass = 0;
    lockable = false;
    GetNode<CollisionShape>("CollisionShape").Disabled = false;
    foreach (Node player in players) player.Call("_lockOn", this, 0);
    parent.Call("_dropBP", GlobalTransform.origin, .75);
}

public void _squish(float power){
    state = states.squished;
    deathTimer.Start(1.5F);
    vulnerableClass = 0;
    lockable = false;
    foreach (Node player in players) player.Call("_lockOn", this, 0);
    parent.Call("_dropBP", GlobalTransform.origin, .75);
}

public void _on(){
    if (!deathTimer.IsStopped() || active) return;
    SetPhysicsProcess(true);
    active = true;
}

public void _off(){
    if (!deathTimer.IsStopped() || !active) return;
    SetPhysicsProcess(false);
    state = states.spin;
    active = false;
    ang = 0;
}

public void _on_DeathTimer_timeout(){
    deathTimer.Stop();
    QueueFree();
    if (lockable) foreach (Node player in players) player.Call("_lockOn", this, 0);
    parent.Call("_spawnTimerSet", this, "spinner", spawnPoint, new float[] {angMod, dirMod});
    //if ((state != states.squished && state != states.launched) && (target.Get("lockOn") == this)) foreach (Node player in players) player.Call("_lockOn", this, 0);
}

}
