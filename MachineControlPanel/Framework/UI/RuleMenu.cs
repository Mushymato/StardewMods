using System.Collections;
using StardewUI;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class RuleMenu(
        RuleHelper ruleHelper,
        Action<string, IEnumerable<RuleIdent>, IEnumerable<string>> saveMachineRules
    ) : ViewMenu<RuleListView>
    {
        protected override RuleListView CreateView()
        {
            return new(ruleHelper, saveMachineRules);
        }
    }
}