/*
Zenduin's Walking Miner Script
Setup: Swinging row of drills
*/
string groupName = "Walk";
bool isSetup = false;
float stepSize = 2; //distance in meters for each cut of drills
float RPM = 1;
float PistonVelocity = 5f; //speed of pistons
int HingeLimit = 90; //maximum angle in degrees

enum Status { Stopped, Waiting, Place, Advance}
Status status;

Program()
{
	Runtime.UpdateFrequency = UpdateFrequency.Once;
	Echo("If you can read this\nclick the 'Run' button!");            
}
IMyExtendedPistonBase LeftPiston;
IMyExtendedPistonBase RightPiston;
IMyLandingGear LeftGear;
IMyLandingGear RightGear;
List<IMyCargoContainer> Containers = new List<IMyCargoContainer>();
List<IMyShipDrill> Drills = new List<IMyShipDrill>();
List<IMyMotorAdvancedStator> Hinge = new List<IMyMotorAdvancedStator>();
IMySoundBlock Alert = null;
StringBuilder setupSB = new StringBuilder();
float TargetPos = 0;
double TotalCargo = 0;        

void Main(string arg, UpdateType updateSource)
{
	if (!isSetup)
	{
		if (GrabBlocks())
		{
			LeftPiston.ShowInTerminal = false;
			LeftPiston.Enabled = false;
			LeftPiston.Velocity = -PistonVelocity;
			RightPiston.ShowInTerminal = false;
			RightPiston.Enabled = false;
			RightPiston.Velocity = -PistonVelocity;
			LeftGear.AutoLock = false;
			LeftGear.ShowInTerminal = false;
			RightGear.AutoLock = false;
			RightGear.ShowInTerminal = false;

			foreach (IMyCargoContainer Cargo in Containers)
			{
				TotalCargo += (double)Cargo.GetInventory().MaxVolume;
				Cargo.ShowInTerminal = false;
			}
			foreach (IMyShipDrill D in Drills)
			{
				D.ShowInTerminal = false;
				D.ShowInInventory = false;
				D.ShowInToolbarConfig = false;
			}
			foreach (IMyMotorAdvancedStator Rotor in Hinge)
			{
				Rotor.LowerLimitDeg = -HingeLimit;
				Rotor.UpperLimitDeg = HingeLimit;
				Rotor.TargetVelocityRPM = RPM;
				Rotor.Enabled = false;
			}
			isSetup = true;
		}
	}            
	Echo($"Zenduin's Walking Miner Script");
	if (isSetup)
	{
		Echo("\nStatus: " + status.ToString());
		switch (status)
		{
			case Status.Stopped:
				{

					break;
				}
			case Status.Waiting:
				{

					break;
				}
			case Status.Place:
				{

					break;
				}
			case Status.Advance:
				{

					break;
				}
		}
	}
	Echo("\nLast setup results:");
	Echo(setupSB.ToString());

	if (updateSource == UpdateType.Terminal || updateSource == UpdateType.Trigger)
	{
		if (arg == "Start")
		{
			if (LeftGear.IsLocked && Hinge[0].Angle > 0) // left leg locked with drills on right side
			{
				LeftPiston.Enabled = false;
				foreach (IMyMotorAdvancedStator S in Hinge)
				{
					S.TargetVelocityRPM = RPM;
					S.Enabled = true;
				}
				foreach (IMyShipDrill D in Drills)
				{
					D.Enabled = true;
				}
				status = Status.Waiting;
			}
			else if (RightGear.IsLocked && Hinge[0].Angle < 0) // right leg locked with drills on left side
			{
				RightPiston.Enabled = false;
				foreach (IMyMotorAdvancedStator S in Hinge)
				{
					S.TargetVelocityRPM = -RPM;
					S.Enabled = true;
				}
				foreach (IMyShipDrill D in Drills)
				{
					D.Enabled = true;
				}
				status = Status.Waiting;
			}
			else
			{
				Echo("Lock a landing gear with the drills on the correct side");
				return;
			}
			Runtime.UpdateFrequency = UpdateFrequency.Update10 | UpdateFrequency.Update100;
		}
		else if (arg == "Stop")
		{
			foreach (IMyShipDrill Drill in Drills)
			{
				Drill.Enabled = false;
			}
			LeftPiston.Enabled = false;
			RightPiston.Enabled = false;
			foreach (IMyMotorAdvancedStator S in Hinge)
			{
				S.Enabled = false;
			}
			status = Status.Stopped;
		}
	}
	if (updateSource == UpdateType.Update10 && isSetup)
	{
		// Manipulate stuff
		switch (status)
		{
			case Status.Stopped:
				{
					Runtime.UpdateFrequency = UpdateFrequency.None;
					break;
				}
			case Status.Waiting:
				{
					if (Hinge[0].Angle > 1.4f)
					{
						TargetPos = LeftPiston.CurrentPosition - stepSize;
						LeftPiston.Retract();
						LeftPiston.Enabled = true;
						foreach (IMyMotorAdvancedStator S in Hinge)
						{
							S.TargetVelocityRPM = -S.TargetVelocityRPM;
						}
						status = Status.Advance;                               
					}
					else if (Hinge[0].Angle < -1.4f)
					{
						TargetPos = RightPiston.CurrentPosition - stepSize;
						RightPiston.Retract();
						RightPiston.Enabled = true;
						foreach (IMyMotorAdvancedStator S in Hinge)
						{
							S.TargetVelocityRPM =  -S.TargetVelocityRPM;
						}
						status = Status.Advance;
					}
					else if ((-.035f < Hinge[0].Angle) && (Hinge[0].Angle < .035f))
					{
						if (Hinge[0].TargetVelocityRPM > 0)
						{
							LeftPiston.Extend();
							LeftGear.AutoLock = true;
							LeftPiston.Enabled = true;
							status = Status.Place;
						}
						else
						{
							RightPiston.Extend();
							RightGear.AutoLock = true;
							RightPiston.Enabled = true;
							status = Status.Place;
						}                                
					}
					break;
				}

			case Status.Place:
				{
					if (Hinge[0].TargetVelocityRPM > 0)
					{
						if (LeftGear.IsLocked)
						{
							LeftGear.AutoLock = false;
							LeftPiston.Enabled = false;
							RightGear.Unlock();
							RightPiston.Retract();
							RightPiston.Enabled = true;
							status = Status.Waiting;
						}
						else if (LeftPiston.CurrentPosition > LeftPiston.HighestPosition * .99) //Failed to lock on to something
						{
							Stop();
						}
					}
					else
					{
						if (RightGear.IsLocked)
						{
							RightGear.AutoLock = false;
							RightPiston.Enabled = false;
							LeftGear.Unlock();
							LeftPiston.Retract();
							LeftPiston.Enabled = true;
							status = Status.Waiting;
						}
						else if (RightPiston.CurrentPosition > RightPiston.HighestPosition * .99) //Failed to lock on to something
						{
							Stop();
						}
					}
					break;
				}
			case Status.Advance:
				{
					if (Hinge[0].TargetVelocityRPM > 0)
					{
						if (RightPiston.CurrentPosition <= TargetPos)
						{
							RightPiston.Enabled = false;
						}
					}
					else
					{
						if (LeftPiston.CurrentPosition <= TargetPos)
						{
							LeftPiston.Enabled = false;
						}
					}
					if ((Hinge[0].Angle < 1f) && (Hinge[0].Angle > -1f)) status = Status.Waiting;

					break;
				}
		}
	}
	if (updateSource == UpdateType.Update100)
	{
		/*
		// Check if cargo is full
		if (TotalCargo > 0)
		{
			double CurrentCargo = 0;
			foreach (IMyCargoContainer Cargo in Containers)
			{
				CurrentCargo += (double)Cargo.GetInventory().MaxVolume;
			}
			if (CurrentCargo / TotalCargo > .95)
			{
				Stop();
				if (Alert != null) Alert.Play();
			}
		}       
		*/
	}

}


private void Stop()
{
	foreach (IMyShipDrill Drill in Drills)
	{
		Drill.Enabled = false;
	}
	foreach (IMyMotorAdvancedStator S in Hinge)
	{
		S.Enabled = false;
	}
	LeftPiston.Enabled = false;
	RightPiston.Enabled = false;
	status = Status.Stopped;
}

bool GrabBlocks()
{
	bool passedSetup = true;
	setupSB.Clear();
	var group = GridTerminalSystem.GetBlockGroupWithName(groupName);
	if (group == null)
	{
		setupSB.AppendLine($">Error: No group named '{groupName}'");
		return false;
	}
	List<IMyExtendedPistonBase> Pistons = new List<IMyExtendedPistonBase>();
	List<IMyLandingGear> Gear = new List<IMyLandingGear>();
	group.GetBlocksOfType<IMyMotorAdvancedStator>(Hinge);
	if (Hinge.Count < 2)
	{
		passedSetup = false;
		setupSB.AppendLine(">Error: No hinge in group");
	}
	List<IMySoundBlock> SoundBlocks = new List<IMySoundBlock>();
	group.GetBlocksOfType<IMySoundBlock>(SoundBlocks);
	if (SoundBlocks.Count > 0) Alert = SoundBlocks[0];
	group.GetBlocksOfType<IMyShipDrill>(Drills);
	if (Drills.Count == 0)
	{
		passedSetup = false;
		setupSB.AppendLine(">Error: No drills in group");
	}
	group.GetBlocksOfType<IMyExtendedPistonBase>(Pistons);
	if (Pistons.Count <= 1)
	{
		passedSetup = false;
		setupSB.AppendLine(">Error: Could not find two properly named pistons in group");
	}
	group.GetBlocksOfType<IMyCargoContainer>(Containers);
	if (Containers.Count == 0)
	{
		setupSB.AppendLine("No containers found in group");
	}
	group.GetBlocksOfType<IMyLandingGear>(Gear);
	foreach (IMyExtendedPistonBase Piston in Pistons)
	{
		if (Piston.CustomName.Contains("L"))
		{
			LeftPiston = Piston;
			foreach (IMyLandingGear G in Gear)
			{
				if (G.CubeGrid == Piston.TopGrid) LeftGear = G;
			}
		}
		else if (Piston.CustomName.Contains("R"))
		{
			RightPiston = Piston;
			foreach (IMyLandingGear G in Gear)
			{
				if (G.CubeGrid == Piston.TopGrid) RightGear = G;
			}
		}
	}
	if (LeftGear == null)
	{
		passedSetup = false;
		setupSB.AppendLine(">Error: Could not find left landing gear");
	}
	if (RightGear == null)
	{
		passedSetup = false;
		setupSB.AppendLine(">Error: Could not find right landing gear");
	}
	if (Drills.Count > 0) setupSB.AppendLine("Drills: " + Drills.Count);
	if (Containers.Count > 0) setupSB.AppendLine("Cargo: " + Containers.Count);
	if (Alert != null) setupSB.AppendLine("Sound alert enabled");


	if (passedSetup)
		setupSB.AppendLine(">Setup Successful!");
	else
		setupSB.AppendLine(">Setup Failed!");
	return passedSetup;
}