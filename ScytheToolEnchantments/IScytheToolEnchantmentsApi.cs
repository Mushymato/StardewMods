using StardewValley;

namespace ScytheToolEnchantments
{
    public interface IScytheToolEnchantmentsApi
    {
        /// <inheritdoc/>
        public bool HasGathererEnchantment(Tool tool);

        /// <inheritdoc/>
        public bool HasHorticulturistEnchantment(Tool tool);

        /// <inheritdoc/>
        public bool HasPalaeontologistEnchantment(Tool tool);

        /// <inheritdoc/>
        public bool HasReaperEnchantment(Tool tool);
    }
}
