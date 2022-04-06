using Godot;
using System;

public class Goon : KinematicBody
{
Vector3 velocity = Vector3.Zero;
Vector3 launchVec = Vector3.Zero;
float yvelocity = 0;
int aggroRange = 20;
int damage = 20;
float speed = 9;
float ang = 0;
bool invincible = false; //in air
bool stunned = false; //not super necessary, just used to determine if we should move the mesh up or not
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
MeshInstance mesh;
RayCast bottom;
Vector3 spawnPoint;
Spatial world;

public override void _Ready(){
    pathTimer = GetNode<Timer>("PathTimer");
    deathTimer = GetNode<Timer>("DeathTimer");
    angleChecker = GetNode<Timer>("AngleChecker");
    target = GetNode<Spatial>("../../playerNode/PlayerBall");
    mesh = GetNode<MeshInstance>("MeshInstance");
    bottom = GetNode<RayCast>("RayCast");
    world = GetNode<Spatial>("../../../World");
    pathTimer.Start(1);
    squishSet[0] = mesh.Scale.x * 1.3F;
    squishSet[1] = mesh.Scale.y * .7F;
    squishSet[2] = mesh.Scale.z * 1.3F;
    spawnPoint = GlobalTransform.origin;
}

public override void _PhysicsProcess(float delta){
    if (state == states.alert) return;
    if (state == states.attack || state == states.search || state == states.repath) _velocityMove(delta);
    else if (state == states.squished){
        mesh.Scale = new Vector3(Mathf.Lerp(mesh.Scale.x, squishSet[0], .2F),Mathf.Lerp(mesh.Scale.y, squishSet[1], .2F),Mathf.Lerp(mesh.Scale.x, squishSet[2], .2F));
        if (!stunned) mesh.Translation = new Vector3(mesh.Translation.x, Mathf.Lerp(mesh.Translation.y, 0, .2F), mesh.Translation.z);
    }
    else if (state == states.launched){
        MoveAndSlide(new Vector3(launchVec.x, yvelocity, launchVec.z), Vector3.Up);
        yvelocity -= 25 * delta;
        if (IsOnFloor()){
            stunned = true;
            state = states.alert;
            pathTimer.Start(3);
            deathTimer.Stop();
            invincible = false;
        }
        else if (IsOnWall()){
            Node collider = (Node)GetSlideCollision(0).Collider;
            if (collider.IsInGroup("walls")) launchVec = launchVec.Bounce(GetSlideCollision(0).Normal);
        }
    }
}

public void _velocityMove(float delta){
    if (!bottom.IsColliding()){
        if (state == states.attack){
            _launch(0, Vector3.Zero);
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
    MoveAndSlide(velocity * skrrt, Vector3.Up);
    if (skrrt < 1){
        skrrt -= .5F * delta;
        if (skrrt < .08F){
            state = states.alert;
            pathTimer.Start(2);
        }
    }
}

public void _launch(float power, Vector3 cVec){
    state = states.launched;
    if (power != 0) launchVec = new Vector3(cVec.x * power * 3, 0, cVec.z * power * 3);
    else launchVec = new Vector3(velocity.x, 0, velocity.z);
    yvelocity = power * 1.5F;
    pathTimer.Stop();
    deathTimer.Start(3);
    invincible = true;
}

public void _squish(float power){ //check power vs health and all that here?
    state = states.squished;
    pathTimer.Stop();
    deathTimer.Start(1.5F);
    invincible = true;
}

public void _on_PathTimer_timeout(){
    pathTimer.Stop();
    skrrt = 1;
    if (stunned){ //move up
        Translation = new Vector3(Translation.x, Translation.y + .3F, Translation.z);
        stunned = false;
        state = states.alert;
        pathTimer.Start(.5F);
    }
    else if (GlobalTransform.origin.DistanceTo(target.GlobalTransform.origin) > aggroRange || stunned){
        state = states.search;
        pathTimer.Start(2);
        velocity = new Vector3((float)GD.RandRange(-3.1F, 3.1F), 0, (float)GD.RandRange(-3.1F, 3.1F)).Normalized() * 1.5F;
    }
    else if (state != states.alert){
        state = states.alert;
        pathTimer.Start(.5F);
    }
    else{
        state = states.attack;
        velocity = new Vector3(target.GlobalTransform.origin - GlobalTransform.origin);
        ang = new Vector2(velocity.x, velocity.z).Angle();
        angleChecker.Start(.25F);
        velocity = new Vector3(velocity.x, 0, velocity.z).Normalized() * speed;
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
    GD.Print("deleted!");
    QueueFree();
    world.Call("_spawnMob", "goon", spawnPoint);
}

}
