using Microsoft.Xna.Framework;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Buildings;
using StardewValley;


namespace CustomBuildingEntry
{
    public class ModEntry : Mod
    {
        public const string MapProp_BuildingEntryLocation = "BuildingEntryLocation";
        private static IMonitor? mon;

        public override void Entry(IModHelper helper)
        {
            mon = Monitor;
            Patch();
        }

        public static void Log(string msg, LogLevel level = LogLevel.Debug)
        {
            mon!.Log(msg, level);
        }

        private void Patch()
        {
            Harmony harmony = new(ModManifest.UniqueID);
            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(Building), nameof(Building.OnUseHumanDoor)),
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(Building_OnUseHumanDoor_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.Method(typeof(GreenhouseBuilding), nameof(GreenhouseBuilding.OnUseHumanDoor)),
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(GreenhouseBuilding_OnUseHumanDoor_Postfix))
                );
            }
            catch (Exception err)
            {
                Log($"Failed to patch CustomBuildingEntry:\n{err}", LogLevel.Error);
            }
        }

        private static void Building_OnUseHumanDoor_Postfix(Building __instance, Farmer who, ref bool __result)
        {
            if (__result)
            {
                try
                {
                    // Do the warp now, and then return false to block vanilla warp
                    GameLocation interior = __instance.GetIndoors();
                    if (interior.TryGetMapPropertyAs(MapProp_BuildingEntryLocation, out Vector2 entryPos, required: false))
                    {
                        who.currentLocation.localSound("doorClose");
                        bool isStructure = __instance.indoors.Value != null;
                        Game1.warpFarmer(interior.NameOrUniqueName, (int)entryPos.X, (int)entryPos.Y, Game1.player.FacingDirection, isStructure);
                        __result = false;
                    }
                }
                catch (Exception err)
                {
                    Log($"Error in Building_OnUseHumanDoor_Postfix:\n{err}", LogLevel.Error);
                }
            }
        }

        private static void GreenhouseBuilding_OnUseHumanDoor_Postfix(GreenhouseBuilding __instance, Farmer who, ref bool __result)
        {
            if (__result)
            {
                try
                {
                    // Do the warp now, and then return false to block vanilla warp
                    GameLocation interior = __instance.GetIndoors();
                    if (interior.TryGetMapPropertyAs(MapProp_BuildingEntryLocation, out Vector2 entryPos, required: false))
                    {
                        who.currentLocation.localSound("doorClose");
                        bool isStructure = __instance.indoors.Value != null;
                        Game1.warpFarmer(interior.NameOrUniqueName, (int)entryPos.X, (int)entryPos.Y, Game1.player.FacingDirection, isStructure);
                        __result = false;
                    }
                }
                catch (Exception err)
                {
                    Log($"Error in GreenhouseBuilding_OnUseHumanDoor_Postfix:\n{err}", LogLevel.Error);
                }
            }

        }
    }
}
