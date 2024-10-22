global using MapTile = xTile.Tiles.Tile;
using HarmonyLib;
using StardewModdingAPI;


namespace MiscMapActionsProperties
{
    public class ModEntry : Mod
    {
        private static IMonitor? mon;
        internal static IManifest? manifest = null;
        internal static string ModId => manifest?.UniqueID ?? "ERROR";

        public override void Entry(IModHelper helper)
        {
            mon = Monitor;
            manifest = ModManifest;
            Harmony harmony = new(ModId);

            Framework.Map.BuildingEntry.Patch(harmony);

            Framework.Terrain.HoeDirtOverride.Register(helper);

            Framework.Tile.ShowConstruct.Register();
            Framework.Tile.AnimalSpot.Patch(harmony);
            Framework.Tile.AnimalSpot.Register(helper);
#if SDV_169
            Framework.Tile.HoleWarp.Register();
#endif
        }

        internal static void Log(string msg, LogLevel level = LogLevel.Debug)
        {
            mon!.Log(msg, level);
        }

        internal static void LogOnce(string msg, LogLevel level = LogLevel.Debug)
        {
            mon!.LogOnce(msg, level);
        }
    }
}
