using StardewUI;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class MachineSelect(
        ModConfig config,
        Action<string, IEnumerable<RuleIdent>, IEnumerable<string>> saveMachineRules,
        Action<bool> exitThisMenu
    ) : WrapperView
    {
        private const int GUTTER = 400;
        private const int GRID_W = 96;
        private int gridCount = 12;
        internal static readonly Sprite CloseButton = new(Game1.mouseCursors, new(337, 494, 12, 12));

        /// <summary>
        /// Make machine select grid view
        /// </summary>
        /// <returns></returns>
        protected override IView CreateView()
        {
            xTile.Dimensions.Size viewportSize = Game1.uiViewport.Size;
            gridCount = (int)MathF.Min(gridCount, MathF.Floor((viewportSize.Width - GUTTER) / 96));
            float menuWidth = gridCount * GRID_W;
            float menuHeight = MathF.Max(400, viewportSize.Height - GUTTER);

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

        /// <summary>
        /// Make machine select grid
        /// </summary>
        /// <returns></returns>
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
                ItemLayout = GridItemLayout.Count(gridCount),
                Children = cells
            }; ;
        }

        /// <summary>
        /// Show a rule list panel for the machine
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowPanel(object? sender, ClickEventArgs e)
        {
            if (sender is MachineCell machineCell)
            {
                machineCell.ruleHelper.GetRuleEntries();
                if (machineCell.ruleHelper.RuleEntries.Count == 0)
                    return;

                var overlay = new RuleListOverlay(
                    machineCell.ruleHelper,
                    saveMachineRules,
                    machineCell.UpdateEdited
                );
                Overlay.Push(overlay);
            }
        }

        /// <summary>
        /// Exit this menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitMenu(object? sender, ClickEventArgs e)
        {
            exitThisMenu(true);
        }

    }
}