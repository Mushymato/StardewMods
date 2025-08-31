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
            ModEntry.Log($"Failed to patch ChestStack:\n{err}", LogLevel.Error);
        }
    }

    private static void FarmHouse__ApplyRenovations_Prefix(FarmHouse __instance, ref (int, string)? __state)
    {
        __state = null;
        if (__instance.upgradeLevel < 2)
        {
            return;
        }
        if (__instance.Map?.RequireLayer("Front")?.Tiles[49, 19] is xTile.Tiles.Tile tile)
        {
            __state = new(tile.TileIndex, tile.TileSheet.Id);
            // ModEntry.Log(__state.ToString() ?? "?");
        }
        else
        {
            __state = (-1, string.Empty);
        }
    }

    private static void FarmHouse__ApplyRenovations_Postfix(FarmHouse __instance, ref (int, string)? __state)
    {
        if (__state == null)
        {
            return;
        }
        else if (__state.Value.Item1 == -1)
        {
            __instance.removeMapTile(49, 19, "Front");
        }
        else
        {
            __instance.setMapTile(49, 19, __state.Value.Item1, "Front", __state.Value.Item2);
        }
    }
}
