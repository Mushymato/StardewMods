using StardewUI;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class MachineMenu(
        Action<string, IEnumerable<RuleIdent>, IEnumerable<string>> saveMachineRules
    ) : ViewMenu<MachineSelect>
    {
        protected override MachineSelect CreateView()
        {
            initializeUpperRightCloseButton();
            return new(saveMachineRules, exitThisMenu: exitThisMenu);
        }
    }
}
