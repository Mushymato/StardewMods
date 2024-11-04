using System.Diagnostics.CodeAnalysis;
using StardewValley.Objects;
using StardewObject = StardewValley.Object;

namespace SprinklerAttachments;

public interface ISprinklerAttachmentsApi
{
    /// <summary>
    /// Perform the sowing activity (planting of seed and fertilizer from chest) on the sprinkler, if applicable.
    /// </summary>
    /// <param name="sprinkler">Stardew.Object</param>
    void ApplySowing(StardewObject sprinkler);
    /// <summary>
    /// Get the sprinkler attachment Stardew.Object if one is present on the sprinkler
    /// </summary>
    /// <param name="sprinkler">Stardew.Object</param>
    /// <param name="attachment">Stardew.Object</param>
    /// <returns></returns>
    bool TryGetSprinklerAttachment(StardewObject sprinkler, [NotNullWhen(true)] out StardewObject? attachment);
    /// <summary>
    /// Get the sprinkler attachment plus the chest holding seed/fertilizer, if one is present on the sprinkler
    /// </summary>
    /// <param name="sprinkler">Stardew.Object</param>
    /// <param name="attachment">Stardew.Object</param>
    /// <param name="chest">Stardew.Chest</param>
    /// <returns></returns>
    bool TryGetIntakeChest(StardewObject sprinkler, [NotNullWhen(true)] out StardewObject? attachment, [NotNullWhen(true)] out Chest? chest);
    /// <summary>
    /// Check if an attachment is allowed to do sowing
    /// </summary>
    /// <param name="attachment"></param>
    /// <returns></returns>
    bool IsSowing(StardewObject attachment);
    /// <summary>
    /// Check if an attachment will pressurize (extend sprinkler range)
    /// </summary>
    /// <param name="attachment"></param>
    /// <returns></returns>
    bool IsPressurize(StardewObject attachment);
}

