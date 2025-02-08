using HarmonyLib;
using MatoTweaks.Tweak;
using StardewModdingAPI;

// using StardewValley;
// using StardewValley.ItemTypeDefinitions;

namespace MatoTweaks;

public class ModEntry : Mod
{
    public class ModConfig
    {
        public bool AtravitaItemSort = true;
        public bool ChestSize = true;
        public int ChestSizeNormal = 80;
        public int ChestSizeBig = 140;
        public bool ChestStack = true;
        public bool FriendshipJewel = true;
        public bool StackCount = true;
    }

    private static IMonitor? mon = null;
    public static ModConfig Config { get; set; } = null!;

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;
        Config = Helper.ReadConfig<ModConfig>();
        Helper.WriteConfig(Config);
        Harmony patcher = new(ModManifest.UniqueID);
        if (Config.AtravitaItemSort)
            AtravitaItemSort.Patch(patcher);
        if (Config.ChestSize)
            ChestSize.Patch(patcher);
        if (Config.ChestStack)
            ChestStack.Patch(patcher);
        if (Config.FriendshipJewel)
            FriendshipJewel.Patch(patcher);
        if (Config.StackCount)
            StackCount.Patch(patcher);

        // helper.ConsoleCommands.Add("icon-edit", "Get icon edit CP defs for an object", GetIconEdit);
    }

    // private void GetIconEdit(string arg1, string[] arg2)
    // {
    //     List<Dictionary<string, object>> changes = [];
    //     foreach (var wf in DataLoader.FloorsAndPaths(Game1.content))
    //     {
    //         ParsedItemData data = ItemRegistry.GetDataOrErrorItem(wf.Value.ItemId);
    //         Dictionary<string, object> editimage = [];
    //         editimage["LogName"] = data.DisplayName;
    //         editimage["Action"] = "EditImage";
    //         editimage["Target"] = data.TextureName;
    //         editimage["FromFile"] = "FROMFILE";
    //         editimage["FromArea"] = data.GetSourceRect();
    //         editimage["ToArea"] = data.GetSourceRect();
    //         changes.Add(editimage);
    //     }
    //     Dictionary<string, object> cpedits = [];
    //     cpedits["Changes"] = changes;
    //     Helper.Data.WriteJsonFile("cpedits.json", cpedits);
    // }

    public static void Log(string msg, LogLevel level = LogLevel.Debug)
    {
        mon!.Log(msg, level);
    }
}
