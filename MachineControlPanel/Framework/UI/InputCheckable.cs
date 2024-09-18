using Microsoft.Xna.Framework;
using StardewUI;
using StardewValley;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class InputCheckable
    {
        public static readonly Color COLOR_DISABLED = Color.Black * 0.9f;
        private bool isChecked = true;
        private readonly ValidInput input;
        private readonly Panel content;
        internal string QId => input.QId;
        // private static readonly Image emojiXImage = new()
        // {
        //     // Layout = LayoutParameters.FixedSize(
        //     //         RuleHelper.EmojiX.Img.Size.X * RuleHelper.EmojiX.Scale,
        //     //         RuleHelper.EmojiX.Img.Size.X * RuleHelper.EmojiX.Scale
        //     //     ),
        //     // Padding = RuleHelper.EmojiX.Edge,
        //     // Sprite = RuleHelper.EmojiX.Img,
        //     // Tint = RuleHelper.EmojiX.Tint ?? Color.White
        //     Layout = LayoutParameters.FixedSize(14 * 3, 15 * 3),
        //     Sprite = new(Game1.mouseCursors, new(269, 471, 14, 15))
        // };
        internal Panel Content => content;
        internal bool IsChecked
        {
            get => isChecked;
            set
            {
                if (value == isChecked)
                {
                    return;
                }
                isChecked = value;
                ((Image)content.Children.First()).Tint = isChecked ? Color.White : COLOR_DISABLED;
            }
        }

        internal InputCheckable(ValidInput input, Panel content) : base()
        {
            this.input = input;
            this.content = content;
            content.HorizontalContentAlignment = Alignment.Middle;
            content.VerticalContentAlignment = Alignment.Middle;
            content.LeftClick += OnLeftClick;
        }

        private void OnLeftClick(object? sender, ClickEventArgs e)
        {
            Game1.playSound("drumkit6");
            IsChecked = !IsChecked;
        }
    }
}