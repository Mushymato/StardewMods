using StardewUI;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class RuleCheckBox : CheckBox
    {
        private readonly RuleEntry rule;
        public RuleIdent Ident => rule.Ident;
        internal RuleCheckBox(RuleEntry rule) : base()
        {
            this.rule = rule;
        }
    }
}