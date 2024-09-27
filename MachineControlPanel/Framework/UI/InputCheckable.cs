using Microsoft.Xna.Framework;
using StardewUI;
using StardewValley;

namespace MachineControlPanel.Framework.UI
{
    /// <summary>
    /// Holds some state for a checkable Panel > Image, not a view.
    /// </summary>
    internal sealed class InputCheckable
    {
        public static readonly Color COLOR_DISABLED = Color.Black * 0.9f;
        private bool isChecked = true;
        private readonly ValidInput input;
        private readonly Panel content;
        internal string QId => input.QId;

        internal Panel Content => content;
        internal Image Icon => (Image)content.Children.First();
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
                Icon.Tint = isChecked ? Color.White : COLOR_DISABLED;
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