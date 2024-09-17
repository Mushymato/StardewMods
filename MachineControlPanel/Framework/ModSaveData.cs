using System.Collections.Immutable;
using StardewModdingAPI;

namespace MachineControlPanel.Framework
{
    public sealed record ModSaveDataEntry(
        ImmutableHashSet<RuleIdent> Rules,
        ImmutableHashSet<string> Inputs
    );
    public sealed record ModSaveDataEntryMessage(
        string QId,
        ModSaveDataEntry? Entry
    );
    public sealed class ModSaveData
    {
        public ISemanticVersion Version { get; set; } = new SemanticVersion(1, 0, 0);
        public Dictionary<string, ModSaveDataEntry> Disabled { get; set; } = [];
    }
}
