using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Internal;
using StardewUI;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;
using Force.DeepCloner;
using System.Data;
using StardewValley.Objects;
using StardewValley.GameData.Objects;


namespace MachineControlPanel.Framework
{
    // suspect i ought to just use sprite with nineslice stuff unclear
    internal sealed record IconEdge(
        Sprite Img,
        Edges Edg,
        float Scale = 4f,
        Color? Tint = null
    );

    internal sealed record RuleItem(
        List<IconEdge> Icons,
        List<string> Tooltip
    );

    internal sealed record RuleEntry(
        RuleIdent Ident,
        bool CanCheck,
        List<RuleItem> Inputs,
        List<RuleItem> Outputs
    )
    {
        internal string Repr => $"{Ident.Item2}.{Ident.Item3}";
    };

    internal class RuleHelper
    {
        internal const string PLACEHOLDER_TRIGGER = "PLACEHOLDER_TRIGGER";
        internal static Integration.IExtraMachineConfigApi? EMC { get; set; } = null;

        internal static Dictionary<string, RuleItem?> contextTagSpriteCache = [];

        internal string Name => bigCraftable.DisplayName;

        internal static IconEdge QuestionIcon => new(new(Game1.mouseCursors, new Rectangle(240, 192, 16, 16)), Edges.NONE);
        // internal static Sprite GreenStar => new(Game1.mouseCursors_1_6, new Rectangle(457, 298, 11, 11));
        internal static IconEdge EmojiX => new(new(ChatBox.emojiTexture, new Rectangle(45, 81, 9, 9)), new(14), 4f);
        internal static IconEdge EmojiExclaim => new(new(ChatBox.emojiTexture, new Rectangle(54, 81, 9, 9)), new(Top: 37), 3f);
        internal static IconEdge EmojiNote => new(new(ChatBox.emojiTexture, new Rectangle(81, 81, 9, 9)), Edges.NONE, 3f);
        internal static IconEdge EmojiBolt => new(new(ChatBox.emojiTexture, new Rectangle(36, 63, 9, 9)), new(Left: 37), 3f);
        internal static IEnumerable<IconEdge> Number(int num)
        {
            int offset = 44;
            while (num > 0)
            {
                // final digit
                int digit = num % 10;
                yield return new IconEdge(
                    new Sprite(Game1.mouseCursors, new Rectangle(368 + digit * 5, 56, 5, 7)),
                    new(offset, 48, 0, 0)
                );
                // unclear why this looks the best, shouldnt it be scale * 5?
                offset -= 12;
                num /= 10;
            }
        }
        internal static IconEdge Quality(int quality)
        {
            return new(
                new(Game1.mouseCursors,
                (quality < 4) ? new Rectangle(338 + (quality - 1) * 8, 400, 8, 8) : new Rectangle(346, 392, 8, 8)),
                new(Top: 37),
                3
            );
        }

        private readonly SObject bigCraftable;
        private readonly MachineData machine;
        internal List<RuleEntry> RuleEntries;


        internal RuleHelper(SObject bigCraftable, MachineData machine)
        {
            this.bigCraftable = bigCraftable;
            this.machine = machine;
            RuleEntries = GetRuleEntries();
        }

        /// <summary>
        /// Two outputs are similar enough if we arent doing anything fun icon wise
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

        private List<RuleEntry> GetRuleEntries()
        {
            List<RuleEntry> entries = [];
            ItemQueryContext context = new();

            // Fuel
            List<RuleItem> sharedFuel = [];
            if (machine.AdditionalConsumedItems != null)
            {
                foreach (MachineItemAdditionalConsumedItems fuel in machine.AdditionalConsumedItems)
                {
                    ParsedItemData itemData = ItemRegistry.GetData(fuel.ItemId);
                    sharedFuel.Add(new RuleItem(
                        [new(new(itemData.GetTexture(), itemData.GetSourceRect()), Edges.NONE),
                         EmojiBolt,
                         ..Number(fuel.RequiredCount)],
                        [itemData.DisplayName]
                    ));
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
                        optLine.Add(new RuleItem([QuestionIcon], [$"SPECIAL {methodName}"]));
                        hasComplex = true;
                    }
                    if (output.ItemId == "DROP_IN")
                    {
                        optLine.Add(new RuleItem([QuestionIcon], [I18n.RuleList_SameAsInput()]));
                    }
                    else if (output.ItemId != null)
                    {
                        IList<ItemQueryResult> itemQueryResults = ItemQueryResolver.TryResolve(
                            output, context,
                            formatItemId: id => id != null ? Regex.Replace(id, "(DROP_IN_ID|DROP_IN_PRESERVE|NEARBY_FLOWER_ID)", "0") : id
                        );
                        foreach (ItemQueryResult res in itemQueryResults)
                        {
                            ParsedItemData itemData = ItemRegistry.GetData(res.Item.QualifiedItemId);
                            if (itemData == null) continue;
                            List<IconEdge> icons = [new(new(itemData.GetTexture(), itemData.GetSourceRect()), Edges.NONE)];
                            List<string> tooltip = [];
                            if (output.Condition != null)
                            {
                                icons.Add(EmojiExclaim);
                                tooltip.AddRange(output.Condition.Split(','));
                            }
                            icons.AddRange(Number(res.Item.Stack));
                            if (res.Item.Quality > 0)
                            {
                                icons.Add(Quality(res.Item.Quality));
                            }
                            tooltip.Add(itemData.DisplayName);
                            optLine.Add(new RuleItem(icons, tooltip));
                        }
                    }
                    if (optLine.Count == 0)
                        continue;

                    if (EMC != null)
                    {
                        List<RuleItem> emcFuel = [];
                        // var extraReq = EMC.GetExtraRequirements(output);
                        // if (extraReq.Count > 0)
                        // {
                        //     outputs
                        // }
                        var extraTagReq = EMC.GetExtraTagsRequirements(output);
                        if (extraTagReq.Count > 0)
                        {
                            foreach ((string tag, int count) in extraTagReq)
                            {
                                var results = GetContextTagRuleItems(tag.Split(','), context, out List<string> negateTags, out IconEdge? qualityIcon);
                                if (results != null)
                                {
                                    foreach (var res in results)
                                    {
                                        res.Tooltip.InsertRange(0, negateTags);
                                        res.Icons.Add(EmojiBolt);
                                        res.Icons.AddRange(Number(count));
                                        if (qualityIcon != null)
                                            res.Icons.Add(qualityIcon);
                                        emcFuel.Add(res);
                                    }
                                }
                            }
                        }
                        if (emcFuel.Count > 0)
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
                List<Tuple<string, int, bool, List<RuleItem>>> inputs = [];
                RuleItem? placeholder = null;
                int seq = -1;
                foreach (MachineOutputTriggerRule trigger in rule.Triggers)
                {
                    seq++;
                    List<RuleItem> inputLine = [];
                    // no item input
                    if (!trigger.Trigger.HasFlag(MachineOutputTrigger.ItemPlacedInMachine))
                    {
                        List<IconEdge> icons = [QuestionIcon];
                        List<string> tooltip = [trigger.Trigger.ToString()];
                        if (trigger.Condition != null)
                        {
                            tooltip.AddRange(trigger.Condition.Split(','));
                        }
                        placeholder = new RuleItem(icons, tooltip);
                        continue;
                    }
                    // item input based rules
                    if (trigger.RequiredItemId != null)
                    {
                        ParsedItemData itemData = ItemRegistry.GetData(trigger.RequiredItemId);
                        if (itemData != null)
                        {
                            RuleItem? preserve = GetPreservedColorAndName(trigger.RequiredTags, itemData, context);
                            if (preserve != null)
                            {
                                inputLine.Add(preserve);
                            }
                            else
                            {
                                inputLine.Add(new RuleItem(
                                    [new(new(itemData.GetTexture(), itemData.GetSourceRect()), Edges.NONE)],
                                    [itemData.DisplayName]
                                ));
                            }
                        }
                    }
                    List<string> negateTags = [];
                    IconEdge? qualityIcon = null;
                    if (trigger.RequiredTags != null)
                    {
                        inputLine.AddRange(GetContextTagRuleItems(trigger.RequiredTags, context, out negateTags, out qualityIcon));
                    }
                    if (inputLine.Count > 0)
                    {
                        bool needExclaim = false;
                        if (negateTags.Count > 0)
                        {
                            inputLine.Last().Tooltip.InsertRange(0, negateTags);
                            needExclaim = true;
                        }
                        if (qualityIcon != null)
                        {
                            inputLine.Last().Icons.Add(qualityIcon);
                        }
                        if (trigger.Condition != null)
                        {
                            inputLine.Last().Tooltip.InsertRange(0, trigger.Condition.Split(','));
                            needExclaim = true;
                        }
                        if (needExclaim)
                            inputLine.Last().Icons.Add(EmojiExclaim);

                        if (trigger.RequiredCount > 0)
                            inputLine.Last().Icons.AddRange(Number(trigger.RequiredCount));

                        if (sharedFuel.Count > 0)
                            inputLine.AddRange(sharedFuel);

                        inputs.Add(new(
                            trigger.Id,
                            seq,
                            trigger.Trigger.HasFlag(MachineOutputTrigger.ItemPlacedInMachine),
                            inputLine
                        ));
                    }
                }
                if (inputs.Count == 0)
                {
                    if (hasComplex)
                    {
                        string invalidMsg = machine.InvalidItemMessage == null ?
                            I18n.RuleList_ComplexInput() :
                            TokenParser.ParseText(machine.InvalidItemMessage);
                        inputs.Add(new(PLACEHOLDER_TRIGGER, -1, false, [new RuleItem([QuestionIcon], [invalidMsg])]));
                    }
                    else if (placeholder != null)
                    {
                        inputs.Add(new(PLACEHOLDER_TRIGGER, -1, false, [placeholder]));
                    }
                }

                if (withEmcFuel.Count > 0)
                {
                    foreach ((List<RuleItem> optLine, List<RuleItem> emcFuel) in withEmcFuel)
                    {
                        foreach ((string triggerId, int idx, bool canCheck, List<RuleItem> inputLine) in inputs)
                        {
                            List<RuleItem> ipt = [.. inputLine, .. emcFuel];

                            entries.Add(new RuleEntry(
                                new(bigCraftable.QualifiedItemId, rule.Id, triggerId, idx),
                                canCheck,
                                ipt,
                                optLine
                            ));
                        }
                    }
                }

                if (outputLine.Count > 0)
                {
                    foreach ((string triggerId, int idx, bool canCheck, List<RuleItem> inputLine) in inputs)
                    {
                        var ipt = inputLine;
                        entries.Add(new RuleEntry(
                            new(bigCraftable.QualifiedItemId, rule.Id, triggerId, idx),
                            canCheck,
                            ipt,
                            outputLine
                        ));
                    }
                }

            }

            return entries;
        }

        internal static RuleItem? GetPreservedColorAndName(List<string>? tags, ParsedItemData baseItem, ItemQueryContext context)
        {
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
                    // id_o_itemid resolves but preserved_item_index_itemid cus that gets added for Object
                    string idTag = $"id_o_{realTag[21..]}";
                    if (ItemQueryResolver.TryResolve(
                        "ALL_ITEMS",
                        context,
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
                        if (ItemQueryResolver.TryResolve(
                            $"FLAVORED_ITEM {preserveType} {preserveItem.ItemId}",
                            context
                        ).FirstOrDefault()?.Item is ColoredObject preserve)
                        {
                            List<IconEdge> icons = [];
                            // drawing with layder 
                            if (preserve.ColorSameIndexAsParentSheetIndex)
                            {
                                icons.Add(new(new(baseItem.GetTexture(), baseItem.GetSourceRect()), Edges.NONE, Tint: preserve.color.Value));
                            }
                            else
                            {
                                icons.Add(new(new(baseItem.GetTexture(), baseItem.GetSourceRect()), Edges.NONE));
                                icons.Add(new(new(baseItem.GetTexture(), baseItem.GetSourceRect(1)), Edges.NONE, Tint: preserve.color.Value));
                            }
                            return new RuleItem(
                                icons,
                                [preserve.DisplayName]
                            );
                        }
                    }
                }
            }
            return null;
        }

        internal static List<RuleItem> GetContextTagRuleItems(IEnumerable<string> tags, ItemQueryContext context, out List<string> negateTags, out IconEdge? qualityIcon)
        {
            List<RuleItem> rules = [];
            List<RuleItem> negateRules = [];
            negateTags = [];
            qualityIcon = null;
            foreach (string tag in tags)
            {
                var result = GetContextTagRuleItem(tag, context, out qualityIcon);
                if (result != null)
                {
                    (RuleItem ctxTag, bool negate) = result;
                    if (negate)
                        negateRules.Add(ctxTag);
                    else
                        rules.Add(ctxTag);
                }
            }

            if (negateRules.Count > 0)
            {
                if (rules.Count == 0)
                    return negateRules;
                foreach (RuleItem ctxTag in negateRules)
                {
                    negateTags.AddRange(ctxTag.Tooltip);
                }
            }

            return rules;
        }

        internal static Tuple<RuleItem, bool>? GetContextTagRuleItem(string tag, ItemQueryContext context, out IconEdge? qualityIcon)
        {
            // need to handle quality_* items
            tag = tag.Trim();
            bool negate = tag.StartsWith('!');
            string realTag = negate ? tag[1..] : tag;

            qualityIcon = null;

            if (contextTagSpriteCache.TryGetValue(tag, out RuleItem? ctxTag))
            {
                // tag was resolved before, and no valid item was found
                if (ctxTag == null)
                    return null;
            }
            else
            {
                bool showNote = true;
                string tooltip = realTag;
                float alpha = 0.5f;

                ParsedItemData? itemData = null;
                // skip preserve sheet index tag
                if (realTag.StartsWith("preserve_sheet_index_"))
                {
                    contextTagSpriteCache[tag] = null;
                    return null;
                }
                // quality based tag
                else if (GetContextTagQuality(tag) is int quality)
                {
                    qualityIcon = Quality(quality);
                    return null;
                }
                // get first item found with this tag
                else if (ItemQueryResolver.TryResolve(
                    "ALL_ITEMS",
                    context,
                    ItemQuerySearchMode.FirstOfTypeItem,
                    $"ITEM_CONTEXT_TAG Target {realTag}"
                ).FirstOrDefault()?.Item is Item item)
                {
                    itemData = ItemRegistry.GetData(item.QualifiedItemId);
                    if (realTag.StartsWith("id_"))
                    {
                        showNote = false;
                        tooltip = itemData.DisplayName;
                        alpha = 1f;
                    }
                }
                if (itemData == null)
                {
                    contextTagSpriteCache[tag] = null;
                    return null;
                }

                List<IconEdge> icons = [];
                icons.Add(new(new(itemData.GetTexture(), itemData.GetSourceRect()), Edges.NONE, Tint: Color.White * alpha));
                if (showNote)
                    icons.Add(EmojiNote);
                if (negate)
                {
                    icons.Add(EmojiX);
                    ctxTag = new RuleItem(icons, [$"NOT {tooltip}"]);
                }
                else
                {
                    ctxTag = new RuleItem(icons, [tooltip]);
                }
                contextTagSpriteCache[tag] = ctxTag;
            }

            return new(new(ctxTag.Icons, ctxTag.Tooltip.DeepClone()), negate);
        }

        internal static int? GetContextTagQuality(string tag)
        {
            return tag switch
            {
                "quality_none" => 0,
                "quality_silver" => 1,
                "quality_gold" => 2,
                "quality_iridium" => 4,
                _ => null
            };
        }
    }
}