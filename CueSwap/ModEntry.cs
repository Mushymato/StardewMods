using StardewModdingAPI;
using StardewValley;

namespace CueSwap;

public class ModEntry : Mod
{
#if DEBUG
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif
    private static IMonitor? mon;

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;
        Patches.Patch(ModManifest.UniqueID);

#if DEBUG
        helper.ConsoleCommands.Add("play_sound", "Play a sound cue.", ConsolePlaySound);
#endif
    }

#if DEBUG
    private void ConsolePlaySound(string command, string[] args)
    {
        if (args.Any())
        {
            Log($"playSound: {args[0]}");
            Game1.playSound(args[0]);
        }
    }
#endif

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
