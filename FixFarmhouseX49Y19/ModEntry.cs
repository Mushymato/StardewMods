using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Extensions;
using StardewValley.Locations;

namespace FixFarmhouseX49Y19;

public class ModEntry : Mod
{
    private static IMonitor? mon = null;

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;
        Patch(new(ModManifest.UniqueID));
    }

    public static void Log(string msg, LogLevel level = LogLevel.Debug)
    {
        mon!.Log(msg, level);
    }

    public static void Patch(Harmony patcher)
    {
        try
        {
            patcher.Patch(
                original: AccessTools.DeclaredMethod(typeof(FarmHouse), "_ApplyRenovations"),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(FarmHouse__ApplyRenovations_Prefix)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(FarmHouse__ApplyRenovations_Postfix))
            );
        }
        catch (Exception err)
        {
            Log($"Failed to patch FixFarmhouseX49Y19:\n{err}", LogLevel.Error);
        }
    }

    private static void FarmHouse__ApplyRenovations_Prefix(ref bool ___displayingSpouseRoom, ref bool __state)
    {
        if (___displayingSpouseRoom)
        {
            __state = true;
            ___displayingSpouseRoom = false;
        }
    }

    private static void FarmHouse__ApplyRenovations_Postfix(ref bool ___displayingSpouseRoom, ref bool __state)
    {
        if (__state)
        {
            ___displayingSpouseRoom = true;
        }
    }
}
