global using MapTile = xTile.Tiles.Tile;
using HarmonyLib;
using StardewModdingAPI;

namespace MiscMapActionsProperties
{
    public class ModEntry : Mod
    {
#if DEBUG
        private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
        private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif
        private static IMonitor? mon;
        internal static IManifest? manifest = null;
        internal static string ModId => manifest?.UniqueID ?? "ERROR";

        public override void Entry(IModHelper helper)
        {
            mon = Monitor;
            manifest = ModManifest;
            Harmony harmony = new(ModId);

            Framework.Buildings.ChestLight.Register(helper);

            Framework.Map.BuildingEntry.Patch(harmony);

            Framework.Terrain.HoeDirtOverride.Register(helper);

            Framework.Tile.ShowConstruct.Register();

            Framework.Tile.AnimalSpot.Patch(harmony);
            Framework.Tile.AnimalSpot.Register(helper);

            Framework.Tile.HoleWarp.Register();

            Framework.Tile.LightSpot.Patch(harmony);
        }

        internal static void Log(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
        {
            mon!.Log(msg, level);
        }

        internal static void LogOnce(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
        {
            mon!.LogOnce(msg, level);
        }
    }
}
