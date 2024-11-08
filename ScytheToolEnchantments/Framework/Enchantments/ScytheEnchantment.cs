using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Tools;

namespace ScytheToolEnchantments.Framework.Enchantments;

public class ScytheEnchantment : BaseEnchantment
{
    public const string IridiumScytheQID = $"(W){MeleeWeapon.iridiumScytheID}";

    public override bool CanApplyTo(Item item)
    {
        return item.QualifiedItemId == IridiumScytheQID;
    }
}
