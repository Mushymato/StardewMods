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
            // RuleHelper ruleHelper = new(
            //     machineCell.itemData.QualifiedItemId,
            //     machineCell.itemData.DisplayName,
            //     machineCell.machine, config
            // );
            // if (ruleHelper.RuleEntries.Count == 0)
            //     return;
            SetChildMenu(new RuleMenu(
                machineCell.ruleHelper,
                saveMachineRules
            ));
        }
    }
}
