using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace MatoTweaks.Tweak;

internal static class ExpMult
{
    public static void Patch(Harmony patcher)
    {
        try
        {
            patcher.Patch(
                original: AccessTools.DeclaredMethod(typeof(Farmer), nameof(Farmer.gainExperience)),
                prefix: new HarmonyMethod(typeof(ExpMult), nameof(Farmer_gainExperience_Prefix))
            );
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to patch ChestStack:\n{err}", LogLevel.Error);
        }
    }

    private static void Farmer_gainExperience_Prefix(Farmer __instance, int which, ref int howMuch)
    {
        int level = which switch
        {
            0 => __instance.farmingLevel.Value,
            3 => __instance.miningLevel.Value,
            1 => __instance.fishingLevel.Value,
            2 => __instance.foragingLevel.Value,
            4 => __instance.combatLevel.Value,
            _ => -1,
        };

        if (level == -1)
        {
            return;
        }
        float mult;
        if (level >= 10)
        {
            mult = ModEntry.Config.ExpMultipliers[10][which];
        }
        else
        {
            mult = ModEntry.Config.ExpMultipliers[0][which];
        }
        ModEntry.Log($"Farmer_gainExperience: {which}: {howMuch} * {mult} = {howMuch * mult}", LogLevel.Debug);
        howMuch = (int)MathF.Ceiling(howMuch * mult);
    }
}
