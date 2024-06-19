using Force.DeepCloner;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using StardewValley.GameData.Machines;

namespace SprinklerAttachments
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.Content.AssetRequested += OnAssetRequested;
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Data/Machines"))
            {
                e.Edit(EditDataMachines, AssetEditPriority.Late);
            }
        }

        private void EditDataMachines(IAssetData asset)
        {
            // QuantityModifier decreaseQuality = new()
            // {
            //     Condition = "ITEM_QUALITY Input 1 4",
            //     Modification = QuantityModifier.ModificationType.Subtract,
            //     Amount = 1
            // };

            IDictionary<string, MachineData> data = asset.AsDictionary<string, MachineData>().Data;
            foreach (KeyValuePair<string, MachineData> kv in data)
            {
                string qItemId = kv.Key;
                MachineData machine = kv.Value;
                if (machine.IsIncubator || machine.OutputRules == null || !machine.AllowFairyDust)
                    continue;

                foreach (MachineOutputRule rule in machine.OutputRules)
                {
                    if (rule.OutputItem == null)
                        continue;
                    if (rule.Triggers.Any((trig) => trig.Trigger != MachineOutputTrigger.ItemPlacedInMachine))
                        continue;
                    rule.OutputItem.ForEach(item =>
                    {
                        if (item is not null && item.OutputMethod == null && item.QualityModifiers == null)
                        {
                            item.QualityModifiers = new(){
                                    new(){
                                        Condition = "ITEM_QUALITY Input 4 4",
                                        Modification = QuantityModifier.ModificationType.Set,
                                        Amount = 2
                                    },
                                    new(){
                                        Condition = "ITEM_QUALITY Input 2 2",
                                        Modification = QuantityModifier.ModificationType.Set,
                                        Amount = 1
                                    },
                            };
                            //item.CopyQuality = true;
                        }
                    });
                }
            }
        }
    }
}
