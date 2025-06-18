using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace CarpenterAutobuy;

public sealed class ModEntry : Mod
{
    public const string ModId = "mushymato.CarpenterAutobuy";

    private sealed record PriceModContext(int Price, List<(int, Item)> Originals);

    private static readonly ConditionalWeakTable<CarpenterMenu.BlueprintEntry, PriceModContext> ExtraCosts = [];
    private ModConfig Config = null!;

    private class ModConfig
    {
        internal readonly KeybindList Key = KeybindList.Parse("Tab");
    }

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        Config = Helper.ReadConfig<ModConfig>();
        Harmony harmony = new(ModId);
        harmony.Patch(
            original: AccessTools.DeclaredPropertyGetter(
                typeof(CarpenterMenu.BlueprintEntry),
                nameof(CarpenterMenu.BlueprintEntry.BuildCost)
            ),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(BlueprintEntry_BuildCost_Postfix))
        );
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Config.Key.JustPressed() || Game1.activeClickableMenu is not CarpenterMenu carpenterMenu)
            return;
        if (ExtraCosts.TryGetValue(carpenterMenu.Blueprint, out PriceModContext? ctx))
        {
            foreach ((int i, Item item) in ctx.Originals)
            {
                carpenterMenu.ingredients.Insert(i, item);
            }
            ExtraCosts.Remove(carpenterMenu.Blueprint);
        }
        else
        {
            List<(int, Item)> originals = [];
            // (O)388 wood
            // (O)390 stone
            int extraCost = 0;
            for (int i = 0; i < carpenterMenu.ingredients.Count; i++)
            {
                Item ingredient = carpenterMenu.ingredients[i];
                if (ingredient.QualifiedItemId == "(O)388" || ingredient.QualifiedItemId == "(O)390")
                {
                    extraCost += ingredient.Stack * ingredient.salePrice();
                    originals.Add(new(i, ingredient));
                }
            }
            foreach ((int i, _) in originals.AsEnumerable().Reverse())
            {
                carpenterMenu.ingredients.RemoveAt(i);
            }
            ExtraCosts.Add(carpenterMenu.Blueprint, new(extraCost, originals));
        }
    }

    private static void BlueprintEntry_BuildCost_Postfix(CarpenterMenu.BlueprintEntry __instance, ref int __result)
    {
        if (ExtraCosts.TryGetValue(__instance, out PriceModContext? ctx))
        {
            __result += ctx.Price;
        }
    }
}
