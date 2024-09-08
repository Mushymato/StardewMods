using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewUI;
using MachineControlPanel.Framework;
using MachineControlPanel.Framework.UI;

namespace MachineControlPanel
{
    public class ModEntry : Mod
    {
        private MachineControl? machineControl;

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;

            Logger.Monitor = Monitor;

            helper.ConsoleCommands.Add(
                "show_focus_test",
                "Show focus test.",
                ConsoleShowFocusTest
            );
        }

        private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            machineControl!.ShowPanel(e.Cursor);
        }

        public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            ModConfig config = Helper.ReadConfig<ModConfig>();
            config.Register(Helper, ModManifest);
            machineControl = new MachineControl(Helper, Monitor, config);
        }

        public static void ConsoleShowFocusTest(string command, string[] args)
        {
            Game1.activeClickableMenu = new FocusTestMenu();
        }
    }
}
