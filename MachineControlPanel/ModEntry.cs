using MachineControlPanel.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewUI;

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

    }
}
