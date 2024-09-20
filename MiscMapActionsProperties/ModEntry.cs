global using MapTile = xTile.Tiles.Tile;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;


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

            Framework.Tile.ShowConstruct.Register();
            Framework.Tile.AnimalSpot.Patch(harmony);

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            Framework.Tile.AnimalSpot.animalSpotsCache.Clear();
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
