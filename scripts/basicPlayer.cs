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
double[] tractionlist = new double[101];
static float baseweight = 1.2F;
float weight = baseweight;
float dashspeed = speedCap * 1.5F;
float boing = 0;
bool boingCharge = false;
bool boingDash = false; //use dashspeed in boing slide (turned on in isRolling() and turned off in boing jump and boing timer)
bool squishSet = false; //only run the mesh squish settings once in _squishNScale
bool squishGrow = true; //tells the _squishNScale script to keep growing
float[] squishRever = new float[] {0,1,0}; //squishReverb[2] was a bool in gd
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
        tractionlist[x] = ((Math.Pow(1.0475D,x)-1)*((Math.Pow(0.01F*x,25)*.29F)+.7F));
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
        ang = Mathf.Lerp(ang,angTarget,.015F + (((float)tractionlist[traction] * .0007F)));
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
}

public void _applyShift(float delta, bool isGrounded){

}

public void _isRolling(float delta){

}

public void _isAirborne(float delta){

}

public void _isWall(float delta){
    
}

public void _isBoinging(float delta){

}

public void _squishNScale(float delta, Vector3 squishNormal, bool reset){

}

public void _rotateMesh(float xvel, float yvel, float delta){

}

}
