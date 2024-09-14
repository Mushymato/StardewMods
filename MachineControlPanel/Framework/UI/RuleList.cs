using Microsoft.Xna.Framework;
using StardewUI;
using StardewValley;

namespace MachineControlPanel.Framework.UI
{
    internal class RuleListView(RuleHelper ruleHelper, HashSet<RuleIdent> disabled, Action<HashSet<RuleIdent>, HashSet<RuleIdent>> saveMachineRules) : WrapperView
    {
        private const int ROW_MARGIN = 4;
        private const int GUTTER_HEIGHT = 350;
        private static Sprite RightCaret => new(Game1.mouseCursors, new(448, 96, 24, 32));
        /// <summary>
        /// Modified 9-slice sprite used for the menu's horizontal divider, meant to be drawn over top of the
        /// <see cref="MenuBorder"/> to denote separate "sub-panels" or "sections" of the menu to group logically very
        /// different menu functions (as opposed to lines on a grid).
        /// </summary>
        public static Sprite ThinVDivider =>
            new(
                Game1.menuTexture,
                SourceRect: new(156, 384, 8, 54)
            );

        private static LayoutParameters IconLayout => LayoutParameters.FixedSize(64, 64);
        private static readonly List<RuleCheckBox> RuleCheckboxes = [];

        protected override IView CreateView()
        {
            var viewportSize = Game1.uiViewport.Size;
            // var menuWidth = MathF.Min(720, viewportSize.Width);
            var menuHeight = MathF.Min(720, viewportSize.Height - GUTTER_HEIGHT);
            // var itemSelector = CreateSidebar(menuHeight);
            LayoutParameters fitWidth = new() { Width = Length.Content(), Height = Length.Px(menuHeight) };
            RuleCheckboxes.Clear();

            return new ScrollableFrameView()
            {
                Name = "RuleListRoot",
                FrameLayout = fitWidth,
                ContentLayout = fitWidth,
                Title = $"{I18n.RuleList_Title()} [{ruleHelper.Name}]",
                Content = CreateRulesList(),
                Footer = Game1.IsMasterGame ? CreateSaveButtons() : null,
            };
        }

        protected IView CreateSaveButtons()
        {


            Button saveLabel = new(hoverBackgroundSprite: UiSprites.ButtonLight)
            {
                Text = I18n.RuleList_Save()
            };
            saveLabel.Click += OnSaveClick;

            Button resetLabel = new(hoverBackgroundSprite: UiSprites.ButtonLight)
            {
                Text = I18n.RuleList_Reset()
            };
            resetLabel.Click += OnResetClick;

            return new Lane()
            {
                Layout = LayoutParameters.FitContent(),
                Orientation = Orientation.Horizontal,
                Children = [saveLabel, resetLabel]
            };
        }

        private void OnSaveClick(object? sender, ClickEventArgs e)
        {
            if (sender is not Button)
                return;

            HashSet<RuleIdent> newEnabled = [];
            HashSet<RuleIdent> newDisabled = [];
            foreach (RuleCheckBox checkbox in RuleCheckboxes)
            {
                if (checkbox.IsChecked)
                    newEnabled.Add(checkbox.Ident);
                else
                    newDisabled.Add(checkbox.Ident);
            }
            saveMachineRules(newEnabled, newDisabled);

            Game1.playSound("bigSelect");
        }

        private void OnResetClick(object? sender, ClickEventArgs e)
        {
            if (sender is not Button)
                return;

            HashSet<RuleIdent> newEnabled = [];
            HashSet<RuleIdent> newDisabled = [];
            foreach (RuleCheckBox checkbox in RuleCheckboxes)
            {
                newEnabled.Add(checkbox.Ident);
                checkbox.IsChecked = true;
            }
            saveMachineRules(newEnabled, newDisabled);

            Game1.playSound("bigDeSelect");
        }

        protected IView CreateRulesList()
        {
            List<RuleEntry> rules = ruleHelper.RuleEntries;

            List<List<RuleEntry>> rulesColumns;
            if (rules.Count < 13)
            {
                rulesColumns = [rules];
            }
            else
            {
                int colS = (int)Math.Ceiling(rules.Count / Math.Min(rules.Count / 12f, 3));
                // rulesColumns = [
                //     rules.GetRange(0, mid),
                //     rules.GetRange(mid, rules.Count - mid),
                // ];
                rulesColumns = [];
                for (int i = 0; i < rules.Count; i += colS)
                {
                    rulesColumns.Add(rules.GetRange(i, Math.Min(colS, rules.Count - i)));
                }
            }

            List<IView> columns = [];
            int seq = 0;
            foreach (var rulesC in rulesColumns)
            {

                int inputSize = rulesC.Max((rule) => rule.Inputs.Count);
                int outputSize = rulesC.Max((rule) => rule.Outputs.Count);
                LayoutParameters inputLayout = new() { Width = Length.Px((64 + ROW_MARGIN * 2) * inputSize + ROW_MARGIN * 2), Height = Length.Content() };
                LayoutParameters outputLayout = new() { Width = Length.Px((64 + ROW_MARGIN * 2) * outputSize + ROW_MARGIN * 2), Height = Length.Content() };

                if (columns.Count > 0)
                {
                    columns.Add(new Image()
                    {
                        Layout = new() { Width = Length.Px(ThinVDivider.Size.X), Height = Length.Stretch() },
                        Fit = ImageFit.Stretch,
                        Sprite = ThinVDivider,
                    });
                }
                columns.Add(new Lane()
                {
                    Name = $"RuleListColumn_{++seq}",
                    Orientation = Orientation.Vertical,
                    Children = rulesC.Select((rule) => CreateRuleListEntry(rule, inputLayout, outputLayout)).ToList(),
                    Margin = new Edges(6),
                });
            }

            return new Lane()
            {
                Name = "RuleList",
                Orientation = Orientation.Horizontal,
                Children = columns,
            };
        }

        protected IView CreateRuleListEntry(RuleEntry rule, LayoutParameters inputLayout, LayoutParameters outputLayout)
        {
            IView firstView;
            if (Game1.IsMasterGame && rule.CanCheck)
            {
                RuleCheckBox checkbox = new(rule)
                {
                    IsChecked = !disabled.Contains(rule.Ident),
                };
                RuleCheckboxes.Add(checkbox);
                firstView = checkbox;
            }
            else
            {
                firstView = new Image()
                {
                    Sprite = disabled.Contains(rule.Ident) ? UiSprites.CheckboxUnchecked : UiSprites.CheckboxChecked,
                    Tint = Color.White * 0.5f,
                    Layout = LayoutParameters.FitContent(),
                    IsFocusable = false
                };
            }

            Lane inputs = FormRuleItemLane(rule.Inputs, $"{rule.Repr}.Inputs");
            inputs.HorizontalContentAlignment = Alignment.End;
            inputs.Layout = inputLayout;
            Lane outputs = FormRuleItemLane(rule.Outputs, $"{rule.Repr}.Outputs");
            outputs.Layout = outputLayout;
            outputs.HorizontalContentAlignment = Alignment.Start;
            Image arrow = new()
            {
                Name = $"{rule.Repr}.Arrow",
                Layout = LayoutParameters.FitContent(),
                Padding = new(20, 16),
                Sprite = RightCaret
            };
            return new Lane()
            {
                Name = $"{rule.Repr}.Lane",
                Layout = LayoutParameters.FitContent(),
                Orientation = Orientation.Horizontal,
                Children = [firstView, inputs, arrow, outputs],
                Margin = new(Left: 12),
                HorizontalContentAlignment = Alignment.Start,
                VerticalContentAlignment = Alignment.Middle,
            };
        }

        protected static Lane FormRuleItemLane(List<RuleItem> ruleItems, string prefix)
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
                    Name = $"{prefix}.{i++}",
                    Layout = IconLayout,
                    Margin = new Edges(ROW_MARGIN),
                    Children = iconImgs,
                    Tooltip = string.Join('\n', ruleItem.Tooltip.Select((tip) => tip.Trim())),
                    // IsFocusable = true
                });
            }
            return new Lane()
            {
                Name = prefix,
                Layout = LayoutParameters.FitContent(),
                Orientation = Orientation.Horizontal,
                VerticalContentAlignment = Alignment.Middle,
                Children = content
            };
        }
    }
}