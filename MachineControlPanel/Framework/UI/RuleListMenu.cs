using MachineControlPanel.Framework.UI.Integration;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class RuleListMenu(
        RuleHelper ruleHelper,
        Action<string, IEnumerable<RuleIdent>, IEnumerable<string>> saveMachineRules,
        bool showExitX = false,
        Action? updateEdited = null
    ) : HoveredItemMenu<RuleListView>
    {
        protected override RuleListView CreateView()
        {
            return new(
                ruleHelper,
                saveMachineRules,
                SetHoverEvents,
                showExitX ? exitThisMenu : null,
                updateEdited: updateEdited
            );
        }
    }
}