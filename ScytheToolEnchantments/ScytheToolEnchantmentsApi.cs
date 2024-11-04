using ScytheToolEnchantments.Framework.Enchantments;
using StardewValley;

namespace ScytheToolEnchantments;

public class ScytheToolEnchantmentsApi : IScytheToolEnchantmentsApi
{
    /// <inheritdoc/>
    public bool HasGathererEnchantment(Tool tool)
    {
        return tool != null && tool.hasEnchantmentOfType<GathererEnchantment>();
    }

    /// <inheritdoc/>
    public bool HasHorticulturistEnchantment(Tool tool)
    {
        return tool != null && tool.hasEnchantmentOfType<HorticulturistEnchantment>();
    }

    /// <inheritdoc/>
    public bool HasPalaeontologistEnchantment(Tool tool)
    {
        return tool != null && tool.hasEnchantmentOfType<PalaeontologistEnchantment>();
    }

    /// <inheritdoc/>
    public bool HasReaperEnchantment(Tool tool)
    {
        return tool != null && tool.hasEnchantmentOfType<ReaperEnchantment>();
    }

    /// <inheritdoc/>
    public bool HasCrescentEnchantment(Tool tool)
    {
        return tool != null && tool.hasEnchantmentOfType<CrescentEnchantment>();
    }
}

