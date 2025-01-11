using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace MatoTweaks.Tweak;

internal static class ChestStack
{
    public const int CHEST_SIZE = 80;
    public const int BIG_CHEST_SIZE = 140;

    public static void Patch(Harmony patcher)
    {
        try
        {
            // make chests bigger
            patcher.Patch(
                original: AccessTools.DeclaredMethod(typeof(ItemGrabMenu), nameof(ItemGrabMenu.FillOutStacks)),
                postfix: new HarmonyMethod(typeof(ChestStack), nameof(ItemGrabMenu_FillOutStacks_Postfix))
            );
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to patch ChestStack:\n{err}", LogLevel.Error);
        }
    }

    private static void ItemGrabMenu_FillOutStacks_Postfix(ItemGrabMenu __instance)
    {
        IList<Item> source = __instance.inventory.actualInventory;
        if (source.Count == 0)
            return;
        IList<Item> target = __instance.ItemsToGrabMenu.actualInventory;
        int max = __instance.ItemsToGrabMenu.capacity;
        if (target.Count >= max)
            return;

        ILookup<string, Item> lookup = target
            .Where((Item item) => item != null)
            .ToLookup((Item item) => item.QualifiedItemId);
        for (int i = 0; i < source.Count; i++)
        {
            Item sourceItem = source[i];
            if (sourceItem != null && lookup[sourceItem.QualifiedItemId].Any())
            {
                target.Add(sourceItem);
                source[i] = null!;
                if (target.Count >= max)
                    return;
            }
        }
        ItemGrabMenu.organizeItemsInList(source);
        ItemGrabMenu.organizeItemsInList(target);
    }
}
