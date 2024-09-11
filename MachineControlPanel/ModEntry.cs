using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewUI;
using StardewValley.GameData.Machines;
using StardewValley;
using MachineControlPanel.Framework;
using MachineControlPanel.Framework.UI;
using SObject = StardewValley.Object;

namespace MachineControlPanel
{
    public class ModEntry : Mod
    {
        private const string SAVEDATA = "save-machine-rules";
        private ModConfig? config = null;
        private ModSaveData? saveData = null;

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;

            Logger.Monitor = Monitor;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            config = Helper.ReadConfig<ModConfig>();
            config.Register(Helper, ModManifest);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            saveData = Helper.Data.ReadSaveData<ModSaveData>(SAVEDATA);
            if (saveData == null)
            {
                saveData = new()
                {
                    Version = ModManifest.Version
                };
                return;
            }
            saveData.Version = ModManifest.Version;
            Console.WriteLine("OnSaveLoaded");
            foreach (var d in saveData.Disabled)
            {
                Console.WriteLine(d);
            }
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            if (saveData != null)
            {
                Console.WriteLine("OnSaving");
                foreach (var d in saveData.Disabled)
                {
                    Console.WriteLine(d);
                }
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

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Data/Machines"))
            {
                e.Edit(EditDataMachines, AssetEditPriority.Late);
            }
        }

        private void SaveMachineRules(HashSet<RuleIdent> enabled, HashSet<RuleIdent> disabled)
        {
            if (saveData != null)
            {
                saveData.Disabled.ExceptWith(enabled);
                saveData.Disabled.UnionWith(disabled);
                Console.WriteLine("SaveMachineRules");
                foreach (var d in saveData.Disabled)
                {
                    Console.WriteLine(d);
                }
                Helper.Data.WriteSaveData(SAVEDATA, saveData);
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

            // DebugPrintMachineRules(machine);
            Game1.activeClickableMenu = new RuleMenu(
                new RuleHelper(bigCraftable, machine),
                saveData!.Disabled,
                SaveMachineRules
            );

            return true;
        }

        private void EditDataMachines(IAssetData asset)
        {

            IDictionary<string, MachineData> data = asset.AsDictionary<string, MachineData>().Data;
            foreach (KeyValuePair<string, MachineData> kv in data)
            {
            }
        }

        internal void Log(string msg, LogLevel level = LogLevel.Debug)
        {
            Monitor.Log(msg, level);
        }
    }
}
