using StardewModdingAPI;

namespace MachineControlPanel.Framework
{
    public sealed class ModSaveData
    {
        public ISemanticVersion Version { get; set; } = new SemanticVersion(1, 0, 0);
        public HashSet<RuleIdent> Disabled { get; set; } = [];
    }
}
