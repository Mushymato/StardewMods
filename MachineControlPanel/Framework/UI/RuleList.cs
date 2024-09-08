using Microsoft.Xna.Framework;
using StardewUI;
using StardewValley;

namespace MachineControlPanel.Framework.UI
{
    internal class RuleListView(RuleHelper rule) : WrapperView
    {
        private const int CONTENT_MARGIN = 6;
        private const int ROW_MARGIN = 4;
        private const int GUTTER_HEIGHT = 150;
        private static Sprite RightCaret => new(Game1.mouseCursors, SourceRect: new(448, 96, 24, 32));
        private static Sprite CheckOn => new(Game1.mouseCursors, SourceRect: new(236, 425, 9, 9));
        private static Sprite CheckOff => new(Game1.mouseCursors, SourceRect: new(227, 425, 9, 9));

        private static LayoutParameters IconLayout => LayoutParameters.FixedSize(64, 64);

        protected override IView CreateView()
        {
            var viewportSize = Game1.uiViewport.Size;
            var menuWidth = MathF.Min(720, viewportSize.Width);
            var menuHeight = MathF.Min(720, viewportSize.Height - GUTTER_HEIGHT * 2);
            var ruleList = CreateRuleList();
            // var itemSelector = CreateSidebar(menuHeight);
            return new ScrollableFrameView()
            {
                Name = "RuleListRoot",
                FrameLayout = LayoutParameters.FixedSize(menuWidth, menuHeight),
                Title = $"{I18n.RuleList_Title()} [{rule.Name}]",
                Content = ruleList,
                // Sidebar = itemSelector,
                // SidebarWidth = 240,
            };
        }

        protected IView CreateRuleList()
        {
            // var entries = rule.GetRuleEntries().Select(CreateRuleListEntry).ToList();
            var rules = rule.GetRuleEntries();
            int inputSize = rules.Max((rule) => rule.Inputs.Count);
            int outputSize = rules.Max((rule) => rule.Outputs.Count);
            LayoutParameters inputLayout = new() { Width = Length.Px(64 * inputSize + ROW_MARGIN * 2), Height = Length.Content() };
            LayoutParameters outputLayout = new() { Width = Length.Px(64 * outputSize + ROW_MARGIN * 2), Height = Length.Content() };
            return new Lane()
            {
                Name = "RuleList",
                Orientation = Orientation.Vertical,
                Children = rules.Select((rule) => CreateRuleListEntry(rule, inputLayout, outputLayout)).ToList(),
                Margin = new Edges(CONTENT_MARGIN),
            };
        }

        private void OnRuleCheckboxClick(object? sender, ClickEventArgs e)
        {
            if (sender is not Image checkbox)
                return;
            checkbox.Sprite = checkbox.Sprite == CheckOn ? CheckOff : CheckOn;
        }

        protected IView CreateRuleListEntry(RuleEntry rule, LayoutParameters inputLayout, LayoutParameters outputLayout)
        {
            Image checkbox = new()
            {
                Name = $"{rule.Id}_Check",
                Layout = LayoutParameters.FixedSize(36, 36),
                Padding = new Edges(14),
                Margin = new Edges(ROW_MARGIN * 2, ROW_MARGIN),
                Sprite = CheckOn,
                IsFocusable = true
            };
            checkbox.Click += OnRuleCheckboxClick;
            Lane inputs = FormRuleItemLane(rule.Inputs, rule.Id, "Inputs");
            inputs.HorizontalContentAlignment = Alignment.End;
            inputs.Layout = inputLayout;
            Lane outputs = FormRuleItemLane(rule.Outputs, rule.Id, "Outputs");
            outputs.Layout = outputLayout;
            outputs.HorizontalContentAlignment = Alignment.Start;
            Image arrow = new()
            {
                Name = $"{rule.Id}_Arrow",
                Layout = LayoutParameters.FitContent(),
                Padding = new(20, 16),
                Sprite = RightCaret
            };
            // Lane ruleLane = new()
            return new Lane()
            {
                Name = $"{rule.Id}_Lane",
                Layout = LayoutParameters.AutoRow(),
                Orientation = Orientation.Horizontal,
                Children = [checkbox, inputs, arrow, outputs],
                HorizontalContentAlignment = Alignment.Start,
                VerticalContentAlignment = Alignment.Middle,
            };
            // return new Frame()
            // {
            //     Name = $"{rule.Id}_Frame",
            //     Layout = LayoutParameters.AutoRow(),
            //     // Border = UiSprites.ControlBorder,
            //     // BorderThickness = UiSprites.ControlBorder.FixedEdges!,
            //     HorizontalContentAlignment = Alignment.Start,
            //     VerticalContentAlignment = Alignment.Middle,
            //     Content = ruleLane,
            // };
        }

        protected static Lane FormRuleItemLane(List<RuleItem> ruleItems, string ruleId, string suffix)
        {
            List<IView> content = [];
            int i = 0;
            foreach (var ruleItem in ruleItems)
            {
                List<IView> iconImgs = [];
                foreach (var icon in ruleItem.Icons)
                {
                    iconImgs.Add(new Image()
                    {
                        Layout = LayoutParameters.FixedSize(
                            icon.Img.Size.X * icon.Scale,
                            icon.Img.Size.X * icon.Scale
                        ),
                        Padding = icon.Edg,
                        Sprite = icon.Img,
                        IsFocusable = true,
                        Tint = icon.Tint ?? Color.White
                    });
                }
                content.Add(new Panel()
                {
                    Name = $"{ruleId}_{suffix}_{i}",
                    Layout = IconLayout,
                    Margin = new Edges(ROW_MARGIN),
                    Children = iconImgs,
                    Tooltip = string.Join('\n', ruleItem.Tooltip.Select((tip) => tip.Trim())),
                    // IsFocusable = true
                });
                i++;
            }
            return new Lane()
            {
                Name = $"{ruleId}_{suffix}",
                Orientation = Orientation.Horizontal,
                VerticalContentAlignment = Alignment.Middle,
                Children = content
            };
        }
    }
}