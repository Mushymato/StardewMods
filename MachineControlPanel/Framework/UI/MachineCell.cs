using Microsoft.Xna.Framework;
using StardewUI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel.Framework.UI
{
    internal class MachineCell : Frame
    {
        internal static readonly Sprite bgSprite = new(Game1.mouseCursors, new(384, 396, 15, 15), new(5), new(Scale: 4));
        internal RuleHelper ruleHelper;
        public Color BgTint
        {
            get => bgTint.Value;
            set => bgTint.Value = value;
        }
        private readonly DirtyTracker<Color> bgTint = new(Color.White);

        internal MachineCell(RuleHelper ruleHelper, ParsedItemData itemData) : base()
        {
            this.ruleHelper = ruleHelper;
            Padding = new(16);
            Background = bgSprite;
            BorderThickness = bgSprite.FixedEdges!;
            Tooltip = ruleHelper.Name;
            IsFocusable = true;
            UpdateEdited();
            Content = new Image()
            {
                Sprite = new(itemData.GetTexture(), itemData.GetSourceRect()),
                Layout = LayoutParameters.FixedSize(64, 128),
                ShadowAlpha = 1,
                IsFocusable = false
            };
        }

        internal void UpdateEdited()
        {
            BackgroundTint = Color.White * (ruleHelper.HasDisabled ? 1 : 0.5f);
        }

    }
}