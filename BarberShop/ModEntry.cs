using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace BarberShop;

public sealed class ModEntry : Mod
{
    public const string ModId = "ESR.BarberShop";

    public override void Entry(IModHelper helper)
    {
        GameLocation.RegisterTileAction(ModId, TileAction);
        helper.ConsoleCommands.Add(ModId, "test barber menu", ConsoleESCustomize);
    }

    private bool TileAction(GameLocation location, string[] arg2, Farmer farmer, Point point)
    {
        CharacterCustomization menu = new(CharacterCustomization.Source.Wizard);
        ResetComponents(menu);
        Game1.activeClickableMenu = menu;
        return true;
    }

    private static void ConsoleESCustomize(string arg1, string[] arg2)
    {
        if (!Context.IsWorldReady)
            return;

        CharacterCustomization menu = new(CharacterCustomization.Source.Wizard);
        if (arg2.Length >= 1)
        {
            ResetComponents(menu);
        }
        Game1.activeClickableMenu = menu;
    }

    private static void ResetComponents(CharacterCustomization menu)
    {
        // remove gender buttons
        menu.genderButtons.Clear();
        // remove skin selection
        menu.leftSelectionButtons.RemoveAll(cc => cc.name == "Skin");
        menu.rightSelectionButtons.RemoveAll(cc => cc.name == "Skin");

        // remove eye color picker
        menu.eyeColorPicker = null;
        // magical knowledge about the label order
        menu.labels.RemoveRange(0, 5);
        // magical knowledge about removing eye color picker
        menu.colorPickerCCs.RemoveRange(0, 3);
        // hide name farmname fav
        menu.source = CharacterCustomization.Source.Dresser;
        menu.nameBoxCC.visible = false;
        menu.favThingBoxCC.visible = false;
        menu.farmnameBoxCC.visible = false;
        // hide random button
        menu.randomButton.visible = false;

        if (Game1.options.snappyMenus && Game1.options.gamepadControls)
        {
            menu.populateClickableComponentList();
            menu.snapToDefaultClickableComponent();
        }
    }
}
