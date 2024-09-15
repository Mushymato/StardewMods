using StardewUI;
using StardewValley;

// TODO: i should just make this a tab, instead of 10000 buttons
namespace MachineControlPanel.Framework.UI
{
    internal sealed class InputShowButton : Button
    {
        private readonly RuleEntry rule;
        // internal static Sprite bgSprite = new(Game1.mouseCursors, new(392, 361, 10, 11));
        // internal static Sprite hvSprite = new(Game1.mouseCursors, new(402, 361, 10, 11));
        internal static Sprite bgSprite = new(Game1.mouseCursors, new(184, 345, 7, 8));
        internal InputItemGrid ItemGrid => new(rule);

        internal InputShowButton(RuleEntry rule) : base(bgSprite)
        {
            this.rule = rule;
            Layout = LayoutParameters.FixedSize(30, 33);
            Margin = new(12);
        }
    }

    internal sealed class InputItemGrid : Grid
    {
        private readonly RuleEntry rule;
        internal Button backButton;
        internal InputItemGrid(RuleEntry rule) : base()
        {
            this.rule = rule;
            backButton = new()
            {
                Text = "Back",
            };
            // Children = [backButton];
        }
    }
}