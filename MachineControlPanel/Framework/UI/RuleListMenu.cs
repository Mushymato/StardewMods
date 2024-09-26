using StardewUI;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class RuleListMenu(
        RuleHelper ruleHelper,
        Action<string, IEnumerable<RuleIdent>, IEnumerable<string>> saveMachineRules,
        bool showExitX = false,
        Action? updateEdited = null
    ) : ViewMenu<RuleListView>
    {
        protected override RuleListView CreateView()
        {
            return new(ruleHelper, saveMachineRules, showExitX ? exitThisMenu : null, updateEdited: updateEdited);
        }
    }
}