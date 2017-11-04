/*
//Whip's Lift Off Script v3 - 11/4/17 
___________________________________________________________________________________
/// DESCRIPTION ///

This script automatically throttles your FORWARD thrusters to optimize
fuel useage while exiting planetary influence. This script also takes 
control of the gyroscopes on the grid in order to ensure that you are on
the optimal escape trajectory. Once your ship leaves the gravity well, the
code will execute a "Turn 'N Burn" to make you come to a stop. 

Make sure 
to point your reference ship controller in the direction you wish to take off!
___________________________________________________________________________________
/// SETUP ///

1. Place a programmable block with this program loaded in it

2. Place a timer block with actions:
    - "trigger now" itself
    - "start" itself
    - run this program with no argument
    
3. Place a ship controller (cockpit, flight seat, remote, etc...)
    - add the phrase "Reference" into it's name somewhere
    - The thrusters that propel the craft FORWARD will automatically be grabbed
    
4. Enter the argument "start" to begin lift-off
___________________________________________________________________________________
/// ARGUMENTS ///

start : starts lift-off procedure

stop : stops lift-off procedure
*/

string shipControllerName = "Reference"; 
double ascentSpeed = 95; 

//===================================================================== 
//                NO TOUCHEY BELOW THIS LINE!!!111!1! 
//===================================================================== 

IMyShipController reference; 
List<IMyThrust> mainThrusters = new List<IMyThrust>(); 
List<IMyGyro> gyros = new List<IMyGyro>(); 

bool isSetup = false; 
bool shouldLiftOff = false; 

const double updatesPerSecond = 10; 
const double updateTime = 1d / updatesPerSecond; 
const double refreshInterval = 10; 
const double minAlignmentTicks = 25;

double currentRefreshTime = 141; 
double timeSinceLastUpdate = 141; 
double alignmemtTicks = 0;

PID velocityPID = new PID(1, .2, .4, -10, 10, updateTime); 

Program() 
{ 
    isSetup = GrabBlocks(); 
} 

void Main(string arg) 
{ 
    Echo($"{alignmemtTicks}");
    //Argument handling 
    switch (arg.ToLower()) 
    { 
        case "start": 
            shouldLiftOff = true; 
            alignmemtTicks = 0;
            break; 

        case "stop": 
            shouldLiftOff = false; 
            DisableGyroOverride(gyros); 
            ApplyThrust(mainThrusters, 0); 
            alignmemtTicks = 0;
            break; 
    } 

    Echo($"Lift Off?: {shouldLiftOff}"); 

    currentRefreshTime += Runtime.TimeSinceLastRun.TotalSeconds; 
    timeSinceLastUpdate += Runtime.TimeSinceLastRun.TotalSeconds; 

    if (!isSetup || currentRefreshTime >= refreshInterval) 
    { 
        isSetup = GrabBlocks(); 
        currentRefreshTime = 0; 
    } 

    if (!isSetup) 
        return; 

    if (timeSinceLastUpdate >= updateTime) 
    { 
        if (shouldLiftOff) 
            LiftOff(); 

        timeSinceLastUpdate = 0; 
    } 
} 

void LiftOff() 
{ 
    var mass = reference.CalculateShipMass().PhysicalMass; 
    var thrustForce = CalculateMaxThrust(mainThrusters); 
    var maxAcceleration = thrustForce / mass; 
    var velocityVec = reference.GetShipVelocities().LinearVelocity; 
    var speed = reference.GetShipSpeed(); 

    var gravityVec = reference.GetNaturalGravity(); 

    Vector3D alignmentVector = new Vector3D(0, 0, 0); 

    

    if (gravityVec.LengthSquared() == 0) //outside gravity well 
    { 
        //execute retro-burn 
        alignmentVector = -velocityVec; 
        
        double deviationAngle = VectorAngleBetween(reference.WorldMatrix.Forward, alignmentVector); 
        Echo($"{deviationAngle}");

        ApplyThrust(mainThrusters, 0); 

        if (deviationAngle < 5.0 / 180.0 * Math.PI) 
        {
            if (alignmemtTicks > minAlignmentTicks)
            {
                reference.DampenersOverride = true; 
                
            }
            else
            {
                reference.DampenersOverride = false; 
                alignmemtTicks++;
            }
        }
        else
            reference.DampenersOverride = false; 

        if (speed < 1) 
        { 
            DisableGyroOverride(gyros); 
            reference.DampenersOverride = true; 
            shouldLiftOff = false; 
            alignmemtTicks = 0;
            return; 
        } 
    } 
    else 
    { 
        var lateralVelocity = velocityVec - VectorProjection(velocityVec, gravityVec);
        alignmentVector = -gravityVec - lateralVelocity / 10;

        var equilibriumThrustPercentage = gravityVec.Length() / maxAcceleration * 100; 
        var thrustAdjustment = velocityPID.Control(ascentSpeed - speed * Math.Sign(velocityVec.Dot(-gravityVec))); 
        ApplyThrust(mainThrusters, equilibriumThrustPercentage + thrustAdjustment); 
    } 

    double pitch, yaw = 0; 
    GetRotationAngles(alignmentVector, reference.WorldMatrix.Forward, reference.WorldMatrix.Left, reference.WorldMatrix.Up, out yaw, out pitch); 

    double pitchSpeed = Math.Round(pitch, 2); 
    double yawSpeed = Math.Round(yaw, 2); 

    ApplyGyroOverride(pitchSpeed, yawSpeed, 0, gyros, reference); 
} 

void ApplyThrust(List<IMyThrust> thrusters, double thrustOverride) 
{ 
    foreach (var block in thrusters) 
        block.SetValue("Override", (float)thrustOverride); 
} 

bool GrabBlocks() 
{ 
    List<IMyShipController> shipControllers = new List<IMyShipController>(); 
    GridTerminalSystem.GetBlocksOfType(shipControllers, x => x.CustomName.Contains(shipControllerName)); 

    if (shipControllers.Count == 0) 
    { 
        Echo($"Error: No ship controller named '{shipControllerName}' were found!"); 
        return false; 
    } 

    reference = shipControllers[0]; 

    GridTerminalSystem.GetBlocksOfType(mainThrusters, x => x.WorldMatrix.Forward == reference.WorldMatrix.Backward); 
    if (mainThrusters.Count == 0) 
    { 
        Echo($"Error: No lift-off thrusters were found!"); 
        return false; 
    } 

    GridTerminalSystem.GetBlocksOfType(gyros); 
    if (gyros.Count == 0) 
    { 
        Echo($"Error: No gyros were found!"); 
        return false; 
    } 

    return true; 
} 

double CalculateMaxThrust(List<IMyThrust> thrusters) 
{ 
    double thrustSum = 0; 
    foreach (var block in thrusters) 
    { 
        thrustSum += block.MaxEffectiveThrust; 
    } 
    return thrustSum; 
} 

void DisableGyroOverride(List<IMyGyro> gyros) 
{ 
    foreach (var block in gyros) 
        block.GyroOverride = false; 
} 

//Whip's ApplyGyroOverride Method v9 - 8/19/17
void ApplyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed, List<IMyGyro> gyro_list, IMyTerminalBlock reference) 
{ 
    var rotationVec = new Vector3D(-pitch_speed, yaw_speed, roll_speed); //because keen does some weird stuff with signs 
    var shipMatrix = reference.WorldMatrix;
    var relativeRotationVec = Vector3D.TransformNormal(rotationVec, shipMatrix); 

    foreach (var thisGyro in gyro_list) 
    { 
        var gyroMatrix = thisGyro.WorldMatrix;
        var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gyroMatrix)); 
 
        thisGyro.Pitch = (float)transformedRotationVec.X;
        thisGyro.Yaw = (float)transformedRotationVec.Y; 
        thisGyro.Roll = (float)transformedRotationVec.Z; 
        thisGyro.GyroOverride = true; 
    } 
}

//Whip's Get Rotation Angles Method v5 - 5/30/17 
void GetRotationAngles(Vector3D v_target, Vector3D v_front, Vector3D v_left, Vector3D v_up, out double yaw, out double pitch) 
{ 
    //Dependencies: VectorProjection() | VectorAngleBetween() 
    var projectTargetUp = VectorProjection(v_target, v_up); 
    var projTargetFrontLeft = v_target - projectTargetUp; 

    yaw = VectorAngleBetween(v_front, projTargetFrontLeft); 
    pitch = VectorAngleBetween(v_target, projTargetFrontLeft); 

    //---Check if yaw angle is left or right   
    //multiplied by -1 to convert from right hand rule to left hand rule 
    yaw = -1 * Math.Sign(v_left.Dot(v_target)) * yaw; 

    //---Check if pitch angle is up or down     
    pitch = Math.Sign(v_up.Dot(v_target)) * pitch; 

    //---Check if target vector is pointing opposite the front vector 
    if (pitch == 0 && yaw == 0 && v_target.Dot(v_front) < 0) 
    { 
        yaw = Math.PI; 
    } 
} 

Vector3D VectorProjection(Vector3D a, Vector3D b) //proj a on b    
{ 
    Vector3D projection = a.Dot(b) / b.LengthSquared() * b; 
    return projection; 
} 

double VectorAngleBetween(Vector3D a, Vector3D b) //returns radians  
{ 
    if (a.LengthSquared() == 0 || b.LengthSquared() == 0) 
        return 0; 
    else 
        return Math.Acos(MathHelper.Clamp(a.Dot(b) / a.Length() / b.Length(), -1, 1)); 
} 

//Whip's PID controller class v4 - 8/27/17
public class PID
{
    double _kP = 0;
    double _kI = 0;
    double _kD = 0;
    double _integralDecayRatio = 0;
    double _lowerBound = 0;
    double _upperBound = 0;
    double _timeStep = 0;
    double _errorSum = 0;
    double _lastError = 0;
    bool _firstRun = true;
    bool _integralDecay = false;
    public double Value {get; private set;}

    public PID(double kP, double kI, double kD, double lowerBound, double upperBound, double timeStep)
    {
        _kP = kP;
        _kI = kI;
        _kD = kD;
        _lowerBound = lowerBound;
        _upperBound = upperBound;
        _timeStep = timeStep;
        _integralDecay = false;
    }
    
    public PID(double kP, double kI, double kD, double integralDecayRatio, double timeStep)
    {
        _kP = kP;
        _kI = kI;
        _kD = kD;
        _timeStep = timeStep;
        _integralDecayRatio = integralDecayRatio;
        _integralDecay = true;
    }

    public double Control(double error)
    {
        //Compute derivative term
        var errorDerivative = (error - _lastError) / _timeStep;
        
        if (_firstRun)
        {
            errorDerivative = 0;
            _firstRun = false;
        }

        //Compute integral term
        if (!_integralDecay)
        {
            _errorSum += error * _timeStep;

            //Clamp integral term
            if (_errorSum > _upperBound)
                _errorSum = _upperBound;
            else if (_errorSum < _lowerBound)
                _errorSum = _lowerBound;
        }
        else
        {
            _errorSum = _errorSum * (1.0 - _integralDecayRatio) + error * _timeStep;
        }

        //Store this error as last error
        _lastError = error;

        //Construct output
        this.Value = _kP * error + _kI * _errorSum + _kD * errorDerivative;
        return this.Value;
    }

    public void Reset()
    {
        _errorSum = 0;
        _lastError = 0;
        _firstRun = true;
    }
}
