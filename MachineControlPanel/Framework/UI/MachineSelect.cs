using StardewUI;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class MachineSelect(Action<MachineCell> showPanelFor) : WrapperView
    {
        const int GUTTER_WIDTH = 500;
        const int GUTTER_HEIGHT = 400;
        protected override IView CreateView()
        {
            xTile.Dimensions.Size viewportSize = Game1.uiViewport.Size;
            float menuWidth = 704;
            float menuHeight = MathF.Max(400, viewportSize.Height - GUTTER_HEIGHT);

            return new ScrollableView()
            {
                Layout = LayoutParameters.FixedSize(menuWidth, menuHeight),
                Content = CreateMachineSelect()
            };
        }

        private Grid CreateMachineSelect()
        {
            var machinesData = DataLoader.Machines(Game1.content);
            List<IView> cells = [];
            foreach ((string qId, MachineData machine) in machinesData)
            {
                if (ItemRegistry.GetDataOrErrorItem(qId) is not ParsedItemData itemData)
                    continue;
                if (machine.IsIncubator || machine.OutputRules == null || !machine.AllowFairyDust)
                    continue;

                MachineCell cell = new(itemData, machine);
                cell.LeftClick += ShowPanel;
                cells.Add(cell);
            }

            return new Grid()
            {
                Name = "InputsGrid",
                ItemLayout = GridItemLayout.Count(8),
                Children = cells
            };
        }

        private void ShowPanel(object? sender, ClickEventArgs e)
        {
            if (sender is MachineCell machineCell)
                showPanelFor(machineCell);
        }

    }
}