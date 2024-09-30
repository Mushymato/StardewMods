using StardewUI;
using StardewValley;

namespace MachineControlPanel.Framework.UI.Integration
{
    /// <summary>
    /// Integration with lookup anything, provide a panel with an Item to use for LA
    /// </summary>
    internal class HoveredItemPanel : Panel
    {
        internal Item? HoveredItem { get; set; }
    }
}
