using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using SprinklerAttachments.Framework;
using SprinklerAttachments.Framework.Integration;
using System.Reflection.Metadata;

namespace SprinklerAttachments
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        private static Framework.Integration.IContentPatcherAPI? ContentPatcherAPI;
        private static IMonitor? mon;
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

        public static void Log(string msg, LogLevel level = LogLevel.Debug)
        {
            mon?.Log(msg, level);
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
            Harmony patcher = new(ModManifest.UniqueID);
            GamePatches.Apply(patcher);
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
        private void ApplySowing(string command, string[] args)
        {
            SprinklerAttachment.ApplySowingToAllSprinklers(verbose: true);
            Log($"OK, performed sowing for all sprinklers.", LogLevel.Info);
        }
    }
}
