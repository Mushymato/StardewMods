using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewUI;
using StardewValley;

namespace MachineControlPanel.Framework.UI
{
    internal sealed record ContentChangeArgs(IView? Nextview);

    internal sealed class RuleListView(
        RuleHelper ruleHelper,
        Action<string, IEnumerable<RuleIdent>, IEnumerable<string>> saveMachineRules
    ) : WrapperView, ITabbable
    {
        private const int ROW_MARGIN = 4;
        private const int COL_MARGIN = 6;
        private const int ROW_W = 64 + ROW_MARGIN * 2;
        private const int BOX_W = ROW_MARGIN * 3;
        private const int MIN_HEIGHT = 400;
        private const int GUTTER_HEIGHT = 400;
        private static Sprite RightCaret => new(Game1.mouseCursors, new(448, 96, 24, 32));
        private static Sprite ThinVDivider =>
            new(
                Game1.menuTexture,
                SourceRect: new(156, 384, 8, 54)
            );
        // private static Sprite TabButton => new(Game1.mouseCursors2, new(0, 224, 24, 12), new(4, 4, 0, 4), new(Scale: 4));
        private static Sprite TabButton => new(Game1.menuTexture, new(0, 256, 44, 60), new(16, 16, 0, 16));
        private static readonly IReadOnlyList<Sprite> Digits = Enumerable
            .Range(0, 10)
            .Select((digit) => new Sprite(Game1.mouseCursors, new Rectangle(368 + digit * 5, 56, 5, 7)))
            .ToImmutableList();
        private readonly Edges tabButtonPassive = new(Bottom: ROW_MARGIN * 2);
        private readonly Edges tabButtonActive = new(Left: 6, Bottom: ROW_MARGIN * 2);

        private static LayoutParameters IconLayout => LayoutParameters.FixedSize(64, 64);
        internal readonly Dictionary<RuleIdent, CheckBox> ruleCheckBoxes = [];
        internal readonly List<InputCheckable> inputChecks = [];
        private ScrollableFrameView? container = null;
        private Lane? rulesList = null;
        private Grid? inputsGrid = null;
        private Button? rulesBtn = null;
        private Button? inputsBtn = null;


        protected override IView CreateView()
        {
            xTile.Dimensions.Size viewportSize = Game1.uiViewport.Size;
            float menuHeight = MathF.Max(MIN_HEIGHT, viewportSize.Height - GUTTER_HEIGHT);
            CreateRulesList(viewportSize, ref menuHeight);
            LayoutParameters fitWidth = new() { Width = Length.Content(), Height = Length.Px(menuHeight) };
            if (ruleHelper.ValidInputs.Any())
                CreateInputsGrid();

            container = new ScrollableFrameView()
            {
                Name = "RuleListRoot",
                FrameLayout = fitWidth,
                ContentLayout = fitWidth,
                Title = I18n.RuleList_Title(name: ruleHelper.Name),
                Content = rulesList,
                Sidebar = inputsGrid != null ? CreateSidebar() : null,
                SidebarWidth = ROW_W,
                Footer = Game1.IsMasterGame ? CreateFooter() : null,
            };

            // measure and get real width of ruleList, then update inputsGrid
            // (ruleHelper.Config.DefaultPage == DefaultPageOption.Inputs && inputsGrid != null) ? inputsGrid : rulesList
            container.Measure(new(viewportSize.Width, viewportSize.Height));

            if (inputsGrid != null)
            {
                inputsGrid.Layout = new() { Width = Length.Px(rulesList!.ContentSize.X), Height = Length.Content() };
                if (ruleHelper.Config.DefaultPage == DefaultPageOption.Inputs)
                {
                    container.Content = inputsGrid;
                }
                UpdateTabButtonMargins();
                // I am do reflection here because I dont want to fork StardewUI
                if (container.GetType().GetField("sidebarContainer", BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo sidebarContaineField &&
                    sidebarContaineField.GetValue(container) is Panel sidebarContainer)
                {
                    sidebarContainer.ZIndex = 2;
                }
            }

            return container;
        }

        private IView CreateSidebar()
        {
            rulesBtn = new(defaultBackgroundSprite: TabButton)
            {
                Name = "RulesBtn",
                Content = new Label()
                {
                    Text = I18n.RuleList_Rules(),
                    Margin = new(Left: 12)
                },
                Layout = LayoutParameters.FixedSize(96, 64),
                Margin = tabButtonActive
            };
            rulesBtn.LeftClick += ShowRules;
            inputsBtn = new(defaultBackgroundSprite: TabButton)
            {
                Name = "InputsBtn",
                Content = new Label()
                {
                    Text = I18n.RuleList_Inputs(),
                    Margin = new(Left: 12)
                },
                Layout = LayoutParameters.FixedSize(96, 64),
                Margin = tabButtonPassive,
            };
            inputsBtn.LeftClick += ShowInputs;

            return new Lane()
            {
                Layout = new() { Width = Length.Px(128), Height = Length.Content() },
                Padding = new(Top: 24),
                Margin = new(Right: -50),
                Orientation = Orientation.Vertical,
                Children = [rulesBtn, inputsBtn]
            };
        }

        private void UpdateTabButtonMargins()
        {
            if (container!.Content == inputsGrid)
            {
                rulesBtn!.Margin = tabButtonPassive;
                inputsBtn!.Margin = tabButtonActive;
            }
            else
            {
                rulesBtn!.Margin = tabButtonActive;
                inputsBtn!.Margin = tabButtonPassive;
            }
        }

        private IView CreateFooter()
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
            };
        }

        private void ShowRules(object? sender, ClickEventArgs e)
        {
            if (sender is Button)
            {
                container!.Content = rulesList;
                UpdateTabButtonMargins();
                Game1.playSound("smallSelect");
            }
        }

        private void ShowInputs(object? sender, ClickEventArgs e)
        {
            if (sender is Button)
            {
                container!.Content = inputsGrid;
                UpdateTabButtonMargins();
                Game1.playSound("smallSelect");
            }
        }

        public bool NextTab()
        {
            if (container!.Content == rulesList)
            {
                container!.Content = inputsGrid;
                UpdateTabButtonMargins();
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
                UpdateTabButtonMargins();
                Game1.playSound("smallSelect");
                return true;
            }
            return false;
        }

        private void SaveRules(object? sender, ClickEventArgs e)
        {
            if (sender is not Button)
                return;
            Game1.playSound("bigSelect");

            saveMachineRules(
                ruleHelper.QId,
                ruleCheckBoxes.Where((kv) => !kv.Value.IsChecked).Select((kv) => kv.Key),
                inputChecks.Where((ic) => !ic.IsChecked).Select((ic) => ic.QId)
            );
        }

        private void ResetRules(object? sender, ClickEventArgs e)
        {
            if (sender is not Button)
                return;
            Game1.playSound("bigDeSelect");

            foreach (var kv in ruleCheckBoxes)
                kv.Value.IsChecked = true;
            foreach (var ic in inputChecks)
                ic.IsChecked = true;

            saveMachineRules(ruleHelper.QId, [], []);
        }

        private void CreateInputsGrid()
        {
            int i = 0;
            List<IView> children = [];
            if (Game1.IsMasterGame)
            {
                foreach (var kv in ruleHelper.ValidInputs)
                {
                    InputCheckable inputCheck = new(
                        kv.Value,
                        FormRuleItemPanel(kv.Value.Item, $"Inputs.{++i}")
                    )
                    {
                        IsChecked = !ruleHelper.CheckInputDisabled(kv.Key)
                    };
                    children.Add(inputCheck.Content);
                    inputChecks.Add(inputCheck);
                }
            }
            else
            {
                foreach (var kv in ruleHelper.ValidInputs)
                {
                    Panel inputIcon = FormRuleItemPanel(kv.Value.Item, $"Inputs.{++i}");
                    ((Image)inputIcon.Children.First()).Tint = !ruleHelper.CheckInputDisabled(kv.Key) ?
                        Color.White : InputCheckable.COLOR_DISABLED;
                    children.Add(inputIcon);
                }
            }
            inputsGrid = new Grid()
            {
                Name = "InputsGrid",
                // Layout = new() { Width = Length.Px(INPUT_GRID_COUNT * ROW_W), Height = Length.Content() },
                ItemLayout = GridItemLayout.Length(ROW_W),
                Children = children
            };
        }

        private void CreateRulesList(xTile.Dimensions.Size viewportSize, ref float menuHeight)
        {
            ruleCheckBoxes.Clear();
            List<RuleEntry> rules = ruleHelper.RuleEntries;
            List<List<RuleEntry>> rulesColumns;
            int colSize = (int)((menuHeight - BOX_W) / ROW_W) - 2;
            if (rules.Count <= colSize)
            {
                menuHeight = MathF.Max(MIN_HEIGHT, ROW_W * rules.Count + BOX_W);
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
                    menuHeight = MathF.Max(MIN_HEIGHT, ROW_W * colSize + BOX_W);
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

                if (columns.Any())
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
                        Sprite = ruleHelper.CheckRuleDisabled(rule.Ident) ? UiSprites.CheckboxUnchecked : UiSprites.CheckboxChecked,
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
                        IsChecked = !ruleHelper.CheckRuleDisabled(rule.Ident),
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
                    Sprite = ruleHelper.CheckRuleDisabled(rule.Ident) ? UiSprites.CheckboxUnchecked : UiSprites.CheckboxChecked,
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
            outputs.Margin = new(Left: ROW_MARGIN * 1);
            outputs.Layout = outputLayout;
            outputs.HorizontalContentAlignment = Alignment.Start;
            children.Add(outputs);

            return new Lane()
            {
                Name = $"{rule.Repr}.Lane",
                Layout = LayoutParameters.FitContent(),
                Orientation = Orientation.Horizontal,
                Children = children,
                Margin = new(Left: ROW_MARGIN * 3),
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
                Panel itemPanel = FormRuleItemPanel(ruleItem, $"{prefix}.{i++}");
                if (ruleItem.Count >= 1)
                {
                    int num = ruleItem.Count;
                    int offset = 44;
                    while (num > 0)
                    {
                        // final digit
                        int digit = num % 10;
                        itemPanel.Children.Add(new Image()
                        {
                            Layout = LayoutParameters.FixedSize(15, 21),
                            Padding = new(Left: offset, Top: 48),
                            Sprite = Digits[digit]
                        });
                        // unclear why this looks the best, shouldnt it be scale * 5?
                        offset -= 12;
                        num /= 10;
                    }
                }
                content.Add(itemPanel);
            }
            return new Lane()
            {
                Name = prefix,
                Layout = LayoutParameters.FitContent(),
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = Alignment.End,
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