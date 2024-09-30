using MachineControlPanel.Framework.UI.Integration;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class MachineMenu(
        Action<string, IEnumerable<RuleIdent>, IEnumerable<string>> saveMachineRules
    ) : HoveredItemMenu<MachineSelect>
    {
        protected override MachineSelect CreateView()
        {
            initializeUpperRightCloseButton();
            return new(saveMachineRules, SetHoverEvents, exitThisMenu: exitThisMenu);
        }
    }
}
