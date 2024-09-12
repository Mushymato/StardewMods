using StardewUI;
using StardewValley;

namespace MachineControlPanel.Framework.UI
{
    internal class RuleCheckBox : CheckBox
    {
        private readonly RuleEntry rule;
        public RuleIdent Ident => rule.Ident;
        internal RuleCheckBox(RuleEntry rule) : base()
        {
            this.rule = rule;
        }
    }
}