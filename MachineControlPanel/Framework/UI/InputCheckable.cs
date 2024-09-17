using Microsoft.Xna.Framework;
using StardewUI;
using StardewValley;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class InputCheckable
    {
        private bool isChecked = true;
        public readonly string QId;
        private readonly Panel content;
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
        public Panel Content => content;
        public bool IsChecked
        {
            get => isChecked;
            set
            {
                if (value == isChecked)
                {
                    return;
                }
                isChecked = value;
                ((Image)content.Children.First()).Tint = isChecked ? Color.White : Color.Black * 0.8f;
            }
        }

        internal InputCheckable(string QId, Panel content) : base()
        {
            this.QId = QId;
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