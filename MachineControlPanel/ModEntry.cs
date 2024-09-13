using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewUI;
using StardewValley.GameData.Machines;
using StardewValley;
using MachineControlPanel.Framework;
using MachineControlPanel.Framework.UI;
using SObject = StardewValley.Object;
using HarmonyLib;

namespace MachineControlPanel
{
    public class ModEntry : Mod
    {
        private const string SAVEDATA = "save-machine-rules";
        private ModConfig? config = null;
        private static IMonitor? mon = null;
        private static ModSaveData? saveData = null;
        internal static ModSaveData SaveData => saveData!;

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;

            Logger.Monitor = Monitor;
            mon = Monitor;

            helper.ConsoleCommands.Add(
                "mcp_reset_savedata",
                "Reset save data associated with this mod.",
                ConsoleResetSaveData
            );
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            config = Helper.ReadConfig<ModConfig>();
            config.Register(Helper, ModManifest);
            Harmony harmony = new(ModManifest.UniqueID);
            GamePatches.Apply(harmony);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            saveData = Helper.Data.ReadSaveData<ModSaveData>(SAVEDATA) ?? new() { Version = ModManifest.Version };
            LogOnce("Disabled machine rules:");
            foreach (var ident in saveData.Disabled)
            {
                LogOnce($"- {ident}");
            }
            saveData.Version = ModManifest.Version;
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            if (saveData != null)
            {
                Helper.Data.WriteSaveData(SAVEDATA, saveData);
            }
            else
                Log("Failed to write machine rules save data.", LogLevel.Warn);
        }

        private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            ShowPanel();
        }

        private void SaveMachineRules(HashSet<RuleIdent> enabled, HashSet<RuleIdent> disabled)
        {
            if (saveData != null)
            {
                saveData.Disabled.ExceptWith(enabled);
                saveData.Disabled.UnionWith(disabled);
                Helper.Data.WriteSaveData(SAVEDATA, saveData);
                return;
            }
            Log("Attempted to save machine rules without save loaded", LogLevel.Warn);
        }

        private bool ShowPanel()
        {
            if (Game1.IsMasterGame && Game1.activeClickableMenu == null && config!.ControlPanelKey.JustPressed())
            {
                // ICursorPosition.GrabTile is unreliable with gamepad controls. Instead recreate game logic.
                Vector2 cursorTile = Game1.currentCursorTile;
                Point tile = Utility.tileWithinRadiusOfPlayer((int)cursorTile.X, (int)cursorTile.Y, 1, Game1.player)
                    ? cursorTile.ToPoint()
                    : Game1.player.GetGrabTile().ToPoint();
                SObject? bigCraftable = Game1.player.currentLocation.getObjectAtTile(tile.X, tile.Y, ignorePassables: true);
                if (bigCraftable != null && bigCraftable.bigCraftable.Value)
                {
                    return ShowPanelFor(bigCraftable);
                }
            }
            return false;
        }

        private bool ShowPanelFor(SObject bigCraftable)
        {
            if (bigCraftable.GetMachineData() is not MachineData machine)
                return false;

            if (machine.IsIncubator || machine.OutputRules == null || !machine.AllowFairyDust)
                return false;

            RuleHelper ruleHelper = new(bigCraftable, machine);
            if (ruleHelper.RuleEntries.Count == 0)
                return false;

            Game1.activeClickableMenu = new RuleMenu(
                ruleHelper,
                saveData!.Disabled,
                SaveMachineRules
            );

            return true;
        }

        private void ConsoleResetSaveData(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                Log("Must load save first.", LogLevel.Error);
                return;
            }
            Helper.Data.WriteSaveData<ModSaveData>(SAVEDATA, null);
            saveData = new() { Version = ModManifest.Version };
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
