using StardewModdingAPI;
using StardewValley.GameData.Machines;

namespace MachineControlPanel.Framework
{
    internal static class Quirks
    {
        /// <summary>
        /// abuse of functional programming
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="modelList"></param>
        /// <param name="getId"></param>
        /// <param name="setIdSeq"></param>
        /// <param name="process"></param>
        private static void EnsureUniqueId<T>(List<T>? modelList, Func<T, string> getId, Action<T, int> setIdSeq, Action<T>? process = null)
        {
            if (modelList == null)
                return;
            Dictionary<string, List<T>> modelById = [];
            foreach (T model in modelList)
            {
                string id = getId(model);
                if (!modelById.ContainsKey(id))
                    modelById[id] = [];
                modelById[id].Add(model);
                process?.Invoke(model);
            }

            foreach ((string key, List<T> models) in modelById)
            {
                if (models.Count > 1)
                {
                    // edit second duplicate and on
                    int i = 1;
                    foreach (T model in models.GetRange(1, models.Count - 1))
                        setIdSeq(model, i++);
                }
            }
        }

        /// <summary>
        /// Check if every MachineOutputRule and every has a unique Id field.
        /// For any field that does not fufill this requirement, append an index.
        /// </summary>
        /// <param name="asset"></param>
        internal static void EnsureUniqueMachineOutputRuleId(IAssetData asset)
        {
            IDictionary<string, MachineData> data = asset.AsDictionary<string, MachineData>().Data;
            foreach ((string qItemId, MachineData machine) in data)
            {
                EnsureUniqueId(
                    machine.OutputRules,
                    (rule) => rule.Id,
                    (rule, seq) => rule.Id = $"{rule.Id}-rule-{seq}",
                    static (rule) =>
                    {
                        EnsureUniqueId(
                            rule.Triggers,
                            (trigger) => trigger.Id,
                            (trigger, seq) => trigger.Id = $"{trigger.Id}-trigger-{seq}"
                        );
                    }
                );
            }
        }
    }
}