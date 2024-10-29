using System.Xml.Serialization;
using StardewValley;
using StardewValley.Tools;

namespace ScytheToolEnchantments.Framework.Enchantments
{
    [XmlType($"Mods_mushymato_{nameof(CrescentEnchantment)}")]
    public class CrescentEnchantment : ScytheEnchantment
    {
        public const int AOE = 4;
        public override string GetName()
        {
            return I18n.Enchantment_Crescent_Name();
        }

        public override bool CanApplyTo(Item item)
        {
            return base.CanApplyTo(item) && ModEntry.Config!.EnableCrescent;
        }

        protected override void _ApplyTo(Item item)
        {
            base._ApplyTo(item);
            if (item is MeleeWeapon weapon)
            {
                weapon.addedAreaOfEffect.Value += AOE;
            }
        }

        protected override void _UnapplyTo(Item item)
        {
            base._UnapplyTo(item);
            if (item is MeleeWeapon weapon)
            {
                weapon.addedAreaOfEffect.Value -= AOE;
            }
        }

    }
}
