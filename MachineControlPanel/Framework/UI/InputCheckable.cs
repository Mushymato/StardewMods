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
        private bool isImplicitOff = false;
        private readonly ValidInput input;
        private readonly Panel content;
        internal string QId => input.Rule.Item?.QualifiedItemId ?? "ERROR";
        internal HashSet<RuleIdent> Idents => input.Idents;

        internal Panel Content => content;
        internal Image Icon => (Image)content.Children.First();
        internal bool IsChecked
        {
            get => isChecked;
            set
            {
                if (value == isChecked)
                    return;
                isChecked = value;
                UpdateTint();
            }
        }
        internal bool IsImplicitOff
        {
            get => isImplicitOff;
            set
            {
                if (value == isImplicitOff)
                    return;
                isImplicitOff = value;
                UpdateTint();
            }
        }

        internal InputCheckable(ValidInput input, Panel content, bool canCheck) : base()
        {
            this.input = input;
            this.content = content;
            content.HorizontalContentAlignment = Alignment.Middle;
            content.VerticalContentAlignment = Alignment.Middle;
            if (canCheck)
                content.LeftClick += OnLeftClick;
        }

        private void OnLeftClick(object? sender, ClickEventArgs e)
        {
            Game1.playSound("drumkit6");
            if (!isImplicitOff)
                IsChecked = !isChecked;
        }

        private void UpdateTint()
        {
            Icon.Tint = isChecked ? Color.White : COLOR_DISABLED;
            if (isImplicitOff)
                Icon.Tint = Icon.Tint * 0.5f;
        }
    }
}