using HarmonyLib;
using MatoTweaks.Tweak;
using StardewModdingAPI;

namespace MatoTweaks;

public class ModEntry : Mod
{
    private static IMonitor? mon = null;

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;
        Harmony patcher = new(ModManifest.UniqueID);
        AtravitaItemSort.Patch(patcher);
        ChestSize.Patch(patcher);
        FriendshipJewel.Patch(patcher);
        StackCount.Patch(patcher);
    }

    public static void Log(string msg, LogLevel level = LogLevel.Debug)
    {
        mon!.Log(msg, level);
    }
}
