using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using StardewUI;
using StardewValley;
using xTile.Dimensions;

namespace MachineControlPanel.Framework.UI
{
    internal sealed record ContentChangeArgs(IView? Nextview);

    internal sealed class RuleListView(RuleHelper ruleHelper, HashSet<RuleIdent> disabled, Action<HashSet<RuleIdent>, HashSet<RuleIdent>> saveMachineRules) : WrapperView
    {
        private const int ROW_MARGIN = 4;
        private const int COL_MARGIN = 6;
        private const int ROW_W = 64 + ROW_MARGIN * 2;
        private const int BOX_W = ROW_MARGIN * 3;
        private const int MIN_HEIGHT = 400;
        private const int GUTTER_HEIGHT = 400;
        private static Sprite RightCaret => new(Game1.mouseCursors, new(448, 96, 24, 32));
        public static Sprite ThinVDivider =>
            new(
                Game1.menuTexture,
                SourceRect: new(156, 384, 8, 54)
            );

        private static LayoutParameters IconLayout => LayoutParameters.FixedSize(64, 64);
        internal readonly Dictionary<RuleIdent, RuleCheckBox> ruleCheckboxes = [];
        private ScrollableFrameView? container = null;
        private Lane? rulesList = null;
        private Lane? footer = null;

        protected override IView CreateView()
        {
            Size viewportSize = Game1.uiViewport.Size;
            float menuHeight = MathF.Max(MIN_HEIGHT, viewportSize.Height - GUTTER_HEIGHT);
            // var itemSelector = CreateSidebar(menuHeight);
            var rules = CreateRulesList(viewportSize, ref menuHeight);
            LayoutParameters fitWidth = new() { Width = Length.Content(), Height = Length.Px(menuHeight) };

            container = new ScrollableFrameView()
            {
                Name = "RuleListRoot",
                FrameLayout = fitWidth,
                ContentLayout = fitWidth,
                Title = $"{I18n.RuleList_Title()} [{ruleHelper.Name}]",
                Content = rules,
                Footer = Game1.IsMasterGame ? CreateSaveButtons() : null,
            };
            return container;
        }

        private IView CreateSaveButtons()
        {
            Button saveLabel = new(hoverBackgroundSprite: UiSprites.ButtonLight)
            {
                Text = I18n.RuleList_Save()
            };
            saveLabel.Click += SaveRules;

            Button resetLabel = new(hoverBackgroundSprite: UiSprites.ButtonLight)
            {
                Text = I18n.RuleList_Reset()
            };
            resetLabel.Click += ResetRules;

            footer = new Lane()
            {
                Layout = LayoutParameters.FitContent(),
                Orientation = Orientation.Horizontal,
                Children = [saveLabel, resetLabel]
            };
            return footer;
        }

        private void SaveRules(object? sender, ClickEventArgs e)
        {
            if (sender is not Button)
                return;

            HashSet<RuleIdent> newEnabled = [];
            HashSet<RuleIdent> newDisabled = [];
            foreach (var kv in ruleCheckboxes)
            {
                Console.WriteLine($"{kv.Key}: {kv.Value.IsChecked}");
                if (kv.Value.IsChecked)
                    newEnabled.Add(kv.Key);
                else
                    newDisabled.Add(kv.Key);
            }
            saveMachineRules(newEnabled, newDisabled);

            Game1.playSound("bigSelect");
        }

        private void ResetRules(object? sender, ClickEventArgs e)
        {
            if (sender is not Button)
                return;

            HashSet<RuleIdent> newEnabled = [];
            HashSet<RuleIdent> newDisabled = [];
            foreach (var kv in ruleCheckboxes)
            {
                newEnabled.Add(kv.Key);
                kv.Value.IsChecked = true;
            }
            saveMachineRules(newEnabled, newDisabled);

            Game1.playSound("bigDeSelect");
        }

        private void ShowItemGrid(object? sender, ClickEventArgs e)
        {
            if (sender is InputShowButton button)
            {
                InputItemGrid itemGrid = button.ItemGrid;
                itemGrid.backButton.Click += ShowRuleList;
                container!.Content = itemGrid;
                container!.Footer = itemGrid.backButton;
                Game1.playSound("bigSelect");
            }
        }

        private void ShowRuleList(object? sender, ClickEventArgs e)
        {
            if (sender is Button)
            {
                container!.Content = rulesList;
                container!.Footer = footer;
                Game1.playSound("bigDeSelect");
            }
        }

        private IView CreateRulesList(Size viewportSize, ref float menuHeight)
        {
            ruleCheckboxes.Clear();
            List<RuleEntry> rules = ruleHelper.RuleEntries;
            List<List<RuleEntry>> rulesColumns;
            int colSize = (int)((menuHeight - BOX_W) / ROW_W);
            if (rules.Count <= colSize)
            {
                menuHeight = ROW_W * rules.Count + BOX_W;
                rulesColumns = [rules];
            }
            else
            {
                float colByWidth = MathF.Ceiling(viewportSize.Width / 640);
                float colByCount = MathF.Ceiling(rules.Count / colSize) + 1;
                if (colByCount > colByWidth)
                {
                    colSize = (int)MathF.Ceiling(rules.Count / colByWidth);
                }
                else
                {
                    colSize = (int)MathF.Ceiling(rules.Count / colByCount);
                    menuHeight = ROW_W * colSize + BOX_W;
                }
                rulesColumns = [];
                for (int i = 0; i < rules.Count; i += colSize)
                {
                    rulesColumns.Add(rules.GetRange(i, Math.Min(colSize, rules.Count - i)));
                }
            }

            List<IView> columns = [];
            int seq = 0;
            foreach (var rulesC in rulesColumns)
            {

                int inputSize = rulesC.Max((rule) => rule.Inputs.Count);
                int outputSize = rulesC.Max((rule) => rule.Outputs.Count);
                LayoutParameters inputLayout = new() { Width = Length.Px(ROW_W * inputSize + ROW_MARGIN * 2), Height = Length.Content() };
                LayoutParameters outputLayout = new() { Width = Length.Px(ROW_W * outputSize + ROW_MARGIN * 2), Height = Length.Content() };

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
                    Margin = new(COL_MARGIN),
                });
            }

            rulesList = new Lane()
            {
                Name = "RuleList",
                // Layout = new() { Width = Length.Content(), Height = Length.Stretch() },
                Orientation = Orientation.Horizontal,
                Children = columns,
            };
            return rulesList;
        }

        private IView CreateRuleListEntry(RuleEntry rule, LayoutParameters inputLayout, LayoutParameters outputLayout)
        {
            List<IView> children = [];
            if (Game1.IsMasterGame && rule.CanCheck)
            {
                // InputShowButton openItems = new(rule);
                // openItems.Click += ShowItemGrid;
                // children.Add(openItems);
                // if (ruleCheckboxes.ContainsKey(rule.Ident))

                if (ruleCheckboxes.TryGetValue(rule.Ident, out RuleCheckBox? existing))
                {
                    // children.Add(new Image()
                    // {
                    //     Sprite = disabled.Contains(rule.Ident) ? UiSprites.CheckboxUnchecked : UiSprites.CheckboxChecked,
                    //     Tint = Color.White * 0.0f,
                    //     Layout = LayoutParameters.FitContent(),
                    //     IsFocusable = false
                    // });
                    children.Add(existing);
                }
                else
                {
                    RuleCheckBox checkBox = new(rule)
                    {
                        IsChecked = !disabled.Contains(rule.Ident),
                        // Tooltip = rule.Ident.ToString()
                    };
                    ruleCheckboxes[rule.Ident] = checkBox;
                    children.Add(checkBox);
                }
            }
            else
            {
                children.Add(new Image()
                {
                    Sprite = disabled.Contains(rule.Ident) ? UiSprites.CheckboxUnchecked : UiSprites.CheckboxChecked,
                    Tint = Color.White * 0.5f,
                    Layout = LayoutParameters.FitContent(),
                    IsFocusable = false
                });
            }

            Lane inputs = FormRuleItemLane(rule.Inputs, $"{rule.Repr}.Inputs");
            inputs.HorizontalContentAlignment = Alignment.End;
            inputs.Layout = inputLayout;
            children.Add(inputs);

            Image arrow = new()
            {
                Name = $"{rule.Repr}.Arrow",
                Layout = LayoutParameters.FitContent(),
                Padding = new(20, 16),
                Sprite = RightCaret
            };
            children.Add(arrow);

            Lane outputs = FormRuleItemLane(rule.Outputs, $"{rule.Repr}.Outputs");
            outputs.Layout = outputLayout;
            outputs.HorizontalContentAlignment = Alignment.Start;
            children.Add(outputs);

            return new Lane()
            {
                Name = $"{rule.Repr}.Lane",
                Layout = LayoutParameters.FitContent(),
                Orientation = Orientation.Horizontal,
                Children = children,
                Margin = new(Left: 12),
                HorizontalContentAlignment = Alignment.Start,
                VerticalContentAlignment = Alignment.Middle,
            };
        }

        private static Lane FormRuleItemLane(List<RuleItem> ruleItems, string prefix)
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