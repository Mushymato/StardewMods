using HarmonyLib;
using SprinklerAttachments.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace SprinklerAttachments;

/// <summary>The mod entry point.</summary>
internal sealed class ModEntry : Mod
{
    private static IMonitor? mon;

    /// <summary>The mod entry point, called after the mod is first loaded.</summary>
    /// <param name="helper">Provides simplified APIs for writing mods.</param>
    public override void Entry(IModHelper helper)
    {
        mon = Monitor;
        Harmony patcher = new(ModManifest.UniqueID);
        GamePatches.Apply(patcher);
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.ConsoleCommands.Add(
            "apply_sowing",
            "Triggers sowing (planting of seed and fertilizer from attachment) on all sprinklers with applicable attachment.",
            ConsoleApplySowing
        );
    }

    /// <summary>Get an API that other mods can access. This is always called after <see cref="Entry"/>.</summary>
    public override object GetApi()
    {
        return new SprinklerAttachmentsApi();
    }

    public static void Log(string msg, LogLevel level = LogLevel.Debug)
    {
        mon!.Log(msg, level);
    }

    /// <summary>
    /// Apply <see cref="GamePatches"/> on game launch
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        SprinklerAttachment.SetUpModCompatibility(Helper);
        SprinklerAttachment.SetUpModConfigMenu(Helper, ModManifest);
    }

    /// <summary>
    /// Sow seeds and fertilizers from any valid sprinkler attachment at the end of the day
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        SprinklerAttachment.ApplySowingToAllSprinklers();
    }

    /// <summary>
    /// Sow seeds and fertilizer now
    /// </summary>
    /// <param name="command"></param>
    /// <param name="args"></param>
    private void ConsoleApplySowing(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Log("Must load save first.", LogLevel.Error);
            return;
        }
        SprinklerAttachment.ApplySowingToAllSprinklers(verbose: true);
        Log($"OK, performed sowing for all sprinklers.", LogLevel.Info);
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;
        SprinklerAttachment.OpenIntakeChest(e.Cursor);
    }
}
