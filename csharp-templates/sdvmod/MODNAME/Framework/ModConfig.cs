using StardewModdingAPI;

namespace MODNAME.Framework;

internal sealed class ModConfig
{
    /// <summary>Restore default config values</summary>
    private void Reset()
    {

    }

    /// <summary>Add mod config to GMCM if available</summary>
    /// <param name="helper"></param>
    /// <param name="mod"></param>
    public void Register(IModHelper helper, IManifest mod)
    {
        Integration.IGenericModConfigMenuApi? GMCM = helper.ModRegistry.GetApi<Integration.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
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
    }
}
