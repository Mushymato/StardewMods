using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Extensions;
using StardewValley.Locations;

namespace MatoTweaks.Tweak;

internal static class FixFarmhouseX49Y19
{
    public static void Patch(Harmony patcher)
    {
        try
        {
            patcher.Patch(
                original: AccessTools.DeclaredMethod(typeof(FarmHouse), "_ApplyRenovations"),
                prefix: new HarmonyMethod(typeof(FixFarmhouseX49Y19), nameof(FarmHouse__ApplyRenovations_Prefix)),
                postfix: new HarmonyMethod(typeof(FixFarmhouseX49Y19), nameof(FarmHouse__ApplyRenovations_Postfix))
            );
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to patch FixFarmhouseX49Y19:\n{err}", LogLevel.Error);
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
