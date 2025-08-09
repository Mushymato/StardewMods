// https://github.com/atravita-mods/StardewMods/blob/1db0a9587f1f5963a2f7e09ebd40824f351326c4/ExperimentalLagReduction/HarmonyPatches/MiniChanges/ItemSortRewrite.cs
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace MatoTweaks.Tweak;

internal static class AtravitaItemSort
{
    public static void Patch(Harmony patcher)
    {
        patcher.Patch(
            original: AccessTools.Method(typeof(Item), nameof(Item.CompareTo)),
            prefix: new HarmonyMethod(
                AccessTools.DeclaredMethod(typeof(AtravitaItemSort), nameof(Item_CompareTo_Prefix))
            )
        );

        patcher.Patch(
            original: AccessTools.DeclaredMethod(typeof(ItemGrabMenu), nameof(ItemGrabMenu.organizeItemsInList)),
            postfix: new HarmonyMethod(
                AccessTools.DeclaredMethod(typeof(AtravitaItemSort), nameof(ItemGrabMenu_organizeItemsInList_Postfix))
            )
        );
    }

    private static readonly HashSet<string> BombItems = ["(O)286", "(O)287", "(O)288"];
    private static readonly string[] ToolClasses =
    [
        "WateringCan",
        "Pan",
        "FishingRod",
        "Scythe",
        "Hoe",
        "Axe",
        "Pickaxe",
    ];

    private static void ItemGrabMenu_organizeItemsInList_Postfix(IList<Item> items)
    {
        if (items != Game1.player.Items)
            return;
        // tools
        Dictionary<string, Item> tools = [];
        foreach (Item item in items)
        {
            if (item == null)
                continue;
            if (item is MeleeWeapon weapon1 && weapon1.isScythe())
            {
                tools["Scythe"] = item;
            }
            else if (
                item is Tool tool
                && tool.GetToolData()?.ClassName is string className
                && ToolClasses.Contains(className)
            )
            {
                tools[className] = item;
            }
        }
        items.RemoveWhere(tools.ContainsValue);
        foreach (string toolClass in ToolClasses)
        {
            if (tools.TryGetValue(toolClass, out Item? item))
            {
                items.Insert(0, item);
            }
        }
        // bombs
        List<Item> bombs = [];
        foreach (Item item in items)
        {
            if (item != null && BombItems.Contains(item.QualifiedItemId))
            {
                bombs.Add(item);
            }
        }
        items.RemoveWhere(bombs.Contains);
        bombs.Sort();
        bombs.Reverse();
        foreach (Item item in bombs)
        {
            items.Insert(0, item);
        }
        // weapon
        if (items.FirstOrDefault(item => item is MeleeWeapon wpn && !wpn.isScythe()) is Item weapon)
        {
            items.Remove(weapon);
            items.Insert(0, weapon);
        }
    }

    private static bool Item_CompareTo_Prefix(Item __instance, object other, ref int __result)
    {
        if (other is not Item otherItem)
        {
            __result = 0;
            return false;
        }

        // sort by type
        __result = __instance.GetItemTypeId().CompareTo(otherItem.GetItemTypeId());
        if (__result != 0)
        {
            return false;
        }

        // sort category first
        __result = otherItem.getCategorySortValue() - __instance.getCategorySortValue();
        if (__result != 0)
        {
            return false;
        }

        // sort by internal name
        string my_name = GetInternalName(__instance);
        string other_name = GetInternalName(otherItem);

        __result = my_name.CompareTo(other_name);

        if (__result != 0)
        {
            return false;
        }

        // sort by qualified Id
        __result = __instance.QualifiedItemId.CompareTo(otherItem.QualifiedItemId);
        if (__result != 0)
        {
            return false;
        }

        // // sort by level for trinkets
        // if (__instance is Trinket me && otherItem is Trinket otherTrinket)
        // {
        //     TrinketEffect myData = me.GetEffect();
        //     TrinketEffect otherData = otherTrinket.GetEffect();

        //     __result = myData.GeneralStat - otherData.GeneralStat;
        //     if (__result != 0)
        //     {
        //         return false;
        //     }
        // }

        // sort by preserve ID for preserves.
        if (
            __instance is SObject myObj
            && myObj.HasTypeObject()
            && otherItem is SObject otherObj
            && otherObj.HasTypeObject()
        )
        {
            string? myPreserveId = myObj.preservedParentSheetIndex.Value;
            string? otherPreserveId = otherObj.preservedParentSheetIndex.Value;

            if (myPreserveId == "-1")
            {
                myPreserveId = null;
            }
            if (otherPreserveId == "-1")
            {
                otherPreserveId = null;
            }

            string? myPreserveName = myPreserveId?.GetInternalObjectName();
            string? otherPreserveName = otherPreserveId?.GetInternalObjectName();

            __result = myPreserveName?.CompareTo(otherPreserveName) ?? (myPreserveName == otherPreserveName ? 0 : -1);
            if (__result != 0)
            {
                return false;
            }
        }

        // sort by quality?
        __result = otherItem.Quality.CompareTo(__instance.Quality);
        if (__result != 0)
        {
            return false;
        }

        // sort by color for colored items
        if (__instance is ColoredObject myColor)
        {
            if (otherItem is ColoredObject otherColor)
            {
                Color myColorV = myColor.color.Value;
                ColorPicker.RGBtoHSV(
                    myColorV.R,
                    myColorV.G,
                    myColorV.B,
                    out float myHue,
                    out float mySat,
                    out float myValue
                );

                Color otherColorV = otherColor.color.Value;
                ColorPicker.RGBtoHSV(
                    otherColorV.R,
                    otherColorV.G,
                    otherColorV.B,
                    out float otherHue,
                    out float otherSat,
                    out float otherValue
                );

                __result = myHue.CompareTo(otherHue);
                if (__result != 0)
                {
                    return false;
                }

                __result = mySat.CompareTo(otherSat);
                if (__result != 0)
                {
                    return false;
                }

                __result = myValue.CompareTo(otherValue);
                if (__result != 0)
                {
                    return false;
                }
            }
            else
            {
                __result = -1;
                return false;
            }
        }
        else if (otherItem is ColoredObject)
        {
            __result = 1;
            return false;
        }

        // sort by stack
        __result = __instance.Stack - otherItem.Stack;
        return __result == 0;
    }

    private static string GetInternalName(this Item item) =>
        ItemRegistry.GetData(item.QualifiedItemId)?.InternalName
        ?? (string.IsNullOrEmpty(item.Name) ? item.DisplayName : item.Name);

    private static string? GetInternalObjectName(this string id) =>
        ItemRegistry.GetTypeDefinition(ItemRegistry.type_object)?.GetData(id)?.InternalName;
}
