/*
When you run this script while in a cockpit, with the argument "run"...
it will detonate any warheads on your grid when you leave cockpit or when cockpit gets blown out.

You can deactivate it by running the script again with the argument "stop"
*/

readonly IMyTextPanel TextPanel;
readonly IMyShipController cock;
bool ARMED;
public Program(){
	this.TextPanel = this.GridTerminalSystem.GetBlockWithName("[!]STATUS") as IMyTextPanel;
	List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyShipController>(list, b => b.CubeGrid == Me.CubeGrid);
	for( int e = 0; e < list.Count; e++ ) {
		IMyShipController block = list[e] as IMyShipController;
		if(block.BlockDefinition.ToString().Contains( "Cockpit" )){
			this.cock = block;
		};
	};
}

public void GetActiveCocks(){
	bool safe = false;
	if (this.cock != null)
		safe = this.cock.IsUnderControl;
	if (!safe){
		if(this.ARMED){
			List <IMyWarhead> WarList = new List<IMyWarhead>();
			GridTerminalSystem.GetBlocksOfType(WarList, b => b.CubeGrid == Me.CubeGrid);
			foreach (IMyWarhead block in WarList)
				block.ApplyAction("Detonate");
			Runtime.UpdateFrequency = UpdateFrequency.None;
			Echo("Boom goes the dynamite");
		} else {
			this.ARMED = true;
			List <IMyWarhead> WarList = new List<IMyWarhead>();
			GridTerminalSystem.GetBlocksOfType(WarList, b => b.CubeGrid == Me.CubeGrid);
			foreach (IMyWarhead block in WarList)
				block.SetValue<bool>("Safety", true);
		}
	}
}

public void Main(string argument, UpdateType updateSource){
	if(argument != ""){
		if(argument == "run"){
			this.ARMED = false;
			if(this.TextPanel != null){
				this.TextPanel.WritePublicText("--RUNNING--");
				this.TextPanel.SetValue("FontColor", Color.Red);
			}
			Runtime.UpdateFrequency = UpdateFrequency.Update100;
		}
		if(argument == "stop"){
			this.ARMED = false;
			List <IMyWarhead> WarList = new List<IMyWarhead>();
			GridTerminalSystem.GetBlocksOfType(WarList, b => b.CubeGrid == Me.CubeGrid);
			foreach (IMyWarhead block in WarList)
				block.SetValue<bool>("Safety", false);
			Runtime.UpdateFrequency = UpdateFrequency.None;
			if(this.TextPanel != null){
				this.TextPanel.WritePublicText("-----SAFE-----");
				this.TextPanel.SetValue("FontColor", Color.Green);
			}
			return;
		}
	}
	if ((updateSource & UpdateType.Update100) != 0){
		GetActiveCocks();
	};
}