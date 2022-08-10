using Godot;
using System;

public class Goon : KinematicBody
{
Vector3 velocity = Vector3.Zero;
Vector3 launchVec = Vector3.Zero;
float yvelocity = 0;
int aggroRange = 24;
int damage = 20;
float speed = 12;
float ang = 0;
int vulnerableClass = 2; //0: none, 1: just crash, 2: killed by dash and crash, 3: just dash
bool stunned = false; //if we just got hit or not (relevant if we don't die in 1 hit)
enum states{
    alert,
    search,
    attack,
    repath,
    squished,
    launched
}
states state = states.alert;
float skrrt = 1;
float[] squishSet = new float[] {0,0,0};
Timer pathTimer;
Timer deathTimer;
Timer angleChecker;
Spatial target;
Godot.Collections.Array players = new Godot.Collections.Array {};
MeshInstance mesh;
RayCast bottom;
Vector3 spawnPoint;
Spatial parent;
MeshInstance arrow;
bool active = false;
bool wallb = false;
[Export] bool lockable = true;
[Export] bool passive = false;



public override void _Ready(){
    pathTimer = GetNode<Timer>("PathTimer");
    deathTimer = GetNode<Timer>("DeathTimer");
    angleChecker = GetNode<Timer>("AngleChecker");
    // target = GetNode<Spatial>("../../../playerNode/VBoxContainer/ViewportContainer/Viewport/PlayerBall");
    mesh = GetNode<MeshInstance>("MeshInstance");
    bottom = GetNode<RayCast>("RayCast");
    parent = GetNode<Spatial>("../.");
    // players = (Godot.Collections.Array)parent.Get("players");
    arrow = GetNode<MeshInstance>("Arrow");
    squishSet[0] = mesh.Scale.x * 1.3F;
    squishSet[1] = mesh.Scale.y * .7F;
    squishSet[2] = mesh.Scale.z * 1.3F;
    spawnPoint = GlobalTransform.origin;
    SetPhysicsProcess(false);
}

public override void _PhysicsProcess(float delta){
    if (state == states.alert) return;
    if (state == states.attack || state == states.search || state == states.repath) _velocityMove(delta);
    else if (state == states.squished){
        mesh.Scale = new Vector3(Mathf.Lerp(mesh.Scale.x, squishSet[0], .2F),Mathf.Lerp(mesh.Scale.y, squishSet[1], .2F),Mathf.Lerp(mesh.Scale.x, squishSet[2], .2F));
        mesh.Translation = new Vector3(mesh.Translation.x, Mathf.Lerp(mesh.Translation.y, 0, .2F), mesh.Translation.z);
    }
    else if (state == states.launched){
        MoveAndSlide(new Vector3(launchVec.x, yvelocity, launchVec.z), Vector3.Up);
        yvelocity -= 25 * delta;
        if (IsOnFloor()){
            if (!stunned){
                mesh.Translation = new Vector3(mesh.Translation.x, mesh.Translation.y - .3F, mesh.Translation.z);
                state = states.alert;
                pathTimer.Start(4);
                deathTimer.Stop();
                stunned = true;
                velocity = Vector3.Zero;
            }
            else _on_DeathTimer_timeout();
        }
        else if (!wallb && IsOnWall()){
            Node collider = (Node)GetSlideCollision(0).Collider;
            if (collider.IsInGroup("walls")){
                launchVec = launchVec.Bounce(GetSlideCollision(0).Normal);
                wallb = true;
            }
        }
    }
}

public void _velocityMove(float delta){
    if (stunned) return;
    MoveAndSlide(velocity * skrrt, Vector3.Up);
    if (!bottom.IsColliding()){
        if (state == states.attack){
            MoveAndSlide(velocity * skrrt * -1, Vector3.Up);
            state = states.search;
            pathTimer.Start(.2F);
            return;
        }
        else if (state == states.search){
            velocity = new Vector3(velocity.x * -1, 0, velocity.z * -1);
            state = states.repath; //get away from the curve
            return;
        }
        else if (state == states.repath){
            MoveAndSlide(velocity * skrrt, Vector3.Up);
            if (bottom.IsColliding()) state = states.search;
            return;
        }
    }
    if (skrrt < 1){
        skrrt -= .5F * delta;
        if (skrrt < .08F){
            state = states.alert;
            pathTimer.Start(1);
        }
    }
}

public void _launch(float power, Vector3 cVec){
    state = states.launched;
    if (power != 0) launchVec = new Vector3(cVec.x * power * 2.25F, 0, cVec.z * power * 2.25F);
    else launchVec = new Vector3(velocity.x, 0, velocity.z);
    yvelocity = power * 1.5F;
    pathTimer.Stop();
    if (stunned){
        vulnerableClass = 0;
        lockable = false;
        foreach (Node player in players) player.Call("_lockOn", this, 0);
        deathTimer.Start(2.5F);
        parent.Call("_dropBP", GlobalTransform.origin, .3);
    }
    else deathTimer.Start(4);
}

public void _squish(float power){ //check power vs health and all that here?
    state = states.squished;
    pathTimer.Stop();
    deathTimer.Start(1.5F);
    vulnerableClass = 0;
    lockable = false;
    foreach (Node player in players) player.Call("_lockOn", this, 0);
    parent.Call("_dropBP", GlobalTransform.origin, .3);
}


public void _on(){
    if (!deathTimer.IsStopped() || active) return;
    if (active && !bottom.IsColliding()){
        _on_DeathTimer_timeout();
        return;
    }
    pathTimer.Start(1);
    state = states.alert;
    SetPhysicsProcess(true);
    active = true;
}

public void _off(){
    if (!deathTimer.IsStopped() || !active) return;
    if (!bottom.IsColliding() || stunned){
        _on_DeathTimer_timeout();
        return;
    }
    pathTimer.Stop();
    angleChecker.Stop();
    SetPhysicsProcess(false);
    active = false;
}

public void _on_PathTimer_timeout(){
    pathTimer.Stop();
    skrrt = 1;
    if (!passive){
        float dist = 0;
        float targ = -1;
        Vector3 location = GlobalTransform.origin;
        foreach (Spatial player in players){
            dist = location.DistanceTo(player.GlobalTransform.origin);
            if (targ == -1 || dist < targ){
                targ = dist;
                target = player;
            }
        }
    }
    if (stunned){ //wake up
        mesh.Translation = new Vector3(mesh.Translation.x, mesh.Translation.y + .3F, mesh.Translation.z);
        stunned = false;
        state = states.alert;
        pathTimer.Start(.5F);
    }
    else if (passive || GlobalTransform.origin.DistanceTo(target.GlobalTransform.origin) > aggroRange){
        state = states.search;
        pathTimer.Start(2);
        velocity = new Vector3((float)GD.RandRange(-3.1F, 3.1F), -10, (float)GD.RandRange(-3.1F, 3.1F)).Normalized() * 4;
    }
    else if (state != states.alert){
        state = states.alert;
        pathTimer.Start(.4F);
    }
    else{
        state = states.attack;
        velocity = new Vector3(target.GlobalTransform.origin - GlobalTransform.origin);
        ang = new Vector2(velocity.x, velocity.z).Angle();
        angleChecker.Start(.5F);
        Vector2 dirVel = new Vector2(speed, 0).Rotated(ang);
        velocity = new Vector3(dirVel.x, -10, dirVel.y);
    }
}

public void _on_AngleChecker_timeout(){ //only fire infrequently
    if (state != states.attack) return;
    Vector3 targVector = new Vector3(target.GlobalTransform.origin - GlobalTransform.origin);
    float targAngle = new Vector2(targVector.x, targVector.z).Angle();
    float maxAngle = 6.28F;
    float difference = (targAngle - ang % maxAngle);
    difference = Math.Abs((2 * difference % maxAngle) - difference);
    if (difference > 1){
        skrrt = .99F;
        angleChecker.Stop();
    }
}

public void _on_DeathTimer_timeout(){
    deathTimer.Stop();
    QueueFree();
    if (lockable) foreach (Node player in players) player.Call("_lockOn", this, 0);
    parent.Call("_spawnTimerSet", GetNode<Spatial>("."), "goon", spawnPoint);
}

}
