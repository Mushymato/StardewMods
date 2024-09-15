global using SObject = StardewValley.Object;
// BigCraftable Id, MachineOutputRule Id, MachineOutputTriggerRule Id, MachineOutputTriggerRule idx
global using RuleIdent = System.Tuple<string, string, string, int>;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewUI;
using StardewValley.GameData.Machines;
using StardewValley;
using MachineControlPanel.Framework;
using MachineControlPanel.Framework.UI;
using MachineControlPanel.Framework.Integration;
using HarmonyLib;

namespace MachineControlPanel
{
    internal sealed class ModEntry : Mod
    {
        private const string SAVEDATA = "save-machine-rules";
        private ModConfig? config = null;
        private static IMonitor? mon = null;
        private static ModSaveData? saveData = null;
        internal static ModSaveData SaveData => saveData!;

        public override void Entry(IModHelper helper)
        {
            mon = Monitor;
            I18n.Init(helper.Translation);
            UI.Initialize(helper, Monitor);


            // shared events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;

            // host only events
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;

            helper.ConsoleCommands.Add(
                "mcp_reset_savedata",
                "Reset save data associated with this mod.",
                ConsoleResetSaveData
            );
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            Harmony harmony = new(ModManifest.UniqueID);
            GamePatches.Apply(harmony);

            config = Helper.ReadConfig<ModConfig>();
            config.Register(Helper, ModManifest);
            var EMC = Helper.ModRegistry.GetApi<IExtraMachineConfigApi>("selph.ExtraMachineConfig");
            if (EMC != null)
            {
                RuleHelper.EMC = EMC;
            }
        }

        private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
        {
            if (!Game1.IsMasterGame)
                return;

            if (saveData != null)
            {
                Helper.Multiplayer.SendMessage(
                    saveData, SAVEDATA,
                    modIDs: [ModManifest.UniqueID],
                    playerIDs: [e.Peer.PlayerID]
                );
            }
        }

        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == ModManifest.UniqueID && e.Type == SAVEDATA)
            {
                saveData = e.ReadAs<ModSaveData>() ?? new() { Version = ModManifest.Version };
                LogSaveData();
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (!Game1.IsMasterGame)
                return;

            saveData = Helper.Data.ReadSaveData<ModSaveData>(SAVEDATA) ?? new() { Version = ModManifest.Version };
            LogSaveData();
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            if (!Game1.IsMasterGame)
                return;

            if (saveData != null)
            {
                saveData.Version = ModManifest.Version;
                Helper.Multiplayer.SendMessage(saveData, SAVEDATA, modIDs: [ModManifest.UniqueID]);
                Helper.Data.WriteSaveData(SAVEDATA, saveData);
                LogSaveData();
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
                saveData.Version = ModManifest.Version;
                Helper.Multiplayer.SendMessage(saveData, SAVEDATA, modIDs: [ModManifest.UniqueID]);
                Helper.Data.WriteSaveData(SAVEDATA, saveData);
                LogSaveData();
                return;
            }
            Log("Attempted to save machine rules without save loaded", LogLevel.Warn);
        }

        private bool ShowPanel()
        {
            if (Game1.activeClickableMenu == null && config!.ControlPanelKey.JustPressed())
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

        internal static void LogSaveData()
        {
            if (saveData == null)
                return;
            if (Game1.IsMasterGame)
                Log("Disabled machine rules:");
            else
                Log("Disabled machine rules (from host):");
            foreach (var ident in saveData.Disabled)
                Log($"- {ident}");
        }
    }
}
