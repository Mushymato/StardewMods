using StardewUI;

namespace MachineControlPanel.Framework.UI
{
    internal class RuleMenu(RuleHelper ruleHelper, HashSet<RuleIdent> disabled, Action<HashSet<RuleIdent>, HashSet<RuleIdent>> saveMachineRules) : ViewMenu<RuleListView>
    {
        protected override RuleListView CreateView()
        {
            return new(ruleHelper, disabled, saveMachineRules);
        }
    }
}