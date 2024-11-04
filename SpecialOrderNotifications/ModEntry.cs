using SpecialOrderNotifications.Framework;
using StardewModdingAPI;

namespace SpecialOrderNotifications;

public class ModConfig
{
    public bool EnableOverlappingDropBoxFix = true;
}
public class ModEntry : Mod
{
    private static IMonitor? mon;
    private static ModConfig? config;

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;
        config = Helper.ReadConfig<ModConfig>();
        Helper.WriteConfig(config);
        GamePatches.Patch(ModManifest.UniqueID);
        if (config.EnableOverlappingDropBoxFix)
            DropboxFix.Register();
    }

    public static void Log(string msg, LogLevel level = LogLevel.Debug)
    {
        mon!.Log(msg, level);
    }
}

