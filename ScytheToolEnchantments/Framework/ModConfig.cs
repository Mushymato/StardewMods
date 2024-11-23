using StardewModdingAPI;

namespace ScytheToolEnchantments.Framework;

internal sealed class ModConfig
{
    public bool EnableGatherer = true;
    public bool EnableHorticulturist = true;
    public bool EnablePalaeontologist = true;
    public bool EnableReaper = true;
    public bool EnableCrescent = true;

    private void Reset()
    {
        EnableGatherer = true;
        EnableHorticulturist = true;
        EnablePalaeontologist = true;
        EnableReaper = true;
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
            reset: () =>
            {
                Reset();
                helper.WriteConfig(this);
            },
            save: () =>
            {
                helper.WriteConfig(this);
            },
            titleScreenOnly: false
        );
        // GMCM.AddBoolOption(
        //     mod: mod,
        //     getValue: () => { return ScytheHarvestGiantCrop; },
        //     setValue: (value) => { ScytheHarvestGiantCrop = value; },
        //     name: I18n.Config_EnchantedScytheCanHarvestGiantCrop_Name,
        //     tooltip: I18n.Config_EnchantedScytheCanHarvestGiantCrop_Description
        // );
        GMCM.AddSectionTitle(
            mod: mod,
            text: I18n.Config_AvailableScytheEnchantments_Name,
            tooltip: I18n.Config_AvailableScytheEnchantments_Description
        );
        GMCM.AddBoolOption(
            mod,
            getValue: () =>
            {
                return EnableGatherer;
            },
            setValue: (value) =>
            {
                EnableGatherer = value;
            },
            name: I18n.Enchantment_Gatherer_Name,
            tooltip: I18n.Enchantment_Gatherer_Description
        );
        GMCM.AddBoolOption(
            mod,
            getValue: () =>
            {
                return EnableHorticulturist;
            },
            setValue: (value) =>
            {
                EnableHorticulturist = value;
            },
            name: I18n.Enchantment_Horticulturist_Name,
            tooltip: I18n.Enchantment_Horticulturist_Description
        );
        GMCM.AddBoolOption(
            mod,
            getValue: () =>
            {
                return EnablePalaeontologist;
            },
            setValue: (value) =>
            {
                EnablePalaeontologist = value;
            },
            name: I18n.Enchantment_Palaeontologist_Name,
            tooltip: I18n.Enchantment_Palaeontologist_Description
        );
        GMCM.AddBoolOption(
            mod,
            getValue: () =>
            {
                return EnableReaper;
            },
            setValue: (value) =>
            {
                EnableReaper = value;
            },
            name: I18n.Enchantment_Reaper_Name,
            tooltip: I18n.Enchantment_Reaper_Description
        );
        GMCM.AddBoolOption(
            mod,
            getValue: () =>
            {
                return EnableCrescent;
            },
            setValue: (value) =>
            {
                EnableCrescent = value;
            },
            name: I18n.Enchantment_Crescent_Name,
            tooltip: I18n.Enchantment_Crescent_Description
        );
    }
}
