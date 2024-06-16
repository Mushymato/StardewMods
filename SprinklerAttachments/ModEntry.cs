using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using SprinklerAttachments.Framework;

namespace SprinklerAttachments
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        public static IMonitor? mon;
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            mon = Monitor;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.ConsoleCommands.Add(
                "apply_sowing",
                "Triggers sowing (planting of seed and fertilizer from attachment) on all sprinklers with applicable attachment.\nThis action is always called at the end of a day.",
                ApplySowing
            );
        }

        /// <summary>
        /// Apply <see cref="GamePatches"/> on game launch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            Harmony patcher = new(ModManifest.UniqueID);
            GamePatches.Apply(patcher, Monitor);
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            Monitor.Log($"Do end of day sowing.", LogLevel.Info);
            SprinklerAttachment.ApplySowing();
        }

        private void ApplySowing(string command, string[] args)
        {
            SprinklerAttachment.ApplySowing();
            Monitor.Log($"OK, performed sowing for all sprinklers.", LogLevel.Info);
        }
    }
}
