using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace MachineControlPanel.Framework
{
    internal enum DefaultPageOption
    {
        Rules = 1,
        Inputs = 2
    }
    internal sealed class ModConfig
    {
        public KeybindList ControlPanelKey { get; set; } = KeybindList.Parse($"{SButton.Q}");
        public DefaultPageOption DefaultPage { get; set; } = DefaultPageOption.Rules;

        private void Reset()
        {
            ControlPanelKey = KeybindList.Parse($"{SButton.MouseLeft}, {SButton.ControllerB}");
            DefaultPage = DefaultPageOption.Rules;
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
            GMCM.AddTextOption(
                mod,
                getValue: () => { return DefaultPage.ToString(); },
                setValue: (value) => { DefaultPage = Enum.Parse<DefaultPageOption>(value); },
                allowedValues: Enum.GetNames<DefaultPageOption>(),
                formatAllowedValue: value => value switch
                {
                    nameof(DefaultPageOption.Rules) => I18n.Config_DefaultPage_MachineRules(),
                    nameof(DefaultPageOption.Inputs) => I18n.Config_DefaultPage_ItemInputs(),
                    _ => "???" // should never happen
                },
                name: I18n.Config_DefaultPage_Name,
                tooltip: I18n.Config_DefaultPage_Description
            );
        }
    }
}