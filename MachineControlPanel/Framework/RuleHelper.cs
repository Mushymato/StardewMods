using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using Force.DeepCloner;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewUI;
using StardewValley.Extensions;
using System.Collections.Immutable;


namespace MachineControlPanel.Framework
{
    /// <summary>
    /// Holds info about how to draw an icon, mostly a wrapper around <seealso cref="Sprite"/>
    /// </summary>
    /// <param name="Img"></param>
    /// <param name="Edg"></param>
    /// <param name="Scale"></param>
    /// <param name="Tint"></param>
    internal sealed record IconEdge(
        Sprite Img,
        Edges? Edg = null,
        float Scale = 4f,
        Color? Tint = null
    )
    {
        internal Edges Edge => Edg ?? Edges.NONE;
    };

    /// <summary>
    /// Represents an item involved in machine rule.
    /// </summary>
    /// <param name="Icons">Base icon, plus any decorations indicating their role (context tag, fuel)</param>
    /// <param name="Tooltip">Hoverover text</param>
    /// <param name="Count">Number required</param>
    /// <param name="QId">Qualified item id, if this is a specific item</param>
    internal sealed record RuleItem(
        List<IconEdge> Icons,
        List<string> Tooltip,
        int Count = 0,
        string? QId = null
    )
    {
        internal RuleItem Copy()
        {
            return new RuleItem(
                new(Icons),
                new(Tooltip),
                Count,
                QId: QId
            );
        }
    };

    /// <summary>
    /// A single machine rule with inputs and outputs.
    /// </summary>
    /// <param name="Ident"></param>
    /// <param name="CanCheck"></param>
    /// <param name="Inputs"></param>
    /// <param name="Outputs"></param>
    internal sealed record RuleEntry(
        RuleIdent Ident,
        bool CanCheck,
        List<RuleItem> Inputs,
        List<RuleItem> Outputs
    )
    {
        internal string Repr => $"{Ident.Item1}.{Ident.Item2}";
    };

    /// <summary>
    /// Valid inputs
    /// </summary>
    /// <param name="Item"></param>
    /// <param name="Idents"></param>
    internal sealed record ValidInput(
        RuleItem Item,
        HashSet<RuleIdent> Idents
    )
    {
        internal string QId => Item.QId ?? "ERROR";
    };

    internal sealed class RuleHelper
    {
        internal const string PLACEHOLDER_TRIGGER = "PLACEHOLDER_TRIGGER";
        internal static Integration.IExtraMachineConfigApi? EMC { get; set; } = null;
        internal static IconEdge QuestionIcon => new(new(Game1.mouseCursors, new Rectangle(240, 192, 16, 16)));
        internal static IconEdge EmojiExclaim => new(new(ChatBox.emojiTexture, new Rectangle(54, 81, 9, 9)), new(Top: 37), 3f);
        internal static IconEdge EmojiBolt => new(new(ChatBox.emojiTexture, new Rectangle(36, 63, 9, 9)), new(Left: 37), 3f);

        internal static IconEdge Quality(int quality)
        {
            return new(
                new(Game1.mouseCursors,
                (quality < 4) ? new Rectangle(338 + (quality - 1) * 8, 400, 8, 8) : new Rectangle(346, 392, 8, 8)),
                new(Top: 37),
                3
            );
        }
        internal readonly List<RuleEntry> RuleEntries = [];
        internal readonly Dictionary<string, ValidInput> ValidInputs = [];

        internal readonly string Name;
        internal readonly string QId;
        internal readonly ModConfig Config;
        private readonly MachineData machine;

        internal RuleHelper(string qId, string displayName, MachineData machine, ModConfig config)
        {
            this.QId = qId;
            this.Name = displayName;
            this.Config = config;
            this.machine = machine;
        }

        internal bool HasDisabled => ModEntry.HasSavedEntry(QId);

        internal bool CheckRuleDisabled(RuleIdent ident)
        {
            return (
                ModEntry.TryGetSavedEntry(QId, out ModSaveDataEntry? msdEntry) &&
                msdEntry.Rules.Contains(ident)
            );
        }

        internal bool CheckInputDisabled(string inputQId)
        {
            return (
                ModEntry.TryGetSavedEntry(QId, out ModSaveDataEntry? msdEntry) &&
                msdEntry.Inputs.Contains(inputQId)
            );
        }

        internal void AddValidInput(ParsedItemData itemData, RuleIdent ident)
        {
            if (itemData.QualifiedItemId == null)
                return;
            if (ValidInputs.TryGetValue(itemData.QualifiedItemId, out ValidInput? valid))
                valid.Idents.Add(ident);
            else
                ValidInputs[itemData.QualifiedItemId] = new(
                    new RuleItem(
                        [new(new(itemData.GetTexture(), itemData.GetSourceRect()))],
                        [itemData.DisplayName],
                        QId: itemData.QualifiedItemId
                    ),
                    [ident]
                );
        }

        internal void AddValidInput(RuleItem ruleItem, RuleIdent ident)
        {
            if (ruleItem.QId == null)
                return;
            if (ValidInputs.TryGetValue(ruleItem.QId, out ValidInput? valid))
                valid.Idents.Add(ident);
            else
                ValidInputs[ruleItem.QId] = new(ruleItem, [ident]);
        }

        /// <summary>
        /// Two outputs are similar enough if we aren't doing anything fun icon wise
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private static bool SimilarEnough(MachineItemOutput first, MachineItemOutput second)
        {
            return (
                first.ItemId == second.ItemId &&
                first.RandomItemId == second.RandomItemId &&
                first.PreserveId == second.PreserveId &&
                first.Quality == second.Quality &&
                first.MinStack == second.MinStack &&
                first.MaxStack == second.MaxStack
            );
        }

        private static List<MachineItemOutput> PrunedMachineItemOutput(List<MachineItemOutput> outputItem)
        {
            List<MachineItemOutput> pruned = [];

            foreach (MachineItemOutput out1 in outputItem)
            {
                int i = 0;
                for (; i < pruned.Count; i++)
                {
                    if (SimilarEnough(out1, pruned[i]))
                    {
                        pruned[i] = out1;
                        break;
                    }
                }
                if (i == pruned.Count)
                {
                    pruned.Add(out1);
                }
            }

            return pruned;
        }

        internal void GetRuleEntries()
        {
            RuleEntries.Clear();
            ValidInputs.Clear();

            // Fuel
            List<RuleItem> sharedFuel = [];
            if (machine.AdditionalConsumedItems != null)
            {
                foreach (MachineItemAdditionalConsumedItems fuel in machine.AdditionalConsumedItems)
                {
                    if (ItemRegistry.GetData(fuel.ItemId) is ParsedItemData itemData)
                    {
                        sharedFuel.Add(new RuleItem(
                            [new(new(itemData.GetTexture(), itemData.GetSourceRect())),
                             EmojiBolt],
                            [itemData.DisplayName],
                            Count: fuel.RequiredCount,
                            QId: itemData.QualifiedItemId
                        ));
                    }
                }
            }

            foreach (MachineOutputRule rule in machine.OutputRules)
            {
                bool hasComplex = false;

                // rule outputs
                List<Tuple<List<RuleItem>, List<RuleItem>>> withEmcFuel = [];
                List<RuleItem> outputLine = [];
                foreach (MachineItemOutput output in PrunedMachineItemOutput(rule.OutputItem))
                {
                    List<RuleItem> optLine = [];
                    if (output.OutputMethod != null) // complex method
                    {
                        string methodName = output.OutputMethod.Split(':').Last();
                        optLine.Add(new RuleItem([QuestionIcon], [I18n.RuleList_SpecialOutput(method: methodName)]));
                        hasComplex = true;
                    }
                    if (output.ItemId == "DROP_IN")
                    {
                        optLine.Add(new RuleItem([QuestionIcon], [I18n.RuleList_SameAsInput()]));
                    }
                    else if (output.ItemId != null)
                    {
                        IList<ItemQueryResult> itemQueryResults = ItemQueryResolver.TryResolve(
                            output, ItemQueryCache.Context,
                            formatItemId: id => id != null ? Regex.Replace(id, "(DROP_IN_ID|DROP_IN_PRESERVE|NEARBY_FLOWER_ID)", "0") : id
                        );
                        foreach (ItemQueryResult res in itemQueryResults)
                        {
                            if (ItemRegistry.GetData(res.Item.QualifiedItemId) is not ParsedItemData itemData) continue;
                            List<IconEdge> icons = [new(new(itemData.GetTexture(), itemData.GetSourceRect()))];
                            List<string> tooltip = [];
                            if (output.Condition != null)
                            {
                                icons.Add(EmojiExclaim);
                                tooltip.AddRange(output.Condition.Split(','));
                            }
                            if (res.Item.Quality > 0)
                            {
                                icons.Add(Quality(res.Item.Quality));
                            }
                            tooltip.Add(itemData.DisplayName);
                            optLine.Add(new RuleItem(
                                icons, tooltip,
                                Count: res.Item.Stack,
                                QId: itemData.QualifiedItemId
                            ));
                        }
                    }
                    if (optLine.Count == 0)
                        continue;

                    if (EMC != null)
                    {
                        List<RuleItem> emcFuel = [];
                        var extraReq = EMC.GetExtraRequirements(output);
                        if (extraReq.Any())
                        {
                            foreach ((string tag, int count) in extraReq)
                            {
                                // TODO: deal with category when a mod actually use it
                                if (ItemRegistry.GetData(tag) is ParsedItemData itemData)
                                {
                                    emcFuel.Add(new RuleItem(
                                        [new(new(itemData.GetTexture(), itemData.GetSourceRect())),
                                        EmojiBolt],
                                        [itemData.DisplayName],
                                        Count: count,
                                        QId: itemData.QualifiedItemId
                                    ));
                                }
                            }
                        }
                        var extraTagReq = EMC.GetExtraTagsRequirements(output);
                        foreach ((string tagExpr, int count) in extraTagReq)
                        {
                            var tags = tagExpr.Split(',');
                            if (ItemQueryCache.TryGetContextTagRuleItemCache(tags, out RuleItem? ctxTag))
                            {
                                RuleItem ctxTagCopy = ctxTag.Copy();
                                IconEdge? qualityIcon = GetContextTagQuality(tags);
                                ctxTagCopy.Icons.Add(EmojiBolt);
                                if (qualityIcon != null)
                                    ctxTagCopy.Icons.Add(qualityIcon);
                                emcFuel.Add(ctxTagCopy);
                            }
                        }
                        if (emcFuel.Any())
                        {
                            withEmcFuel.Add(new(optLine, emcFuel));
                            continue;
                        }
                    }
                    outputLine.AddRange(optLine);
                }
                if (outputLine.Count == 0 && withEmcFuel.Count == 0)
                    continue;

                // rule inputs (triggers)
                List<Tuple<RuleIdent, bool, List<RuleItem>>> inputs = [];
                // RuleItem? placeholder = null;
                foreach (MachineOutputTriggerRule trigger in rule.Triggers)
                {
                    RuleIdent ident = new(rule.Id, trigger.Id);
                    List<RuleItem> inputLine = [];
                    List<ParsedItemData> inputItems = [];
                    // no item input
                    if (!trigger.Trigger.HasFlag(MachineOutputTrigger.ItemPlacedInMachine))
                    {
                        List<string> tooltip = [trigger.Trigger.ToString()];
                        inputLine.Add(new RuleItem([QuestionIcon], tooltip));
                    }
                    // item input based rules
                    if (trigger.RequiredItemId != null)
                    {
                        if (ItemRegistry.GetData(trigger.RequiredItemId) is ParsedItemData itemData)
                        {
                            RuleItem? preserve = GetPreserveRuleItem(trigger.RequiredTags, trigger.RequiredCount, itemData, out string preserveTag);
                            if (preserve != null)
                            {
                                inputLine.Add(preserve);
                                // Don't bother showing specific preserve items in inputs, should just use rules for that
                            }
                            else
                            {
                                inputLine.Add(new RuleItem(
                                    [new(new(itemData.GetTexture(), itemData.GetSourceRect()))],
                                    [itemData.DisplayName],
                                    Count: trigger.RequiredCount,
                                    QId: itemData.QualifiedItemId
                                ));
                                AddValidInput(inputLine.Last().Copy(), ident);
                            }
                        }
                    }
                    IconEdge? qualityIcon = null;
                    List<string> conditions = [];
                    if (trigger.RequiredTags != null)
                    {
                        List<string> inclCondTags = new(trigger.RequiredTags);
                        // for some reason game has ITEM_CONTEXT_TAG Input edible_mushroom instead of RequiredTags edible_mushroom
                        if (trigger.Condition != null)
                        {
                            foreach (string condStr in trigger.Condition.Split(','))
                            {
                                if (condStr.StartsWith("ITEM_CONTEXT_TAG Input "))
                                {
                                    inclCondTags.AddRange(condStr["ITEM_CONTEXT_TAG Input ".Length..].Split(' '));
                                }
                                else if (condStr.StartsWith("!ITEM_CONTEXT_TAG Input "))
                                {
                                    inclCondTags.AddRange(condStr["!ITEM_CONTEXT_TAG Input ".Length..].Split(' ').Select((tag) => $"!{tag}"));
                                }
                                else
                                {
                                    conditions.Add(condStr);
                                }
                            }
                        }
                        if (ItemQueryCache.TryGetContextTagRuleItemCache(inclCondTags, out RuleItem? ctxTag))
                        {
                            inputLine.Add(ctxTag.Copy());
                            if (ctxTag.QId != null)
                                AddValidInput(ItemRegistry.GetData(ctxTag.QId), ident);
                            else
                                PopulateContextTagValidInputs(inclCondTags, ident);
                        }
                        qualityIcon = GetContextTagQuality(inclCondTags);
                    }
                    else if (trigger.Condition != null)
                    {
                        conditions.AddRange(trigger.Condition.Split(','));
                    }
                    if (inputLine.Any())
                    {
                        if (qualityIcon != null)
                        {
                            inputLine.Last().Icons.Add(qualityIcon);
                        }
                        if (conditions.Any())
                        {
                            inputLine.Last().Tooltip.InsertRange(0, conditions);
                            inputLine.Last().Icons.Add(EmojiExclaim);
                        }

                        if (sharedFuel.Any())
                            inputLine.AddRange(sharedFuel);

                        inputs.Add(new(
                            ident,
                            !trigger.Trigger.HasFlag(MachineOutputTrigger.MachinePutDown) &&
                            !trigger.Trigger.HasFlag(MachineOutputTrigger.OutputCollected) &&
                            trigger.Trigger != MachineOutputTrigger.None,
                            inputLine
                        ));
                    }
                }
                if (inputs.Count == 0)
                {
                    if (hasComplex)
                    {
                        inputs.Add(new(
                            new(rule.Id, PLACEHOLDER_TRIGGER),
                            false,
                            [new RuleItem([QuestionIcon], [I18n.RuleList_SpecialInput()])]
                        ));
                    }
                    // else if (placeholder != null)
                    // {
                    //     inputs.Add(new(new(rule.Id, PLACEHOLDER_TRIGGER, -1), false, [placeholder]));
                    // }
                }

                if (withEmcFuel.Any())
                {
                    foreach ((RuleIdent ident, bool canCheck, List<RuleItem> inputLine) in inputs)
                    {
                        foreach ((List<RuleItem> optLine, List<RuleItem> emcFuel) in withEmcFuel)
                        {
                            List<RuleItem> ipt = new(inputLine);
                            foreach (RuleItem emcF in emcFuel)
                            {
                                if (ipt.FindIndex((inL) => (
                                    inL.QId == emcF.QId &&
                                    inL.Icons.Count == emcF.Icons.Count &&
                                    // inL.Tooltip == emcF.Tooltip &&
                                    inL.Icons.Contains(EmojiBolt)
                                )) is int found && found > -1)
                                {
                                    ipt[found] = new RuleItem(
                                        emcF.Icons,
                                        emcF.Tooltip,
                                        Count: ipt[found].Count + emcF.Count,
                                        QId: emcF.QId
                                    );
                                }
                                else
                                {
                                    ipt.Add(emcF);
                                }
                            }
                            RuleEntries.Add(new RuleEntry(
                                ident,
                                canCheck,
                                ipt,
                                optLine
                            ));
                        }
                    }
                }

                if (outputLine.Any())
                {
                    foreach ((RuleIdent ident, bool canCheck, List<RuleItem> inputLine) in inputs)
                    {
                        RuleEntries.Add(new RuleEntry(
                            ident,
                            canCheck,
                            inputLine,
                            outputLine
                        ));
                    }
                }

            }
        }

        internal static RuleItem? GetPreserveRuleItem(List<string>? tags, int count, ParsedItemData baseItem, out string preserveTag)
        {
            preserveTag = "none";
            if (tags == null)
            {
                return null;
            }
            foreach (string tag in tags)
            {
                bool negate = tag.StartsWith('!');
                string realTag = negate ? tag[1..] : tag;
                if (realTag.StartsWith("preserve_sheet_index_"))
                {
                    preserveTag = tag;
                    // id_o_itemid resolves but preserved_item_index_itemid cus that gets added for Object
                    string idTag = $"id_o_{realTag[21..]}";
                    if (ItemQueryResolver.TryResolve(
                        "ALL_ITEMS",
                        ItemQueryCache.Context,
                        ItemQuerySearchMode.FirstOfTypeItem,
                        $"ITEM_CONTEXT_TAG Target {idTag}"
                    ).FirstOrDefault()?.Item is Item preserveItem)
                    {
                        SObject.PreserveType? preserveType = baseItem.QualifiedItemId switch
                        {
                            "(O)348" => SObject.PreserveType.Wine,
                            "(O)344" => SObject.PreserveType.Jelly,
                            "(O)342" => SObject.PreserveType.Pickle,
                            "(O)350" => SObject.PreserveType.Juice,
                            "(O)812" => SObject.PreserveType.Roe,
                            "(O)447" => SObject.PreserveType.AgedRoe,
                            "(O)340" => SObject.PreserveType.Honey,
                            "(O)685" => SObject.PreserveType.Bait,
                            "(O)DriedFruit" => SObject.PreserveType.DriedFruit,
                            "(O)DriedMushrooms" => SObject.PreserveType.DriedMushroom,
                            "(O)SmokedFish" => SObject.PreserveType.SmokedFish,
                            _ => null
                        };
                        if (preserveType == null)
                        {
                            return null;
                        }
                        if (ItemQueryResolver.TryResolve($"FLAVORED_ITEM {preserveType} {preserveItem.ItemId}", ItemQueryCache.Context)
                            .FirstOrDefault()?.Item is ColoredObject preserve)
                        {
                            List<IconEdge> icons = [];
                            // drawing with layder 
                            if (preserve.ColorSameIndexAsParentSheetIndex)
                            {
                                icons.Add(new(new(baseItem.GetTexture(), baseItem.GetSourceRect()), Tint: preserve.color.Value));
                            }
                            else
                            {
                                icons.Add(new(new(baseItem.GetTexture(), baseItem.GetSourceRect())));
                                icons.Add(new(new(baseItem.GetTexture(), baseItem.GetSourceRect(1)), Tint: preserve.color.Value));
                            }
                            return new RuleItem(
                                icons,
                                [preserve.DisplayName],
                                Count: count
                            );
                        }
                    }
                }
            }
            return null;
        }

        internal void PopulateContextTagValidInputs(IEnumerable<string> tags, RuleIdent ident)
        {
            if (ItemQueryCache.TryGetAllItemDataWithTags(tags, out ImmutableList<ParsedItemData>? matchingItemData))
            {
                foreach (ParsedItemData itemData in matchingItemData)
                {
                    if (!ValidInputs.ContainsKey(itemData.QualifiedItemId))
                        AddValidInput(itemData, ident);
                }
            }
        }

        internal static IconEdge? GetContextTagQuality(IEnumerable<string> tags)
        {
            foreach (string tag in tags)
            {
                int quality = tag.Trim() switch
                {
                    "quality_none" => 0,
                    "quality_silver" => 1,
                    "quality_gold" => 2,
                    "quality_iridium" => 4,
                    _ => -1,
                };
                if (quality > -1)
                    return Quality(quality);
            }
            return null;
        }
    }
}