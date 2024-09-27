using StardewUI;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class MachineMenu(
        ModConfig config, Action<string, IEnumerable<RuleIdent>, IEnumerable<string>> saveMachineRules
    ) : ViewMenu<MachineSelect>
    {
        protected override MachineSelect CreateView()
        {
            initializeUpperRightCloseButton();
            return new(config, saveMachineRules, exitThisMenu: exitThisMenu);
        }
    }
}
