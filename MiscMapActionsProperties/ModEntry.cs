using HarmonyLib;
using StardewModdingAPI;


namespace MiscMapActionsProperties
{
    public class ModEntry : Mod
    {
        public const string MapProp_BuildingEntryLocation = "BuildingEntryLocation";
        private static IMonitor? mon;
        internal static IManifest? manifest = null;
        internal static string ModId => manifest?.UniqueID ?? "ERROR";

        public override void Entry(IModHelper helper)
        {
            mon = Monitor;
            manifest = ModManifest;
            Harmony harmony = new(ModId);

            Framework.Map.BuildingEntry.Patch(harmony);

            Framework.Tile.Builder.Register();
            Framework.Tile.AnimaSpot.Patch(harmony);
        }

#if DEBUG
        internal static void Log(string msg, LogLevel level = LogLevel.Debug)
#else
        internal static void Log(string msg, LogLevel level = LogLevel.Trace)
#endif
        {
            mon!.Log(msg, level);
        }

#if DEBUG
        internal static void LogOnce(string msg, LogLevel level = LogLevel.Debug)
#else
        internal static void Log(string msg, LogLevel level = LogLevel.Trace)
#endif
        {
            mon!.LogOnce(msg, level);
        }

    }
}
