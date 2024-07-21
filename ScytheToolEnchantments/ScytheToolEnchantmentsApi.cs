using ScytheToolEnchantments.Framework.Enchantments;
using StardewValley;

namespace ScytheToolEnchantments
{
    public class ScytheToolEnchantmentsApi : IScytheToolEnchantmentsApi
    {
        /// <summary>
        /// Checks if a Tool has GathererEnchantment.
        /// </summary>
        /// <param name="tool">Tool instance</param>
        /// <returns>True if tool has given enchantment</returns>
        public bool HasGathererEnchantment(Tool tool)
        {
            return tool != null && tool.hasEnchantmentOfType<GathererEnchantment>();
        }

        /// <summary>
        /// Checks if a Tool has HorticulturistEnchantment.
        /// </summary>
        /// <param name="tool">Tool instance</param>
        /// <returns>True if tool has given enchantment</returns>
        public bool HasHorticulturistEnchantment(Tool tool)
        {
            return tool != null && tool.hasEnchantmentOfType<HorticulturistEnchantment>();
        }

        /// <summary>
        /// Checks if a Tool has PalaeontologistEnchantment.
        /// </summary>
        /// <param name="tool">Tool instance</param>
        /// <returns>True if tool has given enchantment</returns>
        public bool HasPalaeontologistEnchantment(Tool tool)
        {
            return tool != null && tool.hasEnchantmentOfType<PalaeontologistEnchantment>();
        }

        /// <summary>
        /// Checks if a Tool has ReaperEnchantment.
        /// </summary>
        /// <param name="tool">Tool instance</param>
        /// <returns>True if tool has given enchantment</returns>
        public bool HasReaperEnchantment(Tool tool)
        {
            return tool != null && tool.hasEnchantmentOfType<ReaperEnchantment>();
        }
    }
}
