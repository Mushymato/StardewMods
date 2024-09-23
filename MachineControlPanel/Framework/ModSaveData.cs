using System.Collections.Immutable;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;

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

        /// <summary>
        /// Clear inputs and rules known to be invalid, usually due to removing mods.
        /// It is possible for mod to add trigger and invalidate certain indexes though, can't easily account for that so whatever
        /// </summary>
        public void ClearInvalidData()
        {
            var machinesData = DataLoader.Machines(Game1.content);
            foreach ((string qId, ModSaveDataEntry msdEntry) in Disabled)
            {
                if (ItemRegistry.GetData(qId) is not ParsedItemData itemData || !machinesData.TryGetValue(qId, out MachineData? machine))
                {
                    Disabled.Remove(qId);
                    continue;
                }

                HashSet<RuleIdent> allIdents = [];
                foreach (MachineOutputRule rule in machine.OutputRules)
                {
                    int seq = 0;
                    foreach (MachineOutputTriggerRule trigger in rule.Triggers)
                    {
                        RuleIdent ident = new(rule.Id, trigger.Id, seq++);
                        allIdents.Add(ident);
                    }
                }

                var newRules = msdEntry.Rules.Where(allIdents.Contains).ToImmutableHashSet();
                var newInputs = msdEntry.Inputs.Where((input) => ItemRegistry.GetData(input) != null).ToImmutableHashSet();
                if (newRules.Count != msdEntry.Rules.Count || newInputs.Count != msdEntry.Inputs.Count)
                {
                    if (newRules.IsEmpty && newInputs.IsEmpty)
                        Disabled.Remove(qId);
                    else
                        Disabled[qId] = new(newRules, newInputs);
                }
            }
        }
    }
}
