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
        private const string SAVEDATA = "save-machine-rules";
        private const string SAVEDATA_ENTRY = "save-machine-rules-entry";
        private ModConfig? config = null;
        private static IMonitor? mon = null;
        private static ModSaveData? saveData = null;

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

            Helper.Multiplayer.SendMessage(
                saveData, SAVEDATA,
                modIDs: [ModManifest.UniqueID],
                playerIDs: [e.Peer.PlayerID]
            );
        }

        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == ModManifest.UniqueID)
            {
                switch (e.Type)
                {
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

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (!Game1.IsMasterGame)
                return;
            try
            {
                saveData = Helper.Data.ReadSaveData<ModSaveData>(SAVEDATA) ?? new() { Version = ModManifest.Version };
            }
            catch (JsonSerializationException)
            {
                Log($"Failed to read existing save data, previous settings lost.", LogLevel.Warn);
                saveData = new() { Version = ModManifest.Version };
            }
            LogSaveData();
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            if (!Game1.IsMasterGame)
                return;

            if (saveData == null)
                return;

            saveData.Version = ModManifest.Version;
            Helper.Data.WriteSaveData(SAVEDATA, saveData);
            LogSaveData();
        }

        private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            if (ShowPanel())
                return;
            ShowMachineSelect();
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

        private bool ShowMachineSelect()
        {
            if (Game1.activeClickableMenu == null && config!.MachineSelectKey.JustPressed() && saveData != null)
            {
                Game1.activeClickableMenu = new MachineMenu(config, SaveMachineRules);
            }
            return false;
        }

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

        private bool ShowPanelFor(SObject bigCraftable)
        {
            if (bigCraftable.GetMachineData() is not MachineData machine)
                return false;

            if (machine.IsIncubator || machine.OutputRules == null || !machine.AllowFairyDust)
                return false;

            RuleHelper ruleHelper = new(bigCraftable.QualifiedItemId, bigCraftable.DisplayName, machine, config!);
            if (ruleHelper.RuleEntries.Count == 0)
                return false;

            Game1.activeClickableMenu = new RuleMenu(
                ruleHelper,
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
