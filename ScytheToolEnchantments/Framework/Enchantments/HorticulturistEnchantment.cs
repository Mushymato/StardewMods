using System.Xml.Serialization;
using StardewValley;

namespace ScytheToolEnchantments.Framework.Enchantments;

[XmlType($"Mods_mushymato_{nameof(HorticulturistEnchantment)}")]
public class HorticulturistEnchantment : ScytheEnchantment
{
    public override string GetName()
    {
        return I18n.Enchantment_Horticulturist_Name();
    }

    public override bool CanApplyTo(Item item)
    {
        return base.CanApplyTo(item) && ModEntry.Config!.EnableHorticulturist;
    }
}

