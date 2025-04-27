using HarmonyLib;
using SpecialOrderNotifications.Framework;
using StardewModdingAPI;

namespace SpecialOrderNotifications;

public class ModConfig
{
    public bool EnableOverlappingDropBoxFix = true;
}

public class ModEntry : Mod
{
#if DEBUG
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif

    private static IMonitor? mon;
    private static ModConfig? config;

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;
        config = Helper.ReadConfig<ModConfig>();
        Helper.WriteConfig(config);
        Harmony harmony = new(ModManifest.UniqueID);
        GamePatches.Patch(harmony);
        if (config.EnableOverlappingDropBoxFix)
        {
            Log("Using custom DropBox logic to fix overlaps.");
            DropBoxFix.Register(harmony, helper);
        }
    }

    public static void Log(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.Log(msg, level);
    }

    internal static void LogOnce(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.LogOnce(msg, level);
    }
}
