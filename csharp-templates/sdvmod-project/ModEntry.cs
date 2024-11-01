using MODNAME.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace MODNAME
{
    public class ModEntry : Mod
    {

#if DEBUG
        private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
        private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif

        private static IMonitor? mon;

        internal static ModConfig Config = null!;

        public override void Entry(IModHelper helper)
        {
            mon = Monitor;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            Config = Helper.ReadConfig<ModConfig>();
            Config.Register(Helper, ModManifest);
        }

        /// <summary>SMAPI static monitor Log wrapper</summary>
        /// <param name="msg"></param>
        /// <param name="level"></param>
        internal static void Log(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
        {
            mon!.Log(msg, level);
        }

        /// <summary>SMAPI static monitor LogOnce wrapper</summary>
        /// <param name="msg"></param>
        /// <param name="level"></param>
        internal static void LogOnce(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
        {
            mon!.LogOnce(msg, level);
        }
    }
}