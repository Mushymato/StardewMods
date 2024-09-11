using StardewUI;
using StardewValley;

namespace MachineControlPanel.Framework.UI
{
    internal class TextButton : Banner
    {
        private static Sprite Button => new(Game1.mouseCursors, new(256, 256, 10, 10), new(2), new(Scale: 4));
        private static Sprite ButtonHover => new(Game1.mouseCursors, new(267, 256, 10, 10), new(2), new(Scale: 4));

        internal TextButton() : base()
        {
            // this.rule = rule;
            // Name = $"{rule.Repr}.Check";
            // Layout = LayoutParameters.FixedSize(36, 36);
            // Padding = new Edges(14);
            // IsFocusable = true;
            // Click += OnRuleCheckboxClick;
            // isChecked = initChecked;
            // Sprite = isChecked ? CheckOn : CheckOff;
            Layout = LayoutParameters.FitContent();
            Padding = new(12);
            Margin = new(8);
            Background = Button;
            // BackgroundBorderThickness = Button.FixedEdges!,
            IsFocusable = true;
            PointerEnter += OnButtonPointerEnter;
            PointerLeave += OnButtonPointerLeave;
        }

        private void OnButtonPointerEnter(object? sender, PointerEventArgs e)
        {
            if (sender is not Banner banner)
                return;
            banner.Background = ButtonHover;
        }

        private void OnButtonPointerLeave(object? sender, PointerEventArgs e)
        {
            if (sender is not Banner banner)
                return;
            banner.Background = Button;
        }

    }
}