using StardewUI;
using StardewValley.GameData.Machines;

namespace MachineControlPanel.Framework.UI
{
    internal class RuleMenu(RuleHelper rule) : ViewMenu<RuleListView>
    {
        protected override RuleListView CreateView()
        {
            return new(rule);
        }
    }
}