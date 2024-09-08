using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace MachineControlPanel.Framework
{
    internal sealed class ModConfig
    {
        public KeybindList ControlPanelKey { get; set; } = KeybindList.Parse($"{SButton.LeftControl}+{SButton.M}");

        private void Reset()
        {
            ControlPanelKey = KeybindList.Parse($"{SButton.MouseLeft}, {SButton.ControllerB}");
        }

        public void Register(IModHelper helper, IManifest mod)
        {
            var GMCM = helper.ModRegistry.GetApi<Integration.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (GMCM == null)
            {
                helper.WriteConfig(this);
                return;
            }
            GMCM.Register(
                mod: mod,
                reset: () => { Reset(); helper.WriteConfig(this); },
                save: () => { helper.WriteConfig(this); },
                titleScreenOnly: false
            );
            GMCM.AddKeybindList(
                mod,
                getValue: () => { return ControlPanelKey; },
                setValue: (value) => { ControlPanelKey = value; },
                name: I18n.Config_ControlPanelKey_Name,
                tooltip: I18n.Config_ControlPanelKey_Description
            );
        }
    }
}