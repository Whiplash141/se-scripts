// Logicstics LCD Script by FurtherV
// Version 1.0.0
//
// WARNING: IT IS NOT ENCOURAGED TO RUN THIS SCRIPT AUTOMATICALLY USING TIMERS, MODS OR OTHER SCRIPTS DUE TO... um... performance reasons.
//
// Setup Guide
// To run this script you must have atleast the following blocks on your grid: 1x Programmable Block, 1x Block with an Inventory, 1x LCD Panel.
// Tip: Use the normal 1x1x1 LCD Panels, not text panels and not the wide lcd panel, for a nice formatting!
// Step 1: Tag all LCDs you want to be managed by the script with [LLLCD] OR !LLCD!
// Step 2: Write inside the customdata of those LCD blocks what item type it should contain. Possible itemtypes are: ore, ingot, component, ammo.
// Step 3: Setup an button / buttonpanel to run the PB with this script, as argument you need to enter "update".
// Step 4: Load the script into the programmable block. Compile it. Save and Exit.
// Step 5: Success. If you followed all steps correctly you should now have one or multiple LCDs that show your item counts and update everytime you press the button.
// If you have problems, suggestions or bugs you've found / got, tell them FurtherV - Alex. 
//


//Please do not touch anything below this line.

String[] lcdItemTypes = {"ore","ingot","component","ammo"};

List<IMyTerminalBlock> inventoryBlockList = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> tempBlockList = new List<IMyTerminalBlock>();

Boolean isSetup = false;

Dictionary<String, double> oreAmountDictionary;
Dictionary<String, double> ingotAmountDictionary;
Dictionary<String, double> componentAmountDictionary;
Dictionary<String, double> ammoAmountDictionary;
Dictionary<String, List<IMyTextPanel>> itemtypeLCDListDictionary = new Dictionary<string, List<IMyTextPanel>>();

int blockCount = -1;
int updateCount = 0;

StringBuilder stringBuilder = new StringBuilder();

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Once;
    Echo("Script compiled.");
}

public void Main(string argument, UpdateType updateSource)
{
    if (!isSetup)
    {
        isSetup = true;
        Runtime.UpdateFrequency = UpdateFrequency.Update10;
        resetDictionarys();
        Echo("Script is setup.");
        if (!Me.CustomName.Contains("[LogisticsLCD]"))
        {
            Me.CustomName += " [LogisticsLCD]";
        }
        return;
    }

    if (Runtime.UpdateFrequency == UpdateFrequency.None)
    {
        if (argument.Equals("update", StringComparison.InvariantCultureIgnoreCase))
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }
        return;
    }

    //Update blocklist if blockcount changed
    if (updateCount == 0)
    {
        resetDictionarys();
        Runtime.UpdateFrequency = UpdateFrequency.Update10;
        if (inventoryBlockList.Count == 0) getBlocks();
        if (didBlockCountChange()) getBlocks();
    }

    //Get all items and put them in a big list.
    if (updateCount == 1)
    {
        foreach (var iblock in inventoryBlockList)
        {
            if (iblock != null && iblock.IsFunctional)
            {
                countItems(iblock);
            }
        }
    }

    //Write Ore LCD
    if (updateCount == 2)
    {
        if (itemtypeLCDListDictionary["ore"].Count > 0)
        {
            stringBuilder.Clear();
            addTitelToStringBuilder("Ore Summary", stringBuilder);

            foreach (var key in oreAmountDictionary.Keys)
            {
                double amount = Math.Round(oreAmountDictionary[key], 0);
                String amountString = amount.ToString();
                stringBuilder.Append(" " + key);
                stringBuilder.Append(' ', 34 - key.Length - amountString.Length);
                stringBuilder.Append(amountString);
                stringBuilder.Append("\n");
            }

            foreach (var lcd in itemtypeLCDListDictionary["ore"])
            {
                lcd.Font = "Monospace";
                lcd.FontSize = 0.722f;
                lcd.WritePublicText(stringBuilder);
            }
        }
    }

    //Write Ingot LCD
    if (updateCount == 3)
    {
        if (itemtypeLCDListDictionary["ingot"].Count > 0)
        {
            stringBuilder.Clear();
            addTitelToStringBuilder("Ingot Summary", stringBuilder);

            foreach (var key in ingotAmountDictionary.Keys)
            {
                double amount = Math.Round(ingotAmountDictionary[key],0);
                String amountString = amount.ToString();
                stringBuilder.Append(" " + key);
                stringBuilder.Append(' ', 34 - key.Length - amountString.Length);
                stringBuilder.Append(amountString);
                stringBuilder.Append("\n");
            }

            foreach (var lcd in itemtypeLCDListDictionary["ingot"])
            {
                lcd.Font = "Monospace";
                lcd.FontSize = 0.722f;
                lcd.WritePublicText(stringBuilder);
            }
        }

    }

    //Write Component LCD
    if (updateCount == 4)
    {
        if (itemtypeLCDListDictionary["component"].Count > 0)
        {
            stringBuilder.Clear();
            addTitelToStringBuilder("Component Summary", stringBuilder);

            foreach (var key in componentAmountDictionary.Keys)
            {
                double amount = Math.Round(componentAmountDictionary[key], 0);
                String amountString = amount.ToString();
                stringBuilder.Append(" " + key);
                stringBuilder.Append(' ', 34 - key.Length - amountString.Length);
                stringBuilder.Append(amountString);
                stringBuilder.Append("\n");
            }

            foreach (var lcd in itemtypeLCDListDictionary["component"])
            {
                lcd.Font = "Monospace";
                lcd.FontSize = 0.722f;
                lcd.WritePublicText(stringBuilder);
            }
        }
    }

    //Write Ammo LCD
    if (updateCount == 5)
    {
        if (itemtypeLCDListDictionary["ammo"].Count > 0)
        {
            stringBuilder.Clear();
            addTitelToStringBuilder("Ammo Summary", stringBuilder);

            foreach (var key in ammoAmountDictionary.Keys)
            {
                double amount = Math.Round(ammoAmountDictionary[key], 0);
                String amountString = amount.ToString();
                stringBuilder.Append(" " + key);
                stringBuilder.Append(' ', 34 - key.Length - amountString.Length);
                stringBuilder.Append(amountString);
                stringBuilder.Append("\n");
            }

            foreach (var lcd in itemtypeLCDListDictionary["ammo"])
            {
                lcd.Font = "Monospace";
                lcd.FontSize = 0.722f;
                lcd.WritePublicText(stringBuilder);
            }
        }
    }
    if (updateCount == 6)
    {
        updateCount = 0;
        Runtime.UpdateFrequency = UpdateFrequency.None;
        Echo("Script finished work.");
        return;
    }
    Echo($"Script working...\nUpdate Count: {updateCount}\nInventory Blocks: {inventoryBlockList?.Count}\nOre LCDs {itemtypeLCDListDictionary["ore"].Count}" +
        $"\nIngot LCDs {itemtypeLCDListDictionary["ingot"].Count}\nComponent LCDs {itemtypeLCDListDictionary["component"].Count}\nAmmo LCDs {itemtypeLCDListDictionary["ammo"].Count}");
    updateCount++;
}

void getBlocks()
{
    itemtypeLCDListDictionary.Clear();
    itemtypeLCDListDictionary["ore"] = new List<IMyTextPanel>();
    itemtypeLCDListDictionary["ingot"] = new List<IMyTextPanel>();
    itemtypeLCDListDictionary["component"] = new List<IMyTextPanel>();
    itemtypeLCDListDictionary["ammo"] = new List<IMyTextPanel>();

    inventoryBlockList.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(null, x => BlockCheck(x));
}

void resetDictionarys()
{
    oreAmountDictionary = new Dictionary<string, double>
    {
        {"Iron",0},
        {"Nickel",0},
        {"Silicon",0},
        {"Cobalt",0},
        {"Magnesium",0},
        {"Silver",0},
        {"Gold",0},
        {"Uranium",0},
        {"Platinum",0},
        {"Ice",0}
    };

    ingotAmountDictionary = new Dictionary<string, double>
    {
        {"Iron",0},
        {"Nickel",0},
        {"Silicon",0},
        {"Cobalt",0},
        {"Magnesium",0},
        {"Silver",0},
        {"Gold",0},
        {"Uranium",0},
        {"Platinum",0}
    };

    componentAmountDictionary = new Dictionary<string, double>
    {
        {"SteelPlate",0},
        {"InteriorPlate",0},
        {"Construction",0},
        {"MetalGrid",0},
        {"SmallTube",0},
        {"LargeTube",0},
        {"Motor",0},
        {"Girder",0},
        {"BulletproofGlass",0},
        {"Display",0},
        {"Computer",0},
        {"Reactor",0},
        {"Superconductor",0},
        {"Thrust",0},
        {"GravityGenerator",0},
        {"Medical",0},
        {"RadioCommunication",0},
        {"Detector",0},
        {"Explosives",0},
        {"SolarCell",0},
        {"PowerCell",0},
        {"Canvas",0}
    };

    ammoAmountDictionary = new Dictionary<string, double>();
}

void countItems(IMyTerminalBlock block)
{
    List<IMyInventoryItem> itemList = getItemsFromBlock(block);
    if (itemList.Count == 0) return;
    for(int i=itemList.Count()-1; i>=0; i--)
    {
        IMyInventoryItem item = itemList[i];
        String typeName = item.Content.TypeId.ToString().Replace("MyObjectBuilder_", "");
        if (typeName.StartsWith("O"))
        {
            addItemToDictionary(oreAmountDictionary, item);
        } else if (typeName.StartsWith("I"))
        {
            addItemToDictionary(ingotAmountDictionary, item);
        } else if (typeName.StartsWith("C"))
        {
            addItemToDictionary(componentAmountDictionary, item);
        } else if (typeName.StartsWith("A"))
        {
            addItemToDictionary(ammoAmountDictionary, item);
        }
    }
}

void addItemToDictionary(Dictionary<String, double> dictionary, IMyInventoryItem item)
{
    String subtypeName = item.Content.SubtypeId.ToString();
    double amount = (double)item.Amount;
    if (dictionary.ContainsKey(subtypeName))
    {
        dictionary[subtypeName] += amount;
    } else
    {
        dictionary[subtypeName] = amount;
    }
}

void addTitelToStringBuilder(String title, StringBuilder builder)
{
    title = " " + title + " ";
    int freeSpace = 37;
    int titleLength = title.Length;
    int missingSpaces = freeSpace-(title.Length + 8);
    builder.Append(' ', missingSpaces / 2);
    builder.Append("<===");
    builder.Append(title);
    builder.Append("===>");
    builder.Append(' ', missingSpaces / 2);
    builder.Append("\n\n");
}

List<IMyInventoryItem> getItemsFromBlock(IMyTerminalBlock block)
{
    List<IMyInventoryItem> itemList = new List<IMyInventoryItem>();
    if (!block.HasInventory) return itemList;
    if((block is IMyRefinery || block is IMyAssembler))
    {
        itemList.AddList(block.GetInventory(1).GetItems());
        itemList.AddList(block.GetInventory(0).GetItems());
    } else
    {
        itemList = block.GetInventory().GetItems();
    }
    return itemList;
}

Boolean BlockCheck(IMyTerminalBlock block)
{
    if (!block.IsFunctional) return false;
    IMyTextPanel lcd = block as IMyTextPanel;
    if (lcd!=null && lcd.IsWorking)
    {   
        if (lcd.CustomName.Contains("[LLCD]") || lcd.CustomName.Contains("!LLCD!"))
        {
            foreach (var itemtype in lcdItemTypes)
            {
                if (lcd.CustomData.Equals(itemtype, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (itemtypeLCDListDictionary.ContainsKey(itemtype))
                    {
                        itemtypeLCDListDictionary[itemtype].Add(lcd);
                    }
                    else
                    {
                        var list = new List<IMyTextPanel>();
                        itemtypeLCDListDictionary[itemtype] = list;
                        list.Add(lcd);
                    }
                    break;
                }
            }
        }
    } else if(block.HasInventory)
    {
        inventoryBlockList.Add(block);
    }
    return false;
}

Boolean didBlockCountChange()
{
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(tempBlockList, x => x.IsWorking);
    if (tempBlockList.Count != blockCount)
    {
        blockCount = tempBlockList.Count;
        return true;
    }
    return false;
}
