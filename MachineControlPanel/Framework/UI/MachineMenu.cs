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
            return new(config, ShowPanelFor, exitThisMenu);
        }

        private void ShowPanelFor(MachineCell machineCell)
        {
            machineCell.ruleHelper.GetRuleEntries();
            if (machineCell.ruleHelper.RuleEntries.Count == 0)
                return;

            // Overlay doesn't handle click on floating elements quite right
            // var overlay = new RuleListOverlay(machineCell.ruleHelper, saveMachineRules);
            // Overlay.Push(overlay);
            Game1.activeClickableMenu.SetChildMenu(new RuleListMenu(machineCell.ruleHelper, saveMachineRules, true));
        }
    }
}
