//Whip's Group Renamer v2 - 9/2/17

bool shouldNumber = true;
bool useNumberOnFirstEntry = false;

void Main()
{
    ParseGroups();
}

void ParseGroups()
{
    var groupList = new List<IMyBlockGroup>();
    GridTerminalSystem.GetBlockGroups(groupList);

    var groupBlocks = new List<IMyTerminalBlock>();

    foreach (var group in groupList)
    {
        if (group.Name.ToLower().Contains("default"))
        {
            group.GetBlocks(groupBlocks);
            RenameBlocksToDefault(groupBlocks, shouldNumber);
        }
        else if (group.Name.ToLower().Trim().StartsWith("prefix"))
        {
            var prefix = group.Name.Remove(0, 6).Trim();
            group.GetBlocks(groupBlocks);
            PrefixBlockName(groupBlocks, prefix);
        }
        else if (group.Name.ToLower().Trim().StartsWith("suffix"))
        {
            var suffix = group.Name.Remove(0, 6).Trim();
            group.GetBlocks(groupBlocks);
            SuffixBlockName(groupBlocks, suffix);
        }
        else if (group.Name.ToLower().Trim().StartsWith("rename"))
        {
            var name = group.Name.Remove(0, 6).Trim();
            group.GetBlocks(groupBlocks);
            RenameBlocks(groupBlocks, name, shouldNumber);
        }
    }
}

Dictionary<string, int> blockNames = new Dictionary<string, int>();
void RenameBlocksToDefault(List<IMyTerminalBlock> blocks, bool shouldNumber = true)
{
    blockNames.Clear();

    foreach(var block in blocks)
    {
        var baseName = block.DefinitionDisplayNameText;
        var count = 1;
        if (blockNames.TryGetValue(baseName, out count))
        {
            count++; //iterate our count
            blockNames[baseName] = count;
        }
        else
        {
            blockNames.Add(baseName, 1);
        }

        block.CustomName = shouldNumber ? count > 1 ? baseName + $" {count}" : baseName : baseName;
    }

    Echo($"{blocks.Count} blocks renamed to default");
}

void PrefixBlockName(List<IMyTerminalBlock> blocks, string prefixName)
{
    foreach (var block in blocks)
    {
        if (!block.CustomName.Trim().ToLower().StartsWith(prefixName.ToLower()))
        {
            block.CustomName = $"{prefixName} {block.CustomName}";
        }
    }

    Echo($"{blocks.Count} blocks prefixed with '{prefixName}'");
}

void SuffixBlockName(List<IMyTerminalBlock> blocks, string suffixName)
{
    foreach (var block in blocks)
    {
        if (!block.CustomName.Trim().ToLower().EndsWith(suffixName.ToLower()))
        {
            block.CustomName = $"{block.CustomName} {suffixName}";
        }
    }

    Echo($"{blocks.Count} blocks suffixed with '{suffixName}'");
}

void RenameBlocks(List<IMyTerminalBlock> blocks, string blockName, bool shouldNumber = true)
{
    for(int i = 0; i < blocks.Count; i++)
    {
        var block = blocks[i];
        if (!block.CustomName.ToLower().Contains(blockName.ToLower()))
        {
            block.CustomName = shouldNumber ? i > 0 ? $"{blockName} {i + 1}" : blockName : blockName;
        }
    }

    Echo($"{blocks.Count} blocks renamed to '{blockName}'");
}
