using SpaceCore.Guidebooks;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SpacecoreGuidebookFontFix;

public sealed class ModEntry : Mod
{
    public const string ModId = "mushymato.SpacecoreGuidebookFontFix";

    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        LocalizedContentManager.OnLanguageChange += code =>
        {
            GuidebookFont.Fonts["default"] = Game1.smallFont;
            GuidebookFont.Fonts["tiny"] = Game1.tinyFont;
            GuidebookFont.Fonts["dialogue"] = Game1.dialogueFont;
        };
    }
}
