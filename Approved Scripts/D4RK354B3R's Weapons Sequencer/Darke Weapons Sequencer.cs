string instructions = "Ready to fire! \n Run this script with any of the following parameters: \n fire_toggle\n fire_once\n fire_burst number";

string setupFailed = "Could not find any guns. \n Make sure to set the block group and define the block group name under this block's custom data. \nThe format is:\n name=blockgroup\n\nOther parameters that can be placed here are:\n rps number; \nrpm number; \ndelay number \n\n Recompile this script when ready."; 

string blockGroupName = "";

bool isSetup = false;
/*
Parameters are placed into custom data.
:Example custom data:
name=blockGroupName
rps=10



Commands are split by colons. 
:Recognized commands:
fire_toggle
fire_once
fire_burst num
fire_mouse
rps num
rpm num
delay num

:Example run command:
rps 12: fire_toggle
*/

int fireMode = 0;
/*
fireMode:
0. not firing. inactive.
1. toggled on
2. fire # of shots
3. mouse fire
*/

List<IMyUserControllableGun> guns = new List<IMyUserControllableGun>();

int gunIndex = 0;
int shotsLeft = -1; //the number of shots left
double shotDelay = 0.100; //this is the default delay
double shotTimer = 0.0;
double scriptTime = 0.0;



Program()
{
	Initialization();
	//run initialization
	
	if(isSetup){
		Echo("Block Group: " + blockGroupName);
		Echo(instructions);
	}else{
		Echo(setupFailed);
	}
}

void Main(string arg, UpdateType updateSource)
{
	if(!isSetup){
		return;
	}
	
	if((updateSource & (UpdateType.Trigger | UpdateType.Terminal)) != 0){
		//the script was run by a user.
		
		//parse commands, set state variables, and return.
		ParseCommands(arg);
		if(shotDelay < 0.16){
			Runtime.UpdateFrequency = UpdateFrequency.Update1;
		}else{
			Runtime.UpdateFrequency = UpdateFrequency.Update10;
		}
		
		shotTimer = 0.0;
		scriptTime = 0.0;
		
		SalvoFire(); //salvo fire I guess.
		
		PrintStatus();
		
		
		if(fireMode == 0){
			foreach(IMyUserControllableGun gun in guns){
				gun.Enabled = false;
			}
			Runtime.UpdateFrequency = 0;
		}
		
		return;
	}
	
	if((updateSource & (UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100)) != 0){
		//the script was triggered by itself. This is used for actually salvo firing.
		scriptTime += Math.Max(Runtime.TimeSinceLastRun.TotalSeconds, 0);
		
		SalvoFire();
		
		PrintStatus();
		
		if(fireMode == 0){
			Runtime.UpdateFrequency = 0;
			
			foreach(IMyUserControllableGun gun in guns){
				gun.Enabled = false;
			}
			
			return;
		}
	}
}

void Initialization(){
	isSetup = false;
	bool foundGroup = false;
	
	
	//look for the block group name in custom data
	ParseCommands(Me.CustomData);
	
	if(blockGroupName.Length == 0){
		Echo("No block group name provided...");
		return;
	}
	
	List<IMyBlockGroup> blockGroups = new List<IMyBlockGroup>();
	
	GridTerminalSystem.GetBlockGroups(blockGroups, x => x.Name.ToLower().Contains(blockGroupName.ToLower()));
	
	foreach(IMyBlockGroup BG in blockGroups){
		blockGroupName = BG.Name;
		foundGroup = true;
		
		//don't sequence turrets, only sequence guns that are on my own grid. Only sequence guns that are still working.
        BG.GetBlocksOfType(guns, x => !(x is IMyLargeTurretBase) && x.IsFunctional && x.CubeGrid == Me.CubeGrid);
        guns.Sort((gun1, gun2) => gun1.CustomName.CompareTo(gun2.CustomName));
		break;
	}
	
	foreach(IMyUserControllableGun gun in guns){
		gun.Enabled = false;
	}
	
	if(foundGroup && guns.Count > 0){
		isSetup = true;
	}
}

void ParseCommands(string args){
	//This will read the input args and params from custom_data.
	string[] strings = args.Split('\n');
	if(strings.Length == 1){
		strings = args.Split(':');
	}
	
	
	for(int i = 0; i < strings.Length; i++){
		string arg = strings[i];
		
		arg = arg.Trim();
		
		if(arg.Equals("fire_toggle")){
			if(fireMode != 1) fireMode = 1;
			else fireMode = 0;
			shotsLeft = -1;
		}
		if(arg.Equals("fire_once")){ 
			fireMode = 2;
			shotsLeft = guns.Count;
		}
		if(arg.Equals("fire_mouse")){
			fireMode = 3;
			shotsLeft = -1;
		}
		if(arg.Contains("fire_burst")){
			int num = Convert.ToInt16(arg.Substring(10));
			shotsLeft = num;
			fireMode = 2;
		}
		
		if(arg.Contains("rps")){
			double num = Convert.ToDouble(arg.Substring(4));
			shotDelay = 1/num;
		}
		
		if(arg.Contains("rpm")){
			double num = Convert.ToDouble(arg.Substring(4)) / 60;
			shotDelay = 1/num;
		}
		
		if(arg.Contains("delay")){
			double num = Convert.ToDouble(arg.Substring(6));
			shotDelay = num;
		}
		
		if(arg.Contains("name")){
			blockGroupName = arg.Substring(5).Trim();
		}
	}
}

void PrintStatus(){
	Echo("Block Group: " + blockGroupName);
	if(fireMode == 0){			
		Echo(instructions + "\n");
	}else if(fireMode == 1){
		Echo("Toggle Fire: ON");
	}else if(fireMode == 2){
		Echo("Firing Burst, Remaining shots: " + shotsLeft);
	}
	Echo("Fire rate: " + ( 60 / shotDelay) + " rounds per minute");
}

void SalvoFire(){
	if(fireMode == 0) return;
	if(shotsLeft == 0){
		fireMode = 0;
		return;
	}
	if(scriptTime < shotTimer) return;
	
	if(gunIndex >= guns.Count) gunIndex = 0;
	
	//pewpew
	IMyUserControllableGun gun = guns[gunIndex];
	if(gun == null || !gun.IsFunctional){
		guns.Remove(gun);
		SalvoFire();
		return;
	}
	
	gunIndex ++;
	if(gunIndex >= guns.Count) gunIndex = 0;
	IMyUserControllableGun nextGun = guns[gunIndex];
	
	
	foreach(IMyUserControllableGun otherGun in guns){
		if(otherGun == nextGun) continue;
		if(otherGun == gun) continue;
		otherGun.Enabled = false;
	}
	
	//enable the next gun as well.
	if(nextGun != null) nextGun.Enabled = true;
	//enable and shoot current gun
	if(gun != null){
		gun.Enabled = true;
		
		if(fireMode != 3)
			gun.ApplyAction("ShootOnce");
	}
	
	shotTimer = scriptTime + shotDelay;
	if(shotsLeft > 0) shotsLeft --;
	
}