using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.GameData.Machines;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace MachineControlPanel.Framework
{
    internal static class DictionaryExtension
    {
        /// <summary>
        /// Attempt to get value from key in dictionary being used as a cache.
        /// If the key is not set, create and set it using provided delegate.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="createValue"></param>
        /// <returns></returns>
        public static TValue GetOrCreateValue<TValue>(this Dictionary<string, TValue> dict, string key, Func<string, TValue> createValue)
        {
            if (dict.TryGetValue(key, out TValue? result))
                return result;
            result = createValue(key);
            dict[key] = result;
            return result;
        }
    }
    internal static class RuleHelperCache
    {
        // private static readonly ConditionalWeakTable<string, RuleHelper?> ruleHelperCache = [];
        private static readonly Dictionary<string, RuleHelper?> ruleHelperCache = [];

        /// <summary>Clear cache, usually because Data/Machines was invalidated.</summary>
        internal static void Invalidate()
        {
            ruleHelperCache.Clear();
        }

        internal static bool TryGetRuleHelper(string bigCraftableId, string displayName, MachineData machine, [NotNullWhen(true)] out RuleHelper? ruleHelper)
        {
            ruleHelper = ruleHelperCache.GetOrCreateValue(bigCraftableId, (bcId) => CreateRuleHelper(bcId, displayName, machine));
            return ruleHelper != null;
        }

        internal static RuleHelper? CreateRuleHelper(string qId, string displayName, MachineData machine)
        {
            if (machine.IsIncubator || machine.OutputRules == null || machine.OutputRules.Count == 0 || !machine.AllowFairyDust)
                return null;
            return new(qId, displayName, machine);
            // return ruleHelper.GetRuleEntries() ? ruleHelper : null;
        }
    }

    /// <summary>Cache info about items matching a condition</summary>
    internal static class ItemQueryCache
    {
        // internal static IconEdge EmojiX => new(new(ChatBox.emojiTexture, new Rectangle(45, 81, 9, 9)), new(14), 4f);
        private static readonly List<string> ItemGSQ = [
            "ITEM_CONTEXT_TAG ",
            "ITEM_CATEGORY ",
            "ITEM_HAS_EXPLICIT_OBJECT_CATEGORY ",
            "ITEM_ID ",
            "ITEM_ID_PREFIX ",
            "ITEM_NUMERIC_ID ",
            "ITEM_OBJECT_TYPE ",
            "ITEM_PRICE ",
            "ITEM_QUALITY ",
            "ITEM_STACK ",
            "ITEM_TYPE ",
            "ITEM_EDIBILITY "
        ];
        private static readonly Regex ExcludeTags = new("(quality_|preserve_sheet_index_).+");
        private static readonly Dictionary<string, ImmutableList<ParsedItemData>?> conditionItemDataCache = [];
        private static readonly ItemQueryContext context = new();
        internal static ItemQueryContext Context => context;

        /// <summary>Clear cache, usually because Data/Objects was invalidated.</summary>
        internal static void Invalidate()
        {
            conditionItemDataCache.Clear();
        }

        /// <summary>Probe the complex output delegate to verify that the item data is valid</summary>
        /// <param name="complexOutput"></param>
        internal static IEnumerable<ParsedItemData> FilterByOutputMethod(string qId, List<MachineItemOutput> outputs, IEnumerable<ParsedItemData>? itemDatas)
        {
            SObject machineObj = ItemRegistry.Create<SObject>(qId);
            // magic knowledge that anvil takes trinkets
            if (qId == "(BC)Anvil")
            {
                itemDatas ??= ItemRegistry.RequireTypeDefinition<TrinketDataDefinition>("(TR)").GetAllData();
            }
            else
            {
                itemDatas ??= ItemRegistry.GetObjectTypeDefinition().GetAllData();
            }
            return itemDatas.Where((itemData) =>
            {
                foreach (MachineItemOutput output in outputs)
                {
                    // special case cask, assume valid.
                    // this is because making the machine obj be inside cellar is kinda annoying
                    if (output.OutputMethod == "StardewValley.Objects.Cask, Stardew Valley: OutputCask")
                    {
                        return true;
                    }
                    if (StaticDelegateBuilder.TryCreateDelegate<MachineOutputDelegate>(output.OutputMethod, out var createdDelegate, out var _)
                        && ItemRegistry.Create(itemData.QualifiedItemId) is Item inputItem
                        && createdDelegate(machineObj, inputItem, true, output, Game1.player, out _) != null)
                    {
                        return true;
                    }
                }
                return false;
            });
        }

        /// <summary>Convert some conditions and tags into a condition of specific form</summary>
        /// <param name="condition"></param>
        /// <param name="tags"></param>
        /// <param name="nonItemConditions"></param>
        /// <returns></returns>
        internal static string? NormalizeCondition(string? condition, IEnumerable<string>? tags, out List<string> nonItemConditions)
        {
            nonItemConditions = [];
            SortedSet<string> mergedConds = [];
            if (condition != null)
            {
                foreach (string rawCond in condition.Split(','))
                {
                    string cond = rawCond.Trim();
                    if (ItemGSQ.Any((gsq) => cond.StartsWith(gsq) || cond[1..].StartsWith(gsq)))
                        mergedConds.Add(cond.Replace(" Input ", " Target "));
                    else
                        nonItemConditions.Add(cond);
                }
            }
            if (tags != null)
            {
                List<string> filteredTags = tags.Select((tag) => tag.Trim()).Where(tag => !ExcludeTags.Match(tag).Success).ToList();
                if (filteredTags.Any())
                    mergedConds.Add($"ITEM_CONTEXT_TAG Target {string.Join(' ', filteredTags)}");
            }
            if (!mergedConds.Any())
                return null;

            return string.Join(',', new SortedSet<string>(mergedConds));
        }

        /// <summary>Get conditional item datas from a condition string that should be normalized</summary>
        /// <param name="tag"></param>
        /// <param name="itemDatas"></param>
        /// <returns></returns>
        internal static bool TryGetConditionItemDatas(string condition, [NotNullWhen(true)] out ImmutableList<ParsedItemData>? itemDatas)
        {
            itemDatas = conditionItemDataCache.GetOrCreateValue(condition, CreateConditionItemDatas);
            return itemDatas != null;
        }

        /// <summary>Get conditional item datas from a condition string that should be normalized</summary>
        /// <param name="tag"></param>
        /// <param name="itemDatas"></param>
        /// <returns></returns>
        internal static bool TryGetConditionItemDatas(string? condition, string qId, List<MachineItemOutput> complexOutputs, [NotNullWhen(true)] out ImmutableList<ParsedItemData>? itemDatas)
        {
            itemDatas = null;
            if (condition != null)
                itemDatas = conditionItemDataCache.GetOrCreateValue(condition, CreateConditionItemDatas);
            if (complexOutputs.Any())
                itemDatas = FilterByOutputMethod(qId, complexOutputs, itemDatas).ToImmutableList();
            return itemDatas != null && itemDatas.Any();
        }

        /// <summary>Get list of <see cref="ParsedItemData"/> matching a particular condition</summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        private static ImmutableList<ParsedItemData>? CreateConditionItemDatas(string condition)
        {
            // get all item data that matches a condition
            if (ItemQueryResolver.TryResolve(
                "ALL_ITEMS",
                context,
                ItemQuerySearchMode.All,
                condition
            ) is ItemQueryResult[] results && results.Any())
            {
                return results
                    .Select((res) => ItemRegistry.GetData(res.Item.QualifiedItemId))
                    .ToImmutableList();
            }
            return null;
        }

        /// <summary>Get preserve rule item which is colored, this is not cached rn maybe later</summary>
        /// <param name="tags"></param>
        /// <param name="count"></param>
        /// <param name="baseItem"></param>
        /// <param name="preserveTag"></param>
        /// <returns></returns>
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
                        Context,
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
                        if (ItemQueryResolver.TryResolve($"FLAVORED_ITEM {preserveType} {preserveItem.ItemId}", Context)
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

    }
}