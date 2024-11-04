using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Objects;
using static StardewValley.Objects.Chest;

namespace MatoTweaks.Patches;

internal static class ChestSizeTweak
{
    public static void Patch(Harmony patcher)
    {
        try
        {
            patcher.Patch(
                original: AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.GetActualCapacity)),
                postfix: new HarmonyMethod(typeof(ChestSizeTweak), nameof(Chest_GetActualCapacity_Postfix))
            );
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to patch ChestSizeTweak:\n{err}", LogLevel.Error);
        }
    }

    private static void Chest_GetActualCapacity_Postfix(Chest __instance, ref int __result)
    {
        switch (__instance.SpecialChestType)
        {
            case SpecialChestTypes.BigChest:
                __result = 80;
                break;
            default:
                break;
        }
    }
}
