global using RuleIdent = System.Tuple<string, string, int>;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Internal;
using StardewUI;
using SObject = StardewValley.Object;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;


namespace MachineControlPanel.Framework
{
    internal record IconEdge(
        Sprite Img,
        Edges Edg,
        float Scale = 4f,
        Color? Tint = null
    );

    internal record RuleItem(
        List<IconEdge> Icons,
        List<string> Tooltip
    );

    internal record RuleEntry(
        RuleIdent Ident,
        List<RuleItem> Inputs,
        List<RuleItem> Outputs
    )
    {
        internal string Repr => $"{Ident.Item2}.{Ident.Item3}";
    };

    internal class RuleHelper(SObject bigCraftable, MachineData machine)
    {
        internal static Dictionary<string, Sprite> contextTagSpriteCache = [];

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

        internal IList<RuleEntry> GetRuleEntries()
        {
            List<RuleEntry> entries = [];
            ItemQueryContext context = new();
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
                List<RuleItem> outputLine = [];
                foreach (MachineItemOutput output in rule.OutputItem)
                {
                    if (output.OutputMethod != null) // complex method
                    {
                        string methodName = output.OutputMethod.Split(':').Last();
                        outputLine.Add(new RuleItem([QuestionIcon], [$"SPECIAL {methodName}"]));
                        hasComplex = true;
                    }
                    if (output.ItemId == "DROP_IN")
                    {
                        outputLine.Add(new RuleItem([QuestionIcon], [I18n.RuleList_SameAsInput()]));
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
                            tooltip.Add(itemData.DisplayName);
                            outputLine.Add(new RuleItem(icons, tooltip));
                        }
                    }
                }
                // rule inputs (triggers)
                List<List<RuleItem>> inputs = [];
                RuleItem? placeholder = null;
                foreach (MachineOutputTriggerRule trigger in rule.Triggers)
                {
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
                            Tuple<Color, string>? preserve = GetPreservedColorAndName(trigger.RequiredTags);
                            if (preserve != null)
                            {
                                inputLine.Add(new RuleItem(
                                    [new(new(itemData.GetTexture(), itemData.GetSourceRect(1)),
                                     Edges.NONE, Tint: preserve.Item1)],
                                    [$"{itemData.DisplayName} ({preserve.Item2})"]
                                ));

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
                    if (trigger.RequiredTags != null)
                    {
                        inputLine.AddRange(GetContextTagRuleItem(trigger.RequiredTags, context));
                    }
                    if (inputLine.Count > 0)
                    {
                        if (trigger.Condition != null)
                        {
                            inputLine.Last().Icons.Add(EmojiExclaim);
                            inputLine.Last().Tooltip.InsertRange(0, trigger.Condition.Split(','));
                        }
                        if (trigger.RequiredCount > 0)
                        {
                            inputLine.Last().Icons.AddRange(Number(trigger.RequiredCount));
                        }
                        if (sharedFuel.Count > 0)
                        {
                            inputLine.AddRange(sharedFuel);
                        }
                        inputs.Add(inputLine);
                    }
                }
                if (inputs.Count == 0)
                {
                    if (hasComplex)
                    {
                        string invalidMsg = machine.InvalidItemMessage == null ?
                            I18n.RuleList_ComplexInput() :
                            TokenParser.ParseText(machine.InvalidItemMessage);
                        inputs.Add([new RuleItem([QuestionIcon], [invalidMsg])]);
                    }
                    else if (placeholder != null)
                    {
                        inputs.Add([placeholder]);
                    }
                }

                int seq = 0;
                foreach (List<RuleItem> inputLine in inputs)
                {
                    entries.Add(new RuleEntry(
                        new(bigCraftable.QualifiedItemId, rule.Id, seq++),
                        inputLine,
                        outputLine
                    ));
                }

            }

            return entries;
        }

        internal static Tuple<Color, string>? GetPreservedColorAndName(List<string>? tags)
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
                    if (ItemRegistry.Create(realTag[21..]) is Item item)
                    {
                        // sturgeon >:(
                        return new Tuple<Color, string>(
                            item.QualifiedItemId == "(O)698" ? new Color(61, 55, 42) : TailoringMenu.GetDyeColor(item) ?? Color.Orange,
                            item.DisplayName
                        );
                    }
                }
            }
            return null;
        }

        internal static List<RuleItem> GetContextTagRuleItem(List<string> tags, ItemQueryContext context)
        {
            List<RuleItem> rules = [];
            foreach (string tag in tags)
            {
                List<IconEdge> icons = [];
                bool negate = tag.StartsWith('!');
                string realTag = negate ? tag[1..] : tag;
                bool showNote = true;
                string tooltip = realTag;
                float alpha = 0.5f;
                if (!contextTagSpriteCache.TryGetValue(realTag, out Sprite? icon))
                {
                    ParsedItemData? itemData = null;
                    // id based tags
                    if (realTag.StartsWith("id_"))
                    {
                        string[] parts = realTag.Split('_');
                        itemData = ItemRegistry.GetData(parts.Last());
                        showNote = false;
                        tooltip = itemData.DisplayName;
                        alpha = 1f;
                    }
                    // special case preserve item, skip bc we are doing it outside
                    else if (realTag.StartsWith("preserve_sheet_index_"))
                    {
                        continue;
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
                    }
                    if (itemData == null)
                        continue;

                    icon = new Sprite(itemData.GetTexture(), itemData.GetSourceRect());
                    contextTagSpriteCache[realTag] = icon;
                }

                icons.Add(new(icon, Edges.NONE, Tint: Color.White * alpha));
                if (showNote)
                    icons.Add(EmojiNote);
                if (negate)
                {
                    icons.Add(EmojiX);
                    rules.Insert(0, new RuleItem(icons, [$"NOT {tooltip}"]));
                }
                else
                {
                    rules.Add(new RuleItem(icons, [tooltip]));
                }

            }
            return rules;
        }
    }
}