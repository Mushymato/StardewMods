using Microsoft.Xna.Framework;

// Better Sprinklers Plus
// https://github.com/jamescodesthings/smapi-better-sprinklers
// https://www.nexusmods.com/stardewvalley/mods/17767

namespace SprinklerAttachments.Framework.Integration;

/// <summary>The API which provides access to Better Sprinklers for other mods.</summary>
public interface IBetterSprinklersApi
{
    public static readonly string[] ModIds = new string[] {
            "com.CodesThings.BetterSprinklersPlus",
            "com.gingajamie.BetterSprinklersPlus"
        };

    /// <summary>Get the relative tile coverage by supported sprinkler ID.</summary>
    IDictionary<int, Vector2[]> GetSprinklerCoverage();
}
