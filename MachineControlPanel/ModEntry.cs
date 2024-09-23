global using SObject = StardewValley.Object;
// BigCraftable Id, MachineOutputRule Id, MachineOutputTriggerRule Id, MachineOutputTriggerRule idx
global using RuleIdent = System.Tuple<string, string, int>;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewUI;
using StardewValley.GameData.Machines;
using StardewValley;
using MachineControlPanel.Framework;
using MachineControlPanel.Framework.UI;
using MachineControlPanel.Framework.Integration;
using HarmonyLib;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace MachineControlPanel
{
    internal sealed class ModEntry : Mod
    {
        /// <summary>
        /// Key for save data of this mod.
        /// </summary>
        private const string SAVEDATA = "save-machine-rules";
        /// <summary>
        /// Key for a partial message, e.g. only 1 machine's rules/inputs were changed.
        /// </summary>
        private const string SAVEDATA_ENTRY = "save-machine-rules-entry";
        private ModConfig? config = null;
        private static IMonitor? mon = null;
        private static ModSaveData? saveData = null;

        /// <summary>
        /// Attempt to get a save data entry for a machine
        /// </summary>
        /// <param name="QId"></param>
        /// <param name="msdEntry"></param>
        /// <returns></returns>
        internal static bool TryGetSavedEntry(string QId, [NotNullWhen(true)] out ModSaveDataEntry? msdEntry)
        {
            msdEntry = null;
            if (saveData != null)
                return saveData.Disabled.TryGetValue(QId, out msdEntry);
            return false;
        }

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
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;

            helper.ConsoleCommands.Add(
                "mcp_reset_savedata",
                "Reset save data associated with this mod.",
                ConsoleResetSaveData
            );
        }

        /// <summary>
        /// Read config, get EMC api, do patches
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            config = Helper.ReadConfig<ModConfig>();
            config.Register(Helper, ModManifest);
            var EMC = Helper.ModRegistry.GetApi<IExtraMachineConfigApi>("selph.ExtraMachineConfig");
            if (EMC != null)
                RuleHelper.EMC = EMC;
            Harmony harmony = new(ModManifest.UniqueID);
            GamePatches.Apply(harmony);
        }

        /// <summary>
        /// When someone joins in co-op, send entire saved data over
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
        {
            if (!Game1.IsMasterGame)
                return;

            Helper.Multiplayer.SendMessage(
                saveData, SAVEDATA,
                modIDs: [ModManifest.UniqueID],
                playerIDs: [e.Peer.PlayerID]
            );
        }

        /// <summary>
        /// Receive saved data sent from host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == ModManifest.UniqueID)
            {
                switch (e.Type)
                {
                    // entire saveData
                    case SAVEDATA:
                        try
                        {
                            saveData = e.ReadAs<ModSaveData>();
                            LogSaveData();

                        }
                        catch (JsonSerializationException)
                        {
                            Log($"Failed to read save data sent by host.", LogLevel.Warn);
                            saveData = null;
                        }
                        break;
                    // 1 entry in saveData
                    case SAVEDATA_ENTRY:
                        if (saveData == null)
                        {
                            Log("Received unexpected partial save data.", LogLevel.Error);
                            break;
                        }
                        ModSaveDataEntryMessage msdEntryMsg = e.ReadAs<ModSaveDataEntryMessage>();
                        if (msdEntryMsg.Entry == null)
                            saveData.Disabled.Remove(msdEntryMsg.QId);
                        else
                            saveData.Disabled[msdEntryMsg.QId] = msdEntryMsg.Entry;
                        LogSaveData(msdEntryMsg.QId);
                        break;
                }
            }
        }

        /// <summary>
        /// Read save data on the host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (!Game1.IsMasterGame)
                return;
            try
            {
                saveData = Helper.Data.ReadSaveData<ModSaveData>(SAVEDATA);
                if (saveData == null)
                    saveData = new();
                else
                    saveData.ClearInvalidData();
                saveData.Version = ModManifest.Version;
                Helper.Data.WriteSaveData(SAVEDATA, saveData);
            }
            catch (JsonSerializationException)
            {
                Log($"Failed to read existing save data, previous settings lost.", LogLevel.Warn);
                saveData = new() { Version = ModManifest.Version };
            }
            LogSaveData();
        }

        private void SaveMachineRules(
            string bigCraftableId,
            IEnumerable<RuleIdent> disabledRules,
            IEnumerable<string> disabledInputs
        )
        {
            if (saveData == null)
            {
                Log("Attempted to save machine rules without save loaded", LogLevel.Error);
                return;
            }

            ModSaveDataEntry? msdEntry = null;
            if (!disabledRules.Any() && !disabledInputs.Any())
            {
                saveData.Disabled.Remove(bigCraftableId);
            }
            else
            {
                msdEntry = new(
                    disabledRules.ToImmutableHashSet(),
                    disabledInputs.ToImmutableHashSet()
                );
                saveData.Disabled[bigCraftableId] = msdEntry;
            }
            saveData.Version = ModManifest.Version;
            Helper.Multiplayer.SendMessage(
                new ModSaveDataEntryMessage(bigCraftableId, msdEntry),
                SAVEDATA_ENTRY, modIDs: [ModManifest.UniqueID]
            );
            Helper.Data.WriteSaveData(SAVEDATA, saveData);
            LogSaveData(bigCraftableId);
            return;

        }

        /// <summary>
        /// Try and show either the machine control panel, or page to select machine from
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            if (ShowPanel())
                return;
            ShowMachineSelect();
        }

        /// <summary>
        /// Show the machine selection grid if corresponding button is pressed
        /// </summary>
        /// <returns></returns>
        private bool ShowMachineSelect()
        {
            if (Game1.activeClickableMenu == null && config!.MachineSelectKey.JustPressed() && saveData != null)
            {
                Game1.activeClickableMenu = new MachineMenu(config, SaveMachineRules);
            }
            return false;
        }

        /// <summary>
        /// Show the machine panel if corresponding button is pressed
        /// </summary>
        /// <returns></returns>
        private bool ShowPanel()
        {
            if (Game1.activeClickableMenu == null && config!.ControlPanelKey.JustPressed() && saveData != null)
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

        /// <summary>
        /// Show machine control panel for a big craftable
        /// </summary>
        /// <param name="bigCraftable"></param>
        /// <returns></returns>
        private bool ShowPanelFor(SObject bigCraftable)
        {
            if (bigCraftable.GetMachineData() is not MachineData machine)
                return false;

            if (machine.IsIncubator || machine.OutputRules == null || machine.OutputRules.Count == 0 || !machine.AllowFairyDust)
                return false;

            RuleHelper ruleHelper = new(bigCraftable.QualifiedItemId, bigCraftable.DisplayName, machine, config!);
            ruleHelper.GetRuleEntries();
            if (ruleHelper.RuleEntries.Count == 0)
                return false;

            Game1.activeClickableMenu = new RuleListMenu(
                ruleHelper,
                SaveMachineRules
            );

            return true;
        }

        /// <summary>
        /// Reset save data from this mod, for when things are looking wrong
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
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

        /// <summary>Debug log save data</summary>
        internal static void LogSaveData()
        {
            if (saveData == null || !saveData.Disabled.Any())
                return;
            if (Game1.IsMasterGame)
                Log("Disabled machine rules:");
            else
                Log("Disabled machine rules (from host):");
            foreach (var kv in saveData.Disabled)
            {
                Log(kv.Key);
                foreach (RuleIdent ident in kv.Value.Rules)
                    Log($"* {ident}");
                foreach (string inputQId in kv.Value.Inputs)
                    Log($"- {inputQId}");
            }
        }

        /// <summary>Debug log partial save data</summary>
        internal static void LogSaveData(string qId)
        {
            if (saveData == null)
                return;
            if (Game1.IsMasterGame)
                Log($"Disabled machine rules for {qId}:");
            else
                Log($"Disabled machine rules for {qId}: (from host):");
            if (!saveData.Disabled.TryGetValue(qId, out ModSaveDataEntry? msdEntry))
            {
                Log("= None");
                return;
            }
            foreach (RuleIdent ident in msdEntry.Rules)
                Log($"* {ident}");
            foreach (string inputQId in msdEntry.Inputs)
                Log($"- {inputQId}");
        }
    }
}
