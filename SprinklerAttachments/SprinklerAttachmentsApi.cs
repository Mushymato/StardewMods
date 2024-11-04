using System.Diagnostics.CodeAnalysis;
using StardewValley;
using StardewValley.Objects;
using StardewValley.GameData.Objects;
using StardewObject = StardewValley.Object;
using SprinklerAttachments.Framework;

namespace SprinklerAttachments;

public class SprinklerAttachmentsApi : ISprinklerAttachmentsApi
{
    /// <inheritdoc/>
    public void ApplySowing(StardewObject sprinkler)
    {
        SprinklerAttachment.ApplySowing(sprinkler);
    }
    /// <inheritdoc/>
    public bool TryGetSprinklerAttachment(StardewObject sprinkler, [NotNullWhen(true)] out StardewObject? attachment)
    {
        return SprinklerAttachment.TryGetSprinklerAttachment(sprinkler, out attachment);
    }
    /// <inheritdoc/>
    public bool TryGetIntakeChest(StardewObject sprinkler, [NotNullWhen(true)] out StardewObject? attachment, [NotNullWhen(true)] out Chest? chest)
    {
        return SprinklerAttachment.TryGetIntakeChest(sprinkler, out attachment, out chest);
    }
    /// <inheritdoc/>
    public bool IsSowing(StardewObject attachment)
    {
        if (ItemRegistry.GetData(attachment.QualifiedItemId)?.RawData is ObjectData data)
            return ModFieldHelper.IsSowing(data);
        return false;
    }
    /// <inheritdoc/>
    public bool IsPressurize(StardewObject attachment)
    {
        if (ItemRegistry.GetData(attachment.QualifiedItemId)?.RawData is ObjectData data)
            return ModFieldHelper.IsPressurize(data);
        return false;
    }
}

