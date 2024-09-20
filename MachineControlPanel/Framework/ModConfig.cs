using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace MachineControlPanel.Framework
{
    /// <summary>
    /// Options for default opened page
    /// s
    /// </summary>
    internal enum DefaultPageOption
    {
        Rules = 1,
        Inputs = 2
    }

    internal sealed class ModConfig
    {
        public KeybindList ControlPanelKey { get; set; } = KeybindList.Parse($"{SButton.Q}");
        public KeybindList MachineSelectKey { get; set; } = KeybindList.Parse($"{SButton.LeftControl}+{SButton.Q}");
        public DefaultPageOption DefaultPage { get; set; } = DefaultPageOption.Rules;

        private void Reset()
        {
            ControlPanelKey = KeybindList.Parse($"{SButton.MouseLeft}, {SButton.ControllerB}");
            MachineSelectKey = KeybindList.Parse($"{SButton.LeftControl}+{SButton.Q}");
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
            GMCM.AddKeybindList(
                mod,
                getValue: () => { return MachineSelectKey; },
                setValue: (value) => { MachineSelectKey = value; },
                name: I18n.Config_MachineSelectKey_Name,
                tooltip: I18n.Config_MachineSelectKey_Description
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