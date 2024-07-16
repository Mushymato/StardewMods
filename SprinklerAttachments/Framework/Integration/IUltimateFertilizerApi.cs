using StardewValley;
using StardewValley.TerrainFeatures;

namespace SprinklerAttachments.Framework.Integration
{
    public interface IUltimateFertilizerApi
    {
        public const string ModId = "fox_white25.ultimate_fertilizer";
        /// <summary>Apply fertilizer to a HoeDirt instance without side-effects.</summary>
        /// <param name="dirt">The HoeDirt instance.</param>
        /// <param name="itemId">The fertilizer you want to apply.</param>
        /// <param name="who">Player instance to check if they have perks that boost speed.</param>
        /// <returns>Whether the fertilizer was applied.</returns>
        /// <remarks>This method does not check for valid fertilizer. You can check that manually via <see cref="HoeDirt.CheckApplyFertilizerRules"/>.</remarks>
        bool ApplyFertilizerOnDirt(HoeDirt dirt, string itemId, Farmer who);
    }
}