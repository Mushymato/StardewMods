using StardewUI;
using StardewValley;

namespace MachineControlPanel.Framework.UI
{
    internal class RuleCheckbox : Image
    {
        private static Sprite CheckOn => new(Game1.mouseCursors, SourceRect: new(236, 425, 9, 9));
        private static Sprite CheckOff => new(Game1.mouseCursors, SourceRect: new(227, 425, 9, 9));

        private readonly RuleEntry rule;
        public RuleIdent Ident => rule.Ident;
        private bool isChecked = true;
        public bool IsChecked
        {
            get
            {
                return isChecked;
            }
            set
            {
                isChecked = value;
                Sprite = isChecked ? CheckOn : CheckOff;
            }
        }

        internal RuleCheckbox(RuleEntry rule, bool initChecked) : base()
        {
            this.rule = rule;
            Name = $"{rule.Repr}.Check";
            Layout = LayoutParameters.FixedSize(36, 36);
            Padding = new Edges(14);
            IsFocusable = true;
            Click += OnRuleCheckboxClick;
            isChecked = initChecked;
            Sprite = isChecked ? CheckOn : CheckOff;
        }

        private void OnRuleCheckboxClick(object? sender, ClickEventArgs e)
        {
            if (sender is not RuleCheckbox checkbox)
                return;
            checkbox.IsChecked = !checkbox.IsChecked;
        }
    }
}