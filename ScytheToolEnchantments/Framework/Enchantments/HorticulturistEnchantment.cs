using System.Xml.Serialization;

namespace ScytheToolEnchantments.Framework.Enchantments
{
    [XmlType($"Mods_mushymato_{nameof(HorticulturistEnchantment)}")]
    public class HorticulturistEnchantment : ScytheEnchantment
    {
        public override string GetName()
        {
            return I18n.Enchantment_Horticulturist_Name();
        }
    }
}
