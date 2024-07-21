using StardewValley;

namespace ScytheToolEnchantments
{
    public interface IScytheToolEnchantmentsApi
    {
        /// <summary>
        /// Checks if a Tool has GathererEnchantment.
        /// </summary>
        /// <param name="tool">Tool instance</param>
        /// <returns>True if tool has given enchantment</returns>
        public bool HasGathererEnchantment(Tool tool);

        /// <summary>
        /// Checks if a Tool has HorticulturistEnchantment.
        /// </summary>
        /// <param name="tool">Tool instance</param>
        /// <returns>True if tool has given enchantment</returns>
        public bool HasHorticulturistEnchantment(Tool tool);

        /// <summary>
        /// Checks if a Tool has PalaeontologistEnchantment.
        /// </summary>
        /// <param name="tool">Tool instance</param>
        /// <returns>True if tool has given enchantment</returns>
        public bool HasPalaeontologistEnchantment(Tool tool);

        /// <summary>
        /// Checks if a Tool has ReaperEnchantment.
        /// </summary>
        /// <param name="tool">Tool instance</param>
        /// <returns>True if tool has given enchantment</returns>
        public bool HasReaperEnchantment(Tool tool);
    }
}
