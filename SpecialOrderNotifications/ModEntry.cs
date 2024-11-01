using SpecialOrderNotifications.Framework;
using StardewModdingAPI;

namespace SpecialOrderNotifications
{
    public class ModEntry : Mod
    {
        private static IMonitor? mon;

        public override void Entry(IModHelper helper)
        {
            mon = Monitor;
            GamePatches.Patch(ModManifest.UniqueID);
            DropboxFix.Register();
        }

        public static void Log(string msg, LogLevel level = LogLevel.Debug)
        {
            mon!.Log(msg, level);
        }
    }
}
