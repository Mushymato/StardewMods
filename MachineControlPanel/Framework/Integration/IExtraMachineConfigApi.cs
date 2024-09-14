using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Inventories;

namespace MachineControlPanel.Framework.Integration
{
    public interface IExtraMachineConfigApi
    {
        IList<(string, int)> GetExtraRequirements(MachineItemOutput outputData);
        IList<(string, int)> GetExtraTagsRequirements(MachineItemOutput outputData);
        IList<MachineItemOutput> GetExtraOutputs(MachineItemOutput outputData, MachineData? machineData);
        IList<Item>? GetFuelsForThisRecipe(MachineItemOutput outputData, Item inputItem, IInventory inventory);
    }
}