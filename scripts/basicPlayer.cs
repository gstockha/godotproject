using Godot;
using System;
using System.Collections.Generic;
using MyMath;
public class basicPlayer : KinematicBody{
#region import scripts
//static myMath = (GDScript) GD.Load("res://scripts/math.gd");
//object mymath = (Godot.Object) myMath.New();

#endregion

#region basic movement variables
Vector2 direction_ground;
Vector3 velocity;
float gravity = 23.0F;
float jumpforce = 12.0F;
float yvelocity = -1;
static float bouncebase = .7F;
float bounce = bouncebase;
int bounceCombo = 0;
int bounceComboCap = 3;
float bouncethreshold = 3; //how much yvelocity you need to bounce
float basejumpwindow = 0;
float jumpwindow = 0;
float ang = 0;
float angTarget = 0;
float cameraFriction = 1; //apply to friction after moving camera
bool wallb = false;
float wallbx = 0;
float wallby = 0;
bool idle = true;
bool dashing = false;
bool canCrash = false; //set to true in jump function
int bouncedashing = 0;
bool walldashing = false; //for speed boost after dashing into a wall
bool rolling = true;
bool moving = false;
static int dirsize = 13;
float[,] dir = new float[2,dirsize];
float[] stickdir = new float[] {0,0};
float[] dragdir = new float[] {0,0};
float friction = 0;
static float speedCap = 10;
float speed = speedCap;
int traction = 50;
float[] tractionlist = new float[101];
static float baseweight = 1.2F;
float weight = baseweight;
float dashspeed = speedCap * 1.5F;
float boing = 0;
bool boingCharge = false;
bool boingDash = false; //use dashspeed in boing slide (turned on in isRolling() and turned off in boing jump and boing timer)
bool squishSet = false; //only run the mesh squish settings once in _squishNScale
bool squishGrow = true; //tells the _squishNScale script to keep growing
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

#region iables (init in _Ready())
float collisionBaseScale = .6F;
float[] collisionScales;
Timer boingTimer;
Timer preBoingTimer;
Timer dashtimer;
Timer deathtimer;
MeshInstance mesh;
Spatial collisionShape;
RayCast bottom;
Area checkpoint;
Camera camera;
Label moveNote;
Label tipNote;
Label speedrunNote;
Label prNote;
#endregion

public override void _Ready(){
    #region load nodes
    boingTimer = GetNode<Timer>("boingTimer");
    preBoingTimer = GetNode<Timer>("preBoingTimer");
    dashtimer = GetNode<Timer>("DashTimer");
    deathtimer = GetNode<Timer>("hitBox/deathTimer");
    mesh = GetNode<MeshInstance>("CollisionShape/BallSkin");
    collisionShape = GetNode<Spatial>("CollisionShape");
    bottom = GetNode<RayCast>("RayCast");
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
    for (int x = 0; x < tractionlist.Length; x++){
        tractionlist[x] = (float)((Math.Pow(1.0475D,x)-1)*((Math.Pow(0.01F*x,25)*.29F)+.7F));
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
        controllerStr[3] = "L & R";
        controllerStr[4] = "Start";
        if (Input.GetJoyName(0).BeginsWith("x") || Input.GetJoyName(0).BeginsWith("X")){ //xbox
            controllerStr[1] = "A Button";
            controllerStr[2] = "X Button";
            controllerStr[5] = "Back";
        }
        else{ //other controller
            controllerStr[1] = "Bottom Face Button";
            controllerStr[2] = "Left Face Button";
            controllerStr[5] = "Back";
        }
    }
    int pointer = 0;
    foreach(KeyValuePair<string,string> entry in controlNames){
        controlNames[entry.Key] = controllerStr[pointer];
        pointer ++;
    }
    #endregion
    
    ang = (-1 * Rotation.y);
    Vector3 rotationDeg = collisionShape.RotationDegrees;
    collisionShape.RotationDegrees = new Vector3(rotationDeg.x, ang, rotationDeg.z);
    yvelocity = -1;
    
}

public override void _Process(float delta){ //run physics
    if (boing == 0){ //not boinging
        _controller(delta);
        bool isGrounded = IsOnFloor() || (yvelocity == -1);
        if (isGrounded) _isRolling(delta);
        else if (!IsOnCeiling() && !IsOnWall()) _isAirborne(delta);
        else if (IsOnWall()) _isWall(delta);
        else if (yvelocity > 0) yvelocity *= -1;
        _applyShift(delta,isGrounded);
        if (squishGrow) _squishNScale(delta,bottom.GetCollisionNormal(),true);
    }
    else _isBoinging(delta);
    if (angTarget != 0){
        if (Math.Sign(ang) != Math.Sign(angTarget)){
            float add = MyMathClass.findDegreeDistance(ang,angTarget);
            string turnDir = (string)camera.Get("turnDir");
            if (turnDir == "left") add *= -1;
            ang = angTarget + add;
        }
        ang = Mathf.Lerp(ang,angTarget,.015F + ((tractionlist[traction] * .0007F)));
        if (Math.Round(ang,2,MidpointRounding.AwayFromZero) == Math.Round(angTarget,2,MidpointRounding.AwayFromZero)){
            ang = angTarget;
            angTarget = 0;
        }
    }
}

public void _controller(float delta){
    if (!idle){ //update direction
        stickdir[0] = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        stickdir[1] = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
    }
    moving = (stickdir[0] != 0 || stickdir[1] == 0);
    _applyFriction(delta);
    direction_ground = new Vector2(dragdir[0],dragdir[1]); //direction vector
    float xvel = 0;
    float yvel = 0;
    float mod = 0;
    bool notWall = true;
    if (rolling && !dashing) mod = (speed * friction); //rolling and moving
    else if (!wallb && !dashing) mod = (.9f * speed * friction); //airborne or idle
    else if (wallb && !dashing) notWall = false;
    else if (dashing) mod = dashspeed;
    if (notWall){
        xvel = direction_ground.x * mod;
        yvel = direction_ground.y * mod;
    }
    else{
        xvel = wallbx;
        yvel = wallby;
    }
    velocity = new Vector3(xvel, yvelocity, yvel); //apply velocity
    MoveAndSlide(velocity, Vector3.Up, true);
    if (xvel != 0 || yvel != 0) _rotateMesh(xvel,yvel,delta);
}

public void _applyFriction(float delta){
    int current = dirsize - 1;
    int i;
    for (i = 0; i < current; i++){
        dir[0,i] = dir[0,i+1];
        dir[1,i] = dir[1,i+1];
    }
    dir[0,current] = stickdir[0];
    dir[1,current] = stickdir[1];
    dir[0,current] = MyMathClass.array2dMean(dir, 0);
    dir[1,current] = MyMathClass.array2dMean(dir, 1);
    if (cameraFriction != 1){ //friction after turning camera
        dir[0,current] *= cameraFriction;
        dir[1,current] *= cameraFriction;
        cameraFriction += delta * (2 + (tractionlist[traction] * .04F));
        if (cameraFriction > 1) cameraFriction = 1;
    }
    int signdir;
    if (moving){
        dragdir[0] = MyMathClass.array2dMean(dir,0);
        dragdir[1] = MyMathClass.array2dMean(dir,1);
        for (i = 0; i < 2; i++){
            signdir = Math.Sign(stickdir[i]);
            if (signdir != 0 && Math.Sign(dragdir[i]) != signdir){
                dir[i,current] += (tractionlist[traction] * signdir) * delta;
            }
        }
    }
    else if (dragdir[0] != 0 || dragdir[1] != 0){ //stop at .015 friction if not moving
        for (i = 0; i < 2; i++){
            if (dragdir[i] == 0) continue;
            if (Math.Abs(dragdir[i]) > .015F){ //slowly reduce speed (friction)
                dragdir[i] = MyMathClass.array2dMean(dir,i);
                signdir = Math.Sign(dir[i,current]); //apply shift
                dir[i,current] -= (tractionlist[traction] * .08F * signdir) * baseweight * delta;
                if ((signdir == 1 && dir[i,current] < 0) || (signdir == -1 && dir[i,current] > 0)){
                    dir[i,current] = 0;
                }
            }
            else dragdir[i] = 0;
        }
        var absx = Math.Abs(dragdir[0]);
        var absy = Math.Abs(dragdir[1]);
        friction = (absx > absy) ? absx : absy;
        if (friction > 1) friction = 1;
    }
}

public void _applyShift(float delta, bool isGrounded){
    if (shiftedDir != 0 && !shiftedLinger){ //on shift
        float grav = .05F + (baseweight * .01F);
        float fric = friction;
        Vector3 shift = bottom.GetCollisionNormal();
        if (shiftedDir > 0){ //going down
            if (!dashing) shiftedBoost[0] += delta * (baseweight * 10); //charge up
            else shiftedBoost[0] += delta * (baseweight * 20);
            if (shiftedBoost[0] > 30) shiftedBoost[0] = 30;
            shiftedBoost[1] = shiftedBoost[0]; //records the max shiftedBoost[0]
            if (shift.y != 1){ //make sure we're not passing a flat vector
                bool record = true;
                if (shiftedLastYNorm == 0) shiftedLastYNorm = shift.y;
                else if (Math.Round(shift.y * 10, 2, MidpointRounding.AwayFromZero) >
                Math.Round(shiftedLastYNorm * 10, 2, MidpointRounding.AwayFromZero)){
                    record = false;
                }
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
    else if (shiftedBoost[0] > 0){ //shift linger
        shiftedLinger = true;
        shiftedBoost[0] -= delta * (baseweight * 10);
        if (shiftedBoost[0] < 0) shiftedBoost[0] = 0;
        float momentum = shiftedBoost[0] / shiftedBoost[1];
        if (rampSlope != 0){ //decrease the Y slope vector over time
            rampSlope -= (delta * (1 - shiftedBoost[1] * .01F) * (baseweight * .1F));
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
        else if (!bottom.IsColliding()){
            yvelocity -= (gravity * weight) * delta; //gravity
            shiftedSticky = 0;
        }
    }
}

public void _isRolling(float delta){
    jumpwindow = 0;
	wallb = false;
	canCrash = false;
	idle = false;
    if (!walldashing){ //if landing, cancel dash
        if (dashing && (shiftedDir == 0)){
            bounce = bouncebase;
            dashtimer.Stop();
            boingDash = true;
            dashing = false;
            if (weight != baseweight) bouncedashing = 2; //so you can't crash out of dash
            weight = baseweight;
        }
    }
    else walldashing = false;
    if (yvelocity == -1){ //not bouncing up
        if (moving) rolling = true;
        else{ //not pressing move keys
            if (friction > 0) rolling = true;
            else if (rolling){
                rolling = false;
                friction = 0;
                for (int i = 0; i < dirsize; i++){
                    dir[0,i] = 0;
                    dir[1,i] = 0;
                }
            }
        }
        if (IsOnWall() && rolling){
            Node colliderNode = (Node)GetSlideCollision(0).Collider;
            if (!colliderNode.IsInGroup("obstacles")){
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
            if (GetSlideCollision(0).Normal.y == 1 || boingCharge || bouncedashing == 2){
                if (bouncedashing != 2){ //not crashing (bouncedashing == 2 is crashing)
                    boing = yvelocity * bounce;
                    bouncedashing = 0;
                    if (bounce != bouncebase) bounceCombo = 0; //not full bounce
                }
                else{ //crashing
                    boing = yvelocity * (1 - (weight * .2F));
                    bouncedashing = 1;
                }
                boing *= -1;
                jumpwindow = 0;
                basejumpwindow = (float)Math.Round(boing * .12F,0,MidpointRounding.AwayFromZero);
                if (boingTimer.IsStopped()) boingTimer.Start(boing * .02F);
                rolling = false;
                bounce -= weight * .1F;
            }
            else if (shiftedDir != 0) yvelocity *= bounce * -1; //on a shift
        }
        else{ //dont bounce up
            yvelocity = -1;
            bounce = bouncebase;
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
    }
    if (isWall || dashing){
        wallb = true;
        Vector3 wallbang = velocity.Bounce(GetSlideCollision(0).Normal);
        Vector2 wallang = new Vector2(wallbang.x,wallbang.z);
        _alterDirection(GetSlideCollision(0).Normal);
        if (dashing && !walldashing){
            dashtimer.Stop();
            dashing = false;
            weight = baseweight;
            speed = speedCap;
            bouncedashing = 1;
            walldashing = true;
        }
        if (isWall){
            boing = speed * .7F * friction;
            jumpwindow = 0;
            basejumpwindow = (float)Math.Round(boing * 4, 0, MidpointRounding.AwayFromZero);
            if (boingTimer.IsStopped()) boingTimer.Start(boing * .07F);
        }
    }
}

public void _isBoinging(float delta){
    Node colliderNode = (Node)GetSlideCollision(0).Collider;
    if (bottom.IsColliding() || (colliderNode.IsInGroup("walls") && IsOnWall())){
        if (jumpwindow < basejumpwindow) jumpwindow += 60 * delta;
        else jumpwindow = basejumpwindow;
        if (!wallb && shiftedDir == 0){
            float jumpratio = jumpwindow / basejumpwindow;
            float offset = (speed * bouncebase) / basejumpwindow;
            if (bottom.IsColliding()){
                colliderNode = (Node)bottom.GetCollider();
                if (colliderNode.IsInGroup("slides")){
                    offset *= 2;
                    jumpratio *= baseweight * .015F;
                }
            }
            if (offset > 1) offset = 1;
            stickdir[0] *= (1 - jumpratio);
            stickdir[1] *= (1 - jumpratio);
            _applyFriction(delta);
            float spd = speed * friction * (bouncebase * offset);
            if (boingDash){
                float dashSpd = (dashspeed*friction*(dashspeed/speedCap))*(bouncebase*offset);
                if (dashSpd > spd) spd = dashSpd;
                if (boingCharge && spd > 4 && jumpwindow == 60 * delta) _drawMoveNote("slide");
            }
            velocity = new Vector3(direction_ground.x*spd, yvelocity, direction_ground.y*spd);
            MoveAndSlide(velocity, Vector3.Up, true);
        }
        if (GetSlideCount() > 0 && collisionScales[0] != collisionShape.Scale.x){
            if (!wallb) _squishNScale(delta, bottom.GetCollisionNormal(), false);
            else _squishNScale(delta,GetSlideCollision(0).Normal, false);
        }
    }
    else{
        boingDash = false;
        jumpwindow = 0;
        _squishNScale((gravity * 0.017F), bottom.GetCollisionNormal(), true);
        squishSet = false;
        boing = 0;
        boingTimer.Stop();
        Vector3 rotation = collisionShape.RotationDegrees;
        collisionShape.RotationDegrees = new Vector3(rotation.x,0,rotation.z);
    }
}

public void _squishNScale(float delta, Vector3 squishNormal, bool reset){
    float rate = delta * 60 * .1F;
    if (!reset && !squishSet){
        float squish = boing /  22;
        if (squish > .9) squish = .9F;
        squishReverb[0] = 0;
        squishReverb[1] = 1;
        squishGrow = true;
        if (IsOnFloor() || shiftedDir != 0){
            collisionScales[0] = collisionBaseScale * (1 + (squish * .7F)); //x
            collisionScales[1] = collisionBaseScale * (1 - (squish * .7F)); //y
            collisionScales[2] = collisionScales[0]; //z
        }
        else if (Math.Round(squishNormal.y,0,MidpointRounding.AwayFromZero) == 0){
            Vector3 rotation = collisionShape.RotationDegrees;
            collisionShape.RotationDegrees = new Vector3(rotation.x,0,rotation.z);
            squish *= 1.5F;
            float add = collisionBaseScale * (1 + (squish * .7F));
            float sub = collisionBaseScale * (1 - (squish * .7F));
            int camAng = (int)camera.Get("camsetarray");
            collisionScales[1] = add; //go ahead and set y now
            float normx = (float)Math.Round(squishNormal.x,0,MidpointRounding.AwayFromZero);
            float normz = (float)Math.Round(squishNormal.z,0,MidpointRounding.AwayFromZero);
            bool flip = false;
            if (normx == 0 || normz == 0){ //45 degree flip
                collisionShape.RotationDegrees = new Vector3(rotation.x,45,rotation.z);
                flip = (Math.Sign(Math.Abs(normx)) == 1 && Math.Sign(Math.Abs(normx)) == 0);
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
    else if (reset && squishReverb[0] != squishReverb[1]){ //BbOoOiIinNg!
        if (squishReverb[0] > .75F) squishReverb[0] = .75F;
        float mod = 1;
        for (int i = 0; i < 3; i++){
            if (i == 0){
                if (squishReverb[2] == 0){ //wall bounce proc is false
                    if (collisionShape.Scale.x < collisionBaseScale) mod += squishReverb[0];
                    else if (collisionShape.Scale.x > collisionBaseScale) mod -= squishReverb[0];
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
    float cshx = Mathf.Lerp(collisionShape.Scale.x, collisionScales[0], rate);
    float cshy = Mathf.Lerp(collisionShape.Scale.y, collisionScales[1], rate);
    float cshz = Mathf.Lerp(collisionShape.Scale.z, collisionScales[2], rate);
    collisionShape.Scale = new Vector3(cshz, cshy, cshz);
    Vector3 translations = mesh.Translation;
    if (IsOnFloor() && shiftedDir == 0){
        if (translations.y > 0) mesh.Translation = new Vector3(translations.x, 0, translations.z);
        Vector3 collisionscales = collisionShape.Scale;
        if (collisionscales.y < collisionBaseScale){
            float meshTarg = 0;
            meshTarg -= ((collisionBaseScale * collisionBaseScale * 10) *
            (collisionBaseScale - collisionscales.y) / collisionBaseScale);
            meshTarg *= ((basejumpwindow*.5F)/jumpforce < 1) ? ((basejumpwindow*.5F)/jumpforce) : 1;
            translations = mesh.Translation;
            if (meshTarg < translations.y) mesh.Translation = new Vector3(translations.x,meshTarg,translations.z);
        }
    }
    else if (translations.y != 0){
        if (translations.y < -.5F) mesh.Translation = new Vector3(translations.x, -.5F, translations.z);
        else if (translations.y < 0){
            float newY = translations.y + (delta * Math.Abs(yvelocity));
            mesh.Translation = new Vector3(translations.x, newY, translations.z);
        }
        else mesh.Translation = new Vector3(translations.x, 0, translations.z);
    }
    if (!IsOnFloor() && !IsOnWall()){ //airborne
        if ((collisionShape.Scale.x > collisionScales[0] * (1 - squishReverb[0]))
        && (collisionShape.Scale.x < collisionScales[0] * (1 + squishReverb[0]))){
            squishReverb[0] -= .02F;
            if (squishReverb[0] < 0) squishReverb[0] = 0;
            if (squishReverb[0] == 0) collisionShape.Scale = new Vector3(collisionScales[0],collisionScales[1],collisionScales[2]);
        }
    }
    else if ((basejumpwindow != 0 && jumpwindow/basejumpwindow >= 1) || (boing == 0 && (IsOnFloor() || yvelocity == -1))){
        if (boing == 0) collisionShape.Scale = new Vector3(collisionScales[0],collisionScales[1],collisionScales[2]);
        else{ //windowed
            collisionScales[0] = collisionShape.Scale.x;
            collisionScales[1] = collisionShape.Scale.y;
            collisionScales[2] = collisionShape.Scale.z;
        }
    }
    squishGrow = (collisionShape.Scale.x != collisionBaseScale);
}

public void _capSpeed(float high, float low){
    if (yvelocity < -low) yvelocity = -low;
	else if (yvelocity > high) yvelocity = high;
}
public void _rotateMesh(float xvel, float yvel, float delta){
    Vector3 meshRotation = mesh.Rotation;
    float angy = meshRotation.y;
    if (!wallb){
        Vector2 dragDir = new Vector2(dragdir[1], dragdir[0]);
        angy = dragDir.Angle();
    }
    float xv = Math.Abs(xvel);
    float yv = Math.Abs(yvel);
    float turn = (xv < yv) ? xv : yv;
    turn *= 1.5F * delta;
    mesh.Rotation = new Vector3(meshRotation.x + turn, angy, meshRotation.z);
}

public void _alterDirection(Vector3 alterNormal){

}

public void _jump(){

}

public void _normalJump(){

}

public void _dash(){

}

public override void _Input(InputEvent @event){

}

public void _on_DashtTimer_timeout(){

}

public void _on_boingTimer_timeout(){

}

public void _on_preBoingTimer_timeout(){

}

public void _dieNRespawn(){

}

public void _on_deathtimer_timeout(){

}

public void _on_hitBox_area_entered(Area area){

}

public void _on_hitBox_area_exited(Area area){

}

public void _drawMoveNote(string text){

}

public void _drawTip(string text){

}

}