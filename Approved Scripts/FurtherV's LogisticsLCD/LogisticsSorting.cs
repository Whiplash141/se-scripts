/*
Description
-----------
 
Easy Logistics Sorting 2 (Version 1.0) will be your new favourite inventory sorting script!
Featuring not only general TypeID Sorting but also SubTypeID Sorting in special blocks, assembler and refinery cleaning AND internal alphabetic sorting.

How to Setup the Script
-----------------------
  
1. Put Script in PB.
2. Compile and Save.
3. Open the configuration setting by opening the customdata of the PB.
4. Edit the configuration to your liking. NOTE: You need to edit your quota here!
5. Recompile.
  

Abbreviations
-------------
CC = Cargo Container.
PB = Programmable Block (The block with this script)
   

How to lock and hide an block from the script
---------------------------------------------
1. Add [Locked] to the CCs Name.


How to Add CC to Normal Sorting
------------------------------- 
1. Copy the follwing example into the customData of the CC.
[ItemFilter]
Ore=true
Ingot=false
Component=false
AmmoMagazine=false
PhysicalGunObject=false
GasContainerObject=false
OxygenContainerObject=false

2. Change the boolean values (true and false) to your liking. True means that the CC will try to contain these items. False means the opposite.


How to Add CC to Special Sorting
--------------------------------
1. Copy the follwing example into the customData of the CC.
[Ore]

[Ingot]
Iron=5
[Component]

[AmmoMagazine]

[PhysicalGunObject]

[GasContainerObject]

[OxygenContainerObject]

2. As you can see in the example under [Ingot] we got the word Iron. This means that the CC will try to contain 5 items with the typeId Ingot and the subtype Iron.
2.1 Tip: Do get all typeIDs and subtypeIDs from all items in a container. Setup an LCD Panel called LOG_LCD, set it to show text and run the PB with the following argument "print YOURCARGOCONTAINERNAME"
    Example: print Cargo Container 2

Tips
----
1. By writing "template" into the customData of Special Blocks or normal CC blocks will generate a template for you!

Arguments
---------
Start
    Will start the sorting process.
Stop
    Will stop the sorting process if its running.
Reload
    Will reload the configuration. This will not reload lists.
UpdateList
    Will update all lists.
Print <arg1>
    Will print content of inventory of block which name is equal to <arg1> onto an LCD Panel called LOG_LCD.
*/

        //<==================================>//
        //< DO NOT TOUCH ANYTHING BELOW THIS >//
        //<==================================>//

        //Configuration Variables
        MyIni ini = new MyIni();

        bool restrictToGrid = false;
        bool normalSorting = true;
        bool specialSorting = true;
        bool productionBlockCleaning = true;
        bool autoAssembling = true;
        bool internalSorting = true;
        string specialBlockTag = "[Special]";
        string lockedBlockTag = "[Locked]";
        string configurationSectionName = "Configuration";

        //Permanently Allocated Variables aka Variables which you should not create all the time and reuse
        StringBuilder stringBuilder = new StringBuilder();

        List<IMyTerminalBlock> cargoBlockList = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
        List<IMyRefinery> refineryList = new List<IMyRefinery>();
        List<IMyAssembler> assemblerList = new List<IMyAssembler>();
        List<int> instructionCountList = new List<int>();
        List<double> runtimeList = new List<double>();

        Dictionary<MyDefinitionId, int> quotaDictionary = new Dictionary<MyDefinitionId, int>();
        Dictionary<MyDefinitionId, MyDefinitionId> blueprintDictionary = new Dictionary<MyDefinitionId, MyDefinitionId>();
        Dictionary<String, List<IMyTerminalBlock>> typeCargoDictionary = new Dictionary<string, List<IMyTerminalBlock>>();
        Dictionary<IMyTerminalBlock, ItemRequest[]> specialCargoRequestDictionary = new Dictionary<IMyTerminalBlock, ItemRequest[]>();

        String[] itemTypes = { "Ore", "Ingot", "Component", "AmmoMagazine", "PhysicalGunObject", "GasContainerObject", "OxygenContainerObject" };

        string currentModule = "Idle";
        string MoB = "MyObjectBuilder_";

        IMyAssembler masterAssembler = null;

        bool running = false;

        int lastBlockCount = -1;
        int runCount = 0;

        double scriptSpeed = 20; //In Hz
        double waitTime = 0;

        //Statemachine Queue
        Queue<IEnumerator<Boolean>> tasks = new Queue<IEnumerator<Boolean>>();
        IEnumerator<Boolean> currentTask;

        public struct ItemRequest
        {
            public double amount { get; set; }
            public MyDefinitionId itemDefinitonId { get; set; }
        }

        public Program()
        {
            CheckProgrammableBlockName();
            ReadScriptConfiguration();
            Echo("Script compiled.");
            RunCommand("start");
            RunCommand("stop");
        }

        public void Main(string argument, UpdateType updateType)
        {
            try
            {
                if (!String.IsNullOrEmpty(argument))
                {
                    RunCommand(argument);
                    return;
                }
                if ((updateType & (UpdateType.Update10 | UpdateType.Update1 | UpdateType.Update100)) != 0)
                {
                    waitTime -= Runtime.TimeSinceLastRun.TotalSeconds;
                    if(waitTime <= 0)
                    {
                        waitTime = (1 / scriptSpeed);
                        //Original By Digi
                        if (tasks.Count > 0)
                        {
                            currentTask = tasks.Peek();
                            if (Runtime.CurrentInstructionCount >= 40000)
                            {
                                RunCommand("stop");
                                currentModule = "Too Many Instructions.";
                                EchoDiagonstics();
                                return;
                            }
                            var hasMoreCode = currentTask.MoveNext();

                            if (!hasMoreCode)
                            {
                                currentTask.Dispose();
                                tasks.Dequeue();
                            }
                        }
                        else
                        {
                            currentModule = "\nScript execution complete.";
                            EchoDiagonstics();
                            currentTask = null;
                            Runtime.UpdateFrequency = UpdateFrequency.None;
                            running = false;
                            runCount = 0;
                            //Echo("Script execution complete.");
                            instructionCountList.Clear();
                            runtimeList.Clear();
                            return;
                        }
                        EchoDiagonstics();
                        ProfilerGraph();
                    }
                }
            } catch (Exception e)
            {
                Echo("An error occurred during script execution.");
                Echo($"Exception: {e}\n---");
                running = false;
                throw;
            }
        }

        public void RunCommand(String command)
        {
            if(command.Equals("start", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!running)
                {
                    ResetProfiler();
                    tasks.Enqueue(ListManaging());
                    if (normalSorting) tasks.Enqueue(SortCargo());
                    if (productionBlockCleaning) tasks.Enqueue(CleanProductionBlocks());
                    if (autoAssembling) tasks.Enqueue(AutoAssembly());
                    if (specialSorting) tasks.Enqueue(FillSpecialBlocks());
                    if (internalSorting) tasks.Enqueue(SortInternal());
                    Runtime.UpdateFrequency = UpdateFrequency.Update1;
                    running = true;
                }
                return;
            }
            if(command.Equals("stop", StringComparison.InvariantCultureIgnoreCase))
            {
                if (currentTask != null)
                {
                    currentTask.Dispose();
                    currentTask = null;
                }
                tasks.Clear();
                Runtime.UpdateFrequency = UpdateFrequency.None;
                currentModule = "Idle";
                lastBlockCount = -1;
                running = false;
                runCount = 0;
                return;
            }
            if (command.Equals("reload", StringComparison.InvariantCultureIgnoreCase))
            {
                if(tasks.Count==0) ReadScriptConfiguration();
                lastBlockCount = -1;
                return;
            }
            if(command.Equals("updateList", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!running)
                {
                    ReadScriptConfiguration();
                    lastBlockCount = -1;
                    tasks.Enqueue(ListManaging());
                    Runtime.UpdateFrequency = UpdateFrequency.Update1;
                    running = true;
                }
                return;
            }
            if(command.StartsWith("print", StringComparison.InvariantCultureIgnoreCase))
            {
                string cargoName = command.Substring(6);
                IMyTerminalBlock tb = GridTerminalSystem.GetBlockWithName(cargoName);
                IMyTextPanel logLCD = GridTerminalSystem.GetBlockWithName("LOG_LCD") as IMyTextPanel;
                if (tb == null || logLCD == null) return;
                if (!IsValidCargoBlock(tb)) return;
                stringBuilder.Clear();
                stringBuilder.Append("FORMAT: TYPEID/SUBTYPEID\n");
                foreach (var item in tb.GetInventory().GetItems())
                {
                    stringBuilder.Append($"{item.Content.TypeId.ToString().Replace(MoB,"")}/{item.Content.SubtypeId.ToString()}\n");
                }
                logLCD.WritePublicText(stringBuilder);
                Echo("Content Logged to LCD named LOG_LCD");
                return;
            }
            //TODO: Add customdata bulk copy command
        }

        public IEnumerator<Boolean> ListManaging()
        {
            currentModule = "List Managing";
            yield return true;
            yield return true;
            GridTerminalSystem.GetBlocks(allBlocks);
            yield return true;
            if (allBlocks.Count != lastBlockCount)
            {
                lastBlockCount = allBlocks.Count;
                specialCargoRequestDictionary.Clear();
                refineryList.Clear();
                assemblerList.Clear();
                if(typeCargoDictionary.Count > 0)
                {
                    foreach (var key in typeCargoDictionary.Keys)
                    {
                        typeCargoDictionary[key].Clear();
                    }
                } else
                {
                    foreach (var itemType in itemTypes)
                    {
                        typeCargoDictionary[itemType] = new List<IMyTerminalBlock>();
                    }
                }
                foreach (var block in allBlocks)
                {
                    Boolean addToCargoList = true;
                    foreach (var thing in filterBlock(block))
                    {
                        if (thing == false)
                        {
                            addToCargoList = false;
                            break;
                        }
                        yield return thing;
                    }
                    if (addToCargoList)
                    {
                        cargoBlockList.Add(block);
                    }
                    yield return true;
                }
                allBlocks.Clear();
                //GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cargoBlockList, x => filterBlock(x));
                yield return true;
            }
            yield return true;
            foreach (var assembler in assemblerList)
            {
                if (!IsValidCargoBlock(assembler)) continue;
                if (assembler.CooperativeMode == false)
                {
                    masterAssembler = assembler;
                    break;
                }
            }
            yield return true;
        }

        public IEnumerator<Boolean> SortCargo()
        {
            currentModule = "Cargo Sorting";
            foreach (var cargoBlock in cargoBlockList)
            {
                if (!IsValidCargoBlock(cargoBlock)) continue;
                if(cargoBlock.InventoryCount == 1)
                {
                    IMyInventory inv = cargoBlock.GetInventory();
                    if (!inv.IsItemAt(0)) continue;
                    List<IMyInventoryItem> itemList = inv.GetItems();
                    for(int i = itemList.Count -1; i >= 0; i--)
                    {
                        bool costly = MoveItemToDestination(cargoBlock, itemList, i);
                        if (costly)
                        {
                            yield return true;
                        }
                    }
                }
            }
            yield return true;
        }

        public IEnumerator<Boolean> CleanProductionBlocks()
        {
            currentModule = "Production Block cleaning";
            foreach (var refinery in refineryList)
            {
                for (int inventoryID = 0; inventoryID < refinery.InventoryCount; inventoryID++)
                {
                    IMyInventory inv;
                    if (!refinery.Enabled)
                    {
                        inv = refinery.GetInventory(0);
                        if (inv.IsItemAt(0))
                        {
                            List<IMyInventoryItem> itemList = inv.GetItems();
                            for (int i = itemList.Count - 1; i >= 0; i--)
                            {
                                MoveItemToDestination(refinery, itemList, i, 0);
                                yield return true;
                            }
                        }
                    }

                    inv = refinery.GetInventory(1);
                    if (inv.IsItemAt(0))
                    {
                        List<IMyInventoryItem> itemList = inv.GetItems();
                        for (int i = itemList.Count - 1; i >= 0; i--)
                        {
                            MoveItemToDestination(refinery, itemList, i, 1);
                            yield return true;
                        }
                    }
                }
            }

            yield return true;

            foreach (var assembler in assemblerList)
            {
                for (int inventoryID = 0; inventoryID < assembler.InventoryCount; inventoryID++)
                {
                    IMyInventory inv;
                    if (!assembler.Enabled || assembler.Mode==MyAssemblerMode.Disassembly)
                    {
                        inv = assembler.GetInventory(0);
                        if (inv.IsItemAt(0))
                        {
                            List<IMyInventoryItem> itemList = inv.GetItems();
                            for (int i = itemList.Count - 1; i >= 0; i--)
                            {
                                MoveItemToDestination(assembler, itemList, i, 0);
                                yield return true;
                            }
                        }
                    }

                    inv = assembler.GetInventory(1);
                    if (inv.IsItemAt(0))
                    {
                        List<IMyInventoryItem> itemList = inv.GetItems();
                        for (int i = itemList.Count - 1; i >= 0; i--)
                        {
                            MoveItemToDestination(assembler, itemList, i, 1);
                            yield return true;
                        }
                    }
                }
            }
            yield return true;
        }

        public IEnumerator<Boolean> FillSpecialBlocks()
        {
            currentModule = "Special Block filling";
            foreach (var specialBlock in specialCargoRequestDictionary.Keys)
            {
                if (!IsValidCargoBlock(specialBlock)) continue;
                ItemRequest[] requests = specialCargoRequestDictionary[specialBlock];
                for(int i=0; i < requests.Length; i++)
                {
                    IMyInventory specialInv = specialBlock.GetInventory();
                    ItemRequest request = requests[i];
                    MyDefinitionId id = request.itemDefinitonId;
                    double wantedAmount = request.amount;
                    if (wantedAmount == 0) continue;
                    double missingAmount = wantedAmount;
                    int targetIndex = 0;
                    String typeId = id.TypeId.ToString().Replace(MoB, "");

                    List<IMyTerminalBlock> sourceList = typeCargoDictionary[typeId];

                    if (sourceList.Count == 0) continue;

                    if (!IsBlocklistSorted(sourceList))
                    {
                        sourceList.Sort((x, y) => GetFilledPercentage(y.GetInventory()).CompareTo(GetFilledPercentage(x.GetInventory())));
                    }

                    if (specialInv.IsItemAt(0))
                    {
                        List<IMyInventoryItem> currentItemList = specialInv.GetItems();
                        for(int j = currentItemList.Count -1; j >= 0; j--)
                        {
                            if (currentItemList[j].GetDefinitionId() == id)
                            {
                                targetIndex = j;
                                missingAmount = wantedAmount - ((double)currentItemList[j].Amount);
                                break;
                            }
                        }
                    }

                    if (missingAmount <= 0) continue;

                    for(int j=0; j < sourceList.Count; j++)
                    {
                        IMyTerminalBlock sourceBlock = sourceList[j];
                        if (!IsValidCargoBlock(sourceBlock)) continue;
                        IMyInventory sourceInv = sourceBlock.GetInventory();
                        if (!sourceInv.IsItemAt(0)) continue;
                        List<IMyInventoryItem> itemList = sourceBlock.GetInventory().GetItems();
                        for(int itemIndex = itemList.Count -1; itemIndex >= 0; itemIndex--)
                        {
                            if (!sourceInv.IsItemAt(itemIndex)) continue;
                            IMyInventoryItem item = itemList[itemIndex];
                            if (item.GetDefinitionId() == id)
                            {
                                double foundAmount = (double)item.Amount;
                                if(foundAmount > missingAmount)
                                {
                                    foundAmount = missingAmount;
                                }
                                missingAmount -= foundAmount;
                                sourceInv.TransferItemTo(specialInv, itemIndex, targetIndex, true, (VRage.MyFixedPoint) foundAmount);
                                yield return true;
                            }
                        }
                    }
                }
                yield return true;
            }
            yield return true;
        }

        public IEnumerator<Boolean> SortInternal()
        {
            currentModule = "Internal Sorting";
            foreach (var cargoBlock in cargoBlockList)
            {
                if (!IsValidCargoBlock(cargoBlock)) continue;
                if (cargoBlock.InventoryCount == 1)
                {
                    IMyInventory inventory = cargoBlock.GetInventory();
                    List<IMyInventoryItem> items = inventory.GetItems();
                    if (items.Count < 2 || items.Count>30) continue;
                    for (int i = 0; i <= items.Count - 1; i++)
                    {
                        for (int j = i + 1; j <= items.Count - 1; j++)
                        {
                            var contentA = items[i].Content;
                            var contentB = items[j].Content;
                            string typeIdA = contentA.TypeId.ToString();
                            string typeIdB = contentB.TypeId.ToString();
                            string subtypeIdA = contentA.SubtypeId.ToString();
                            string subtypeIdB = contentB.SubtypeId.ToString();
                            if(contentA != contentB)
                            {
                                if(typeIdA.CompareTo(typeIdB) < 0)
                                {
                                    inventory.TransferItemTo(inventory, i, j, true);
                                } else if (typeIdA.CompareTo(typeIdB) == 0)
                                {
                                    if(subtypeIdA.CompareTo(subtypeIdB) <= 0)
                                    {
                                        inventory.TransferItemTo(inventory, i, j, true);
                                    }
                                }
                            } else
                            {
                                inventory.TransferItemTo(inventory, i, j, true);
                            }
                            items = inventory.GetItems();
                            yield return true;
                        }
                    }
                }
                yield return true;
            }
            yield return true;
        }

        public IEnumerator<Boolean> AutoAssembly()
        {
            currentModule = "AutoAssembly";
            if (masterAssembler == null || !IsValidCargoBlock(masterAssembler) || masterAssembler.Mode!=MyAssemblerMode.Assembly)
            {
                yield break;
            }
            foreach (var quotaId in quotaDictionary.Keys)
            {
                int amount = quotaDictionary[quotaId];
                if (amount > 0)
                {
                    int currentAmount = 0;
                    string itemType = quotaId.TypeId.ToString().Replace(MoB, "");
                    masterAssembler.CustomData = itemType;
                    yield return true;
                    if(typeCargoDictionary.ContainsKey(itemType) && typeCargoDictionary[itemType].Count > 0)
                    {
                        foreach (var cargoBlock in typeCargoDictionary[itemType])
                        {
                            if (!IsValidCargoBlock(cargoBlock)) continue;
                            IMyInventory inv = cargoBlock.GetInventory();
                            if (inv.IsItemAt(0))
                            {
                                foreach (var item in inv.GetItems())
                                {
                                    if (item.GetDefinitionId() == quotaId)
                                    {
                                        currentAmount += (int)item.Amount;
                                    }
                                }
                            }
                        }
                        int missingAmount = amount - currentAmount;
                        if (missingAmount > 0)
                        {
                            Boolean workingBlueprint = HasBlueprint(quotaId);
                            yield return true;
                            if (workingBlueprint)
                            {
                                MyDefinitionId blueprint = blueprintDictionary[quotaId];
                                foreach (var assembler in assemblerList)
                                {
                                    if (!IsValidCargoBlock(assembler)) continue;
                                    List<MyProductionItem> proItemList = new List<MyProductionItem>();
                                    assembler.GetQueue(proItemList);
                                    foreach (var proItem in proItemList)
                                    {
                                        if (proItem.BlueprintId == blueprint)
                                        {
                                            missingAmount -= (int)proItem.Amount;
                                        }
                            }
                                }
                                if(missingAmount > 0)
                                {
                                    masterAssembler.AddQueueItem(blueprint, (VRage.MyFixedPoint) missingAmount);
                                }
                            }
                        }
                    }
                }
                yield return true;
            }
            yield return true;
        }

        IEnumerable<Boolean> filterBlock(IMyTerminalBlock tb)
        {

            //Check if block is functional
            if (!IsValidCargoBlock(tb)) yield return false;

            //Check if its a special block
            if (tb.CustomName.Contains(specialBlockTag) && specialSorting)
            {
                string data = tb.CustomData;
                if (String.IsNullOrEmpty(data) || data.Equals("template"))
                {
                    tb.CustomData = GetSpecialTemplate();
                    yield return false;
                }
                MyIniParseResult result;
                if(ini.TryParse(data, out result))
                {
                    List<MyIniKey> keyList = new List<MyIniKey>();
                    foreach (var itemType in itemTypes)
                    {
                        keyList.Clear();
                        if (ini.ContainsSection(itemType))
                        {
                            ini.GetKeys(itemType, keyList);
                            foreach (var key in keyList)
                            {
                                if (!key.IsEmpty)
                                {
                                    MyDefinitionId id;
                                    double amount = ini.Get(key).ToDouble(0);
                                    if (amount == 0) continue;
                                    if (MyDefinitionId.TryParse($"{MoB}{itemType}",key.Name, out id))
                                    {
                                        ItemRequest request = new ItemRequest();
                                        request.itemDefinitonId = id;
                                        request.amount = amount;
                                        if (specialCargoRequestDictionary.ContainsKey(tb))
                                        {
                                            specialCargoRequestDictionary[tb][specialCargoRequestDictionary[tb].Length] = request;
                                        } else
                                        {
                                            specialCargoRequestDictionary[tb] = new ItemRequest[]{request};
                                        }
                                    }
                                }
                                yield return true;
                            }
                        }
                        yield return true;
                    }
                } else
                {
                    tb.CustomData += "; ERROR WHILE PARSING\n; Usually this error happens when you got too many newlines somewhere.\n; But It could also be something else.";
                }
                yield return false;
            }

            yield return true;

            //Check if its a production block
            if(tb is IMyRefinery)
            {
                refineryList.Add(tb as IMyRefinery);
                yield return false;
            }
            if(tb is IMyAssembler)
            {
                assemblerList.Add(tb as IMyAssembler);
                yield return false;
            }
            yield return true;

            string customData = tb.CustomData;
            if (!String.IsNullOrEmpty(customData))
            {
                if(customData.Equals("template"))
                {
                    tb.CustomData = GetItemFilterTemplate();
                } else
                {
                    MyIniParseResult result;
                    if (ini.TryParse(customData, "ItemFilter", out result))
                    {
                        yield return true;
                        List<MyIniKey> keyList = new List<MyIniKey>();
                        ini.GetKeys(keyList);
                        foreach (var key in keyList)
                        {
                            String keyName = key.Name;
                            if (itemTypes.Contains(keyName))
                            {
                                if (ini.Get(key).ToBoolean(false)) typeCargoDictionary[keyName].Add(tb);
                            }
                            yield return true;
                        }
                    }
                }
            }
            yield return true;
        }

        bool IsValidCargoBlock(IMyTerminalBlock tb)
        {
            //Existence and Functionality
            if (tb == null || Closed(tb) || !tb.IsFunctional) return false;

            //Has Actually an Inventory
            if (!tb.HasInventory) return false;

            if (restrictToGrid)
            {
                if (Me.CubeGrid != tb.CubeGrid) return false;
            }

            //Is not Locked
            if (tb.CustomName.Contains(lockedBlockTag)) return false;

            //Ownership Checks
            if ((tb.GetUserRelationToOwner(Me.OwnerId) == MyRelationsBetweenPlayerAndBlock.Enemies))return false;
            if ((tb.GetUserRelationToOwner(Me.OwnerId) == MyRelationsBetweenPlayerAndBlock.NoOwnership))return false;
            if ((tb.GetUserRelationToOwner(Me.OwnerId) == MyRelationsBetweenPlayerAndBlock.Neutral)) return false;

            //Check if its a reactor, a cockpit, a h2/o2 generator, a tank, a gun or an turret. This so that those blocks are not emptied.
            if (tb is IMyReactor || tb is IMyCockpit || tb is IMyGasGenerator || tb is IMyGasTank || tb is IMyUserControllableGun || tb is IMyLargeConveyorTurretBase) return false;
            return true;
        }

        bool Closed(IMyTerminalBlock block)
        {
            return (Vector3D.IsZero(block.WorldMatrix.Translation));
        }

        bool TransferItem(IMyInventory origin, int originIndex, IMyInventoryItem itemToTransfer, IMyInventory destination, VRage.MyFixedPoint? amount = null)
        {
            int targetIndex = 0;
            bool result;
            List<IMyInventoryItem> destinationItemList = destination.GetItems();
            for(int i = destinationItemList.Count - 1; i >= 0; i--)
            {
                IMyInventoryItem currentItem = destinationItemList[i];
                if(itemToTransfer.Content.TypeId == currentItem.Content.TypeId)
                {
                    if(itemToTransfer.Content.SubtypeId == currentItem.Content.SubtypeId)
                    {
                        targetIndex = i;
                        break;
                    }
                }
            }
            result = origin.TransferItemTo(destination, originIndex, targetIndex, true, amount);
            return result;
        }

        bool IsBlocklistSorted(List<IMyTerminalBlock> tbList)
        {
            for(int i = 1; i < tbList.Count; i++)
            {
                if(GetFilledPercentage(tbList[i-1].GetInventory()).CompareTo(GetFilledPercentage(tbList[i].GetInventory())) < 0)
                {
                    return false;
                }
            }
            return true;
        }

        double GetFilledPercentage(IMyInventory inv)
        {
            double maxVolume = (double) inv.MaxVolume;
            double currentVolume = (double)inv.CurrentVolume;
            double percentage = currentVolume / maxVolume;
            return percentage;
        }

        bool HasBlueprint(MyDefinitionId itemDef)
        {
            if (masterAssembler == null || !IsValidCargoBlock(masterAssembler))
            {
                return false;
            }

            if (blueprintDictionary.ContainsKey(itemDef))
            {
                return true;
            }
            else
            {
                string[] variants = { "", "Component", "Magazine" };
                bool blueprintCorrect = false;
                MyDefinitionId blueprint;

                for (int i = 0; i < variants.Length; i++)
                {
                    string itemBlueprint = $"{MoB}BlueprintDefinition/" + itemDef.SubtypeName.Replace("Item", "") + variants[i];
                    bool parsed = MyDefinitionId.TryParse(itemBlueprint, out blueprint);
                    if (!parsed)
                        continue;
                    // Test this without a try-catch
                    blueprintCorrect = masterAssembler.CanUseBlueprint(blueprint);

                    if (blueprintCorrect)
                    {
                        blueprintDictionary[itemDef] = blueprint;
                        return true;
                    }
                }
            }
            return false;
        }

        string GetDefaultConfig()
        {
            stringBuilder.Clear();
            stringBuilder.Append($"[{configurationSectionName}]\n");
            stringBuilder.Append($"{nameof(specialBlockTag)}={specialBlockTag}\n");
            stringBuilder.Append($"{nameof(lockedBlockTag)}={lockedBlockTag}\n");
            stringBuilder.Append($"{nameof(restrictToGrid)}={restrictToGrid}\n");
            stringBuilder.Append($"{nameof(normalSorting)}={normalSorting}\n");
            stringBuilder.Append($"{nameof(specialSorting)}={specialSorting}\n");
            stringBuilder.Append($"{nameof(productionBlockCleaning)}={productionBlockCleaning}\n");
            stringBuilder.Append($"{nameof(autoAssembling)}={autoAssembling}\n");
            stringBuilder.Append($"{nameof(internalSorting)}={internalSorting}\n\n");
            stringBuilder.Append("[Quota]\n");
            stringBuilder.Append("Component/SteelPlate=5");
            return stringBuilder.ToString();
        }

        string GetItemFilterTemplate()
        {
            stringBuilder.Clear();
            stringBuilder.Append("[ItemFilter]");
            foreach (var itemType in itemTypes)
            {
                stringBuilder.Append($"\n{itemType}=false");
            }
            return stringBuilder.ToString();
        }

        string GetSpecialTemplate()
        {
            stringBuilder.Clear();
            for(int i=0; i<itemTypes.Length; i++)
            {
                if(i == itemTypes.Length - 1)
                {
                    stringBuilder.Append($"[{itemTypes[i]}]");
                } else
                {
                    stringBuilder.Append($"[{itemTypes[i]}]\n\n");
                }
            }
            return stringBuilder.ToString();
        }

        bool MoveItemToDestination(IMyTerminalBlock sourceBlock, List<IMyInventoryItem> itemList, int i, int invID=0)
        {
            bool costly = false;
            if (!IsValidCargoBlock(sourceBlock)) return costly;
            IMyInventory sourceInv = sourceBlock.GetInventory(invID);
            if (!sourceInv.IsItemAt(i)) return costly;
            IMyInventoryItem currentItem = itemList[i];
            VRage.MyFixedPoint amount = currentItem.Amount;
            string typeId = currentItem.Content.TypeId.ToString().Replace(MoB, "");
            List<IMyTerminalBlock> destionationList = typeCargoDictionary[typeId];
            if (destionationList.Count > 0)
            {
                if (!IsBlocklistSorted(destionationList))
                {
                    destionationList.Sort((x, y) => GetFilledPercentage(y.GetInventory()).CompareTo(GetFilledPercentage(x.GetInventory())));
                }
                if (destionationList.Contains(sourceBlock) && (GetFilledPercentage(sourceBlock.GetInventory()) >= 0.95) || destionationList[0] == sourceBlock) return costly;
                IMyTerminalBlock finalDestination = null;
                costly = true;
                foreach (var destination in destionationList)
                {
                    if (!IsValidCargoBlock(destination)) continue;
                    if (GetFilledPercentage(destination.GetInventory()) < 0.95)
                    {
                        if (destination.GetInventory().CanItemsBeAdded(amount, currentItem.GetDefinitionId()))
                        {
                            finalDestination = destination;
                            if (!TransferItem(sourceInv, i, currentItem, finalDestination.GetInventory(), amount)) continue;
                            break;
                        }
                    }
                }
            }
            return costly;
        }

        void CheckProgrammableBlockName()
        {
            if (!Me.CustomName.Contains("[LogisticsSorting]")) Me.CustomName += " [LogisticsSorting]";
        }

        void ReadScriptConfiguration()
        {
            quotaDictionary.Clear();
            string customData = Me.CustomData;
            if(String.IsNullOrEmpty(customData) || String.IsNullOrWhiteSpace(customData))
            {
                Me.CustomData = GetDefaultConfig();
                return;
            }
            MyIniParseResult result;
            Boolean error = true;
            if (ini.TryParse (customData, out result))
            {
                if (ini.ContainsSection(configurationSectionName))
                {
                    error = false;
                    ini.Get(configurationSectionName, nameof(specialBlockTag)).TryGetString(out specialBlockTag);
                    ini.Get(configurationSectionName, nameof(lockedBlockTag)).TryGetString(out lockedBlockTag);

                    ini.Get(configurationSectionName, nameof(restrictToGrid)).TryGetBoolean(out restrictToGrid);
                    ini.Get(configurationSectionName, nameof(normalSorting)).TryGetBoolean(out normalSorting);
                    ini.Get(configurationSectionName, nameof(specialSorting)).TryGetBoolean(out specialSorting);
                    ini.Get(configurationSectionName, nameof(productionBlockCleaning)).TryGetBoolean(out productionBlockCleaning);
                    ini.Get(configurationSectionName, nameof(autoAssembling)).TryGetBoolean(out autoAssembling);
                    ini.Get(configurationSectionName, nameof(internalSorting)).TryGetBoolean(out internalSorting);

                }

                if (ini.ContainsSection("Quota"))
                {
                    List<MyIniKey> keyList = new List<MyIniKey>();
                    ini.GetKeys("Quota", keyList);
                    foreach (var key in keyList)
                    {
                        int amount = ini.Get(key).ToInt32(0);
                        if(amount > 0)
                        {
                            string[] keyNameSplit = key.Name.Split('/');
                            if (keyNameSplit.Length == 2)
                            {
                                if(keyNameSplit[0].Equals("AmmoMagazine", StringComparison.InvariantCultureIgnoreCase) || keyNameSplit[0].Equals("Component", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    try
                                    {
                                        MyDefinitionId defId;
                                        bool success = MyDefinitionId.TryParse($"{MoB}{keyNameSplit[0]}/{keyNameSplit[1]}", out defId);
                                        if (success)
                                        {
                                            quotaDictionary.Add(defId, amount);
                                        }
                                    } catch { }
                                }
                            }
                        }
                    }
                }
                Echo("Configuration loaded.");
            }
            if (error)
            {
                Me.CustomData = GetDefaultConfig();
                Echo("Error while loading config.\nConfig reset to default.");
            }
        }

        void EchoDiagonstics()
         {
            int instructionCount = Runtime.CurrentInstructionCount;
            instructionCountList.Add(instructionCount);
            int maxInstructionCount = instructionCountList.Max();
            int minInstructionCount = instructionCountList.Min();
            double averageInstructionCount = instructionCountList.Average();
            double lastRunTimeMs = Runtime.LastRunTimeMs;
            runtimeList.Add(lastRunTimeMs);

            stringBuilder.Clear();
            stringBuilder.Append($"Current Module: {currentModule}\n\n");
            stringBuilder.Append("<=== Diagnostics ===>\n");
            stringBuilder.Append($"Run Count: {runCount}\n");
            stringBuilder.Append($"Speed in Hz: {scriptSpeed}\n");
            stringBuilder.Append($"Instruction Count: {instructionCount} / {Runtime.MaxInstructionCount}\n");
            stringBuilder.Append($"Instruction Max: {maxInstructionCount}\n");
            stringBuilder.Append($"Instruction Min: {minInstructionCount}\n");
            stringBuilder.Append($"Instruction Average: {Math.Floor(averageInstructionCount)}\n");
            stringBuilder.Append($"RunTime: {Math.Round(lastRunTimeMs, 5)} ms\n");
            stringBuilder.Append($"Average RunTime: {Math.Round(runtimeList.Average(), 5)} ms\n");
            stringBuilder.Append($"Max RunTime: {Math.Round(runtimeList.Max(), 5)} ms");

            runCount++;

            Echo(stringBuilder.ToString());
        }

        int count = 1;
        int maxSeconds = 30;
        StringBuilder profile = new StringBuilder();
        bool hasWritten = false;

        void ResetProfiler()
        {
            count = 1;
            maxSeconds = 12;
            profile = new StringBuilder();
            hasWritten = false;
        }

        void ProfilerGraph()
        {
            if ((count <= maxSeconds * scriptSpeed) && (tasks.Count>0))
            {
                double timeToRunCode = Runtime.LastRunTimeMs;

                profile.Append($"{timeToRunCode.ToString()}").Append("\n");
                count++;
            }
            else if(!hasWritten)
            {
                var screen = GridTerminalSystem.GetBlockWithName("DEBUG") as IMyTextPanel;
                screen?.WritePublicText(profile.ToString());
                screen?.ShowPublicTextOnScreen();
                if (screen != null) hasWritten = true;
            }
        }