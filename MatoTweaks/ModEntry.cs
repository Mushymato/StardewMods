using HarmonyLib;
using MatoTweaks.Tweak;
using StardewModdingAPI;
using StardewValley;

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
        public bool ExpMult = true;
        public Dictionary<int, Dictionary<int, float>> ExpMultipliers =
            new()
            {
                [0] = new()
                {
                    // farming
                    [0] = 1f,
                    // mining
                    [3] = 1f,
                    // fishing
                    [1] = 1f,
                    // foraging
                    [2] = 1f,
                    // combat
                    [4] = 1f,
                },
                [10] = new()
                {
                    // farming
                    [0] = 1f,
                    // mining
                    [3] = 1f,
                    // fishing
                    [1] = 3f,
                    // foraging
                    [2] = 1f,
                    // combat
                    [4] = 1f,
                },
            };
        public bool FixFarmhouseX49Y19 = true;
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
        if (Config.ExpMult)
            ExpMult.Patch(patcher);
        if (Config.FixFarmhouseX49Y19)
            FixFarmhouseX49Y19.Patch(patcher);

        patcher.Patch(
            AccessTools.Method(typeof(Fence), nameof(Fence.minutesElapsed)),
            prefix: new HarmonyMethod(typeof(ModEntry), nameof(Fence_minutesElapsed_Prefix))
        );
    }

    private static void Fence_minutesElapsed_Prefix(int minutes)
    {
        Log($"Fence_minutesElapsed_Prefix: {minutes}");
    }

    public static void Log(string msg, LogLevel level = LogLevel.Debug)
    {
        mon!.Log(msg, level);
    }
}
