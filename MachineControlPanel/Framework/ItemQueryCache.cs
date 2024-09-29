using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace MachineControlPanel.Framework
{
    /// <summary>Cache info about a context tag</summary>
    internal static class ItemQueryCache
    {
        internal static IconEdge EmojiX => new(new(ChatBox.emojiTexture, new Rectangle(45, 81, 9, 9)), new(14), 4f);
        internal static IconEdge EmojiNote => new(new(ChatBox.emojiTexture, new Rectangle(81, 81, 9, 9)), Scale: 3f);
        private static readonly ConditionalWeakTable<string, RuleItem?> contextTagRuleItemCache = [];
        private static readonly ConditionalWeakTable<string, ImmutableList<ParsedItemData>?> multiContextTagItemDataCache = [];
        private static readonly ConditionalWeakTable<string, ImmutableHashSet<ParsedItemData>?> contextTagItemDataCache = [];
        private static readonly ItemQueryContext context = new();
        internal static ItemQueryContext Context => context;

        /// <summary>Clear cache, usually because Data/Objects was invalidated.</summary>
        internal static void Invalidate()
        {
            contextTagRuleItemCache.Clear();
            multiContextTagItemDataCache.Clear();
            contextTagItemDataCache.Clear();
            CreateSpecialContextTagRuleItems();
        }

        internal static void CreateSpecialContextTagRuleItems()
        {

        }

        /// <summary>
        /// Try to obtain list of item data for a context tag and populate contextTagItemDataCache as required.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="itemDatas"></param>
        /// <returns></returns>
        internal static bool TryGetContextTagRuleItemCache(IEnumerable<string> tags, [NotNullWhen(true)] out RuleItem? ctxTag)
        {
            string tagsStr = string.Join(',', tags.ToImmutableSortedSet());
            ctxTag = contextTagRuleItemCache.GetValue(tagsStr, CreateContextTagRuleItem);
            return ctxTag != null;
        }

        /// <summary>
        /// Make
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private static RuleItem? CreateContextTagRuleItem(string tagExpr)
        {
            var tags = tagExpr.Split(',');
            if (!TryGetAllItemDataWithTags(tags, out ImmutableList<ParsedItemData>? matchingItemData))
                return null;
            ParsedItemData itemData = matchingItemData.First();
            // special case only 1 matched item
            if (matchingItemData.Count == 1)
            {
                return new RuleItem(
                    [new(new(itemData.GetTexture(), itemData.GetSourceRect()))],
                    [itemData.DisplayName],
                    QId: itemData.QualifiedItemId
                );
            }
            // general case make a representative item
            // List<string> tooltips = tags.Select((tag) =>
            // {
            //     tag = tag.Trim();
            //     bool negate = tag.StartsWith('!');
            //     string realTag = negate ? tag[1..] : tag;
            //     return negate ? $"NOT {tag}" : tag;
            // }).ToList();
            List<string> tooltips = [];
            bool onlyNegate = true;
            foreach (string rawTag in tags)
            {
                string tag = rawTag.Trim();
                bool negate = tag.StartsWith('!');
                onlyNegate &= negate;
                string realTag = negate ? tag[1..] : tag;
                tooltips.Add(negate ? $"NOT {realTag}" : realTag);
            }

            List<IconEdge> icons = [
                new(new(itemData.GetTexture(), itemData.GetSourceRect()), Tint: Color.White * 0.5f),
                EmojiNote
            ];
            if (onlyNegate)
                icons.Add(EmojiX);
            return new RuleItem(icons, tooltips);
        }

        /// <summary>
        /// Make the context tag repr item from specific item data instead of doing query
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="itemData"></param>
        /// <returns></returns>
        private static RuleItem? CreateSpecialContextTagRuleItem(string tag, ParsedItemData itemData)
        {
            return new RuleItem(
                [new(new(itemData.GetTexture(), itemData.GetSourceRect()), Tint: Color.White * 0.5f),
                 EmojiNote],
                [tag]
            );
        }

        /// <summary>
        /// Try to obtain hashset of all items with matching tags and populate <see cref="multiContextTagItemDataCache"> as required.
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="itemDatas"></param>
        /// <returns></returns>
        internal static bool TryGetAllItemDataWithTags(IEnumerable<string> tags, [NotNullWhen(true)] out ImmutableList<ParsedItemData>? itemDatas)
        {
            string tagsStr = string.Join(',', tags.ToImmutableSortedSet());
            itemDatas = multiContextTagItemDataCache.GetValue(tagsStr, GetAllItemDataWithTags);
            return itemDatas != null;
        }

        /// <summary>
        /// Get items that have all tag in a list
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        private static ImmutableList<ParsedItemData>? GetAllItemDataWithTags(string tagExpr)
        {
            var tags = tagExpr.Split(',');
            List<ImmutableHashSet<ParsedItemData>> matchingItemDatas = [];
            List<ImmutableHashSet<ParsedItemData>> negatingItemDatas = [];
            foreach (string rawTag in tags)
            {
                string tag = rawTag.Trim();
                bool negate = tag.StartsWith('!');
                string realTag = negate ? tag[1..] : tag;
                if (TryGetContextTagItemData(realTag, out ImmutableHashSet<ParsedItemData>? ctItemData))
                {
                    if (negate)
                        negatingItemDatas.Add(ctItemData);
                    else
                        matchingItemDatas.Add(ctItemData);
                }
            }
            if (!matchingItemDatas.Any() && !negatingItemDatas.Any())
                return null;
            HashSet<ParsedItemData> resultItemDatas = [];
            if (matchingItemDatas.Any())
            {
                resultItemDatas.AddRange(matchingItemDatas.First());
                foreach (var itemDatas in matchingItemDatas.Skip(1))
                    resultItemDatas.IntersectWith(itemDatas);
            }
            // if only negate tags are present, start with all item data
            if (negatingItemDatas.Any() && !resultItemDatas.Any())
                resultItemDatas.AddRange(ItemRegistry.GetObjectTypeDefinition().GetAllData());
            foreach (var itemDatas in negatingItemDatas)
                resultItemDatas.ExceptWith(itemDatas);
            return resultItemDatas.Any() ? resultItemDatas.ToImmutableList() : null;
        }

        /// <summary>
        /// Try to obtain list of item data for a context tag and populate <see cref="contextTagItemDataCache"> as required.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="itemDatas"></param>
        /// <returns></returns>
        private static bool TryGetContextTagItemData(string tag, [NotNullWhen(true)] out ImmutableHashSet<ParsedItemData>? itemDatas)
        {
            itemDatas = contextTagItemDataCache.GetValue(tag, CreateContextTagItemDatas);
            return itemDatas != null;
        }

        /// <summary>Get list of item data with a given context tag.</summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private static ImmutableHashSet<ParsedItemData>? CreateContextTagItemDatas(string tag)
        {
            tag = tag.Trim();
            // get all item data associated with this tag
            if (ItemQueryResolver.TryResolve(
                "ALL_ITEMS",
                context,
                ItemQuerySearchMode.All,
                $"ITEM_CONTEXT_TAG Target {tag}"
            ) is ItemQueryResult[] results && results.Any())
            {
                return results.Select((res) => ItemRegistry.GetData(res.Item.QualifiedItemId)).ToImmutableHashSet();
            }
            return null;
        }

        /// <summary>Get list of item data with a given context tag.</summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private static ImmutableHashSet<ParsedItemData>? CreateConditionItemDatas(string condition)
        {
            // get all item data that matches a condition
            if (ItemQueryResolver.TryResolve(
                "ALL_ITEMS",
                context,
                ItemQuerySearchMode.All,
                condition
            ) is ItemQueryResult[] results && results.Any())
            {
                return results.Select((res) => ItemRegistry.GetData(res.Item.QualifiedItemId)).ToImmutableHashSet();
            }
            return null;
        }

    }
}