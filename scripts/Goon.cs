using Godot;
using System;

public class Goon : KinematicBody
{
Vector3[] path = new Vector3[0];
Vector3 velocity = Vector3.Zero;
Vector3 launchVec = Vector3.Zero;
float yvelocity = 0;
int aggroRange = 20;
int damage = 20;
float speed = 5;
bool launched = false;
bool invincible = false; //in air
bool stunned = false;
bool squished = false;
bool lingerMove = false;
float[] squishSet = new float[] {0,0,0};
int currentPathIndex = 0;
float ang = 0;
float angTarget = 0;
Timer pathTimer;
Timer deathTimer;
Spatial target;
Navigation nav;
MeshInstance mesh;
RayCast bottom;
Vector3 spawnPoint;
Spatial world;

public override void _Ready(){
    pathTimer = GetNode<Timer>("PathTimer");
    deathTimer = GetNode<Timer>("DeathTimer");
    target = GetNode<Spatial>("../../playerNode/PlayerBall");
    nav = GetNode<Navigation>("../../Navigation");
    mesh = GetNode<MeshInstance>("MeshInstance");
    bottom = GetNode<RayCast>("RayCast");
    world = GetNode<Spatial>("../../../World");
    pathTimer.Start(1);
    squishSet[0] = mesh.Scale.x * 1.3F;
    squishSet[1] = mesh.Scale.y * .7F;
    squishSet[2] = mesh.Scale.z * 1.3F;
    spawnPoint = Translation;
}

public override void _PhysicsProcess(float delta){
    if (squished){
        mesh.Scale = new Vector3(Mathf.Lerp(mesh.Scale.x, squishSet[0], .2F),Mathf.Lerp(mesh.Scale.y, squishSet[1], .2F),Mathf.Lerp(mesh.Scale.x, squishSet[2], .2F));
        if (!stunned) mesh.Translation = new Vector3(mesh.Translation.x, Mathf.Lerp(mesh.Translation.y, 0, .2F), mesh.Translation.z);
    }
    else if (launched){
        MoveAndSlide(new Vector3(launchVec.x, yvelocity, launchVec.z), Vector3.Up);
        yvelocity -= 20 * delta;
        if (IsOnFloor()){
            launched = false;
            pathTimer.Start(3);
            deathTimer.Stop();
            invincible = false;
        }
    }
    else if (path.Length > 0 || lingerMove) _moveToTarget();
}

public void _moveToTarget(){
    if (!lingerMove && GlobalTransform.origin.DistanceTo(path[currentPathIndex]) >= .1F){
        _velocityMove(path[currentPathIndex] - GlobalTransform.origin);
    }
    else if (lingerMove && bottom.IsColliding()){
        pathTimer.Stop();
        pathTimer.Start(2);
        lingerMove = false;
        stunned = false;
        path = nav.GetSimplePath(GlobalTransform.origin, target.GlobalTransform.origin);
        currentPathIndex = 0;
        ang = angTarget;
        angTarget = new Vector2(path[currentPathIndex].x, path[currentPathIndex].z).Angle();
    }
    else if (!lingerMove){
        currentPathIndex ++;
        if (currentPathIndex > (path.Length - 1)) lingerMove = true;
        else{
            ang = angTarget;
            angTarget = new Vector2(path[currentPathIndex].x, path[currentPathIndex].z).Angle();
        }
    }
}

public void _velocityMove(Vector3 vec){
    Vector2 laterDir = new Vector2(vec.x, vec.z).Rotated(ang);
    if (ang != angTarget){
        ang = Mathf.Lerp(ang, angTarget, .01F);
        if (Mathf.Round(ang * 10) == Mathf.Round(angTarget * 10)) ang = angTarget;
    }
    velocity = new Vector3(laterDir.x, vec.y, laterDir.y).Normalized() * speed;
    MoveAndSlide(velocity, Vector3.Up);
}

public void _launch(float power, Vector3 cVec){
    launched = true;
    launchVec = new Vector3(cVec.x * power * 3, 0, cVec.z * power * 3);
    yvelocity = power * 1.5F;
    path = new Vector3[0];
    pathTimer.Stop();
    deathTimer.Start(3);
    invincible = true;
    stunned = true;
}

public void _squish(float power){
    //check power vs health and all that here?
    squished = true;
    path = new Vector3[0];
    pathTimer.Stop();
    deathTimer.Start(1.5F);
    invincible = true;
}

public void _on_PathTimer_timeout(){
    pathTimer.Stop();
    pathTimer.Start(2);
    lingerMove = false;
    stunned = false;
    path = nav.GetSimplePath(GlobalTransform.origin, target.GlobalTransform.origin);
    currentPathIndex = 0;
    ang = angTarget;
    angTarget = new Vector2(path[currentPathIndex].x, path[currentPathIndex].z).Angle();
}

public void _on_DeathTimer_timeout(){
    GD.Print("deleted!");
    QueueFree();
    world.Call("_spawnMob", "goon", spawnPoint);
}

}
