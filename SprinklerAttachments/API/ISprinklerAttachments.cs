using System.Diagnostics.CodeAnalysis;
using StardewObject = StardewValley.Object;

namespace SprinklerAttachments.API
{
    public interface ISprinklerAttachmentsAPI
    {
        void ApplySowing(StardewObject sprinkler);
        bool TryGetSprinklerAttachment(StardewObject sprinkler, [NotNullWhen(true)] out StardewObject? attachment);
        bool TryGetIntakeChest(StardewObject sprinkler, [NotNullWhen(true)] out StardewObject? attachment);
        bool IsSowing(StardewObject attachment);
        bool IsPressurize(StardewObject attachment);

    }
}
