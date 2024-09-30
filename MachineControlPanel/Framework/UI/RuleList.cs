using System.Collections.Immutable;
using MachineControlPanel.Framework.UI.Integration;
using Microsoft.Xna.Framework;
using StardewUI;
using StardewValley;

namespace MachineControlPanel.Framework.UI
{
    internal sealed record ContentChangeArgs(IView? Nextview);

    internal sealed class RuleListView(
        RuleHelper ruleHelper,
        Action<string, IEnumerable<RuleIdent>, IEnumerable<string>> saveMachineRules,
        Action<HoveredItemPanel> setHoverEvents,
        Action<bool>? exitThisMenu = null,
        Action? updateEdited = null
    ) : WrapperView, IPageable
    {
        /// Geometry
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
        private static Sprite TabButton => new(Game1.menuTexture, new(0, 256, 44, 60), new(16, 16, 0, 16));
        private static readonly IReadOnlyList<Sprite> Digits = Enumerable
            .Range(0, 10)
            .Select((digit) => new Sprite(Game1.mouseCursors, new Rectangle(368 + digit * 5, 56, 5, 7)))
            .ToImmutableList();
        private readonly Edges tabButtonPassive = new(Bottom: ROW_MARGIN * 2);
        private readonly Edges tabButtonActive = new(Bottom: ROW_MARGIN * 2, Right: -6);

        private static LayoutParameters IconLayout => LayoutParameters.FixedSize(64, 64);
        internal readonly Dictionary<RuleIdent, CheckBox> ruleCheckBoxes = [];
        internal readonly List<InputCheckable> inputChecks = [];
        private ScrollableView? container;
        private Lane? rulesList = null;
        private Grid? inputsGrid = null;
        private Button? rulesBtn = null;
        private Button? inputsBtn = null;

        /// <summary>
        /// Create rule list view, with tabs and footer buttons
        /// </summary>
        /// <returns></returns>
        protected override IView CreateView()
        {
            xTile.Dimensions.Size viewportSize = Game1.uiViewport.Size;
            float menuHeight = MathF.Max(MIN_HEIGHT, viewportSize.Height - GUTTER_HEIGHT);
            rulesList = CreateRulesList(viewportSize, ref menuHeight);

            List<IView> vItems = [];
            List<IView> hItems = [];
            container = new()
            {
                Name = "RuleList.Scroll",
                Layout = new() { Width = Length.Content(), Height = Length.Px(menuHeight) },
                Content = rulesList,
            };

            Frame scrollBox = new()
            {
                Name = "RuleList.Frame",
                Layout = LayoutParameters.FitContent(),
                Background = UiSprites.MenuBackground,
                Border = UiSprites.MenuBorder,
                BorderThickness = UiSprites.MenuBorderThickness,
                Content = container,
            };

            if (ruleHelper.ValidInputs.Any())
            {
                inputsGrid = CreateInputsGrid();
                container.Measure(new(viewportSize.Width, viewportSize.Height));
                inputsGrid.Layout = new() { Width = Length.Px(rulesList!.ContentSize.X), Height = Length.Content() };
                scrollBox.FloatingElements.Add(new(CreateSidebar(), FloatingPosition.BeforeParent));

                if (ModEntry.Config.DefaultPage == DefaultPageOption.Inputs)
                {
                    container.Content = inputsGrid;
                    UpdateTabButtonMargins();
                }
            }

            Banner banner = new()
            {
                Layout = LayoutParameters.FitContent(),
                Margin = new(Top: -85),
                Padding = new(12),
                Background = UiSprites.BannerBackground,
                BackgroundBorderThickness =
                (UiSprites.BannerBackground.FixedEdges ?? Edges.NONE)
                * (UiSprites.BannerBackground.SliceSettings?.Scale ?? 1),
                Text = ruleHelper.Name
            };

            vItems.Add(banner);
            vItems.Add(scrollBox);
            vItems.Add(CreateFooter());

            Lane center = new()
            {
                Name = "RuleList.Body",
                Layout = LayoutParameters.FitContent(),
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = Alignment.Middle,
                VerticalContentAlignment = Alignment.Middle,
                Children = vItems,
            };
            if (exitThisMenu != null)
            {
                Button closeBtn = new(defaultBackgroundSprite: MachineSelect.CloseButton)
                {
                    Margin = new Edges(Left: 48),
                    Layout = LayoutParameters.FixedSize(48, 48)
                };
                closeBtn.LeftClick += ExitMenu;
                center.FloatingElements.Add(new(closeBtn, FloatingPosition.AfterParent));
            }

            return center;
        }

        /// <summary>
        /// Create the sidebar for changing pages
        /// </summary>
        /// <returns></returns>
        private Lane CreateSidebar()
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
                Layout = new() { Width = Length.Px(96), Height = Length.Content() },
                Padding = new(Top: 32),
                Margin = new(Right: -20),
                HorizontalContentAlignment = Alignment.End,
                Orientation = Orientation.Vertical,
                Children = [rulesBtn, inputsBtn],
                ZIndex = 2
            };
        }

        /// <summary>
        /// Move tab position depending on current page
        /// </summary>
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

        /// <summary>
        /// Make footer buttons for save/reset of rules
        /// </summary>
        /// <returns></returns>
        private Lane CreateFooter()
        {
            List<IView> children;
            if (Game1.IsMasterGame)
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
                children = [saveBtn, resetBtn];
            }
            else
            {
                children = [new Frame(){
                    Padding = new(12),
                    Background = UiSprites.ButtonDark,
                    BorderThickness = UiSprites.ButtonLight.FixedEdges!,
                    Content = new Label(){
                        Text = I18n.RuleList_FooterNote()
                    }
                }];
            }

            return new Lane()
            {
                Layout = LayoutParameters.FitContent(),
                Orientation = Orientation.Horizontal,
                Children = children
            };
        }

        /// <summary>
        /// Change to rules list on click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowRules(object? sender, ClickEventArgs e)
        {
            if (sender is Button)
            {
                container!.Content = rulesList;
                UpdateTabButtonMargins();
                Game1.playSound("smallSelect");
            }
        }

        /// <summary>
        /// Change to inputs on click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowInputs(object? sender, ClickEventArgs e)
        {
            if (sender is Button)
            {
                container!.Content = inputsGrid;
                UpdateTabButtonMargins();
                Game1.playSound("smallSelect");
            }
        }

        /// <summary>
        /// Save rules
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            updateEdited?.Invoke();

            foreach (InputCheckable checkable in inputChecks)
                checkable.IsImplicitOff = ruleHelper.CheckInputImplicitDisabled(checkable.Idents);
        }

        /// <summary>
        /// Reset saved rules
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            updateEdited?.Invoke();

            foreach (InputCheckable checkable in inputChecks)
                checkable.IsImplicitOff = ruleHelper.CheckInputImplicitDisabled(checkable.Idents);
        }

        /// <summary>
        /// Exit menu on click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitMenu(object? sender, ClickEventArgs e)
        {
            exitThisMenu!(true);
        }

        /// <summary>
        /// Make inputs page
        /// </summary>
        /// <returns></returns>
        private Grid CreateInputsGrid()
        {
            int i = 0;
            List<IView> children = [];
            foreach ((string key, ValidInput input) in ruleHelper.ValidInputs)
            {
                InputCheckable inputCheck = new(
                    input,
                    FormRuleItemPanel(input.Rule, $"Inputs.{++i}"),
                    Game1.IsMasterGame
                )
                {
                    IsChecked = !ruleHelper.CheckInputDisabled(key),
                    IsImplicitOff = ruleHelper.CheckInputImplicitDisabled(input.Idents)
                };
                children.Add(inputCheck.Content);
                if (Game1.IsMasterGame)
                    inputChecks.Add(inputCheck);
            }

            return new Grid()
            {
                Name = "InputsGrid",
                ItemLayout = GridItemLayout.Length(ROW_W),
                Children = children
            };
        }

        /// <summary>
        /// Make rules page
        /// </summary>
        /// <param name="viewportSize"></param>
        /// <param name="menuHeight"></param>
        /// <returns></returns>
        private Lane CreateRulesList(xTile.Dimensions.Size viewportSize, ref float menuHeight)
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

            return new Lane()
            {
                Name = "RuleList",
                // Layout = new() { Width = Length.Content(), Height = Length.Stretch() },
                Layout = LayoutParameters.FitContent(),
                Orientation = Orientation.Horizontal,
                Children = columns,
            };
        }

        /// <summary>
        /// Make a single entry in rules
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="inputLayout"></param>
        /// <param name="outputLayout"></param>
        /// <returns></returns>
        private IView CreateRuleListEntry(RuleEntry rule, LayoutParameters inputLayout, LayoutParameters outputLayout)
        {
            List<IView> children = [];
            if (Game1.IsMasterGame && rule.CanCheck)
            {
                if (ruleCheckBoxes.ContainsKey(rule.Ident))
                {
#if DEBUG
                    children.Add(new Image()
                    {
                        Sprite = ruleHelper.CheckRuleDisabled(rule.Ident) ? UiSprites.CheckboxUnchecked : UiSprites.CheckboxChecked,
                        Tint = Color.White * 0.5f,
                        Layout = LayoutParameters.FitContent(),
                        IsFocusable = false
                    });
#else
                    children.Add(ruleCheckBoxes[rule.Ident]);
#endif
                }
                else
                {
                    CheckBox checkBox = new()
                    {
                        IsChecked = !ruleHelper.CheckRuleDisabled(rule.Ident),
                        Tooltip = rule.Ident.ToString()
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

        /// <summary>
        /// Make a horizontal lane of RuleItems
        /// </summary>
        /// <param name="ruleItems"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private Lane FormRuleItemLane(List<RuleItem> ruleItems, string prefix)
        {
            List<IView> content = [];
            int i = 0;
            foreach (var ruleItem in ruleItems)
            {
                Panel itemPanel = FormRuleItemPanel(ruleItem, $"{prefix}.{i++}");
                if (ruleItem.Count > 1)
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

        /// <summary>
        /// Make a Panel for a rule item, can have several images on top of each other
        /// </summary>
        /// <param name="ruleItem"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private Panel FormRuleItemPanel(RuleItem ruleItem, string name)
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
            HoveredItemPanel itemPanel = new()
            {
                Name = name,
                Layout = IconLayout,
                Margin = new Edges(ROW_MARGIN),
                Children = iconImgs,
                Tooltip = string.Join('\n', ruleItem.Tooltip.Select((tip) => tip.Trim())),
                IsFocusable = true,
                HoveredItem = ruleItem.Item
            };
            setHoverEvents(itemPanel);
            return itemPanel;
        }

        /// <summary>
        /// Implement gamepad R to next page
        /// </summary>
        /// <returns></returns>
        public bool NextPage()
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

        /// <summary>
        /// Implement gamepad L to prev page
        /// </summary>
        /// <returns></returns>
        public bool PreviousPage()
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
    }
}