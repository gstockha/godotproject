using Godot;
using System;

public class Goon : KinematicBody
{
Vector3[] path = new Vector3[0];
Vector3 velocity = Vector3.Zero;
float yvelocity = 0;
int aggroRange = 20;
int damage = 20;
float speed = 5;
bool launched = false;
bool invincible = false;
bool squished = false;
int currentPathIndex = 0;
Timer pathTimer;
Timer deathTimer;
Spatial target;
Navigation nav;
MeshInstance mesh;

public override void _Ready(){
    pathTimer = GetNode<Timer>("PathTimer");
    deathTimer = GetNode<Timer>("DeathTimer");
    target = GetNode<Spatial>("../../playerNode/PlayerBall");
    nav = GetNode<Navigation>("../../Navigation");
    mesh = GetNode<MeshInstance>("MeshInstance");
    pathTimer.Start(2);
}

public override void _PhysicsProcess(float delta){
    if (squished) return;
    if (launched){
        MoveAndSlide(new Vector3(velocity.x, yvelocity, velocity.z), Vector3.Up);
        yvelocity -= 20 * delta;
        if (IsOnFloor()){
            launched = false;
            pathTimer.Start(2);
            deathTimer.Stop();
            invincible = false;
        }
    }
    else if (path.Length > 0) _moveToTarget();
}

public void _moveToTarget(){
    if (currentPathIndex > (path.Length - 1)) return;
    if (GlobalTransform.origin.DistanceTo(path[currentPathIndex]) >= .1F){
        Vector3 direction = path[currentPathIndex] - GlobalTransform.origin;
        velocity = direction.Normalized() * speed;
        MoveAndSlide(velocity, Vector3.Up);
    }
    else currentPathIndex ++;
}

public void _launch(float power, Vector3 cVec){
    launched = true;
    velocity = new Vector3(cVec.x * power * 2, 0, cVec.z * power * 2);
    yvelocity = power;
    path = new Vector3[0];
    pathTimer.Stop();
    deathTimer.Start(3);
    invincible = true;
}

public void _squish(float power){
    //check power vs health and all that here?
    squished = true;
    path = new Vector3[0];
    pathTimer.Stop();
    deathTimer.Start(1.5F);
    invincible = true;
    mesh.Scale = new Vector3(mesh.Scale.x * 1.5F, mesh.Scale.y * .5F, mesh.Scale.z * 1.5F);
    mesh.Translation = new Vector3(mesh.Translation.x, mesh.Translation.y - 1, mesh.Translation.z);
}

public void _on_PathTimer_timeout(){
    path = nav.GetSimplePath(GlobalTransform.origin, target.GlobalTransform.origin);
    currentPathIndex = 0;
}

public void _on_DeathTimer_timeout(){
    GD.Print("deleted!");
    QueueFree();
}

}
