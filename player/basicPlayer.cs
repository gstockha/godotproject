using Godot;
using System;
using System.Collections.Generic;
using MyMath;

public class basicPlayer : KinematicBody{

#region basic movement variables
Vector2 direction_ground;
Vector3 velocity;
Vector3 platformStickDifference = Vector3.Zero; //stick on thumps and stuff
float gravity = 23.0F;
float jumpForce = 11.5F;
float yvelocity = -1;
static float bounceBase = .7F;
float bounce = bounceBase;
int bounceCombo = 0;
int bounceComboCap = 3;
float basejumpwindow = 0;
float jumpwindow = 0;
float ang = 0;
float angTarget = 0;
int camLock = 0;
bool angDelayFriction = true; //player friction modifies angTarget lerping or not
bool wallb = false;
float wallbx = 0;
float wallby = 0;
int idle = 1; //0 not idle, 1 out of spawn, 2 stat allocation or something else
bool smushed = false;
bool launched = false;
bool invincible = false;
bool dashing = false;
bool sliding = false;
int hasJumped = 0; //set to 2 (strong) in jump function, 1 in boing timer timeout (soft) (distinction for leeway jumping) CANCRASH == 2 == HASJUMPED
int bounceDashing = 0; //determine if you're crashing or can crash, 1 means post crash/walldash, 2 means bounce on crash
bool walldashing = false; //for speed boost after dashing into a wall
bool canDash = false;
bool rolling = true; //ball is rolling
bool moving = false; //you're actually moving the ball
static int dirSize = 10;
float[,] dir = new float[2,dirSize];
float[] stickDir = new float[] {0,0};
float[] moveDir = new float[] {0,0};
float friction = 0;
float wallFriction = 0;
static float speedBase = 14;
float speed = speedBase;
int speedPoints, weightPoints, sizePoints, energyPoints, bouncePoints = 0;
int traction = 0;
float[] tractionList = new float[31];
static float baseWeight = 1.2F;
float weight = baseWeight;
int bp, bpUnspent, bpSpent = 0;
float dashSpeed = 20;
float boing = 0;
bool boingCharge = false;
bool boingDash = false; //use dashSpeed in boing slide (turned on in isRolling() and turned off in boing jump and boing timer)
bool squishSet = false; //only run the mesh squish settings once in squishNScale
float[] squishReverb = new float[] {0,1,0}; //0 is target jiggle intensity, 1 is the comparison bool guard (so we don't fire the code too much), 2 is a bool for airborne
#endregion

#region shift variables
float lastTranslationY = 0; //for shifted
float shiftedLastYNorm = 0; //downward slope boost
Vector3 shiftedLingerVec = new Vector3(0,0,0); //lingering momentum
float shiftedDir = 0;
int shiftedSticky = -1;
float[] shiftedBoost = new float[] {0,0};
float rampSlope = 0;
bool shiftedLinger = false;
#endregion

#region misc. game variables
bool slowMo = false;
bool speedRun = false;
string statSet = "traction";
Dictionary<string, string> controlNames = new Dictionary<string, string>(){
    {"roll", ""}, {"jump",""}, {"dash",""}, {"camera",""}, {"restart",""}, {"speedrun",""}, {"target", ""}
};
#endregion

#region onready variables (init in _Ready())
Dictionary<string, ProgressBar> statLabels;
float collisionBaseScale = .6F;
float[] collisionScales;
Timer boingTimer;
Timer preBoingTimer;
Timer dashTimer;
Timer smushTimer;
Timer deathtimer;
Timer invincibleTimer;
MeshInstance mesh;
MeshInstance shadow;
Spatial skinBody;
CollisionShape hitBoxShape;
CollisionShape collisionShape;
RayCast floorCast;
RayCast leewayCast;
RayCast trampolineCast;
RayCast shadowCast;
Area checkpoint;
Camera camera;
Label moveNote;
Label tipNote;
Label prNote;
Label bpSpendNote;
Control statUI;
Spatial lockOn = null;
Node globals;

#endregion

public override void _Ready(){
    #region load nodes
    globals = GetNode<Node>("/root/globals");
    boingTimer = GetNode<Timer>("boingTimer");
    preBoingTimer = GetNode<Timer>("preBoingTimer");
    dashTimer = GetNode<Timer>("DashTimer");
    smushTimer = GetNode<Timer>("SmushTimer");
    invincibleTimer = GetNode<Timer>("invincibleTimer");
    deathtimer = GetNode<Timer>("hitBox/deathTimer");
    collisionShape = GetNode<CollisionShape>("CollisionShape");
    hitBoxShape = GetNode<CollisionShape>("hitBox/CollisionShape");
    mesh = GetNode<MeshInstance>("skinBody/BallSkin");
    shadow = GetNode<MeshInstance>("shadowCast/shadowSkin");
    skinBody = GetNode<Spatial>("skinBody");
    floorCast = GetNode<RayCast>("floorCast");
    leewayCast = GetNode<RayCast>("leewayCast");
    trampolineCast = GetNode<RayCast>("trampolineCast");
    shadowCast = GetNode<RayCast>("shadowCast");
    checkpoint = GetNode<Area>("../../checkpoints/checkpointSpawn");
    camera = GetNode<Camera>("Position3D/playerCam");
    tipNote = GetNode<Label>("../../tipNote");
    bpSpendNote = GetNode<Label>("statUI/bpSpendNote");
    statUI = GetNode<Control>("statUI");
    statLabels = new Dictionary<string, ProgressBar>(){
        {"weight", GetNode<ProgressBar>("statUI/gravityBar")},
        {"traction", GetNode<ProgressBar>("statUI/tractionBar")},
        {"bounce", GetNode<ProgressBar>("statUI/bounceBar")},
        {"size", GetNode<ProgressBar>("statUI/girthBar")},
        {"speed", GetNode<ProgressBar>("statUI/speedBar")},
        {"energy", GetNode<ProgressBar>("statUI/energyBar")}
    };
    if (Owner.Name == "demoWorld"){
        traction = 10;
        speedPoints = 10;
        _setStat(98, "traction");
        _setStat(98, "speed");
        moveNote = GetNode<Label>("../../moveNote");
    }
    else{
        moveNote = null;
    }
    #endregion

    #region initialize data structures
    //collisionScales
    collisionScales = new float[] {collisionBaseScale,collisionBaseScale,collisionBaseScale};
    //traction
    float x = 50;
    for (int p = 0; p < tractionList.Length; p++){
        tractionList[p] = (float)((Math.Pow(1.0475D,x)-1)*((Math.Pow(.01F*x,2)*.01F)+.7F)); //old (pre dirSize alteration)
        // tractionList[p] = (float)((Math.Pow(1.025D,x)-1)*((Math.Pow(0.01F*x,25)*.05F)+2.4F));
        // GD.Print("tractionList[" + p.ToString() + "]: " + tractionList[p].ToString());
        x += 1.66F;
    }
    //control dictionary
    string[] controllerStr = new string[8];
    if (Input.IsJoyKnown(0) == false){ //keyboard mouse
        controllerStr[0] = "WASD or Arrow Keys";
        controllerStr[1] = "Space";
        controllerStr[2] = "Shift or C";
        controllerStr[3] = "Q & E or Z & X";
        controllerStr[4] = "R";
        controllerStr[5] = "T";
        controllerStr[6] = "Tab";
    }
    else{ //controller
        controllerStr[0] = "Left Joystick";
        controllerStr[3] = "L & R or Right Joystick";
        controllerStr[4] = "Start";
        controllerStr[6] = "Joystick";
        if (Input.GetJoyName(0).BeginsWith("x") || Input.GetJoyName(0).BeginsWith("X")){ //xbox
            controllerStr[1] = "the A Button";
            controllerStr[2] = "the X Button";
            controllerStr[5] = "Back";
        }
        else if (Input.GetJoyName(0).BeginsWith("d") || Input.GetJoyName(0).BeginsWith("D")){ //dualshock
            controllerStr[1] = "Cross Button";
            controllerStr[2] = "Square Button";
            controllerStr[5] = "Select";
        }
        else{ //other controller
            controllerStr[1] = "Bottom Face Button";
            controllerStr[2] = "Left Face Button";
            controllerStr[5] = "Select";
        }
    }
    controlNames["roll"] = controllerStr[0];
    controlNames["jump"] = controllerStr[1];
    controlNames["dash"] = controllerStr[2];
    controlNames["camera"] = controllerStr[3];
    controlNames["restart"] = controllerStr[4];
    controlNames["speedrun"] = controllerStr[5];
    controlNames["target"] = controllerStr[6];
    #endregion
    //skinBody.RotationDegrees = new Vector3(skinBody.RotationDegrees.x,45,skinBody.RotationDegrees.z);
    ang = (-1 * Rotation.y);
    yvelocity = -1;
    Translation = new Vector3(Translation.x, Translation.y + 2, Translation.z);
}

public override void _PhysicsProcess(float delta){ //run physics
    if (smushed) return;
    if (boing == 0){ //not boinging
        _controller(delta);
        bool isGrounded = (IsOnFloor() || yvelocity == -1);
        if (isGrounded) _isRolling(delta);
        else if (!IsOnCeiling() && !IsOnWall()) _isAirborne(delta);
        else if (IsOnWall()) _isWall(delta);
        else if (yvelocity > 0){ //ceiling bounce
            squishReverb[0] = yvelocity * .066F;
            float warpRate = squishReverb[0] * 1.25F;
            if (warpRate > .65F) warpRate = .65F;
            skinBody.Scale = new Vector3(collisionBaseScale*(1+warpRate),collisionBaseScale*(1-warpRate),collisionBaseScale*(1+warpRate));
            collisionScales[0] = .9F;
            collisionScales[1] = .3F;
            collisionScales[2] = collisionScales[0];
            squishSet = false;
            yvelocity *= -1;
        }
        _applyShift(delta, isGrounded);
        //if (collisionScales[0] != collisionBaseScale || launched) _squishNScale(delta, new Vector3(0,0,0), true);
        if (boing != 0 || boingCharge || squishReverb[0] != 0) _squishNScale(delta, new Vector3(0,0,0), true);
    }
    else _isBoinging(delta);
    _turnDelay();
    _lockOn(false, delta);
}

public void _controller(float delta){
    if (idle == 0){ //update direction
        stickDir[0] = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        stickDir[1] = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
        if (Mathf.Abs(stickDir[0]) > Mathf.Abs(stickDir[1])) stickDir[0] = Mathf.Round(stickDir[0]);
        else stickDir[1] = Mathf.Round(stickDir[1]);
    }
    else{
        stickDir[0] = 0;
        stickDir[1] = 0;
    }
    moving = (stickDir[0] != 0 || stickDir[1] != 0);
    _applyFriction(delta);
    direction_ground = new Vector2(moveDir[0],moveDir[1]).Rotated(ang).Normalized();
    float xvel = 0;
    float yvel = 0;
    float mod = 0;
    bool notWall = true;
    if (rolling && !dashing) mod = (speed * friction); //rolling and moving
    else if (!wallb && !dashing) mod = (.9f * speed * friction); //airborne or idle
    else if (wallb && !dashing) notWall = false;
    else if (dashing){
        mod = dashSpeed;
        if (angTarget != 0){ //alter direction vector
            if (lockOn == null || myMath.findDegreeDistance(ang,angTarget) < 1.5F) direction_ground = new Vector2(moveDir[0],moveDir[1]).Rotated(angTarget).Normalized();
        }
    }
    if (notWall){
        xvel = direction_ground.x * mod;
        yvel = direction_ground.y * mod;
    }
    else{
        if (moving && (Mathf.Sign(wallbx) != Mathf.Sign(direction_ground.x) || Mathf.Sign(wallby) != Mathf.Sign(direction_ground.y))){ //wallb air control
            wallFriction += .01F * delta * (((traction + 50) * .33F) + 40);
            if (wallFriction > .9F) wallFriction = .9F;
        }
        if (wallFriction != 0) MoveAndSlide(new Vector3(direction_ground.x*(speed*wallFriction),0,direction_ground.y*(speed*wallFriction)),Vector3.Up,true);
        xvel = wallbx * (1 - wallFriction);
        yvel = wallby * (1 - wallFriction);
    }
    velocity = new Vector3(xvel, yvelocity, yvel); //apply velocity
    MoveAndSlide(velocity, Vector3.Up, true);
    if (xvel != 0 || yvel != 0) _rotateMesh(xvel,yvel,delta);
}

public void _applyFriction(float delta){//}, float camDrag){
    int current = dirSize - 1;
    int i;
    for (i = 0; i < current; i++){
        dir[0,i] = dir[0,i+1];// * camDrag;
        dir[1,i] = dir[1,i+1];// * camDrag;
    }
    dir[0,current] = stickDir[0];// * camDrag;
    dir[1,current] = stickDir[1];// * camDrag;
    dir[0,current] = myMath.array2dMean(dir, 0);
    dir[1,current] = myMath.array2dMean(dir, 1);
    // if (camDrag != 1) return;
    int signdir = 0;
    if (moving){
        moveDir[0] = myMath.array2dMean(dir,0);
        moveDir[1] = myMath.array2dMean(dir,1);
        for (i = 0; i < 2; i++){
            if (!float.IsNaN(stickDir[i])) signdir = Math.Sign(stickDir[i]);
            else signdir = Math.Sign(moveDir[i]);
            if (signdir != 0 && Math.Sign(moveDir[i]) != signdir){
                // dir[i,current] += (tractionList[(traction/5)] * signdir) * delta;
                dir[i,current] += (6.5F + (traction * .1F)) * signdir * delta;
            }
        }
    }
    else if (moveDir[0] != 0 || moveDir[1] != 0){ //stop at .015 friction if not moving
        for (i = 0; i < 2; i++){
            if (moveDir[i] == 0) continue;
            if (Math.Abs(moveDir[i]) > .015F){ //slowly reduce speed (friction)
                moveDir[i] = myMath.array2dMean(dir,i);   
                signdir = Math.Sign(dir[i,current]); //apply shift
                //dir[i,current] -= (tractionList[traction] * .03F * signdir) * delta;
                dir[i,current] -= (1 + (traction * .05F)) * signdir * delta;
                if ((signdir == 1 && dir[i,current] < 0) || (signdir == -1 && dir[i,current] > 0)){
                    dir[i,current] = 0;
                }
            }
            else moveDir[i] = 0;
        }
    }
    float absx = Math.Abs(moveDir[0]);
    float absy = Math.Abs(moveDir[1]);
    float tempFric = (absx > absy) ? absx : absy;
    float tractionBoost = .15F;//.04F + (traction * .005F);
    friction = (moving && tempFric > (friction - tractionBoost)) ? tempFric + tractionBoost : tempFric;
    if (friction > 1) friction = 1;
    // GD.Print(friction);

}

public void _applyShift(float delta, bool isGrounded){
    bool floorCastTouching = floorCast.IsColliding();
    if (shiftedDir != 0 && !shiftedLinger){ //on shift
        if (floorCastTouching){ //make sure you're still on ground
            float grav = .05F + (baseWeight * .01F);
            float fric = friction;
            Vector3 shift = floorCast.GetCollisionNormal();
            if (shiftedDir > 0){ //going down
                if (!dashing) shiftedBoost[0] += delta * (speedBase * friction); //charge up
                else shiftedBoost[0] += delta * (speedBase * friction * 1.5F);
                if (shiftedBoost[0] > 30) shiftedBoost[0] = 30;
                shiftedBoost[1] = shiftedBoost[0]; //records the max shiftedBoost[0]
                if (shift.y != 1){ //make sure we're not passing a flat vector
                    bool record = true;
                    if (shiftedLastYNorm == 0) shiftedLastYNorm = shift.y;
                    else if (Mathf.Round(shift.y * 10) > Mathf.Round(shiftedLastYNorm * 10)) record = false;
                    else shiftedLastYNorm = shift.y;
                    if (record){ //save the last rolling vector
                        float fallRate = (lastTranslationY - Translation.y) * 10;
                        if (fallRate <= 0) fallRate = 0;
                        else if (fallRate > 1) fallRate = 1;
                        lastTranslationY = Translation.y;
                        fric *= (shiftedBoost[0] * (1 - shiftedLastYNorm)) * fallRate;
                        shiftedLingerVec = new Vector3(shift.x*grav*fric, 0, shift.z*grav*fric);
                    }
                    _rotateMesh(shiftedLingerVec.x*2*60, shiftedLingerVec.z*2*60, delta);
                }
            }
            else if (shiftedDir < 0) shiftedBoost[0] = 0;
            MoveAndCollide(new Vector3(shift.x*fric*grav, shiftedSticky, shift.z*fric*grav));
        }
        else if (jumpwindow != 0){ //fixes a shiftedDir glitch where you abruptly fall off of slopes
            yvelocity -= (gravity * weight) * delta;
            shiftedSticky = 0;
            floorCastTouching = true; //so we don't apply it twice (below)
            GD.Print("thing");
        }
        if (dashing && dashTimer.IsStopped()) dashTimer.Start(.5F + myMath.roundTo(.014F * bouncePoints, 100));
    }
    else if (shiftedBoost[0] > 0){ //shift linger
        shiftedLinger = true;
        shiftedBoost[0] -= delta * (baseWeight * 10);
        if (shiftedBoost[0] < 0) shiftedBoost[0] = 0;
        float momentum = shiftedBoost[0] / shiftedBoost[1];
        if (rampSlope != 0){ //decrease the Y slope vector over time
            rampSlope -= (delta * (1 - shiftedBoost[1] * .01F) * (baseWeight * .1F));
            if (rampSlope < 0) rampSlope = 0;
        }
        MoveAndCollide(new Vector3((shiftedLingerVec.x*momentum)*friction,rampSlope,(shiftedLingerVec.z*momentum)*friction));
        _rotateMesh(shiftedLingerVec.x*60*momentum, shiftedLingerVec.z*60*momentum, delta);
        if (shiftedBoost[0] < 0 || momentum < .01F || friction == 0){
            shiftedBoost[0] = 0;
            shiftedBoost[1] = 0;
            shiftedLinger = false;
            rampSlope = 0;
        }
    }
    if (isGrounded){ //if isRolling, check shift status
        if (GetSlideCount() > 0){ //shifted check
            shiftedSticky = 0;
            Node colliderNode = (Node)GetSlideCollision(0).Collider;
            if (GetFloorNormal().y < 1 == false || colliderNode.IsInGroup("flats")){ //normal ground
                if (dashing){
                    dashing = false;
                    walldashing = false;
                    // dashSpeed = speedBase * 1.5F;
                }
				shiftedDir = 0;
				rampSlope = 0;
				shiftedLastYNorm = 0;
            }
            else{ //shifted ground
                if (shiftedDir != 0){
                    bool lockVel = true;//(shiftedBoost[0] <= 0);
                    shiftedDir = (lastTranslationY - Translation.y);// * delta * 60;
                    if (shiftedDir > 0){ //going down slope
                        shiftedSticky = -1;
                        rampSlope = 0;
                        lockVel = false;
                    }
                    else if (shiftedLinger){
                        if (colliderNode.IsInGroup("ramps") && friction > .7 && rampSlope < (1 - GetFloorNormal().y)){ //get downward Y normal
                            rampSlope = (1 - GetFloorNormal().y) * friction;
                        }
                    }
                    if (lockVel) yvelocity = -1;
                }
                else shiftedDir = -.1F;
                lastTranslationY = Translation.y;
            }
        }
        else if (!floorCastTouching){
            yvelocity -= (gravity * weight) * delta;
            shiftedSticky = 0;
        }
    }
}

public void _isRolling(float delta){
    jumpwindow = 0;
	wallb = false;
	hasJumped = 0;
    boingDash = false;
    canDash = true;
    if (!walldashing){ //if landing, cancel dash
        if (dashing && (shiftedDir == 0)){
            bounce = bounceBase;
            dashTimer.Stop();
            boingDash = true;
            dashing = false;
            if (weight != baseWeight) bounceDashing = 2; //so you can't crash out of dash
        }
    }
    else walldashing = false;
    weight = baseWeight;
    if (yvelocity == -1){ //not bouncing up
        if (moving) rolling = true;
        else{ //not pressing move keys
            if (friction > 0) rolling = true;
            else if (rolling){
                rolling = false;
                friction = 0;
                for (int i = 0; i < dirSize; i++){
                    dir[0,i] = 0;
                    dir[1,i] = 0;
                }
            }
        }
        if (IsOnWall()){
            Node colliderNode = (Node)GetSlideCollision(0).Collider;
            if (colliderNode.IsInGroup("obstacles") || colliderNode.IsInGroup("mobs")) return;
            if (rolling){
                _alterDirection(GetSlideCollision(0).Normal); //reverse direction
                if (dashing){
                    dashTimer.Stop();
                    dashing = false;
                    //speed = speedBase;
                }
            }
        }
    }
    else if (yvelocity < -1){ //falling (to bounce)
        if (yvelocity < 0 && yvelocity > -1) yvelocity = -1;
        Node colliderNode = (Node)GetSlideCollision(0).Collider;
        // if (launched || yvelocity != -1 && (boingCharge || bounceDashing == 2 || !colliderNode.IsInGroup("obstacles") && (yvelocity * bounce) * -1 > (jumpForce * baseWeight * .5F) || 
        // (yvelocity * bounce) * -1 > (jumpForce * 1.2F)) || idle == 1){
        bool onShift = colliderNode.IsInGroup("shifts");
        launched = launched || -yvelocity > 40 + (weightPoints * .2F);
        if ((boingCharge || (bounceDashing == 2 && yvelocity != -1)) && idle != 1){
            if (!onShift || yvelocity < (weight * 100 - 100) * -1 || boingCharge || bounceDashing == 2){
                if (bounceDashing != 2){ //not crashing (bounceDashing == 2 is crashing)
                    boing = yvelocity * bounce;
                    bounceDashing = 0;
                    // if (bounce != bounceBase) bounceCombo = 0; //not full bounce
                }
                else{ //crashing
                    boing = yvelocity * (bounce * (1 - (weight * .2F)));
                    bounceDashing = 1;
                }
                boing *= -1;
                jumpwindow = 0;
                basejumpwindow = Mathf.Round(boing * 1.2F);
                if (boingTimer.IsStopped()) boingTimer.Start(boing * .02F);
                rolling = false;
                // bounce -= weight * .1F;
            }
            else if (shiftedDir != 0) yvelocity *= bounce * -1; //on a shift
        }
        else{ //dont bounce up
            if (!onShift){
                if (yvelocity <= -10){
                    float squish = myMath.roundTo(-yvelocity * .025F, 100);
                    squishReverb[0] = Mathf.Clamp(squish, .25F, .7F);
                    if (squish > .5F) squish = .5F;
                    collisionScales[0] = collisionBaseScale * (1 - squish); //x
                    collisionScales[1] = collisionBaseScale * (1 + squish); //y
                    collisionScales[2] = collisionScales[0]; //z
                }
                yvelocity = -1;
            }
            bounce = bounceBase;
            bounceCombo = 0;
        }
        if (idle != 2) idle = 0;
    }
    else if (shiftedDir > 0) yvelocity = -1; //prevents slope cheesing
    launched = false;
}

public void _isAirborne(float delta){
	if (weight == baseWeight && (yvelocity > 0 || dashing)){ //gravity
        if (!dashing) yvelocity -= (gravity * 1.2F) * delta;
        else yvelocity -= (gravity * (1.2F - (bouncePoints * .015F))) * delta;
    }
    else yvelocity -= (gravity * weight) * delta;
	rolling = false;
    if (yvelocity < -50) yvelocity = -50; //_capSpeed(22,50);
    // Node leewayCollision = (Node)leewayCast.GetCollider();
    // if (leewayCollision == null || !leewayCollision.IsInGroup("shifts")) return;
    // float grav = .05F + (baseWeight * .01F);
    // float fric = friction;
    // Vector3 shift = leewayCast.GetCollisionNormal();
    // if (!dashing) shiftedBoost[0] += delta * (speedBase * friction); //charge up
    // else shiftedBoost[0] += delta * (speedBase * friction * 1.5F);
    // if (shiftedBoost[0] > 30) shiftedBoost[0] = 30;
    // shiftedBoost[1] = shiftedBoost[0]; //records the max shiftedBoost[0]
    // if (shift.y != 1){ //make sure we're not passing a flat vector
    //     bool record = true;
    //     if (shiftedLastYNorm == 0) shiftedLastYNorm = shift.y;
    //     else if (Mathf.Round(shift.y * 10) > Mathf.Round(shiftedLastYNorm * 10)) record = false;
    //     else shiftedLastYNorm = shift.y;
    //     if (record){ //save the last rolling vector
    //         lastTranslationY = Translation.y;
    //         fric *= (shiftedBoost[0] * (1 - shiftedLastYNorm));
    //         shiftedLingerVec = new Vector3(shift.x*grav*fric, 0, shift.z*grav*fric);
    //     }
    //     _rotateMesh(shiftedLingerVec.x*2*60, shiftedLingerVec.z*2*60, delta);
    // }
    // GD.Print("shit");
    // MoveAndCollide(new Vector3(shift.x*fric*grav, shiftedSticky, shift.z*fric*grav));
}

public void _isWall(float delta){
    yvelocity -= (gravity * weight) * delta; //gravity
    if (!boingCharge && !launched) return;
    bool isWall = false;
    if (GetSlideCount() > 0){
        Spatial colliderNode = (Spatial)GetSlideCollision(0).Collider;
        isWall = colliderNode.IsInGroup("walls") || launched;
        if (isWall) platformStickDifference = colliderNode.Translation - Translation;
        else if (!isWall && colliderNode.IsInGroup("mobs")) return; //if enemy, leave
    }
    if (isWall || dashing){
        wallb = true;
        wallFriction = 0;
        if (dashing && !walldashing){
            dashTimer.Stop();
            dashing = false;
            weight = baseWeight;
            //speed = speedBase;
            bounceDashing = 1;
            walldashing = true;
        }
        if (isWall){
            if (!launched){
                float fricMod = (friction > .4F) ? friction * .75F : .4F * .75F;
                boing = speed * .7F * fricMod;
                if (boing < 3) boing = 3;
                jumpwindow = 0;
                basejumpwindow = Mathf.Round(boing * 6);
                boingTimer.Stop();
                boingTimer.Start(boing * .1F);
            }
            else{
                wallbx = GetSlideCollision(0).Normal.x * Mathf.Abs(wallbx);
                wallby = GetSlideCollision(0).Normal.z * Mathf.Abs(wallby);
                //launched = false;
                return;
            }
        }
        wallbx = GetSlideCollision(0).Normal.x * 2;
        wallby = GetSlideCollision(0).Normal.z * 2;
        _alterDirection(GetSlideCollision(0).Normal);
    }
    else if (!isWall){
        wallb = false;
        boingTimer.Stop();
    }
}

public void _isBoinging(float delta){
    bool isWall = IsOnWall();
    bool isThump = false;
    if (isWall){
        Spatial colliderNode = (Spatial)GetSlideCollision(0).Collider;
        isWall = colliderNode.IsInGroup("walls");
        isThump = colliderNode.IsInGroup("thumps");
    }
    if (floorCast.IsColliding() || isWall){
        if (jumpwindow < basejumpwindow) jumpwindow += 60 * delta;
        else jumpwindow = basejumpwindow;
        sliding = false;
        if (!wallb && shiftedDir == 0){
            float jumpratio = jumpwindow / basejumpwindow;
            float offset = 9 / basejumpwindow;
            if (floorCast.IsColliding()){
                Node floorCastNode = (Node)floorCast.GetCollider();
                if (floorCastNode.IsInGroup("slides")){
                    offset *= 2;
                    jumpratio *= 1.2F * .015F;
                }
            }
            if (offset > 1) offset = 1;
            stickDir[0] *= (1 - jumpratio);
            stickDir[1] *= (1 - jumpratio);
            _applyFriction(delta);
            float fric = friction * (1 + ((10 - dirSize) * .1F));
            float spd = 13 * fric * (.7F * offset);
            if (boingDash){
                float dashSpd = (20 * fric * 2) * (.7F * offset);
                if (dashSpd > spd) spd = dashSpd;
            }
            velocity = new Vector3(direction_ground.x*spd, yvelocity, direction_ground.y*spd);
            if (spd > 7){
                if (!sliding) _drawMoveNote("slide");
                sliding = true;
            }
            MoveAndSlide(velocity, Vector3.Up, true);
        }
        else if (isThump){ //stick to thump
            if (!floorCast.IsColliding()){
                Spatial colliderNode = (Spatial)GetSlideCollision(0).Collider;
                Translation = colliderNode.Translation - platformStickDifference;
            }
            else{
                boingDash = false;
                jumpwindow = 0;
                _squishNScale((gravity * 0.017F), new Vector3(0,0,0), true);
                squishSet = false;
                boing = 0;
                boingTimer.Stop();
            }
        }
        if (GetSlideCount() > 0 && collisionScales[0] != skinBody.Scale.x){
            if (!wallb) _squishNScale(delta, floorCast.GetCollisionNormal(), false);
            else _squishNScale(delta, GetSlideCollision(0).Normal, false);
        }
    }
    else{
        boingDash = false;
        jumpwindow = 0;
        _squishNScale((gravity * 0.017F), new Vector3(0,0,0), true);
        squishSet = false;
        boing = 0;
        boingTimer.Stop();
        //skinBody.RotationDegrees = new Vector3(skinBody.RotationDegrees.x,0,skinBody.RotationDegrees.z);
    }
}

public void _squishNScale(float delta, Vector3 squishNormal, bool reset){
    float rate = delta * 6;
    if (!reset && !squishSet){
        float squish = boing /  22;
        if (squish > .9F) squish = .9F;
        squishReverb[0] = 0;
        squishReverb[1] = 1;
        if (IsOnFloor() || (shiftedDir != 0 && Mathf.Round(squishNormal.y) != 0)){ //on floor OR on shift and not on a wall
            collisionScales[0] = collisionBaseScale * (1 + (squish * .7F)); //x
            collisionScales[1] = collisionBaseScale * (1 - (squish * .7F)); //y
            collisionScales[2] = collisionScales[0]; //z
        }
        else if (Mathf.Round(squishNormal.y) == 0){ //on a wall
            Vector3 rotation = skinBody.RotationDegrees;
            //skinBody.RotationDegrees = new Vector3(rotation.x,0,rotation.z);
            squish *= 1.5F;
            float add = collisionBaseScale * (1 + (squish * .7F));
            float sub = collisionBaseScale * (1 - (squish * .7F));
            // int camAng = (int)camera.Call("findClosestCamSet", RotationDegrees.y);
            collisionScales[1] = add; //go ahead and set y now
            // float normx = Mathf.Round(squishNormal.x);
            // float normz = Mathf.Round(squishNormal.z);
            // bool flip = false;
            // if (normx == 0 || normz == 0){ //45 degree flip
            //     //skinBody.RotationDegrees = new Vector3(rotation.x,45,rotation.z);
            //     flip = (Math.Sign(Math.Abs(normx)) == 1 && Math.Sign(Math.Abs(normz)) == 0);
            // }
            // else flip = (normx == 1 && normz == 1) || (normx == -1 && normz == -1);
            // if (flip){
            //     float temp = add;
            //     add = sub;
            //     sub = temp;
            // }
            // if (camAng == 1 || camAng == 3){
                // collisionScales[0] = sub; //x
                // collisionScales[2] = add; //z
            // }
            // else if (camAng == 0 || camAng == 2){
                collisionScales[0] = add; //x
                collisionScales[2] = sub; //z
            // }
        }
        squishSet = true;
    }
    else if (reset && squishReverb[0] != squishReverb[1]){ //BbOoOiIinNg! jiggle
        if (squishReverb[0] > .75F) squishReverb[0] = .75F;
        float mod = 1;
        for (int i = 0; i < 3; i++){
            if (i == 0){
                if (squishReverb[2] == 0){ //wall bounce proc is false
                    if (skinBody.Scale.x < collisionBaseScale) mod += squishReverb[0];
                    else mod -= squishReverb[0];
                }
                else{ //if wallbounce, alter jiggle pattern
                    squishReverb[2] = 0;
                    squishReverb[0] *= 1.25F;
                    mod += squishReverb[0];
                }
            }
            else if (mod < 1) mod = 1 + squishReverb[0];
            else mod = 1 - squishReverb[0];
            collisionScales[i] = collisionBaseScale * mod; //reset
        }
        squishReverb[1] = squishReverb[0];
        rate *= .9F; //make boing good
    }
    skinBody.Scale =
    new Vector3(Mathf.Lerp(skinBody.Scale.x, collisionScales[0], rate),
    Mathf.Lerp(skinBody.Scale.y, collisionScales[1], rate),
    Mathf.Lerp(skinBody.Scale.z, collisionScales[2], rate));
    Vector3 translations = mesh.Translation;
    if (boing != 0 && IsOnFloor() && shiftedDir == 0){ //crush the ball into the floor
    //if (IsOnFloor() && shiftedDir == 0){ //crush the ball into the floor
        if (translations.y > 0) mesh.Translation = new Vector3(translations.x, 0, translations.z);
        Vector3 collisionscales = skinBody.Scale;
        if (collisionscales.y < collisionBaseScale){
            float meshTarg = 0 - (3 * (collisionBaseScale - collisionscales.y) / collisionBaseScale);
            meshTarg *= ((basejumpwindow*.5F)/jumpForce < 1) ? ((basejumpwindow*.5F)/jumpForce) : 1;
            translations = mesh.Translation;
            if (meshTarg < translations.y) mesh.Translation = new Vector3(translations.x,meshTarg,translations.z);
        }
    }
    else if (translations.y != 0){ //undo the crush effect
        if (translations.y < -.5F) mesh.Translation = new Vector3(translations.x, -.5F, translations.z);
        else if (translations.y < 0){
            float newY = translations.y + (delta * Math.Abs(yvelocity));
            mesh.Translation = new Vector3(translations.x, newY, translations.z);
        }
        else mesh.Translation = new Vector3(translations.x, 0, translations.z);
    }
    // if (!IsOnFloor() && (!wallb || !IsOnWall())){ //airborne
    if (boing == 0){
        if (IsOnFloor() || yvelocity == -1){ //ground or airborne
            if (skinBody.Scale.y > (collisionScales[1] * (1 - squishReverb[0]))
            && skinBody.Scale.y < (collisionScales[1] * (1 + squishReverb[0]))){
                squishReverb[0] -= .02F;
                if (squishReverb[0] < 0) squishReverb[0] = 0;
                if (squishReverb[0] == 0) skinBody.Scale = new Vector3(collisionScales[0],collisionScales[1],collisionScales[2]);
            }
        }
        else if (!wallb || !IsOnWall()){ //ground or airborne
            if (skinBody.Scale.x > (collisionScales[0] * (1 - squishReverb[0]))
            && skinBody.Scale.x < (collisionScales[0] * (1 + squishReverb[0]))){
                squishReverb[0] -= .02F;
                if (squishReverb[0] < 0) squishReverb[0] = 0;
                if (squishReverb[0] == 0) skinBody.Scale = new Vector3(collisionScales[0],collisionScales[1],collisionScales[2]);
            }
        }
    }
    else if (basejumpwindow != 0 && jumpwindow/basejumpwindow >= 1){ //windowed
        collisionScales[0] = skinBody.Scale.x;
        collisionScales[1] = skinBody.Scale.y;
        collisionScales[2] = skinBody.Scale.z;
    }
    // else if (jumpwindow == 0 && boing == 0 && (IsOnFloor() || yvelocity == -1)){
    //     skinBody.Scale = new Vector3(collisionBaseScale,collisionBaseScale,collisionBaseScale);
    //     GD.Print("set");
    // }
}

public void _rotateMesh(float xvel, float yvel, float delta){
    Vector3 meshRotation = mesh.Rotation;
    float angy = meshRotation.y;
    if (!wallb){
        Vector2 dragdir = new Vector2(moveDir[1], moveDir[0]);
        angy = dragdir.Angle();
    }
    float xv = Math.Abs(xvel);
    float yv = Math.Abs(yvel);
    float turn = (xv > yv) ? xv : yv;
    turn *= 1.5F * delta;
    mesh.Rotation = new Vector3(meshRotation.x + turn, angy, meshRotation.z);
}

// public void _capSpeed(float high, float low){
//     if (yvelocity < -low) yvelocity = -low;
// 	else if (yvelocity > high) yvelocity = high;
// }

public void _turnDelay(){
    if (angTarget == 0) return;
    if (camLock != 2) camLock = 1;
    float tractionFriction;
    if (camLock < 2) tractionFriction = (angDelayFriction) ? tractionList[30] * .001F : 0;
    else tractionFriction = tractionList[traction] * .0008F;
    ang = Mathf.LerpAngle(ang,angTarget,.015F + tractionFriction);
    if (lockOn != null) return;
    // GD.Print("ang: " + ang.ToString() + ", angTarget: " + angTarget.ToString());
    if (camLock == 1){
        if (lockOn == null && Math.Sign(ang) != Math.Sign(angTarget)){
            float add = myMath.findDegreeDistance(ang,angTarget);
            if ((string)camera.Get("turnDir") == "left") add *= -1;
            ang = angTarget + add;
        }
        if (myMath.roundTo(ang,10) == myMath.roundTo(angTarget,10)){
            ang = angTarget;
            angTarget = 0;
            angDelayFriction = true;
            camLock = 0;
        }
    }
}

public void _lockOn(bool triggerScript, float delta){
    if (lockOn == null){
        if (camLock == 0 && moveDir[0] != 0){// && !wallb){//(Math.Abs(moveDir[0]) > .05F){
            float addAng = 0;
            addAng += ((moveDir[0] * 2) * (speed * .05F)) * .01F;
            addAng *= (moveDir[1] > 0) ?  1 + (moveDir[1] * 1.1F) : 1 + (moveDir[1] * .6F); //speed up ang if moving backward, slow it down if forward
            ang += addAng * delta * 60;
            Rotation = new Vector3(Rotation.x, ang * -1, Rotation.z);
        }
        return;
    }
    if (triggerScript == true || IsInstanceValid(lockOn) == false){// || IsInstanceValid(lockOn)){
        camera.Call("_findLockOn", new object{}); //turn off lockOn on camera node
        return;
    }
    Vector3 target = lockOn.GlobalTransform.origin;
    float oldRot = Rotation.y;
    LookAt(new Vector3(target.x, Translation.y, target.z), Vector3.Up);
    //if (angTarget == 0 && myMath.findDegreeDistance(oldRot, Rotation.y) < 1.5F) ang = Rotation.y * -1;
    angTarget = Rotation.y * -1;
    Rotation = new Vector3(Rotation.x, Mathf.LerpAngle(ang * -1, Rotation.y, .015F + (tractionList[traction] * .0007F)), Rotation.z);
}

public void _alterDirection(Vector3 alterNormal){
    Vector2 rotDir = new Vector2(moveDir[0], moveDir[1]).Rotated(ang);
    Vector3 wallbang = new Vector3(rotDir.x, 0, rotDir.y).Bounce(alterNormal);
    rotDir = new Vector2(wallbang.z, wallbang.x).Rotated(ang);
    for (int i = 0; i < dirSize; i++){
        dir[0,i] = rotDir.y;
        dir[1,i] = rotDir.x;
    }
}

public void _jump(){
    if (smushed || idle > 0) return;
    boingCharge = true;
    bool trampolined = trampolineCast.IsColliding() && yvelocity >= -1;
    if (trampolined){
        boing = yvelocity * 1.25F;
        if (boing < 15 + (bouncePoints * .1F)) boing = 15 + (bouncePoints * .1F);
    }
    if (boing != 0){ //boing jump
        yvelocity = boing;
        boingDash = false;
        _squishNScale((gravity * .017F), new Vector3(0,0,0), true);
        squishSet = false;
        boing = 0;
        boingTimer.Stop();
        //skinBody.RotationDegrees = new Vector3(skinBody.RotationDegrees.x,0,skinBody.RotationDegrees.z);
        bool slopeSquish = false;
        if (!trampolined && shiftedDir != 0){ //boing jump off a slope
            Vector3 wallbang = velocity.Bounce(floorCast.GetCollisionNormal());
            Vector2 wallang = new Vector2(wallbang.x, wallbang.z);
            wallb = true;
            _alterDirection(floorCast.GetCollisionNormal());
            wallFriction = 0;
            slopeSquish = true;
            wallbx = wallang.x * .5F;
            wallby = wallang.y * .5F;
        }
        float lastyvel = yvelocity + (gravity * .017F); //yvel times rough delta estimate
        if (lastyvel > 20 && !trampolined) lastyvel = 20;
        int combo = bounceCombo;
        if (combo > bounceComboCap) combo = bounceComboCap;
        if (basejumpwindow < 1) basejumpwindow = 1;
        float windowRatio = jumpwindow / basejumpwindow;
        if (jumpwindow > 0){ //reward late boing
            jumpwindow = (float)Mathf.Ceil((jumpwindow + 1) * (bounce / bounceBase));
            if (jumpwindow < 1) jumpwindow = 1;
        }
        string chargedNote = "";
        if (windowRatio >= 1) chargedNote += "charged ";
        if (slopeSquish) _drawMoveNote(chargedNote + "slopeboing");
        float nuyvel = 0;
        if (bounceDashing != 1){ //regular boingjump
            jumpwindow = (jumpwindow / basejumpwindow * .75F) + bounceBase;
            nuyvel = myMath.roundTo((jumpForce*(1 + combo * .035F)) * jumpwindow, 10);
            bounceCombo += 1;
            if (!wallb && !slopeSquish){
                if (chargedNote == ""){
                    string comboStr = (combo > 0) ? " x" + (bounceCombo).ToString() : "";
                    _drawMoveNote("boing" + comboStr);
                }
                else _drawMoveNote("chargedboing");
            }
            else{
                if (!slopeSquish){
                    _drawMoveNote(chargedNote + "wallboing");
                    wallbx *= (jumpForce * (.2F + (.1F * jumpwindow)));
                    wallby *= (jumpForce * (.2F + (.1F * jumpwindow)));
                    nuyvel *= .6F + (.2F * jumpwindow);
                }
                squishReverb[2] = 1; //set wall jiggle to true
            }
            yvelocity = (nuyvel > lastyvel) ? nuyvel : lastyvel; //never go below a dirbble boing
        }
        else{ //crashing or walldashing
            jumpwindow = (jumpwindow / basejumpwindow) + bounceBase;
            bounceDashing = 0;
            nuyvel = myMath.roundTo((jumpForce * (1.3F - (bouncePoints * .002F))) * jumpwindow,10);
            if (wallb){ //if off wall
                if (!slopeSquish){
                    _drawMoveNote(chargedNote + "crash wallboing");
                    wallbx *= (jumpForce * (.4F + (.25F * jumpwindow)));
                    wallby *= (jumpForce * (.4F + (.25F * jumpwindow)));
                    nuyvel *= .4F + (.25F * jumpwindow);
                }
                nuyvel *= windowRatio * .65F;
                lastyvel *= windowRatio * .65F;
                squishReverb[2] = 1; //set wall jiggle to true
            }
            else if (!slopeSquish) _drawMoveNote(chargedNote + "crashboing");
            if (lastyvel > nuyvel || lastyvel == 20) nuyvel += lastyvel * .2F;
            yvelocity = (nuyvel > lastyvel) ? nuyvel : lastyvel; //never go below a dirbble boing
        }
        squishReverb[0] = yvelocity * .035F;
        if (wallb) squishReverb[0] += .5F;
        //_capSpeed(22, 50);
        if (!trampolined && yvelocity > (jumpForce + 10)) yvelocity = jumpForce + 10;
        jumpwindow = 0;
        bounce = bounceBase;
    }
    else if (yvelocity == -1 || (IsOnFloor() && shiftedDir == 0) || (shiftedDir != 0 && floorCast.IsColliding())){
        if (preBoingTimer.IsStopped() && shiftedDir == 0) preBoingTimer.Start(.2F); //idle charge jump
        else _normalJump(); //jump normal jump on shift
    }
    else return;
    hasJumped = 2;
    if (shiftedDir != 0) shiftedSticky = 0;
}

public void _normalJump(){
    if (smushed || idle > 0) return;
	boingCharge = false;
	_drawMoveNote("boing");
	yvelocity = jumpForce - Mathf.Round(((Translation.y - collisionBaseScale) - leewayCast.GetCollisionPoint().y) * 7) * .5F;
	squishReverb[0] = yvelocity * .035F;
	preBoingTimer.Stop();
	hasJumped = 2;
	if (shiftedDir != 0) shiftedSticky = 0;
}

public void _dash(){
    if (smushed || idle > 0) return;
    if ((moving || (moveDir[0] != 0 || moveDir[1] != 0)) && !dashing){
        if (leewayCast.IsColliding() && canDash && hasJumped == 0 && shiftedDir == 0){ // on ground and not on shift
            yvelocity = 5.75F;
            dashSpeed = 20;
            _drawMoveNote("dash");
            dashTimer.Stop();
            dashTimer.Start(.5F + myMath.roundTo(.014F * bouncePoints, 100));
            canDash = false;
        }
        else if (hasJumped > 0 && !IsOnWall()){ // in air and not on shift
            dashTimer.Stop();
            weight = baseWeight * 3;
            shiftedDir = 0; // don't need to apply shifted gravity anymore if doing this
            _drawMoveNote("crash");
        }
        else if (shiftedDir != 0){ // is on shift
            dashSpeed = 25;
            _drawMoveNote("slope dash");
        }
        else return;
        int i; //if your moveDir doesn't match your stickDir, override
        if (Math.Sign(stickDir[0]) != 0 && Math.Sign(moveDir[0]) != Math.Sign(stickDir[0])) for (i = 0; i < dirSize; i++) dir[0,i] = 0;//stickDir[0] * friction;
        if (Math.Sign(stickDir[1]) != 0 && Math.Sign(moveDir[1]) != Math.Sign(stickDir[1])) for (i = 0; i < dirSize; i++) dir[1,i] = 0;//stickDir[1] * friction;
        dashing = true;
    }
}

public void _launch(Vector3 launchVec, float power, bool alterDir){
    #region turn off boing charge and boing
    if (boing != 0 || !preBoingTimer.IsStopped()){
        boingDash = false;
        jumpwindow = 0;
        boing = 0;
        boingTimer.Stop();
        preBoingTimer.Stop();
        boingCharge = false;
    }
    #endregion
    if (dashing){
        dashTimer.Stop();
        dashing = false;
        weight = baseWeight;
        //speed = speedBase;
    }
    if (alterDir){
        wallb = true;
        wallbx = launchVec.x;
        wallby = launchVec.z;
        wallFriction = 0;
    }
    launched = true;
    yvelocity = power;
    hasJumped = yvelocity >= (bounceBase * jumpForce * 1.5F) ? 1 : hasJumped; //soft has jumped else what it was
    _squishNScale((gravity * .017F), new Vector3(0,0,0), true);
    squishSet = false;
    squishReverb[0] = yvelocity * .08F;
    squishReverb[2] = 1; //proc wall wiggle
}

public override void _Input(InputEvent @event){
    if (@event.IsActionPressed("jump")) _jump();
    else if (@event.IsActionReleased("jump")){
        if (boingCharge){  //below check: leeway ray and moving up and haven't hard jumped (hasJumped != 2) or yvel = -1 or wall
            if (IsOnFloor() || yvelocity == -1 || IsOnWall() && wallb || (leewayCast.IsColliding() && yvelocity > 0 && hasJumped != 2 && boing == 0)){
                if (boing != 0){
                    if (!boingTimer.IsStopped()) boingTimer.Stop();
                    _jump();
                }
                else _normalJump();
            }
            boingCharge = false;
        }
    }
    else if (@event.IsActionPressed("dash")) _dash();
    else if (@event.IsActionPressed("game_restart")) _dieNRespawn();
    else if (@event.IsActionPressed("speedrun_reset")){
        if (Owner.Name != "demoWorld") return;
        Area checkpnt = (Area)GetNode("../../checkpoints/checkpointSpawn");
        Translation = checkpnt.GlobalTransform.origin;
        if (!speedRun){
            _drawTip("Speedrun mode activated!\nPress T to restart speedrun");
            speedRun = true;
            Timer textTimer = (Timer)GetNode("../../tipNote/Timer");
            if (!textTimer.IsStopped()) textTimer.Stop();
            textTimer.Start(2);
        }
    }
    else if (@event.IsActionPressed("add_stat")) _setStat(99, statSet);
    else if (@event.IsActionPressed("sub_stat")) _setStat(-99, statSet);
    else if (@event.IsActionPressed("set_traction")) statSet = "traction";
    else if (@event.IsActionPressed("set_speed")) statSet = "speed";
    else if (@event.IsActionPressed("set_weight")) statSet = "weight";
    else if (@event.IsActionPressed("set_size")) statSet = "size";
    else if (@event.IsActionPressed("set_bounce")) statSet = "bounce";
    else if (@event.IsActionPressed("set_energy")) statSet = "energy";
    else if (@event.IsActionPressed("reset_stats")) _setStat(0, "reset");
    else if (@event.IsActionPressed("mario_stats")) _setStat(0, "mario");
    else if (@event.IsActionPressed("max_stats")) _setStat(0, "max");
    else if (@event.IsActionPressed("slow-mo")){
        if (!slowMo) Engine.TimeScale = .3F;
        else Engine.TimeScale = 1;
        slowMo = !slowMo;
    }
    else if (@event.IsActionPressed("debug_restart")){
        _setStat(0, "reset");
        GetTree().ChangeScene((string)globals.Get("currentScene"));
    }
    else if (@event.IsActionPressed("end_game")) GetTree().Quit();
    else if (@event.IsActionPressed("fullscreen")) OS.WindowFullscreen = !OS.WindowFullscreen;
    else if (@event.IsActionPressed("return_hub")){
        _setStat(0, "reset");
        GetTree().ChangeScene("res://levels/hub.tscn");
    }
}

public void _on_DashTimer_timeout(){
    dashing = false;
    walldashing = false;
    // dashSpeed = speedBase * 1.5F;
}

public void _on_boingTimer_timeout(){
    if (boingCharge) return;
    _squishNScale((gravity * .017F), new Vector3(0,0,0), true);
    squishSet = false;
    if (!wallb){
        yvelocity = boing;
        if (yvelocity > 20) yvelocity = 20;
        squishReverb[0] = yvelocity * .035F;
    }
    else{
        squishReverb[0] = boing * .2F;
        squishReverb[2] = 1; //proc wall wiggle
    }
    hasJumped = yvelocity >= (bounceBase * jumpForce * 1.5F) ? 1 : hasJumped; //soft has jumped else what it was
    boingDash = false;
    jumpwindow = 0;
    boing = 0;
    //skinBody.RotationDegrees = new Vector3(skinBody.RotationDegrees.x,0,skinBody.RotationDegrees.z);
}

public void _on_preBoingTimer_timeout(){
    boing = jumpForce;
    jumpwindow = 0;
    basejumpwindow = Mathf.Round(boing * 1.2F);
}

public void _on_SmushTimer_timeout(){
    smushed = false;
    Translate(new Vector3(0, 1.5F, 0));
    _squishNScale(gravity * .017F, new Vector3(0,0,0), true);
}

public void _dieNRespawn(){
    if (idle != 2) idle = 1;
    smushed = false;
    yvelocity = 1;
    stickDir[0] = 0;
    stickDir[1] = 0;
    weight = baseWeight;
    dashing = false;
    dashTimer.Stop();
    walldashing = false;
    // dashSpeed = speedBase * 1.5F;
    wallb = false;
    hasJumped = 0;
    shiftedDir = 0;
    shiftedLinger = false;
    boingDash = false;
    preBoingTimer.Stop();
    _squishNScale(gravity * .017F, new Vector3(0,0,0), true);
    squishSet = false;
    boing = 0;
    //skinBody.RotationDegrees = new Vector3(skinBody.RotationDegrees.x,0,skinBody.RotationDegrees.z);
    boingCharge = false;
    boingTimer.Stop();
    for (int i = 0; i < dirSize; i++){
        dir[0,i] = 0;
        dir[1,i] = 0;
    }
    Translation = new Vector3(checkpoint.GlobalTransform.origin.x, checkpoint.GlobalTransform.origin.y + 2, checkpoint.GlobalTransform.origin.z);
    camera.Call("_setToDefaults"); //turn off player: lockOn, camLock, angTarget ; turn off cam: lockOn, heightMove, angMove, lerpMove
}

public void _on_deathtimer_timeout(){
    Area hitbox = (Area)GetNode("hitBox");
    Godot.Collections.Array areas = hitbox.GetOverlappingAreas();
    for (int i = 0; i < areas.Count; i++){
        hitbox = (Area)GetNode(areas[i].ToString());
        if (hitbox.IsInGroup("killboxes")){
            _dieNRespawn();
            return;
        }
    }
}

public void _on_invincibleTimer_timeout(){
    invincible = false;
}

public void _on_hitBox_area_entered(Area area){
    Godot.Collections.Array groups = area.GetGroups();
    for (int i = 0; i < groups.Count; i++){
        switch(groups[i].ToString()){
            case "mobs": _collisionDamage((Spatial)area.GetParentSpatial()); break;
            case "thumps": _collisionDamage((Spatial)area.GetParentSpatial()); break;
            case "trampolines":
                float boingPower = !area.Owner.Name.BeginsWith("big") ? 16 : 30;
                boingPower += Mathf.Abs(yvelocity * .25F);
                _launch(Vector3.Zero, boingPower, false);
                break;
            case "projectiles": _collisionDamage(area); break;
            case "splashes": _collisionDamage((Spatial)area.GetParentSpatial()); break;
            case "lavas": _collisionDamage(area); break;
            case "checkpoints": checkpoint = area; break;
            case "killboxes":
                if (!area.Name.BeginsWith("delay")) _dieNRespawn();
                else if (deathtimer.IsStopped()) deathtimer.Start(2);
                break;
            case "warps":
                Area checkpoint1 = (Area)GetNode("../../checkpoints/checkpointSpawn");
                Translation = checkpoint1.GlobalTransform.origin;
                Label prNote = GetNode<Label>("../../prNote");
                Label speedrunNote = GetNode<Label>("../../speedrunNote");
                if (speedRun){
                    if (prNote.Text == "" || (float)speedrunNote.Get("time") < (float)speedrunNote.Get("prtime")){
                        prNote.Text = "PR: " + speedrunNote.Text;
                        speedrunNote.Set("prtime", speedrunNote._Get("time"));
                    }
                }
                else{
                    _drawTip("Speedrun mode activated!\nPress " + controlNames["speedrun"] + " to restart speedrun");
                    speedRun = true;
                    Timer textTimer = (Timer)GetNode("../../tipNote/Timer");
                    if (!textTimer.IsStopped()) textTimer.Stop();
                    textTimer.Start(2);
                }
                speedrunNote.Set("time", 0);
                speedrunNote.Set("timerOn", false);
                break;
            case "tips":
                string str = (string)area.Get("note");
                switch(area.Name){
					case "moveTip": str = controlNames["roll"] + " to Roll"; break;
                    case "jumpTip": str = "Press and release\n" + controlNames["jump"] + " to Boing"; break;
                    case "bounceTip": str = "Try boinging quickly to\nBoing combo!"; break;
                    case "camTip": str = controlNames["camera"] + "\nto pan the camera"; break;
                    case "restartTip": 
                        str = controlNames["restart"] + " to restart from checkpoint\n" +
                        controlNames["speedrun"] + " to start speedrun mode";
                        break;
                    case "boingTip": str = "Hold " + controlNames["jump"] + " for longer\nto charge a Boing"; break;
                    case "boingTip2": str = "Pro tip: when in doubt, charge your Boings!"; break;
                    case "dashTip": str = controlNames["dash"] + " to Dash"; break;
                    case "slideTip": str = "Hold " + controlNames["jump"] + "\nafter Dashing to slide"; break;
                    //case "slideTip2": str = "You can slide super far on glass!"; break;
                    case "crashTip": str = "Dash in mid-air\nto Crash"; break;
                    case "crashTip2": str = "Jump after a Crash\nto Crashboing"; break;
                    case "crashTip3": str = "Try charging a big Crashboing\nto get over the wall!"; break;
                    case "wallTip": str = "Charge a Boing on a wall\nto Wallboing"; break;
                    case "wallTip2": str = "There are a couple ways to get up this..."; break;
                    case "shiftTip": str = "Roll down slopes to go fast!"; break;
                    case "shiftTip2": str = "Boing off ramps at high speeds the last\nsecond to get some air!"; break;
                    case "part1Tip": str = "Grats on making it this far. You got it!"; break;
                    case "part3Tip": str = "Take your time..."; break;
                    case "part4Tip": str = "Try dashing into the wall and\ncharge a Wallboing off of it!"; break;
                    case "endTip": str = "That's all for now. Good job!\nTravel down to restart in speedrun mode!"; break;
                    case "targetingTip": str = "Push " + controlNames["target"] + " to lock on enemies!\n" +
                    controlNames["camera"] + " to change target"; break;
                    case "welcomeTip": str = "Welcome to the\nBoing Boing Bros prototype!"; break;
                    case "tutorialTip": str = "Enter the tube above to do the\nAdvanced movement trial!"; break;
                    case "pyramidTip": str ="This way to the new Pyramid Zone!\nPress H to return here at anytime"; break;
                }
                if (str != "") _drawTip(str);
                break;
            case "camerasets":
                bool typeArea = area.Name.Contains("Area");
                if (typeArea || (bool)camera.Get("autoBuffer") == true){ //have triggered the buffer (to make it only triggerable via a direction)
                    string[] tag = (typeArea) ? area.Name.Split("cameraArea") : area.Name.Split("cameraset");
                    tag = tag[1].Split("-");
                    bool neg = tag[1].Contains("n");
                    string[] dir = (neg) ? tag[1].Split("n") : new string[] {tag[1]};
                    if (tag[0] != "R" || tag[0] != "L") camera.Call("_auto_move_camera", dir[0].ToInt(), tag[0], neg); //height camera
                    else{ //not height camera, check timer buffer
                        Timer setDelay = (Timer)GetNode("Position3D/playerCam/setDelay");
                        if (setDelay.IsStopped()) camera.Call("_auto_move_camera", dir[0].ToInt(), tag[0], neg);
                    }
                }
                break;
            case "camerabuffers":
                camera.Set("autoBuffer",true);
                Timer bufferTimer = (Timer)GetNode("Position3D/playerCam/bufferTimer");
                bufferTimer.Stop();
                bufferTimer.Start(1);
                break;
            case "boingPoints":
                int newBp = (area.Name.BeginsWith("5")) ? 5 : 1;
                bp += newBp;
                int bpTotal = (int)globals.Get("bpTotal");
                // bpNote.Text = bp.ToString() + " / " + bpTotal.ToString() + " BP";
                int oldUnspent = bpUnspent;
                int totalBp = bpSpent + bpUnspent;
                while(newBp > 0 && totalBp < 90){
                    bpUnspent ++;
                    newBp --;
                    totalBp = bpSpent + bpUnspent;
                }
                if (bpSpent < 90){
                    do{
                        statUI.Call("_check_PresetList", bpSpent, bpUnspent);
                    } while (bpUnspent > 0 && bpSpent < 90 && (int)statUI.Get("bpPreset") > 0);
                }
                // GD.Print("done");
                area.QueueFree();
                if (bp >= bpTotal) _drawTip("You've collected all the Boing Points!\n... Go touch grass");
                break;
        }
    }
}

public void _collisionDamage(Spatial collisionNode){
    Godot.Collections.Array groups = collisionNode.GetGroups();
    int damage, vulnerableClass; //0: none, 1: just crash, 2: killed by dash and crash, 3: just dash
    bool doShake = true;
    bool notCrashing = (!dashing || weight <= baseWeight);
    float vecx, vecz, power;
    Vector3 launch;
    for (int i = 0; i < groups.Count; i++){
        switch(groups[i].ToString()){
            case("goons"):
                #region
                if (invincible) return;
                vulnerableClass = (int)collisionNode.Get("vulnerableClass");
                if (vulnerableClass == 0) return;
                damage = (int)collisionNode.Get("damage");
                Vector3 vel = (Vector3)collisionNode.Get("velocity");
                power = (damage / baseWeight) * .5F;
                if (dashing || (sliding && boing != 0)){
                    if (notCrashing && vulnerableClass > 1){
                        collisionNode.Call("_launch", collisionBaseScale * (10 + (speed * friction * .1F)), new Vector3(direction_ground.x, 0, direction_ground.y));
                        float weightPowerMod = 1 - (baseWeight * .3F);
                        if (weightPowerMod > 1) weightPowerMod = 1;
                        power *= weightPowerMod; //don't send me as far
                        doShake = false;
                    }
                    else if (!notCrashing && vulnerableClass != 3 && GlobalTransform.origin.y > collisionNode.GlobalTransform.origin.y){
                        collisionNode.Call("_squish", collisionBaseScale * ((baseWeight * 10) * .5F)); //crashing
                        power *= 1.5F;
                        doShake = false;
                    }
                }
                if (vel != Vector3.Zero) launch = new Vector3(vel.x * power * .3F, 0, vel.z * power * .3F);
                else{
                    vecx = (velocity.x != 0) ? velocity.x : .1F;
                    vecz = (velocity.z != 0) ? velocity.z : .1F;
                    launch = new Vector3(vecx * -1, 0, vecz * -1).Normalized();    
                }
                _launch(launch, power, notCrashing);
                invincible = true;
                invincibleTimer.Start(.1F);
                if (doShake) camera.Call("_shakeMove", 10, damage * .1F, 0);
                break;
                #endregion
            case("moles"):
                #region
                if (invincible) return;
                vulnerableClass = (int)collisionNode.Get("vulnerableClass");
                if (vulnerableClass == 0) return;
                power = (15 / baseWeight) * .5F;
                Timer springTimer = (Timer)collisionNode.Get("springTimer");
                if (!springTimer.IsStopped()) power *= 2;
                if (dashing || (sliding && boing != 0)){
                    if (notCrashing && vulnerableClass > 1){
                        collisionNode.Call("_launch", collisionBaseScale * (10 + (speed * friction * .1F)), new Vector3(direction_ground.x, 0, direction_ground.y));
                        float weightPowerMod = 1 - (baseWeight * .5F);
                        if (weightPowerMod > 1) weightPowerMod = 1;
                        power *= weightPowerMod; //don't send me as far
                        doShake = false;
                    }
                    else if (!notCrashing && vulnerableClass != 3 && GlobalTransform.origin.y > collisionNode.GlobalTransform.origin.y){
                        collisionNode.Call("_squish", collisionBaseScale * ((baseWeight * 10) * .5F)); //crashing
                        power *= 1.5F;
                        doShake = false;
                    }
                    dashTimer.Stop();
                    dashing = false;
                    weight = baseWeight;
                    //speed = speedBase;
                }
                vecx = (velocity.x != 0) ? velocity.x : .1F;
                vecz = (velocity.z != 0) ? velocity.z : .1F;
                launch = new Vector3(vecx * -1 * power * 2, 0, vecz * -1 * power * 2).Normalized();
                _launch(launch, power, notCrashing);
                invincible = true;
                invincibleTimer.Start(.1F);
                if (doShake) camera.Call("_shakeMove", 10, 2.5F, 0);
                break;
                #endregion
            case("thumps"):
                #region
                if (!collisionNode.Name.BeginsWith("push")){
                    if (smushed) break;
                    boingCharge = false;
                    boingTimer.Stop();
                    preBoingTimer.Stop();
                    Position3D thumpBottomPos = (Position3D)collisionNode.Get("bottomPosition");
                    Translation = new Vector3(Translation.x, thumpBottomPos.GlobalTransform.origin.y, Translation.z);
                    damage = (int)collisionNode.Get("damage");
                    smushed = true;
                    collisionScales[0] = 1.7F * collisionBaseScale;
                    collisionScales[1] = .4F * collisionBaseScale;
                    collisionScales[2] = collisionScales[0];
                    skinBody.Scale = new Vector3(collisionScales[0], collisionScales[1], collisionScales[2]);
                    mesh.Translation = new Vector3(mesh.Translation.x, -.05F, mesh.Translation.z);
                    smushTimer.Stop();
                    smushTimer.Start(damage);
                }
                else{
                    string name = collisionNode.Name;
                    power = (!name.Contains("slow")) ? 15 : 5;
                    vecx = -1;
                    vecz = -1;
                    if (name.BeginsWith("pushOO")){ vecx = 1; vecz = 1; } //retard logic
                    else if (name.BeginsWith("pushON")){ vecx = 1; vecz = -1; }
                    else if (name.BeginsWith("pushNO")){ vecx = -1; vecz =1; }
                    launch = new Vector3(vecx * power, 0, vecz * power);
                    _launch(launch, power, notCrashing);
                }
                break;
                #endregion
            case("projectiles"):
                #region
                //if ((bool)collisionNode.Get("invincible")) return;
                if (invincible) return;
                damage = (int)collisionNode.Get("damage");
                Vector3 trajectory = ((Vector3)collisionNode.Get("trajectory") - collisionNode.GlobalTransform.origin).Normalized();
                power = (damage / baseWeight);
                launch = new Vector3(trajectory.x * -1 * power, 0, trajectory.z * -1 * power);
                _launch(launch, power, true);
                collisionNode.Call("_on_DeleteTimer_timeout");
                invincible = true;
                invincibleTimer.Start(.1F);
                camera.Call("_shakeMove", damage * .5F, damage * .15F, 0);
                break;
                #endregion
            case("lavas"):
                #region
                //if ((bool)collisionNode.Get("invincible")) return;
                if (invincible) return;
                damage = 25;
                _launch(Vector3.Zero, damage, false);
                camera.Call("_shakeMove", 10, damage * .1F, 0);
                invincible = true;
                invincibleTimer.Start(.1F);
                break;
                #endregion
            }
    }
}

public void _on_hitBox_area_exited(Area area){
    Godot.Collections.Array groups = area.GetGroups();
    for (int i = 0; i < groups.Count; i++){
        switch(groups[i].ToString()){
            case "checkpoints":
                if (speedRun && area.Name == "checkpointSpawn"){
                    Label speedrunNote = GetNode<Label>("../../speedrunNote");
                    speedrunNote.Set("timerOn", true);
                    speedrunNote.Set("time", 0);
                }
                break;
            case "killboxes": if (area.Name.BeginsWith("delay")) deathtimer.Stop(); break;
            case "tips": 
                Timer textTimer = (Timer)GetNode("../../tipNote/Timer");
                if (!textTimer.IsStopped()) textTimer.Stop();
                textTimer.Start(2);
                break;
            case "camerasets":
                if (!area.Name.Contains("Modal")) continue;
                camera.Call("_auto_move_camera", 0, "O", false);
                break;
        }
    }
}

public void _setStat(int points, string stat){
    if (stat == "max" || stat == "mario" || stat == "reset"){
        points = (stat == "reset") ? 0 : (stat == "max") ? 30 : 15; 
        traction = points;
        speedPoints = points;
        weightPoints = points;
        sizePoints = points;
        bouncePoints = points;
        energyPoints = points;
        _setStat(98, "traction");
        _setStat(98, "speed");
        _setStat(98, "weight");
        _setStat(98, "size");
        _setStat(98, "bounce");
        _setStat(98, "energy");
        return;
    }
    int oldPoints = 0;
    int hax = 0;
    bool overflow = false;
    if (Mathf.Abs(points) > 90){
        if (points == 99){
            points = 5 * Mathf.Sign(points);
            hax = 1;
        }
        else hax = 2;
    }
    else if (bpUnspent < points){
        points = bpUnspent;
    }
    switch (stat){
        case "traction":
            oldPoints = traction;
            if (oldPoints == 30 && hax < 1) return;
            if (hax < 2) traction += points;
            overflow = traction > 30;
            traction = Mathf.Clamp(traction, 0, 30);
            int oldDirSize = dirSize;
            dirSize = 10 - (traction / 5);
            if (dirSize != oldDirSize){
                float meanX = myMath.array2dMean(dir,0);
                float meanZ = myMath.array2dMean(dir,1);
                dir = new float[2,dirSize];
                for (int i = 0; i < dirSize; i++){
                    dir[0,i] = meanX;
                    dir[1,i] = meanZ;
                }
            }
            // GD.Print(dirSize);
            // GD.Print("traction " + traction.ToString());
            statLabels[stat].Value = traction;
            break;
        case "speed":
            oldPoints = speedPoints;
            if (oldPoints == 30 && hax < 1) return;
            if (hax < 2) speedPoints += points;
            overflow = speedPoints > 30;
            speedPoints = Mathf.Clamp(speedPoints, 0, 30);
            bool setSpd = speed == speedBase;
            speedBase = 14 + myMath.roundTo(speedPoints * .134F, 10);
            if (setSpd) speed = speedBase;
            // GD.Print("speed " + speedBase.ToString());
            statLabels[stat].Value = speedPoints;
            break;
        case "weight":
            oldPoints = weightPoints;
            if (oldPoints == 30 && hax < 1) return;
            if (hax < 2) weightPoints += points;
            overflow = weightPoints > 30;
            weightPoints = Mathf.Clamp(weightPoints, 0, 30);
            bool setWeight = weight == baseWeight;
            baseWeight = 1.2F + myMath.roundTo(weightPoints * .04F, 100);
            if (setWeight) weight = baseWeight;
            // GD.Print("weight " + baseWeight.ToString());
            dashSpeed = 20 + (.1F * weightPoints);
            statLabels[stat].Value = weightPoints;
            break;
        case "size":
            oldPoints = sizePoints;
            if (oldPoints == 30 && hax < 1) return;
            if (hax < 2) sizePoints += points;
            overflow = sizePoints > 30;
            sizePoints = Mathf.Clamp(sizePoints, 0, 30);
            float increase = .6F + myMath.roundTo(sizePoints * (.015F + (sizePoints * .000165F)), 100);
            collisionBaseScale = increase;
            collisionBaseScale = myMath.roundTo(collisionBaseScale, 100);
            hitBoxShape.Scale = new Vector3(collisionBaseScale + .05F, collisionBaseScale + .05F, collisionBaseScale + .05F);
            float scaleRatio = (collisionBaseScale - .6F) / .6F;
            floorCast.Scale = new Vector3(1, 2 + myMath.roundTo((1.9F * scaleRatio), 100), 1);
            floorCast.Translation = new Vector3(0, 1 + myMath.roundTo(scaleRatio * .9F, 100), 0);
            collisionShape.Scale = new Vector3(collisionBaseScale, collisionBaseScale, collisionBaseScale);
            shadowCast.Set("shadowScale", collisionBaseScale);
            if (IsOnFloor() || yvelocity == -1){
                Translation = new Vector3(Translation.x, floorCast.GetCollisionPoint().y + (collisionBaseScale * 2), Translation.z);
                for (int c = 0; c < 3; c++) collisionScales[c] = collisionBaseScale;
            }
            // float scaleRatio = myMath.roundTo((increase - .6F) / ((.6F + myMath.roundTo(sizePoints * (.015F + (30 * .000165F)), 100)) - .6F) * 30, 100);
            // floorCast.Scale = new Vector3(1, 2 + myMath.roundTo((.065F * scaleRatio), 100), 1);
            // floorCast.Translation = new Vector3(0, 1 + (scaleRatio * .032F), 0);
            statLabels[stat].Value = sizePoints;
            // GD.Print(scaleRatio);
            // GD.Print("collisionBaseScale " + collisionBaseScale.ToString());
            // GD.Print("hitbox scale " + hitBoxShape.Scale.ToString());
            // GD.Print("floorCast scale " + floorCast.Scale.ToString());
            // GD.Print("floorCast translationY " + floorCast.Translation.y.ToString());
            break;
        case "bounce":
            oldPoints = bouncePoints;
            if (oldPoints == 30 && hax < 1) return;
            if (hax < 2) bouncePoints += points;
            overflow = bouncePoints > 30;
            bouncePoints = Mathf.Clamp(bouncePoints, 0, 30);
            jumpForce = 11.5F + myMath.roundTo(bouncePoints * .2F, 10);
            bounceComboCap = 3 + Mathf.FloorToInt(bouncePoints / 5);
            statLabels[stat].Value = bouncePoints;
            // GD.Print("jumpForce " + jumpForce.ToString());
            // GD.Print("bounceComboCap " + bounceComboCap.ToString());
            break;
        case "energy":
            
            break;
    }
    if (hax < 1){
        if (overflow) points = 30 - oldPoints;
        bpSpent += points;
        bpUnspent -= points;
    }
}

public void _drawMoveNote(string text){
    if (moveNote == null) return;
    moveNote.Text = text;
    moveNote.Set("alpha", 2.5);
    moveNote.AddColorOverride("font_color", new Color(1,1,1,1));
}

public void _drawTip(string text){
    Timer textTimer = (Timer)GetNode("../../tipNote/Timer");
    if (!textTimer.IsStopped()) textTimer.Stop();
    tipNote.Text = text;
}

}