/* ZEPPELIN CONTROL PROGRAM 
 * by Solater, edited by D4RK3 54B3R (I added PID control and fixed some other things for TLB)
 *  
 * Quick Start Instructions 
 *  
 * 1. Place a remote control, make sure it is upright and facing in the forward direction. Add to its name, "[rc]"(Without quotations). 
 *  
 * 2a. Place however many balloons as you wish, making sure they are in the same conveyor network. Make sure they are all in a group called "balloon"(Without quotations). 
 * 2b. Place however many hydrogen tanks as you wish, making sure they are in the same conveyor network. Make sure they are all in a group called "ballast"(Without quotations). 
 *  
 * 3. Place your desired amount of hydrogen farm. These will be used to fill up the balloons when needed, so, if you are in survival, make sure they are well stocked with ice! Make sure they are on the same conveyor network as the balloons. Make sure they are all in a group called "hydroFarm"(Without quotations) 
 *  
 * 4. Place a hydrogen thruster facing whichever way. These will be used to dispose of excess hydrogen in the balloons. Put them in a group called "exhaust"(without quotations). 
 *  
 * 5. To start the script, simply run it. I recommend running it with the "reset" command before doing anything else, and make sure your gas cells are only about 70% full.
 *  
 * 6. Happy flying! Try not to change altitude too rapidly (more than 500m at a time). The script will turn itself off when you park and leave the zeppelin.
 *
 * :SCRIPT COMMANDS:
 * sea <number> 		Set target altitude to <number> kilometers above sea level
 * add <number>			Add <number> to current target altitude
 * reset				Set current altitude to target altitude and reset PID state
 * ter <number>			Set target altitude to <number> kilometers above current ground level
 */ 

const bool useExhaust = true; //Whether or not this should use the hydrogen exhaust to dump hydrogen.
const bool useHydroFarm = true; //Whether or not this should use the hydrogen farms to gain hydrogen.
const double passiveHydroThreshold = 0.95; //How much to fill the ballast tanks to when using hydrogen farms.
static double passiveCellThreshold = 0.8;

//Constant Values 
static double BALLOON_NEWTONS = 520846.0377344; //Amount of Newtons of force exerted by each balloon 
static string BALLOON_NAME = "balloon"; //Balloons' names must be in this group 
static double G = 9.81; //Acceleration imparted by gravity on the earth 
static string RC_NAME = "[rc]"; //Remote control's name must contain this 
static string OXYGEN_NAME = "hydroFarm"; //Oxygen generators must be in this group 
static string THRUST_NAME = "exhaust"; //Thrusters used as exhaust must be in this group 
static string BALLAST_NAME = "ballast";//Ballast tanks used to absorb hydrogen
static string LCD_NAME = "[lcd]"; //LCD used to display information must contain this in its name 
static double ERROR_MARGIN = 0.0015; //Amount of error program will tolerate in terms of filled ratio 

static double lcdUpdateDelay = 1000;
static double lcdUpdateTimer = 0;

double errorClamp = 0.15;

double P = 1;
double I = 1.5;
double D = -0.03;
double I_decay = 0.8;

//Variables 
double desiredAltitude = 3.5; 
double currentAltitude = 0;
static double lastTargetRatio = -1;

double currVertVel; 
double lastAltitude; 

int secondsElapsed; 
int msElapsed;

double gravity = G; 
double balloonForce = BALLOON_NEWTONS; 

string planet = "earth"; 

List<IMyGasTank> balloons = new List<IMyGasTank>(); 
List<IMyGasTank> ballasts = new List<IMyGasTank>(); 
IMyRemoteControl rc; 
IMyTextPanel lcd; 
List<IMyOxygenFarm> oxygenGenerators = new List<IMyOxygenFarm>(); 
List<IMyThrust> thrusters = new List<IMyThrust>(); 

bool init = false; 


double PID_sum;
double lastError;
 

public void Main(string argument) 
{ 
	// The main entry point of the script, invoked every time 
	// one of the programmable block's Run actions are invoked. 
	//  
	// The method itself is required, but the argument above 
	// can be removed if not needed. 

	secondsElapsed = Runtime.TimeSinceLastRun.Seconds; 
	msElapsed = Runtime.TimeSinceLastRun.Milliseconds; 
	String printout=""; 
	 
	if (!init) 
	{ 
		findComponents(); 
		Echo("INITIALIZED");
		
		init = true; 
	} 
	Runtime.UpdateFrequency = UpdateFrequency.Update10; 

	//
	rc.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out currentAltitude); 
	currentAltitude/=1000;
	findVertVel(currentAltitude); 
	
	//Argument parsing
	if(!argument.Equals("") && argument != null){
		string[] argumentSplit = argument.Split(';'); 

		for (int i = 0; i < argumentSplit.Length; i++) 
		{ 
			string[] argumentFields = argumentSplit[i].Split(' '); 
			String value; 
			float value_float; 

			if (argumentFields.Length == 2) 
			{ 
				value = argumentFields[1]; 
			} 
			else 
			{ 
				value = null; 
			} 

			switch (argumentFields[0].ToLower()) 
			{ 
				case "reset":
					PID_sum = 0;
					lastError = 0;
					desiredAltitude = currentAltitude;
					break;
				case "add": //altitude addition
					if (!float.TryParse(value, out value_float)) 
					{ 
						Echo("Unable to parse altitude argument"); 
						return; 
					} 
					desiredAltitude += value_float; 
					break;
				case "sea": //Altitude based on sea level 
					if (!float.TryParse(value, out value_float)) 
					{ 
						Echo("Unable to parse altitude argument"); 
						return; 
					} 
					desiredAltitude = value_float; 
					break; 
				case "ter": //Altitude using terrain as reference 
					if (!float.TryParse(value, out value_float)) 
					{ 
						Echo("Unable to parse altitudeterrain argument"); 
						return; 
					} 
					double surfaceAltitude = 0; 
					rc.TryGetPlanetElevation(MyPlanetElevation.Surface, out surfaceAltitude); 
					double difference = (currentAltitude - surfaceAltitude) / 1000; 
					desiredAltitude = value_float + difference; 
					break; 
				case "planet": //planet 
					switch (value) 
					{ 
						case "earth": 
							planet = "earth"; 
							gravity = G * 1; 
							break; 
						case "mars": 
							planet = "mars"; 
							gravity = G * 0.9; 
							break; 
						case "alien": 
							planet = "alien"; 
							gravity = G * 1.1; 
							break; 
					} 
					break; 
				case "bforce": //balloon force 
					if (!float.TryParse(value, out value_float)) 
					{ 
						Echo("Unable to parse balloonforce argument"); 
						return; 
					} 
					balloonForce = value_float * 1000; 
					break; 
			} 

		} 
	}


	//check to see if parked
	bool disable = false;
	if((double)rc.CalculateShipMass().PhysicalMass <= (double)rc.CalculateShipMass().BaseMass){
		List<IMyLandingGear> gears = new List<IMyLandingGear>();
		GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(gears);
		foreach(IMyLandingGear gear in gears){
			if(gear.IsLocked){
				disable = true;
				break;
			}
		}
		
		List<IMyShipConnector> connectors = new List<IMyShipConnector>();
		GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(connectors);
		foreach(IMyShipConnector connector in connectors){
			if(connector.IsLocked || connector.IsConnected){
				disable = true;
				break;
			}
		}
	}
	if(rc.CalculateShipMass().BaseMass == 0 || (double)rc.CalculateShipMass().PhysicalMass == 0){
		disable = true;
	}
	
	//compute fill ratio of ballast tanks
	double avgTankFill = 0;
	foreach(IMyGasTank tank in ballasts){
		avgTankFill += tank.FilledRatio;
	}
	avgTankFill /= ballasts.Count();

	
	//clamp error to limit oscillation.
	double error = desiredAltitude - currentAltitude;
	if(error > errorClamp) error = errorClamp;
	if(error < -errorClamp) error = -errorClamp;
	
	double currentRatio = getTotalFilledRatio(); 
	double deltaError = currVertVel;//(error - lastError) * msElapsed / 1000.0;
	
	double pTerm = 0;
	double iTerm = 0;
	double dTerm = 0;
	
	double targetRatio = getNeededFilledRatio(desiredAltitude); 	
	
	//initialize lastTargetRatio
	if(lastTargetRatio < 0){
		lastTargetRatio = passiveCellThreshold;
	}
	
	if(!disable){			
		PID_sum += (error + lastError) * (msElapsed) / 2000.0;
		PID_sum *= I_decay;
		pTerm = P * error;
		iTerm = I * PID_sum;
		dTerm = D * deltaError;
		
		//clamp potential runaway terms ;_;
		//if(pTerm < -targetRatio / 2) pTerm = -targetRatio / 2;
		if(iTerm < -targetRatio / 2) iTerm = -targetRatio / 2;
		if(dTerm < -targetRatio / 2) dTerm = -targetRatio / 2;
	}
	
	targetRatio += pTerm + iTerm + dTerm; 	
	
	double deviation = Math.Abs(targetRatio - currentRatio); 
	
	if (currentRatio < targetRatio && deviation > ERROR_MARGIN && !disable) 
	{ 
		//increase ratio
		toggleThrust(false); 
		toggleOxygen(false); 
		toggleBalloon(true);
		toggleBallast(false);
		
		if(avgTankFill <= passiveHydroThreshold){
			toggleOxygen(true);
		}else{
			toggleOxygen(false); 
		}
		
		printout += "Increasing filled ratio\n"; 
	} 
	else if (currentRatio > targetRatio && deviation > ERROR_MARGIN && !disable) 
	{ 
		//decrease ratio
		toggleThrust(false); 
		toggleOxygen(false); 
		toggleBallast(true);
		toggleBalloon(false);
		
		
		if(avgTankFill > 0.999){
			toggleThrust(true);
		}
		
		printout += "Decreasing filled ratio\n"; 
	} 
	else 
	{ 
		if(!disable){
		//maintain ratio
			toggleThrust(false); 
			toggleBallast(false);
			toggleBalloon(false);
			
			if(avgTankFill <= passiveHydroThreshold)
				toggleBallast(true);
			else
				toggleBallast(false);
				
			
			if(currentRatio <= targetRatio)
				toggleBalloon(true);
			else
				toggleBalloon(false);
						
			printout += "Maintaining filled ratio\n"; 
		} else { //if it is parked...
			
			if(currentRatio > lastTargetRatio + 0.1){
				toggleThrust(true);
			}else{
				toggleThrust(false); 
			}
			
			if(avgTankFill <= passiveHydroThreshold || currentRatio <= lastTargetRatio){
				toggleOxygen(true);
				if(avgTankFill <= passiveHydroThreshold)
					toggleBallast(true);
				else
					toggleBallast(false);
					
				
				if(currentRatio <= lastTargetRatio)
					toggleBalloon(true);
				else
					toggleBalloon(false);
			}else{
				toggleBalloon(false);
				toggleOxygen(false);
				toggleBallast(false); 
			}
		}
	} 

	if(!disable){
		if(targetRatio > 0){
			lastTargetRatio = targetRatio;
		}else{
			targetRatio = lastTargetRatio;
		}
	}
	
	
	
	//print output
	lastError = error;
	lcdUpdateTimer -= msElapsed;
	
	if(lcdUpdateTimer <= 0){
		
		lcdUpdateTimer = lcdUpdateDelay;
		
		if(disable){
			//check cockpits
			bool hasPilot = false;
			List<IMyCockpit> cockpits = new List<IMyCockpit>();
			GridTerminalSystem.GetBlocksOfType<IMyCockpit>(cockpits);
			foreach(IMyCockpit cockpit in cockpits){
				if(cockpit.IsUnderControl){
					hasPilot = true;
					break;
				}
			}
			if(!hasPilot){
				
				if(avgTankFill <= passiveHydroThreshold || currentRatio <= lastTargetRatio){
					Runtime.UpdateFrequency = UpdateFrequency.Update100;
				}else{
					Runtime.UpdateFrequency = UpdateFrequency.None;
				}
				
				printout = "";
				printout += "Zeppelin Control disabled. \nRun script to restart control. \n";
				printout += "Current filled ratio: " + Math.Round(currentRatio * 100, 3) + "%\n"; 
				printout += "Tank filled ratio: " + Math.Round(avgTankFill * 100, 3) + "%\n"; 
				
				printout += "Current Altitude: " + Math.Round(currentAltitude, 3) + " km\n"; 
				lcd?.WritePublicText(printout); 
				PID_sum = 0;
				lastError = 0;
				desiredAltitude = currentAltitude;
				
				return;
			}
		}
		
		if (targetRatio > 1) 
		{ 
			printout += "Desired ratio impossible to reach\n"; 
		} 
		if(disable) printout += "Zeppelin is parked... " + "\n"; 
		printout += "Current filled ratio: " + Math.Round(currentRatio * 100, 3) + "%\n"; 
		printout += "Target filled ratio: " + Math.Round(targetRatio * 100, 3) + "%\n"; 
		printout += "Tank filled ratio: " + Math.Round(avgTankFill * 100, 3) + "%\n"; 
		printout += "Current Altitude: " + Math.Round(currentAltitude, 3) + " km\n"; 
		printout += "Desired altitude: " + Math.Round(desiredAltitude, 3) + " km\n"; 
		
		printout += "Current vertical velocity: " + Math.Round(currVertVel, 2) + " m/s\n"; 
		//printout += "Atmo Density: " + Math.Round(getAtmosphericDensity(currentAltitude), 2) + " \n"; 

		printout+="\n";
		
		Echo(printout); 

		lcd?.WritePublicText(printout); 
	}

} 

void findComponents() 
{ 


	List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>(); 
	GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(blocks); 


	foreach (IMyRemoteControl block in blocks) 
	{ 
		if (block.CustomName.Contains(RC_NAME)) 
		{ 
			rc = block as IMyRemoteControl; 
		} 
	} 
	
	GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(blocks); 

	foreach (IMyTextPanel block in blocks) 
	{ 
		if (block.CustomName.Contains(LCD_NAME)) 
		{ 
			lcd = block as IMyTextPanel; 
			lcd.WritePublicText("Waiting for commands");
		} 
	} 
	
	GridTerminalSystem.GetBlockGroupWithName(BALLOON_NAME)?.GetBlocksOfType<IMyGasTank>(balloons); 
	GridTerminalSystem.GetBlockGroupWithName(BALLAST_NAME)?.GetBlocksOfType<IMyGasTank>(ballasts); 
	GridTerminalSystem.GetBlockGroupWithName(THRUST_NAME)?.GetBlocksOfType<IMyThrust>(thrusters); 
	GridTerminalSystem.GetBlockGroupWithName(OXYGEN_NAME)?.GetBlocksOfType<IMyOxygenFarm>(oxygenGenerators); 


	Echo("Balloons - " + balloons.Count); 

	foreach (IMyGasTank balloon in balloons) 
	{ 
		Echo(balloon.CustomName); 
	} 
	
	Echo("Ballasts - " + ballasts.Count); 

	foreach (IMyGasTank tank in ballasts) 
	{ 
		Echo(tank.CustomName); 
	} 

	Echo("Remote Control - " + rc.CustomName); 

	Echo("Oxygen Generators - " + oxygenGenerators.Count); 
	
	
	foreach (IMyOxygenFarm oxygen in oxygenGenerators) 
	{ 
		Echo(oxygen.CustomName); 
	}
	

	Echo("Thrusters - " + thrusters.Count); 

	foreach (IMyThrust thrust in thrusters) 
	{ 
		Echo(thrust.CustomName); 
	} 

} 

double estimateBouyancyAltitude(double density)
{ 
	double alt = 0; 
	if (planet.Equals("earth") || planet.Equals("mars")) 
	{ 
		if(density > 1) density = 1;
		alt = (density - 0.999714809) / (-0.0712151286);
	} 
	else if (planet.Equals("alien")) 
	{ 
		if(density > 1.2) density = 1.2;
		alt = (density - 1.20032669) / (-0.0854040886);
	} 
	return alt; 
} 

double getAtmosphericDensity(double altitudeKM) 
{ 
	double eff = 0; 
	if (planet.Equals("earth") || planet.Equals("mars")) 
	{ 
		eff = -0.0712151286 * altitudeKM + 0.999714809; 
		if (eff > 1) 
		{ 
			eff = 1; 
		} 
	} 
	else if (planet.Equals("alien")) 
	{ 
		eff = -0.0854040886 * altitudeKM + 1.20032669; 
		if (eff > 1.2) 
		{ 
			eff = 1.2; 
		} 
	} 
	return eff; 
} 

double getNeededFilledRatio(double desiredAltitude) 
{ 
	double ratio = ((double)rc.CalculateShipMass().PhysicalMass * gravity) / (balloons.Count * balloonForce * getAtmosphericDensity(desiredAltitude)); 
	return ratio; 
} 

double getBaseFilledRatio(double desiredAltitude) 
{ 
	double ratio = ((double)rc.CalculateShipMass().BaseMass * gravity) / (balloons.Count * balloonForce * getAtmosphericDensity(desiredAltitude)); 
	return ratio; 
} 

double getTotalFilledRatio() 
{ 
	double total = 0; 

	foreach (IMyGasTank balloon in balloons) 
	{ 
		total += balloon.FilledRatio; 
	} 

	return total / balloons.Count(); 
} 

void toggleThrust(Boolean on) 
{ 
	if(!useExhaust) return;
	foreach (IMyThrust thrust in thrusters) 
	{ 
		thrust.SetValueFloat("Override", 100); 
		thrust.Enabled = on; 
	} 
} 

void toggleOxygen(Boolean on) 
{ 
	foreach (IMyOxygenFarm oxygen in oxygenGenerators) 
	{ 
		if(on && useHydroFarm){
			oxygen.ApplyAction("OnOff_On");
		}else{
			oxygen.ApplyAction("OnOff_Off");
		}
	} 
} 

void toggleBallast(Boolean on)
{
	foreach(IMyGasTank tank in ballasts){
		tank.Stockpile = on;
	}
}

void toggleBalloon(Boolean on){
	foreach(IMyGasTank balloon in balloons){
		balloon.Stockpile = on;
	}
}

void findVertVel(double currentAltitudeM) 
{ 
	Vector3D gravVec = rc.GetNaturalGravity();
	gravVec.Normalize();
	
	currVertVel = -rc.GetShipVelocities().LinearVelocity.Dot(gravVec);
	lastAltitude = currentAltitudeM; 
}