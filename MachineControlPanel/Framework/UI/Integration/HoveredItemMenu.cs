using StardewUI;
using StardewValley;

namespace MachineControlPanel.Framework.UI.Integration
{
    /// <summary>
    /// Integration with lookup anything, which expects a Item field called "hoveredItem" (or "HoveredItem")
    /// </summary>
    public abstract class HoveredItemMenu<T> : ViewMenu<T> where T : IView
    {
        public Item? hoveredItem; // wish this was a prop weh
        internal virtual void OnPointerEnter(object? sender, PointerEventArgs e)
        {
            if (sender is HoveredItemPanel panel)
                hoveredItem = panel.HoveredItem;
        }
        internal virtual void OnPointerLeave(object? sender, PointerEventArgs e)
        {
            hoveredItem = null;
        }
        internal virtual void SetHoverEvents(HoveredItemPanel hoverPanel)
        {
            hoverPanel.PointerEnter += OnPointerEnter;
            hoverPanel.PointerLeave += OnPointerLeave;
        }
        internal virtual void RemoveHoverEvents(HoveredItemPanel hoverPanel)
        {
            hoverPanel.PointerEnter -= OnPointerEnter;
            hoverPanel.PointerLeave -= OnPointerLeave;
        }
    }
}