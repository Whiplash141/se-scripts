
/*
 * D4RK3 54B3R's Skimmer Drive Mode script.
 * This script is a handy script for vehicles that are capable of either driving as a skimmer, or driving as a standard rover.
 * When it is run, it will automatically set the wheel settings appropriate for whichever the current mode is.
 * Run this script with the argument "toggle" to switch between rover mode or skimmer mode.
 * 
 * This is intended with the use of hydrogen skimmers, but can later be expanded to include those powered with atmo-thrusters.
 * 
 *
 *
 * Steps for setting up:
 * 1. Grid needs a programmable block with this on it, along with a Block Group of all of the drive wheels in rover mode.
 *
 * 2. (optional) You can also put on lights on the vehicle to reflect the drive mode of the vehicle.
	RED is for Skimmer mode, YELLOW is for Rover mode.
 *
 * 3. Stick this script on your control toolbar with the argument "toggle"
 * 
 * 4. The global variables below are essentially configuration variables. The names are mostly self-explanatory, and must match the desired settings and block group names.
 * 
 */

 
 
bool driveMode = true; //true for ROVER MODE, false for SKIMMER MODE

const string wheelsGroup = "Wheels"; //This is the name for the wheels group

const string statusLightsGroup = "Mode Lights"; //This is the name for the Lights group. The Lights will reflect the drive status.


const float roverPower = 100;
const float roverFriction = 75;
const float roverStrength = 75;
const float roverDamping = 95;
const bool roverSteering = true;
const float roverSteeringAngle = 25;

Color roverColor = new Color(255, 255, 0, 255);


const float skimmerPower = 100;
const float skimmerFriction = 0;
const float skimmerStrength = 95;
const float skimmerDamping = 95;

Color skimmerColor = new Color(255, 0, 0, 255);


/* Leave the stuff below here alone */



public Program() {

	
	Main("");

}


public void Main(string argument) {

	if(argument == "toggle"){
		//changing drive modes!
		driveMode = !driveMode;
	}
	
	
	//get the wheels list
	IMyBlockGroup wheelsBlockGroup = GridTerminalSystem.GetBlockGroupWithName(wheelsGroup);
	List<IMyMotorSuspension> wheelsList = new List<IMyMotorSuspension>(); 
	wheelsBlockGroup.GetBlocksOfType<IMyMotorSuspension>(wheelsList);
	
	
	/* // This is commented out for now, since the script doesn't do anything with cockpits. It might in the future however.
	//get the cockpits
	List<IMyCockpit> cockpitsList = new List<IMyCockpit>();
	GridTerminalSystem.GetBlocksOfType<IMyCockpit>(cockpitsList);
	*/
	
	//get the lights
	IMyBlockGroup lightsBlockGroup = GridTerminalSystem.GetBlockGroupWithName(statusLightsGroup);
	List<IMyInteriorLight> lightsList = new List<IMyInteriorLight>(); 
	lightsBlockGroup?.GetBlocksOfType<IMyInteriorLight>(lightsList);
	
	//get the gas tanks
	List<IMyGasTank> tanksList = new List<IMyGasTank>(); 
	GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tanksList);
	
	
	if(driveMode){ //ROVER MODE
		//UPDATE WHEELS
		Echo("ROVER MODE");
		
		foreach(IMyMotorSuspension wheel in wheelsList){
			wheel.SetValue<float>("Power", roverPower);
			wheel.SetValue<float>("Friction", roverFriction);
			wheel.SetValue<float>("Strength", roverStrength);
			//wheel.SetValue<float>("Damping", roverDamping);
			
			wheel.SetValue<bool>("Steering", roverSteering);
			wheel.SetValue<bool>("Propulsion", true);
		}
		
		Echo("UPDATED WHEELS");
		
		//DISABLE ALL HYDROGEN TANKS
		foreach(IMyGasTank tank in tanksList){
			tank.SetValue<bool>("Stockpile", true);
		}
		
		Echo("DISABLED TANKS");
		
		//UPDATE THE LIGHTS
		foreach(IMyInteriorLight light in lightsList){
			light.SetValue("Color", roverColor);
		}
		
		Echo("UPDATED LIGHTS");
		
	}else{ //SKIMMER MODE
		//UPDATE WHEELS
		Echo("SKIMMER MODE");
		
		foreach(IMyMotorSuspension wheel in wheelsList){
			wheel.SetValue<float>("Power", skimmerPower);
			wheel.SetValue<float>("Friction", skimmerFriction);
			wheel.SetValue<float>("Strength", skimmerStrength);
			//wheel.SetValue<float>("Damping", skimmerDamping);
			
			wheel.SetValue<bool>("Steering", false);
			wheel.SetValue<bool>("Propulsion", false);
		}
		Echo("UPDATED WHEELS");
		
		//ENABLE ALL HYDROGEN TANKS
		foreach(IMyGasTank tank in tanksList){
			tank.SetValue<bool>("Stockpile", false);
		}
		Echo("ENABLED TANKS");
		
		
		//UPDATE THE LIGHTS
		foreach(IMyInteriorLight light in lightsList){
			light.SetValue("Color", skimmerColor);
		}
		Echo("UPDATED LIGHTS");
		
	}
	
	
}

