using System.Text.RegularExpressions;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewUI;
using StardewValley.Delegates;
using StardewValley.Objects;
using HarmonyLib;
using System.Text;


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
        internal static IconEdge EmojiNote => new(new(ChatBox.emojiTexture, new Rectangle(81, 81, 9, 9)), Scale: 3f);
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

        private bool populated = false;
        internal readonly List<RuleEntry> RuleEntries = [];
        internal readonly Dictionary<string, ValidInput> ValidInputs = [];

        internal readonly string Name;
        internal readonly string QId;
        private readonly MachineData machine;

        internal RuleHelper(string qId, string displayName, MachineData machine)
        {
            this.QId = qId;
            this.Name = displayName;
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

        /// <summary>Add item data valid inputs</summary>
        /// <param name="itemData"></param>
        /// <param name="ident"></param>
        internal void AddValidInput(ParsedItemData itemData, RuleIdent ident)
        {
            if (itemData == null || itemData.QualifiedItemId == null)
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

        /// <summary>Add rule item to valid inputs</summary>
        /// <param name="ruleItem"></param>
        /// <param name="ident"></param>
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

        internal bool GetRuleEntries(bool force = false)
        {
            if (!force && populated)
                return RuleEntries.Any();

            populated = false;
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
                List<MachineItemOutput> complexOutputs = [];

                // rule outputs
                List<Tuple<List<RuleItem>, List<RuleItem>>> withEmcFuel = [];
                List<RuleItem> outputLine = [];
                foreach (MachineItemOutput output in PrunedMachineItemOutput(rule.OutputItem))
                {
                    List<RuleItem> optLine = [];
                    if (output.OutputMethod != null)
                    {
                        complexOutputs.Add(output);
                        string methodName = output.OutputMethod.Split(':').Last().Trim();
                        optLine.Add(new RuleItem([QuestionIcon], [I18n.RuleList_SpecialOutput(method: methodName)]));
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
                            string? normalized = ItemQueryCache.NormalizeCondition(null, tags, out List<string> _);
                            // case 2: only ITEM_CONTEXT_TAG Target !<tag> or !ITEM_CONTEXT_TAG Target <tag>
                            if (normalized != null && ItemQueryCache.TryGetConditionItemDatas(normalized, out ImmutableList<ParsedItemData>? matchingItemDatas))
                            {
                                RuleItem condRule = MakeReprRuleItem(matchingItemDatas, normalized);
                                if (GetContextTagQuality(tags) is IconEdge qualityIcon)
                                    condRule.Icons.Add(qualityIcon);
                                condRule.Icons.Add(EmojiBolt);
                                emcFuel.Add(condRule);
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
                    IconEdge? qualityIcon = null;
                    List<string> nonItemConditions = [];
                    // no item input
                    if (!trigger.Trigger.HasFlag(MachineOutputTrigger.ItemPlacedInMachine))
                    {
                        List<string> tooltip = [trigger.Trigger.ToString()];
                        inputLine.Add(new RuleItem([QuestionIcon], tooltip));
                        if (trigger.Condition != null)
                            nonItemConditions = new(trigger.Condition.Split(','));
                    }
                    else
                    {
                        // item input based rules
                        if (trigger.RequiredItemId != null)
                        {
                            if (ItemRegistry.GetData(trigger.RequiredItemId) is ParsedItemData itemData)
                            {
                                RuleItem? preserve = ItemQueryCache.GetPreserveRuleItem(trigger.RequiredTags, trigger.RequiredCount, itemData, out string preserveTag);
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
                            if (trigger.RequiredTags != null)
                            {
                                qualityIcon = GetContextTagQuality(trigger.RequiredTags);
                            }
                        }


                        string? normalized = ItemQueryCache.NormalizeCondition(trigger.Condition, trigger.RequiredTags, out nonItemConditions);
                        if (ItemQueryCache.TryGetConditionItemDatas(normalized, QId, complexOutputs, out ImmutableList<ParsedItemData>? matchingItemDatas))
                        {
                            foreach (ParsedItemData itemData in matchingItemDatas)
                            {
                                if (!ValidInputs.ContainsKey(itemData.QualifiedItemId))
                                    AddValidInput(itemData, ident);
                            }
                            if (matchingItemDatas.Any())
                                inputLine.Add(MakeReprRuleItem(matchingItemDatas, normalized ?? I18n.RuleList_SpecialInput()));
                        }
                    }

                    if (inputLine.Any())
                    {
                        if (qualityIcon != null)
                        {
                            inputLine.Last().Icons.Add(qualityIcon);
                        }
                        if (nonItemConditions.Any())
                        {
                            inputLine.Last().Tooltip.InsertRange(0, nonItemConditions);
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
                if (complexOutputs.Any())
                {
                    if (inputs.Count == 0)
                    {
                        inputs.Add(new(
                            new(rule.Id, PLACEHOLDER_TRIGGER),
                            false,
                            [new RuleItem([QuestionIcon], [I18n.RuleList_SpecialInput()])]
                        ));
                    }
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

            populated = true;
            return RuleEntries.Any();
        }

        internal static RuleItem MakeReprRuleItem(ImmutableList<ParsedItemData> matchingItemDatas, string condition)
        {
            ParsedItemData firstItem = ItemRegistry.GetData(matchingItemDatas.First().QualifiedItemId);
            if (matchingItemDatas.Count == 1)
            {
                return new RuleItem(
                    [new(new(firstItem.GetTexture(), firstItem.GetSourceRect()))],
                    [firstItem.DisplayName],
                    QId: firstItem.QualifiedItemId
                );
            }
            else
            {
                List<string> tooltips = [];
                foreach (string cond in condition.Split(','))
                {
                    if (cond.StartsWith("ITEM_CONTEXT_TAG Target ") || cond.StartsWith("!ITEM_CONTEXT_TAG Target "))
                    {
                        bool negate = cond[0] == '!';
                        string[] tags = cond[(negate ? "!ITEM_CONTEXT_TAG Target ".Length : "ITEM_CONTEXT_TAG Target ".Length)..].Split(' ');
                        foreach (string tag in tags)
                        {
                            if (tag == "")
                                continue;
                            string realTag = tag[0] == '!' ? tag[1..] : tag;
                            if (tag[0] == '!' != negate) // XOR
                                tooltips.Add($"NOT {realTag}");
                            else
                                tooltips.Add(realTag);
                        }
                    }
                    else
                    {
                        tooltips.Add(cond);
                    }
                }
                return new RuleItem(
                    [new(new(firstItem.GetTexture(), firstItem.GetSourceRect()), Tint: Color.White * 0.5f), EmojiNote],
                    tooltips
                );
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