using Godot;
using System;
using System.Collections.Generic;
using MyMath;
public class basicPlayer : KinematicBody{

#region basic movement variables
Vector2 direction_ground;
Vector3 velocity;
float gravity = 23.0F;
float jumpforce = 12.0F;
float yvelocity = -1;
static float bounceBase = .7F;
float bounce = bounceBase;
int bounceCombo = 0;
int bounceComboCap = 3;
float bouncethreshold = 3; //how much yvelocity you need to bounce
float basejumpwindow = 0;
float jumpwindow = 0;
float ang = 0;
float angTarget = 0;
bool camLock = false;
bool angDelayFriction = true; //player friction modifies angTarget lerping or not
bool wallb = false;
float wallbx = 0;
float wallby = 0;
bool idle = true;
bool dashing = false;
int hasJumped = 0; //set to 2 (strong) in jump function, 1 in boing timer timeout (soft) (distinction for leeway jumping) CANCRASH == 2 == HASJUMPED
int bounceDashing = 0;
bool walldashing = false; //for speed boost after dashing into a wall
bool rolling = true;
bool moving = false;
static int dirSize = 13;
float[,] dir = new float[2,dirSize];
float[] stickDir = new float[] {0,0};
float[] moveDir = new float[] {0,0};
float friction = 0;
float wallFriction = 0;
static float speedCap = 12;
float speed = speedCap;
int traction = 50;
float[] tractionList = new float[101];
static float baseWeight = 1.2F;
float weight = baseWeight;
float dashSpeed = speedCap * 1.5F;
float boing = 0;
bool boingCharge = false;
bool boingDash = false; //use dashSpeed in boing slide (turned on in isRolling() and turned off in boing jump and boing timer)
bool squishSet = false; //only run the mesh squish settings once in _squishNScale
float[] squishReverb = new float[] {0,1,0}; //squishReverb[2] was a bool in gd
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
Dictionary<string, string> controlNames = new Dictionary<string, string>(){
    {"roll", ""}, {"jump",""}, {"dash",""}, {"camera",""}, {"restart",""}, {"speedrun",""}
};
#endregion

#region onready variables (init in _Ready())
float collisionBaseScale = .6F;
float[] collisionScales;
Timer boingTimer;
Timer preBoingTimer;
Timer dashtimer;
Timer deathtimer;
MeshInstance mesh;
MeshInstance shadow;
Spatial collisionShape;
RayCast floorCast;
RayCast leewayCast;
RayCast shadowCast;
Area checkpoint;
Camera camera;
Label moveNote;
Label tipNote;
Label speedrunNote;
Label prNote;
Spatial lockOn = null;

#endregion

public override void _Ready(){
    #region load nodes
    boingTimer = GetNode<Timer>("boingTimer");
    preBoingTimer = GetNode<Timer>("preBoingTimer");
    dashtimer = GetNode<Timer>("DashTimer");
    deathtimer = GetNode<Timer>("hitBox/deathTimer");
    mesh = GetNode<MeshInstance>("CollisionShape/BallSkin");
    shadow = GetNode<MeshInstance>("shadowCast/shadowSkin");
    collisionShape = GetNode<Spatial>("CollisionShape");
    floorCast = GetNode<RayCast>("floorCast");
    leewayCast = GetNode<RayCast>("leewayCast");
    shadowCast = GetNode<RayCast>("shadowCast");
    checkpoint = GetNode<Area>("../checkpoints/checkpoint1");
    camera = GetNode<Camera>("Position3D/playerCam");
    moveNote = GetNode<Label>("../../moveNote");
    tipNote = GetNode<Label>("../../tipNote");
    speedrunNote = GetNode<Label>("../../speedrunNote");
    prNote = GetNode<Label>("../../prNote");
    #endregion

    #region initialize data structures
    //collisionScales
    collisionScales = new float[] {collisionBaseScale,collisionBaseScale,collisionBaseScale};
    //traction
    for (int x = 0; x < tractionList.Length; x++){
        tractionList[x] = (float)((Math.Pow(1.0475D,x)-1)*((Math.Pow(0.01F*x,25)*.29F)+.7F));
    }
    //control dictionary
    string[] controllerStr = new string[6];
    if (Input.IsJoyKnown(0) == false){ //keyboard mouse
        controllerStr[0] = "WASD or Arrow Keys";
        controllerStr[1] = "Space";
        controllerStr[2] = "Shift or C";
        controllerStr[3] = "Q & E or Z & X";
        controllerStr[4] = "R";
        controllerStr[5] = "T";
    }
    else{ //controller
        controllerStr[0] = "Left Joystick";
        controllerStr[3] = "L & R or Right Joystick";
        controllerStr[4] = "Start";
        if (Input.GetJoyName(0).BeginsWith("x") || Input.GetJoyName(0).BeginsWith("X")){ //xbox
            controllerStr[1] = "the A Button";
            controllerStr[2] = "the X Button";
            controllerStr[5] = "Back";
        }
        else{ //other controller
            controllerStr[1] = "Bottom Face Button";
            controllerStr[2] = "Left Face Button";
            controllerStr[5] = "Back";
        }
    }
    controlNames["roll"] = controllerStr[0];
    controlNames["jump"] = controllerStr[1];
    controlNames["dash"] = controllerStr[2];
    controlNames["camera"] = controllerStr[3];
    controlNames["restart"] = controllerStr[4];
    controlNames["speedrun"] = controllerStr[5];
    #endregion
    //collisionShape.RotationDegrees = new Vector3(collisionShape.RotationDegrees.x,45,collisionShape.RotationDegrees.z);
    ang = (-1 * Rotation.y);
    yvelocity = -1;
    
}

public override void _PhysicsProcess(float delta){ //run physics
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
            collisionShape.Scale = new Vector3(collisionBaseScale*(1+warpRate),collisionBaseScale*(1-warpRate),collisionBaseScale*(1+warpRate));
            collisionScales[0] = .9F;
            collisionScales[1] = .3F;
            collisionScales[2] = collisionScales[0];
            squishSet = false;
            yvelocity *= -1;
        }
        _applyShift(delta, isGrounded);
        if (!isGrounded || collisionScales[0] != collisionBaseScale) _squishNScale(delta, new Vector3(0,0,0), true);
    }
    else _isBoinging(delta);
    _turnDelay();
    _lockOn(false, delta);
}

public void _controller(float delta){
    if (!idle){ //update direction
        stickDir[0] = Mathf.Round(Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"));
        stickDir[1] = Mathf.Round(Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up"));
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
        if (angTarget != 0) direction_ground = new Vector2(moveDir[0],moveDir[1]).Rotated(angTarget).Normalized(); //alter direction vector
    }
    if (notWall){
        xvel = direction_ground.x * mod;
        yvel = direction_ground.y * mod;
    }
    else{
        if (moving && (Mathf.Sign(wallbx) != Mathf.Sign(direction_ground.x) || Mathf.Sign(wallby) != Mathf.Sign(direction_ground.y))){ //wallb air control
            wallFriction += .01F * delta * ((traction * .25F) + 40);
            if (wallFriction > 1) wallFriction = 1;
        }
        if (wallFriction != 0) MoveAndSlide(new Vector3(direction_ground.x*(speed*wallFriction),0,direction_ground.y*(speed*wallFriction)),Vector3.Up,true);
        xvel = wallbx * (1 - wallFriction);
        yvel = wallby * (1 - wallFriction);
    }
    velocity = new Vector3(xvel, yvelocity, yvel); //apply velocity
    MoveAndSlide(velocity, Vector3.Up, true);
    if (xvel != 0 || yvel != 0) _rotateMesh(xvel,yvel,delta);
}

public void _applyFriction(float delta){
    int current = dirSize - 1;
    int i;
    for (i = 0; i < current; i++){
        dir[0,i] = dir[0,i+1];
        dir[1,i] = dir[1,i+1];
    }
    dir[0,current] = stickDir[0];
    dir[1,current] = stickDir[1];
    dir[0,current] = myMath.array2dMean(dir, 0);
    dir[1,current] = myMath.array2dMean(dir, 1);
    int signdir = 0;
    if (moving){
        moveDir[0] = myMath.array2dMean(dir,0);
        moveDir[1] = myMath.array2dMean(dir,1);
        for (i = 0; i < 2; i++){
            if (!float.IsNaN(stickDir[i])) signdir = Math.Sign(stickDir[i]);
            else signdir = Math.Sign(moveDir[i]);
            if (signdir != 0 && Math.Sign(moveDir[i]) != signdir){
                dir[i,current] += (tractionList[traction] * signdir) * delta;
            }
        }
    }
    else if (moveDir[0] != 0 || moveDir[1] != 0){ //stop at .015 friction if not moving
        for (i = 0; i < 2; i++){
            if (moveDir[i] == 0) continue;
            if (Math.Abs(moveDir[i]) > .015F){ //slowly reduce speed (friction)
                moveDir[i] = myMath.array2dMean(dir,i);   
                signdir = Math.Sign(dir[i,current]); //apply shift
                dir[i,current] -= (tractionList[traction] * .08F * signdir) * baseWeight * delta;
                if ((signdir == 1 && dir[i,current] < 0) || (signdir == -1 && dir[i,current] > 0)){
                    dir[i,current] = 0;
                }
            }
            else moveDir[i] = 0;
        }
    }
    float absx = Math.Abs(moveDir[0]);
    float absy = Math.Abs(moveDir[1]);
    friction = (absx > absy) ? absx : absy;
    if (friction > 1) friction = 1;
}

public void _applyShift(float delta, bool isGrounded){
    bool floorCastTouching = floorCast.IsColliding();
    if (shiftedDir != 0 && !shiftedLinger){ //on shift
        if (floorCastTouching){ //make sure you're still on ground
            float grav = .05F + (baseWeight * .01F);
            float fric = friction;
            Vector3 shift = floorCast.GetCollisionNormal();
            if (shiftedDir > 0){ //going down
                if (!dashing) shiftedBoost[0] += delta * (baseWeight * 10); //charge up
                else shiftedBoost[0] += delta * (baseWeight * 20);
                if (shiftedBoost[0] > 30) shiftedBoost[0] = 30;
                shiftedBoost[1] = shiftedBoost[0]; //records the max shiftedBoost[0]
                if (shift.y != 1){ //make sure we're not passing a flat vector
                    bool record = true;
                    if (shiftedLastYNorm == 0) shiftedLastYNorm = shift.y;
                    else if (Mathf.Round(shift.y * 10) > Mathf.Round(shiftedLastYNorm * 10)) record = false;
                    else shiftedLastYNorm = shift.y;
                    if (record){ //save the last rolling vector
                        fric *= (shiftedBoost[0] * (1 - shiftedLastYNorm));
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
        }
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
                    dashSpeed = speedCap * 1.5F;
                }
				shiftedDir = 0;
				rampSlope = 0;
				shiftedLastYNorm = 0;
            }
            else{ //shifted ground
                if (shiftedDir != 0){
                    shiftedDir = (lastTranslationY - Translation.y) * delta * 60;
                    if (shiftedDir > 0){ //going down slope
                        shiftedSticky = -1;
                        rampSlope = 0;
                    }
                    else if (shiftedLinger && colliderNode.IsInGroup("ramps")){
                        if (friction > .7 && rampSlope < (1 - GetFloorNormal().y)){ //get downward Y normal
                            rampSlope = (1 - GetFloorNormal().y) * friction;
                        }
                    }
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
	idle = false;
    if (!walldashing){ //if landing, cancel dash
        if (dashing && (shiftedDir == 0)){
            bounce = bounceBase;
            dashtimer.Stop();
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
                    dashtimer.Stop();
                    dashing = false;
                    speed = speedCap;
                }
            }
        }
    }
    else if (yvelocity < -1){ //falling (to bounce)
        if (yvelocity < 0 && yvelocity > -1) yvelocity = -1;
        if ((yvelocity * bounce) * -1 > bouncethreshold && yvelocity != -1){
            if ((GetSlideCollision(0).Normal.y > .95F && GetSlideCollision(0).Normal.y < 1.05F) || boingCharge || bounceDashing == 2){
                if (bounceDashing != 2){ //not crashing (bounceDashing == 2 is crashing)
                    boing = yvelocity * bounce;
                    bounceDashing = 0;
                    if (bounce != bounceBase) bounceCombo = 0; //not full bounce
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
                bounce -= weight * .1F;
            }
            else if (shiftedDir != 0) yvelocity *= bounce * -1; //on a shift
        }
        else{ //dont bounce up
            yvelocity = -1;
            bounce = bounceBase;
            bounceCombo = 0;
        }
    }
    else if (shiftedDir > 1) yvelocity = -1; //prevents slope cheesing
}

public void _isAirborne(float delta){
	yvelocity -= (gravity * weight) * delta; //gravity
	rolling = false;
	_capSpeed(22,50);
}

public void _isWall(float delta){
    yvelocity -= (gravity * weight) * delta; //gravity
    bool isWall = false;
    if (GetSlideCount() > 0){
        Node colliderNode = (Node)GetSlideCollision(0).Collider;
        isWall = colliderNode.IsInGroup("walls");
        if (!isWall && colliderNode.IsInGroup("mobs")) return; //if enemy, leave
    }
    if (isWall || dashing){
        wallb = true;
        wallFriction = 0;
        wallbx = GetSlideCollision(0).Normal.x * 2;
        wallby = GetSlideCollision(0).Normal.z * 2;
        _alterDirection(GetSlideCollision(0).Normal);
        if (dashing && !walldashing){
            dashtimer.Stop();
            dashing = false;
            weight = baseWeight;
            speed = speedCap;
            bounceDashing = 1;
            walldashing = true;
        }
        if (isWall){
            float fricMod = (friction > .4F) ? friction * .75F : .4F * .75F;
            boing = speed * .7F * fricMod;
            if (boing < 3) boing = 3;
            jumpwindow = 0;
            basejumpwindow = Mathf.Round(boing * 6);
            boingTimer.Stop();
            boingTimer.Start(boing * .1F);
        }
    }
    else if (!isWall){
        wallb = false;
        boingTimer.Stop();
    }
}

public void _isBoinging(float delta){
    bool isWall = IsOnWall();
    if (isWall){
        Node colliderNode = (Node)GetSlideCollision(0).Collider;
        isWall = colliderNode.IsInGroup("walls");
    }
    if (floorCast.IsColliding() || isWall){
        if (jumpwindow < basejumpwindow) jumpwindow += 60 * delta;
        else jumpwindow = basejumpwindow;
        if (!wallb && shiftedDir == 0){
            float jumpratio = jumpwindow / basejumpwindow;
            float offset = (speed * bounceBase) / basejumpwindow;
            if (floorCast.IsColliding()){
                Node floorCastNode = (Node)floorCast.GetCollider();
                if (floorCastNode.IsInGroup("slides")){
                    offset *= 2;
                    jumpratio *= baseWeight * .015F;
                }
            }
            if (offset > 1) offset = 1;
            stickDir[0] *= (1 - jumpratio);
            stickDir[1] *= (1 - jumpratio);
            _applyFriction(delta);
            float spd = speed * friction * (bounceBase * offset);
            if (boingDash){
                float dashSpd = (dashSpeed*friction*(dashSpeed/speedCap))*(bounceBase*offset);
                if (dashSpd > spd) spd = dashSpd;
                if (boingCharge && spd > 4 && jumpwindow == 60 * delta) _drawMoveNote("slide");
            }
            velocity = new Vector3(direction_ground.x*spd, yvelocity, direction_ground.y*spd);
            MoveAndSlide(velocity, Vector3.Up, true);
        }
        if (GetSlideCount() > 0 && collisionScales[0] != collisionShape.Scale.x){
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
        //collisionShape.RotationDegrees = new Vector3(collisionShape.RotationDegrees.x,0,collisionShape.RotationDegrees.z);
    }
}

public void _squishNScale(float delta, Vector3 squishNormal, bool reset){
    float rate = delta * 6;
    if (!reset && !squishSet){
        float squish = squish = boing /  22;
        if (squish > .9F) squish = .9F;
        squishReverb[0] = 0;
        squishReverb[1] = 1;
        if (IsOnFloor() || (shiftedDir != 0 && Mathf.Round(squishNormal.y) != 0)){ //on floor OR on shift and not on a wall
            collisionScales[0] = collisionBaseScale * (1 + (squish * .7F)); //x
            collisionScales[1] = collisionBaseScale * (1 - (squish * .7F)); //y
            collisionScales[2] = collisionScales[0]; //z
        }
        else if (Mathf.Round(squishNormal.y) == 0){ //on a wall
            Vector3 rotation = collisionShape.RotationDegrees;
            //collisionShape.RotationDegrees = new Vector3(rotation.x,0,rotation.z);
            squish *= 1.5F;
            float add = collisionBaseScale * (1 + (squish * .7F));
            float sub = collisionBaseScale * (1 - (squish * .7F));
            int camAng = (int)camera.Call("findClosestCamSet", RotationDegrees.y);
            collisionScales[1] = add; //go ahead and set y now
            float normx = Mathf.Round(squishNormal.x);
            float normz = Mathf.Round(squishNormal.z);
            bool flip = false;
            if (normx == 0 || normz == 0){ //45 degree flip
                //collisionShape.RotationDegrees = new Vector3(rotation.x,45,rotation.z);
                flip = (Math.Sign(Math.Abs(normx)) == 1 && Math.Sign(Math.Abs(normz)) == 0);
            }
            else flip = (normx == 1 && normz == 1) || (normx == -1 && normz == -1);
            if (flip){
                float temp = add;
                add = sub;
                sub = temp;
            }
            if (camAng == 1 || camAng == 3){
                collisionScales[0] = sub; //x
                collisionScales[2] = add; //z
            }
            else if (camAng == 0 || camAng == 2){
                collisionScales[0] = add; //x
                collisionScales[2] = sub; //z
            }
        }
        squishSet = true;
    }
    else if (reset && squishReverb[0] != squishReverb[1]){ //BbOoOiIinNg! jiggle
        if (squishReverb[0] > .75F) squishReverb[0] = .75F;
        float mod = 1;
        for (int i = 0; i < 3; i++){
            if (i == 0){
                if (squishReverb[2] == 0){ //wall bounce proc is false
                    if (collisionShape.Scale.x < collisionBaseScale) mod += squishReverb[0];
                    else mod -= squishReverb[0];
                }
                else{ //if wallbounce, alter jiggle pattern
                    squishReverb[2] = 0;
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
    collisionShape.Scale =
    new Vector3(Mathf.Lerp(collisionShape.Scale.x, collisionScales[0], rate),
    Mathf.Lerp(collisionShape.Scale.y, collisionScales[1], rate),
    Mathf.Lerp(collisionShape.Scale.z, collisionScales[2], rate));
    Vector3 translations = mesh.Translation;
    if (IsOnFloor() && shiftedDir == 0){ //crush the ball into the floor
        if (translations.y > 0) mesh.Translation = new Vector3(translations.x, 0, translations.z);
        Vector3 collisionscales = collisionShape.Scale;
        if (collisionscales.y < collisionBaseScale){
            float meshTarg = 0 - ((collisionBaseScale * collisionBaseScale * 12) *
            (collisionBaseScale - collisionscales.y) / collisionBaseScale);
            meshTarg *= ((basejumpwindow*.5F)/jumpforce < 1) ? ((basejumpwindow*.5F)/jumpforce) : 1;
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
    if (!IsOnFloor() && (!wallb || !IsOnWall())){ //airborne
        if (collisionShape.Scale.x > (collisionScales[0] * (1 - squishReverb[0]))
        && collisionShape.Scale.x < (collisionScales[0] * (1 + squishReverb[0]))){
            squishReverb[0] -= .02F;
            if (squishReverb[0] < 0) squishReverb[0] = 0;
            if (squishReverb[0] == 0) collisionShape.Scale = new Vector3(collisionScales[0],collisionScales[1],collisionScales[2]);
        }
    }
    else if (basejumpwindow != 0 && jumpwindow/basejumpwindow >= 1){
        collisionScales[0] = collisionShape.Scale.x; //windowed
        collisionScales[1] = collisionShape.Scale.y;
        collisionScales[2] = collisionShape.Scale.z;
    }
    else if (jumpwindow == 0 && boing == 0 && (IsOnFloor() || yvelocity == -1)) collisionShape.Scale = new Vector3(collisionBaseScale,collisionBaseScale,collisionBaseScale);
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

public void _capSpeed(float high, float low){
    if (yvelocity < -low) yvelocity = -low;
	else if (yvelocity > high) yvelocity = high;
}

public void _turnDelay(){
    if (angTarget == 0) return;
    camLock = true;
    if (lockOn == null && Math.Sign(ang) != Math.Sign(angTarget)){
        float add = myMath.findDegreeDistance(ang,angTarget);
        if ((string)camera.Get("turnDir") == "left") add *= -1;
        ang = angTarget + add;
    }
    if (angDelayFriction) ang = Mathf.LerpAngle(ang,angTarget,.015F + ((tractionList[traction] * .0007F)));
    else ang = Mathf.LerpAngle(ang,angTarget,.015F + ((tractionList[0] * .0007F)));
    if (myMath.roundTo(ang,10) == myMath.roundTo(angTarget,10)){
        ang = angTarget;
        angTarget = 0;
        angDelayFriction = true;
        camLock = false;
    }
}

public void _alterDirection(Vector3 alterNormal){
    Vector3 wallbang = new Vector3(moveDir[0], 0, moveDir[1]).Bounce(alterNormal);
    int camArray = (int)camera.Call("findClosestCamSet", RotationDegrees.y);
    if (camArray == 1 || camArray == 3) wallbang.z *= -1;
    else if (camArray == 0 || camArray == 2) wallbang.x *= -1;
    #region set angTarget (DEFUNCT)
    // int[] camAngs = new int[] {135,45,-45,-135};
    // float camAng = Mathf.Deg2Rad(camAngs[camArray]) * -1;
    // if (myMath.roundTo(ang, 100) != myMath.roundTo(camAng, 100)){
    //     float lastAng = myMath.roundTo(Mathf.Rad2Deg(ang), 100) + 180;
    //     float targAng = camAngs[camArray] + 180;
    //     string turnDir = "";
    //     if (camArray == 1 || camArray == 2) turnDir = (lastAng < targAng) ? "right" : "left";
    //     else if (camArray == 0) turnDir = (lastAng < targAng && lastAng > 225) ? "right" : "left";
    //     else turnDir = (lastAng < targAng || lastAng > 315) ? "right" : "left";
    //     angTarget = Rotation.y * -1;
    //     angDelayFriction = true;
    //     GD.Print(targAng);
    //     ang = camAng;
    // }
    #endregion
    for (int i = 0; i < dirSize; i++){
        dir[0,i] = wallbang.z;
        dir[1,i] = wallbang.x;
    }
}

public void _lockOn(bool triggerScript, float delta){
    if (lockOn == null){
        if (camLock == false && moveDir[0] != 0 && !wallb){//(Math.Abs(moveDir[0]) > .05F){
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
    Vector3 target = lockOn.Translation;
    float oldRot = Rotation.y;
    LookAt(new Vector3(target.x, Translation.y, target.z), Vector3.Up);
    if (angTarget != 0 && !dashing){
        Rotation = new Vector3(Rotation.x, Mathf.LerpAngle(ang * -1, Rotation.y, .015F + (tractionList[traction] * .0007F)), Rotation.z);
        angTarget = Rotation.y * -1;
    }
    else{
        Rotation = new Vector3(Rotation.x, Mathf.LerpAngle(oldRot, Rotation.y, .015F + (tractionList[traction] * .0007F)), Rotation.z);
        ang = Rotation.y * -1;
    }
}

public void _jump(){
    boingCharge = true;
    if (boing != 0){ //boing jump
        yvelocity = boing;
        boingDash = false;
        _squishNScale((gravity * .017F), new Vector3(0,0,0), true);
        squishSet = false;
        boing = 0;
        boingTimer.Stop();
        //collisionShape.RotationDegrees = new Vector3(collisionShape.RotationDegrees.x,0,collisionShape.RotationDegrees.z);
        bool slopeSquish = false;
        if (shiftedDir != 0){ //boing jump off a slope
            Vector3 wallbang = velocity.Bounce(floorCast.GetCollisionNormal());
            Vector2 wallang = new Vector2(wallbang.x, wallbang.z);
            _alterDirection(floorCast.GetCollisionNormal());
            wallb = true;
            wallFriction = 0;
            slopeSquish = true;
            wallbx = wallang.x * .5F;
            wallby = wallang.y * .5F;
        }
        float lastyvel = yvelocity + (gravity * .017F); //yvel times rough delta estimate
        if (lastyvel > 20) lastyvel = 20;
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
        if (slopeSquish) _drawMoveNote(chargedNote + "slopejump");
        float nuyvel = 0;
        if (bounceDashing != 1){ //regular boingjump
            jumpwindow = (jumpwindow / basejumpwindow * .75F) + bounceBase;
            nuyvel = myMath.roundTo((jumpforce*(1 + combo * .035F)) * jumpwindow, 10);
            bounceCombo += 1;
            if (!wallb && !slopeSquish){
                if (chargedNote == "") _drawMoveNote("boingjump");
                else _drawMoveNote("chargedjump");
            }
            else{
                if (!slopeSquish){
                    _drawMoveNote(chargedNote + "walljump");
                    wallbx *= (jumpforce * (.2F + (.1F * jumpwindow)));
                    wallby *= (jumpforce * (.2F + (.1F * jumpwindow)));
                    nuyvel *= .6F + (.2F * jumpwindow);
                }
                squishReverb[2] = 1; //set wall jiggle to true
            }
            yvelocity = (nuyvel > lastyvel) ? nuyvel : lastyvel; //never go below a dirbble boing
        }
        else{ //crashing or walldashing
            jumpwindow = (jumpwindow / basejumpwindow) + bounceBase;
            bounceDashing = 0;
            nuyvel = myMath.roundTo((jumpforce * (1 + bounceComboCap * .1F)) * jumpwindow,10);
            if (wallb){ //if off wall
                if (!slopeSquish){
                    _drawMoveNote(chargedNote + "crash walljump");
                    wallbx *= (jumpforce * (.4F + (.25F * jumpwindow)));
                    wallby *= (jumpforce * (.4F + (.25F * jumpwindow)));
                    nuyvel *= .4F + (.25F * jumpwindow);
                }
                nuyvel *= windowRatio * .65F;
                lastyvel *= windowRatio * .65F;
                squishReverb[2] = 1; //set wall jiggle to true
            }
            else if (!slopeSquish) _drawMoveNote(chargedNote + "crashjump");
            if (lastyvel > nuyvel || lastyvel == 20) nuyvel += lastyvel * .2F;
            yvelocity = (nuyvel > lastyvel) ? nuyvel : lastyvel; //never go below a dirbble boing
        }
        squishReverb[0] = yvelocity * .035F;
        _capSpeed(22, 50);
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
	boingCharge = false;
	_drawMoveNote("jump");
	yvelocity = jumpforce - Mathf.Round(((Translation.y - collisionBaseScale) - leewayCast.GetCollisionPoint().y) * 7) * .5F;
	squishReverb[0] = yvelocity * .035F;
	preBoingTimer.Stop();
	hasJumped = 2;
	if (shiftedDir != 0) shiftedSticky = 0;
}

public void _dash(){
if ((moving || (moveDir[0] != 0 || moveDir[1] != 0)) && !dashing){
        if (leewayCast.IsColliding() && hasJumped == 0 && shiftedDir == 0){ // on ground and not on shift
            yvelocity = jumpforce * .5F;
            _drawMoveNote("dash");
        }
        else if (hasJumped > 0 && !IsOnWall()){ // in air and not on shift
            dashtimer.Stop();
            weight = baseWeight * 3;
            shiftedDir = 0; // don't need to apply shifted gravity anymore if doing this
            _drawMoveNote("crash");
        }
        else if (shiftedDir != 0){ // is on shift
            dashtimer.Start(.3F);
            dashSpeed = speedCap * 2;
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
        dashtimer.Stop();
        dashing = false;
        weight = baseWeight;
        speed = speedCap;
    }
    if (alterDir){
        wallb = true;
        wallbx = launchVec.x;
        wallby = launchVec.z;
        wallFriction = 0;
        _alterDirection(launchVec.Normalized());
    }
    yvelocity = power;
    hasJumped = yvelocity >= (bounceBase * jumpforce) ? 1 : hasJumped; //soft has jumped else what it was
    _squishNScale((gravity * .017F), new Vector3(0,0,0), true);
    squishSet = false;
    squishReverb[0] = power * .08F;
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
        Area checkpnt = (Area)GetNode("../checkpoints/checkpoint1");
        Translation = checkpnt.Translation;
        if (!speedRun){
            _drawTip("Speedrun mode activated!\nPress T to restart speedrun");
            speedRun = true;
            Timer textTimer = (Timer)GetNode("../../tipNote/Timer");
            if (!textTimer.IsStopped()) textTimer.Stop();
            textTimer.Start(2);
        }
    }
    else if (@event.IsActionPressed("add_traction")){
        if (traction < 100) traction += 10;
        else traction = 0;
        GD.Print("traction " + traction.ToString());
    }
    else if (@event.IsActionPressed("sub_traction")){
        if (traction > 0) traction -= 10;
        else traction = 100;
        GD.Print("traction " + traction.ToString());
    }
    else if (@event.IsActionPressed("slow-mo")){
        if (!slowMo) Engine.TimeScale = .3F;
        else Engine.TimeScale = 1;
        slowMo = !slowMo;
    }
    else if (@event.IsActionPressed("debug_restart")) GetTree().ReloadCurrentScene();
    else if (@event.IsActionPressed("end_game")) GetTree().Quit();
    else if (@event.IsActionPressed("fullscreen")) OS.WindowFullscreen = !OS.WindowFullscreen;
}

public void _on_DashTimer_timeout(){
    dashing = false;
    walldashing = false;
    dashSpeed = speedCap * 1.5F;
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
        squishReverb[0] = boing * .12F;
        squishReverb[2] = 1; //proc wall wiggle
    }
    hasJumped = yvelocity >= (bounceBase * jumpforce) ? 1 : hasJumped; //soft has jumped else what it was
    boingDash = false;
    jumpwindow = 0;
    boing = 0;
    //collisionShape.RotationDegrees = new Vector3(collisionShape.RotationDegrees.x,0,collisionShape.RotationDegrees.z);
}

public void _on_preBoingTimer_timeout(){
    boing = jumpforce;
    jumpwindow = 0;
    basejumpwindow = Mathf.Round(boing * 1.2F);
}

public void _dieNRespawn(){
    idle = true;
    yvelocity = 1;
    stickDir[0] = 0;
    stickDir[1] = 0;
    weight = baseWeight;
    dashing = false;
    dashtimer.Stop();
    walldashing = false;
    dashSpeed = speedCap * 1.5F;
    wallb = false;
    hasJumped = 0;
    shiftedDir = 0;
    shiftedLinger = false;
    boingDash = false;
    preBoingTimer.Stop();
    _squishNScale(gravity * .017F, new Vector3(0,0,0), true);
    squishSet = false;
    boing = 0;
    //collisionShape.RotationDegrees = new Vector3(collisionShape.RotationDegrees.x,0,collisionShape.RotationDegrees.z);
    boingCharge = false;
    boingTimer.Stop();
    for (int i = 0; i < dirSize; i++){
        dir[0,i] = 0;
        dir[1,i] = 0;
    }
    Translation = checkpoint.GlobalTransform.origin;
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

public void _on_hitBox_area_entered(Area area){
    Godot.Collections.Array groups = area.GetGroups();
    for (int i = 0; i < groups.Count; i++){
        switch(groups[i].ToString()){
            case "mobs": _collisionDamage((Spatial)area.Owner); break;
            case "hurts": _collisionDamage(area); break;
            case "checkpoints": checkpoint = area; break;
            case "killboxes":
                if (!area.Name.BeginsWith("delay")) _dieNRespawn();
                else if (deathtimer.IsStopped()) deathtimer.Start(2);
                break;
            case "warps":
                Area checkpoint1 = (Area)GetNode("../checkpoints/checkpoint1");
                Translation = checkpoint1.Translation;
                if (speedRun){
                    if (prNote.Text == "" || (float)speedrunNote.Get("time") < (float)speedrunNote.Get("prtime")){
                        prNote.Text = "PR: " + speedrunNote.Text;
                        speedrunNote.Set("prtime", speedrunNote._Get("time"));
                    }
                }
                else{
                    _drawTip("Speedrun mode activated!\nPress T to restart speedrun");
                    speedRun = true;
                    Timer textTimer = (Timer)GetNode("../../tipNote/Timer");
                    if (!textTimer.IsStopped()) textTimer.Stop();
                    textTimer.Start(2);
                }
                speedrunNote.Set("time", 0);
                speedrunNote.Set("timerOn", false);
                break;
            case "tips":
                string str = "";
                switch(area.Name){
					case "moveTip": str = controlNames["roll"] + " to Roll"; break;
                    case "jumpTip": str = "Press " + controlNames["jump"] + " to Jump"; break;
                    case "bounceTip": str = "Try jumping right when you hit the\nground to Boingjump"; break;
                    case "camTip": str = controlNames["camera"] + "\nto pan the camera"; break;
                    case "restartTip": 
                        str = controlNames["restart"] + " to restart from checkpoint\n" +
                        controlNames["speedrun"] + " to start speedrun mode";
                        break;
                    case "boingTip": str = "Hold " + controlNames["jump"] + "\nto Chargejump"; break;
                    case "boingTip2": str = "Pro tip: Holding " + controlNames["jump"] + " will help you\nget the most out of your jumps!"; break;
                    case "dashTip": str = controlNames["dash"] + " to Dash"; break;
                    case "slideTip": str = "Hold " + controlNames["jump"] + " after dashing\nto Slide"; break;
                    //case "slideTip2": str = "You can slide super far on glass!"; break;
                    case "crashTip": str = "Dash in mid-air\nto Crash"; break;
                    case "crashTip2": str = "Jump after a Crash\nto Crashjump"; break;
                    case "crashTip3": str = "Try charging a big Crashjump\nto get over the wall!"; break;
                    case "wallTip": str = "Jump after hitting a wall\nto Walljump"; break;
                    case "wallTip2": str = "You can charge walljumps too!"; break;
                    case "shiftTip": str = "Roll down slopes to go fast!"; break;
                    case "shiftTip2": str = "Jump off ramps at high speeds\nto get some air!"; break;
                    case "part1Tip": str = "Grats on making it this far. You got it!"; break;
                    case "part3Tip": str = "Take your time..."; break;
                    case "part4Tip": str = "Try dashing into the wall and\ncharge a Walljump off of it!"; break;
                    case "endTip": str = "That's all for now. Good job!\nTravel down to restart in speedrun mode!"; break;
                }
                if (str != "") _drawTip(str);
                break;
            case "camerasets":
                if ((bool)camera.Get("autoBuffer") == true){ //have triggered the buffer (to make it only triggerable via a direction)
                    string[] tag = area.Name.Split("cameraset");
                    tag = tag[1].Split("-");
                    if (tag[0] != "R" || tag[0] != "L") camera.Call("_auto_move_camera", tag[1].ToInt(), tag[0]); //height camera
                    else{ //not height camera, check timer buffer
                        Timer setDelay = (Timer)GetNode("Position3D/playerCam/setDelay");
                        if (setDelay.IsStopped()) camera.Call("_auto_move_camera", tag[1].ToInt(), tag[0]);
                    }
                }
                break;
            case "camerabuffers":
                camera.Set("autoBuffer",true);
                Timer bufferTimer = (Timer)GetNode("Position3D/playerCam/bufferTimer");
                bufferTimer.Stop();
                bufferTimer.Start(1);
                break;
        }
    }
}

public void _on_hitBox_area_exited(Area area){
    Godot.Collections.Array groups = area.GetGroups();
    for (int i = 0; i < groups.Count; i++){
        switch(groups[i].ToString()){
            case "checkpoints":
                if (speedRun && area.Name == "checkpoint1"){
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
        }
    }
}

public void _collisionDamage(Spatial collisionNode){
    Godot.Collections.Array groups = collisionNode.GetGroups();
    int damage;
    bool notCrashing = (!dashing || weight <= baseWeight);
    float vecx, vecz, power;
    Vector3 launch;
    for (int i = 0; i < groups.Count; i++){
        switch(groups[i].ToString()){
            case("goons"):
                #region
                if ((bool)collisionNode.Get("invincible")) return;
                damage = (int)collisionNode.Get("damage");
                Vector3 vel = (Vector3)collisionNode.Get("velocity");
                power = (damage / baseWeight) * (.4F + (friction * .1F));
                if (dashing){
                    if (notCrashing){
                        collisionNode.Call("_launch", power, new Vector3(direction_ground.x, 0, direction_ground.y));
                        float weightPowerMod = 1 - (baseWeight * .3F);
                        if (weightPowerMod > 1) weightPowerMod = 1;
                        power *= weightPowerMod; //don't send me as far
                    }
                    else{
                        collisionNode.Call("_squish", power); //crashing
                        power *= 1 + ((bounceBase + (baseWeight * .5F)) * .5F);
                    }
                }
                if (vel != Vector3.Zero) launch = new Vector3(vel.x * power * .3F, 0, vel.z * power * .3F);
                else{
                    vecx = (velocity.x != 0) ? velocity.x : .1F;
                    vecz = (velocity.z != 0) ? velocity.z : .1F;
                    launch = new Vector3(vecx * -1, 0, vecz * -1).Normalized();    
                }
                _launch(launch, power, notCrashing);
                break;
                #endregion
            case("moles"):
                #region
                if ((bool)collisionNode.Get("invincible")) return;
                power = baseWeight * .5F * 25;
                Timer springTimer = (Timer)collisionNode.Get("springTimer");
                if (!springTimer.IsStopped()) power *= 3;
                if (dashing){
                    if (notCrashing){
                        collisionNode.Call("_launch", power, new Vector3(direction_ground.x, 0, direction_ground.y));
                        float weightPowerMod = 1 - (baseWeight * .5F);
                        if (weightPowerMod > 1) weightPowerMod = 1;
                        power *= weightPowerMod; //don't send me as far
                    }
                    else{
                        collisionNode.Call("_squish", power); //crashing
                        power *= 1 + ((bounceBase + (baseWeight * .5F)) * .5F);
                    }
                    dashtimer.Stop();
                    dashing = false;
                    weight = baseWeight;
                    speed = speedCap;
                }
                vecx = (velocity.x != 0) ? velocity.x : .1F;
                vecz = (velocity.z != 0) ? velocity.z : .1F;
                launch = new Vector3(vecx * -1 * power * 2, 0, vecz * -1 * power * 2).Normalized();
                _launch(launch, power, notCrashing);
                break;
                #endregion
            case("hurts"):
                #region
                //if ((bool)collisionNode.Get("invincible")) return;
                damage = (int)collisionNode.Get("power");
                Vector3 bltTrajectory = collisionNode.Translation.Normalized();
                power = baseWeight * .5F * 25;
                launch = new Vector3(bltTrajectory.x * power, 0, bltTrajectory.z * power);
                _launch(launch, power, true);
                collisionNode.Call("_on_DeleteTimer_timeout");
                break;
                #endregion
            }
    }
}

public void _drawMoveNote(string text){
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