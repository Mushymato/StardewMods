using MachineControlPanel.Framework.UI.Integration;
using StardewUI;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class RuleListOverlay(
        RuleHelper ruleHelper,
        Action<string, IEnumerable<RuleIdent>, IEnumerable<string>> saveMachineRules,
        Action<HoveredItemPanel> setHoverEvents,
        Action? updateEdited = null) : FullScreenOverlay
    {
        protected override RuleListView CreateView()
        {
            return new(ruleHelper, saveMachineRules, setHoverEvents, updateEdited: updateEdited);
        }
    }
}