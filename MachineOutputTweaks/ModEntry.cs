using System.Net;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;
using StardewObject = StardewValley.Object;

namespace MachineOutputTweaks
{
    public class ModEntry : Mod
    {
        private static readonly string[] ExcludedMachines = {
            "(BC)20", // recycling machine
        };
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
            IDictionary<string, MachineData> data = asset.AsDictionary<string, MachineData>().Data;
            foreach (KeyValuePair<string, MachineData> kv in data)
            {
                string qItemId = kv.Key;
                MachineData machine = kv.Value;
                if (machine.IsIncubator || machine.OutputRules == null || !machine.AllowFairyDust || ExcludedMachines.Contains(qItemId))
                    continue;

                foreach (MachineOutputRule rule in machine.OutputRules)
                {
                    if (rule.OutputItem == null)
                        continue;
                    if (rule.Triggers.Any((trig) => trig.Trigger != MachineOutputTrigger.ItemPlacedInMachine))
                        continue;
                    rule.OutputItem.ForEach(item =>
                    {
                        if (item is null || item.OutputMethod != null)
                            return;
                        bool isArtisan = false;
                        if (ItemRegistry.GetData(item.ItemId) is ParsedItemData itemData)
                            isArtisan = itemData.Category == StardewObject.artisanGoodsCategory;
                        else
                        {
                            string[] splitArgs = ArgUtility.SplitBySpace(item.ItemId);
                            if (splitArgs.Length < 3)
                                return;
                            isArtisan = true;
                            if (Utility.TryParseEnum<StardewObject.PreserveType>(splitArgs[1], out var type))
                                isArtisan = type != StardewObject.PreserveType.Bait && type != StardewObject.PreserveType.Roe;
                        }
                        if (isArtisan) // keep quality artisan recipes, 
                        {
                            if (item.Quality == 2)
                            { // special case large milk/egg, copy quality, but produce 2
                                item.StackModifiers ??= new List<QuantityModifier>();
                                item.StackModifiers.Add(new()
                                {
                                    Modification = QuantityModifier.ModificationType.Add,
                                    Amount = 1
                                });
                                item.Quality = -1;
                            }
                            item.CopyQuality = true;
                        }
                        else // increase output depending on input quality
                        {
                            item.StackModifiers ??= new List<QuantityModifier>();
                            item.StackModifiers.Add(new()
                            {
                                Condition = "ITEM_QUALITY Input 2 2",
                                Modification = QuantityModifier.ModificationType.Add,
                                Amount = 1
                            });
                            item.StackModifiers.Add(new()
                            {
                                Condition = "ITEM_QUALITY Input 4 4",
                                Modification = QuantityModifier.ModificationType.Add,
                                Amount = 2
                            });
                        }
                    });
                }
            }
        }
    }
}
