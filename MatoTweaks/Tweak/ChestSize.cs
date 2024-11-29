using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Objects;
using static StardewValley.Objects.Chest;

namespace MatoTweaks.Tweak;

internal static class ChestSize
{
    public static void Patch(Harmony patcher)
    {
        try
        {
            patcher.Patch(
                original: AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.GetActualCapacity)),
                postfix: new HarmonyMethod(typeof(ChestSize), nameof(Chest_GetActualCapacity_Postfix))
            );
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to patch ChestSize:\n{err}", LogLevel.Error);
        }
    }

    private static void Chest_GetActualCapacity_Postfix(Chest __instance, ref int __result)
    {
        __result = __instance.SpecialChestType switch
        {
            SpecialChestTypes.BigChest => 140,
            _ => 80,
        };
    }
}
