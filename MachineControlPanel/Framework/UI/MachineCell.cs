using StardewUI;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel.Framework.UI
{
    internal class MachineCell : Frame
    {
        internal static readonly Sprite bgSprite = new(Game1.mouseCursors, new(384, 396, 15, 15), new(5), new(Scale: 4));
        internal ParsedItemData itemData;
        internal MachineData machine;

        internal MachineCell(ParsedItemData itemData, MachineData machine)
        {
            this.itemData = itemData;
            this.machine = machine;
            IsFocusable = true;
            Padding = new(12);
            Background = bgSprite;
            BorderThickness = bgSprite.FixedEdges!;
            Tooltip = itemData.DisplayName;

            Content = new Image()
            {
                Sprite = new(itemData.GetTexture(), itemData.GetSourceRect()),
                Layout = LayoutParameters.FixedSize(64, 128),
                ShadowAlpha = 1
            };
        }
    }
}