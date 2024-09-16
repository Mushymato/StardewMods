using Microsoft.Xna.Framework;
using StardewUI;
using StardewValley;
using xTile.Dimensions;

namespace MachineControlPanel.Framework.UI
{
    internal sealed record ContentChangeArgs(IView? Nextview);

    internal sealed class RuleListView(RuleHelper ruleHelper, HashSet<RuleIdent> disabled, Action<HashSet<RuleIdent>, HashSet<RuleIdent>> saveMachineRules) : WrapperView, ITabbable
    {
        private const int ROW_MARGIN = 4;
        private const int COL_MARGIN = 6;
        private const int ROW_W = 64 + ROW_MARGIN * 2;
        private const int BOX_W = ROW_MARGIN * 3;
        private const int MIN_HEIGHT = 400;
        private const int GUTTER_HEIGHT = 400;
        private const int INPUT_GRID_COUNT = 12;
        private static Sprite RightCaret => new(Game1.mouseCursors, new(448, 96, 24, 32));
        private static Sprite ThinVDivider =>
            new(
                Game1.menuTexture,
                SourceRect: new(156, 384, 8, 54)
            );
        private static Sprite TabButton => new(Game1.mouseCursors2, new(0, 224, 16, 12), FixedEdges: new(0, 4));

        private static LayoutParameters IconLayout => LayoutParameters.FixedSize(64, 64);
        internal readonly Dictionary<RuleIdent, CheckBox> ruleCheckBoxes = [];
        internal readonly Dictionary<RuleIdent, Panel> inputChecks = [];
        private ScrollableFrameView? container = null;
        private Lane? rulesList = null;
        private Grid? inputsGrid = null;

        protected override IView CreateView()
        {
            Size viewportSize = Game1.uiViewport.Size;
            float menuHeight = MathF.Max(MIN_HEIGHT, viewportSize.Height - GUTTER_HEIGHT);
            // var itemSelector = CreateSidebar(menuHeight);
            CreateRulesList(viewportSize, ref menuHeight);
            LayoutParameters fitWidth = new() { Width = Length.Content(), Height = Length.Px(menuHeight) };
            if (ruleHelper.ValidInputs.Count > 0)
                CreateInputsGrid();

            container = new ScrollableFrameView()
            {
                Name = "RuleListRoot",
                FrameLayout = fitWidth,
                ContentLayout = fitWidth,
                Title = I18n.RuleList_Title(name: ruleHelper.Name),
                Content = rulesList,
                Sidebar = ruleHelper.ValidInputs.Count > 0 ? CreateSideBar() : null,
                SidebarWidth = ROW_W,
                Footer = Game1.IsMasterGame ? CreateSaveButtons() : null,
            };
            return container;
        }

        private IView CreateSideBar()
        {
            Button rulesBtn = new(hoverBackgroundSprite: UiSprites.ButtonLight)
            {
                Name = "RulesBtn",
                Text = I18n.RuleList_Rules(),
                Layout = IconLayout
            };
            rulesBtn.LeftClick += ShowRules;

            Button inputsBtn = new(hoverBackgroundSprite: UiSprites.ButtonLight)
            {
                Name = "InputsBtn",
                Text = I18n.RuleList_Inputs(),
                Layout = IconLayout
            };
            inputsBtn.LeftClick += ShowInputs;
            return new Lane()
            {
                Layout = LayoutParameters.FitContent(),
                Orientation = Orientation.Vertical,
                Children = [rulesBtn, inputsBtn]
            };
        }

        private void ShowRules(object? sender, ClickEventArgs e)
        {
            if (sender is Button)
            {
                container!.Content = rulesList;
                Game1.playSound("smallSelect");
            }
        }

        private void ShowInputs(object? sender, ClickEventArgs e)
        {
            if (sender is Button)
            {
                container!.Content = inputsGrid;
                Game1.playSound("smallSelect");
            }
        }

        public bool NextTab()
        {
            if (container!.Content == rulesList)
            {
                container!.Content = inputsGrid;
                Game1.playSound("smallSelect");
                return true;
            }
            return false;
        }

        public bool PreviousTab()
        {
            if (container!.Content == inputsGrid)
            {
                container!.Content = rulesList;
                Game1.playSound("smallSelect");
                return true;
            }
            return false;
        }

        private IView CreateSaveButtons()
        {
            Button saveBtn = new(hoverBackgroundSprite: UiSprites.ButtonLight)
            {
                Name = "SaveBtn",
                Text = I18n.RuleList_Save()
            };
            saveBtn.LeftClick += SaveRules;

            Button resetBtn = new(hoverBackgroundSprite: UiSprites.ButtonLight)
            {
                Name = "ResetBtn",
                Text = I18n.RuleList_Reset()
            };
            resetBtn.LeftClick += ResetRules;

            return new Lane()
            {
                Layout = LayoutParameters.FitContent(),
                Orientation = Orientation.Horizontal,
                Children = [saveBtn, resetBtn]
            }; ;
        }

        private void SaveRules(object? sender, ClickEventArgs e)
        {
            if (sender is not Button)
                return;

            HashSet<RuleIdent> newEnabled = [];
            HashSet<RuleIdent> newDisabled = [];
            foreach (var kv in ruleCheckBoxes)
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
            foreach (var kv in ruleCheckBoxes)
            {
                newEnabled.Add(kv.Key);
                kv.Value.IsChecked = true;
            }
            saveMachineRules(newEnabled, newDisabled);
            Game1.playSound("bigDeSelect");
        }

        private void CreateInputsGrid()
        {
            int i = 0;
            // int count = (int)MathF.Floor(viewportSize.Width / (2 * ROW_W));
            List<IView> inputPanels = [];
            foreach (var kv in ruleHelper.ValidInputs)
            {
                InputCheckable inputPanel = new(FormRuleItemPanel(kv.Value, $"Inputs.{++i}"))
                {
                    IsChecked = false
                };
                inputPanels.Add(inputPanel.Content);
            }
            inputsGrid = new Grid()
            {
                Name = "InputsGrid",
                Layout = new() { Width = Length.Px(INPUT_GRID_COUNT * ROW_W), Height = Length.Content() },
                ItemLayout = GridItemLayout.Count(INPUT_GRID_COUNT),
                Children = inputPanels
            };
        }

        private void CreateRulesList(Size viewportSize, ref float menuHeight)
        {
            ruleCheckBoxes.Clear();
            List<RuleEntry> rules = ruleHelper.RuleEntries;
            List<List<RuleEntry>> rulesColumns;
            int colSize = (int)((menuHeight - BOX_W) / ROW_W) - 2;
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
        }

        private IView CreateRuleListEntry(RuleEntry rule, LayoutParameters inputLayout, LayoutParameters outputLayout)
        {
            List<IView> children = [];
            if (Game1.IsMasterGame && rule.CanCheck)
            {
                // InputShowButton openItems = new(rule);
                // openItems.LeftClick += ShowItemGrid;
                // children.Add(openItems);
                // if (ruleCheckBoxes.ContainsKey(rule.Ident))

                // if (ruleCheckBoxes.TryGetValue(rule.Ident, out CheckBox? existing))
                if (ruleCheckBoxes.ContainsKey(rule.Ident))
                {
                    children.Add(new Image()
                    {
                        Sprite = disabled.Contains(rule.Ident) ? UiSprites.CheckboxUnchecked : UiSprites.CheckboxChecked,
                        Tint = Color.White * 0.0f,
                        Layout = LayoutParameters.FitContent(),
                        IsFocusable = false
                    });
                    // children.Add(existing);
                }
                else
                {
                    CheckBox checkBox = new()
                    {
                        IsChecked = !disabled.Contains(rule.Ident),
                        // Tooltip = rule.Ident.ToString()
                    };
                    ruleCheckBoxes[rule.Ident] = checkBox;
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
                content.Add(FormRuleItemPanel(ruleItem, $"{prefix}.{i++}"));
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

        private static Panel FormRuleItemPanel(RuleItem ruleItem, string name)
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
                    Padding = icon.Edge,
                    Sprite = icon.Img,
                    Tint = icon.Tint ?? Color.White
                });
            }
            return new Panel()
            {
                Name = name,
                Layout = IconLayout,
                Margin = new Edges(ROW_MARGIN),
                Children = iconImgs,
                Tooltip = string.Join('\n', ruleItem.Tooltip.Select((tip) => tip.Trim())),
                IsFocusable = true
            };
        }
    }
}