using StardewUI;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class MachineSelect(ModConfig config, Action<MachineCell> showPanelFor, Action<bool> exitThisMenu) : WrapperView
    {
        const int GUTTER_WIDTH = 500;
        const int GUTTER_HEIGHT = 400;
        internal static readonly Sprite CloseButton = new(Game1.mouseCursors, new(337, 494, 12, 12));

        protected override IView CreateView()
        {
            xTile.Dimensions.Size viewportSize = Game1.uiViewport.Size;
            float menuWidth = 704;
            float menuHeight = MathF.Max(400, viewportSize.Height - GUTTER_HEIGHT);

            ScrollableView scrollableView = new()
            {
                Name = "MachineSelect.View",
                Layout = LayoutParameters.FixedSize(menuWidth, menuHeight),
                Content = CreateMachineSelect()
            };
            Panel wrapper = new()
            {
                Layout = LayoutParameters.FitContent(),
                Children = [scrollableView]
            };
            Button closeBtn = new(defaultBackgroundSprite: CloseButton)
            {
                Margin = new Edges(Left: 96),
                Layout = LayoutParameters.FixedSize(48, 48)
            };
            closeBtn.LeftClick += ExitMenu;
            wrapper.FloatingElements.Add(new(closeBtn, FloatingPosition.AfterParent));

            return wrapper;
        }

        private IView CreateMachineSelect()
        {
            var machinesData = DataLoader.Machines(Game1.content);
            List<IView> cells = [];
            foreach ((string qId, MachineData machine) in machinesData)
            {
                if (ItemRegistry.GetData(qId) is not ParsedItemData itemData)
                    continue;
                if (machine.IsIncubator || machine.OutputRules == null || !machine.AllowFairyDust)
                    continue;

                RuleHelper ruleHelper = new(qId, itemData.DisplayName, machine, config);
                MachineCell cell = new(ruleHelper, itemData)
                {
                    Name = $"MachineSelect.{qId}"
                };
                cell.LeftClick += ShowPanel;

                cells.Add(cell);
            }
            return new Grid()
            {
                Name = "MachineSelect.Grid",
                ItemLayout = GridItemLayout.Count(8),
                Children = cells
            }; ;
        }

        private void ShowPanel(object? sender, ClickEventArgs e)
        {
            if (sender is MachineCell machineCell)
                showPanelFor(machineCell);
        }

        private void ExitMenu(object? sender, ClickEventArgs e)
        {
            exitThisMenu(true);
        }

    }
}