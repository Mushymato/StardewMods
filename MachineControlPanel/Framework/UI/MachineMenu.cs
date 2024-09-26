using StardewUI;
using StardewValley;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class MachineMenu(
        ModConfig config, Action<string, IEnumerable<RuleIdent>, IEnumerable<string>> saveMachineRules
    ) : ViewMenu<MachineSelect>
    {
        protected override MachineSelect CreateView()
        {
            initializeUpperRightCloseButton();
            return new(config, ShowPanelFor, exitThisMenu: exitThisMenu);
        }

        private void ShowPanelFor(MachineCell machineCell)
        {
            machineCell.ruleHelper.GetRuleEntries();
            if (machineCell.ruleHelper.RuleEntries.Count == 0)
                return;

            // Overlay doesn't handle click on floating elements quite right
            var overlay = new RuleListOverlay(
                machineCell.ruleHelper,
                saveMachineRules,
                machineCell.UpdateEdited
            );
            Overlay.Push(overlay);
        }
    }
}
