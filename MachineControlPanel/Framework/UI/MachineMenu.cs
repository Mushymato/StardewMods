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
            return new(config, ShowPanelFor);
        }

        private void ShowPanelFor(MachineCell machineCell)
        {
            machineCell.ruleHelper.GetRuleEntries();
            if (machineCell.ruleHelper.RuleEntries.Count == 0)
                return;
            var overlay = new RuleListOverlay(machineCell.ruleHelper, saveMachineRules);
            Overlay.Push(overlay);
        }
    }
}
