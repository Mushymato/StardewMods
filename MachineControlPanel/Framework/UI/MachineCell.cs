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
        internal RuleHelper ruleHelper;

        internal MachineCell(RuleHelper ruleHelper, ParsedItemData itemData) : base()
        {
            this.ruleHelper = ruleHelper;
            Padding = new(12);
            Background = bgSprite;
            BorderThickness = bgSprite.FixedEdges!;
            Tooltip = ruleHelper.Name;
            IsFocusable = true;

            Content = new Image()
            {
                Sprite = new(itemData.GetTexture(), itemData.GetSourceRect()),
                Layout = LayoutParameters.FixedSize(64, 128),
                ShadowAlpha = 1,
                IsFocusable = false
            };
        }
    }
}