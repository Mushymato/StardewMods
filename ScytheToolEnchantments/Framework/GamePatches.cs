using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using ScytheToolEnchantments.Framework.Enchantments;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using StardewObject = StardewValley.Object;

namespace ScytheToolEnchantments.Framework;

internal sealed class GamePatches
{
    /// <summary>Make sure only 1 append to BaseEnchantment._enchantments is done</summary>
    private static bool enchantmentsInit = false;

    public static void Patch(IManifest ModManifest)
    {
        Harmony harmony = new(ModManifest.UniqueID);
        try
        {
            // allow scythe forge
            harmony.Patch(
                original: AccessTools.Method(typeof(MeleeWeapon), nameof(MeleeWeapon.Forge)),
                transpiler: new HarmonyMethod(typeof(GamePatches), nameof(MeleeWeapon_Forge_Transpiler))
                {
                    before = ["DaLion.Enchantments"],
                }
            ); // tooltip draw
            harmony.Patch(
                original: AccessTools.Method(typeof(MeleeWeapon), nameof(MeleeWeapon.drawTooltip)),
                transpiler: new HarmonyMethod(typeof(GamePatches), nameof(MeleeWeapon_drawTooltip_Transpiler))
            );
            harmony.Patch(
                original: AccessTools.Method(
                    typeof(MeleeWeapon),
                    nameof(MeleeWeapon.getExtraSpaceNeededForTooltipSpecialIcons)
                ),
                transpiler: new HarmonyMethod(
                    typeof(GamePatches),
                    nameof(MeleeWeapon_getExtraSpaceNeededForTooltipSpecialIcons_Transpiler)
                )
            );
            // these two could be 1 GetAvailableEnchantments transpiler, but weh IL ctor
            harmony.Patch(
                original: AccessTools.Method(typeof(BaseEnchantment), nameof(BaseEnchantment.GetAvailableEnchantments)),
                postfix: new HarmonyMethod(
                    typeof(GamePatches),
                    nameof(BaseEnchantment_GetAvailableEnchantments_Postfix)
                )
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(BaseEnchantment), nameof(BaseEnchantment.ResetEnchantments)),
                postfix: new HarmonyMethod(typeof(GamePatches), nameof(BaseEnchantment_ResetEnchantments_Postfix))
            );
            // enchant effects
            harmony.Patch(
                original: AccessTools.Method(typeof(Tree), nameof(Tree.performToolAction)),
                prefix: new HarmonyMethod(typeof(GamePatches), nameof(Tree_performToolAction_Prefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Grass), nameof(Grass.TryDropItemsOnCut)),
                postfix: new HarmonyMethod(typeof(GamePatches), nameof(Grass_TryDropItemsOnCut_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Crop), nameof(Crop.harvest)),
                // // plan A: postfix
                // postfix: new HarmonyMethod(typeof(GamePatches), nameof(Crop_harvest_Postfix))
                // // plan B: transpile
                // transpiler: new HarmonyMethod(typeof(GamePatches), nameof(Crop_harvest_Transpiler))
                // plan C: prefix save state, postfix make drops
                prefix: new HarmonyMethod(typeof(GamePatches), nameof(Crop_harvest_Prefix)),
                postfix: new HarmonyMethod(typeof(GamePatches), nameof(Crop_harvest_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(GiantCrop), nameof(GiantCrop.performToolAction)),
                transpiler: new HarmonyMethod(typeof(GamePatches), nameof(GiantCrop_performToolAction_Transpiler))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(MeleeWeapon), nameof(MeleeWeapon.DoDamage)),
                prefix: new HarmonyMethod(typeof(GamePatches), nameof(MeleeWeapon_DoDamage_Prefix))
            );
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to patch ScytheToolEnchantments:\n{err}", LogLevel.Error);
        }
    }

    private static bool IsScytheAndNotIridium(MeleeWeapon weapon)
    {
        return weapon.isScythe() && weapon.QualifiedItemId != ScytheEnchantment.IridiumScytheQID;
    }

    private static bool HasCrescentEnchantment(Tool tool)
    {
        return (tool is MeleeWeapon weapon && weapon.isScythe() && weapon.hasEnchantmentOfType<CrescentEnchantment>());
    }

    private static IEnumerable<CodeInstruction> MeleeWeapon_drawTooltip_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        try
        {
            CodeMatcher matcher = new(instructions, generator);
            matcher
                .MatchStartForward(
                    new CodeMatch[]
                    {
                        new(OpCodes.Ldarg_0),
                        new(OpCodes.Callvirt, AccessTools.Method(typeof(Tool), nameof(Tool.isScythe))),
                        new(OpCodes.Brtrue),
                    }
                )
                .Advance(1)
                .SetInstruction(
                    new CodeInstruction(
                        OpCodes.Call,
                        AccessTools.Method(typeof(GamePatches), nameof(IsScytheAndNotIridium))
                    )
                );

            return matcher.Instructions();
        }
        catch (Exception err)
        {
            ModEntry.Log($"Error in MeleeWeapon_drawTooltip_Transpiler:\n{err}", LogLevel.Error);
            return instructions;
        }
    }

    private static IEnumerable<CodeInstruction> MeleeWeapon_getExtraSpaceNeededForTooltipSpecialIcons_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        try
        {
            CodeMatcher matcher = new(instructions, generator);
            matcher
                .MatchStartForward(
                    new CodeMatch[]
                    {
                        new(OpCodes.Ldarg_0),
                        new(OpCodes.Callvirt, AccessTools.Method(typeof(Tool), nameof(Tool.isScythe))),
                        new(OpCodes.Brtrue_S),
                    }
                )
                .Advance(1)
                .SetInstruction(
                    new CodeInstruction(
                        OpCodes.Call,
                        AccessTools.Method(typeof(GamePatches), nameof(IsScytheAndNotIridium))
                    )
                );

            return matcher.Instructions();
        }
        catch (Exception err)
        {
            ModEntry.Log(
                $"Error in MeleeWeapon_getExtraSpaceNeededForTooltipSpecialIcons_Transpiler:\n{err}",
                LogLevel.Error
            );
            return instructions;
        }
    }

    private static IEnumerable<CodeInstruction> MeleeWeapon_Forge_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        try
        {
            CodeMatcher matcher = new(instructions, generator);
            matcher
                .MatchStartForward(
                    new CodeMatch[]
                    {
                        new(OpCodes.Ldarg_0),
                        new(OpCodes.Callvirt, AccessTools.Method(typeof(Tool), nameof(Tool.isScythe))),
                        new(OpCodes.Brfalse_S),
                    }
                )
                .Advance(1)
                .SetInstruction(
                    new CodeInstruction(
                        OpCodes.Call,
                        AccessTools.Method(typeof(GamePatches), nameof(IsScytheAndNotIridium))
                    )
                );

            return matcher.Instructions();
        }
        catch (Exception err)
        {
            ModEntry.Log($"Error in MeleeWeapon_Forge_Transpiler:\n{err}", LogLevel.Error);
            return instructions;
        }
    }

    private static void BaseEnchantment_GetAvailableEnchantments_Postfix(ref List<BaseEnchantment> __result)
    {
        try
        {
            if (enchantmentsInit)
                return;
            __result.Add(new PalaeontologistEnchantment());
            __result.Add(new GathererEnchantment());
            __result.Add(new HorticulturistEnchantment());
            __result.Add(new ReaperEnchantment());
            __result.Add(new CrescentEnchantment());
            enchantmentsInit = true;
        }
        catch (Exception err)
        {
            ModEntry.Log($"Error in BaseEnchantment_GetAvailableEnchantments_Postfix:\n{err}", LogLevel.Error);
        }
    }

    private static void BaseEnchantment_ResetEnchantments_Postfix()
    {
        enchantmentsInit = false;
    }

    private static void Grass_TryDropItemsOnCut_Postfix(Grass __instance, Tool tool, bool addAnimation = true)
    {
        try
        {
            if (tool == null)
                return;
            if (__instance.grassType.Value == 1 || __instance.grassType.Value == 7)
            {
                if (tool.hasEnchantmentOfType<GathererEnchantment>() && Random.Shared.NextBool())
                    Game1.createItemDebris(
                        ItemRegistry.Create("(O)178"),
                        new Vector2(__instance.Tile.X * 64f + 32f, __instance.Tile.Y * 64f + 32f),
                        -1
                    );
                if (tool.hasEnchantmentOfType<PalaeontologistEnchantment>())
                    PalaeontologistEnchantment.DropItems(__instance.Tile);
            }
        }
        catch (Exception err)
        {
            ModEntry.Log($"Error in Grass_TryDropItemsOnCut_Prefix:\n{err}", LogLevel.Error);
        }
    }

    private static void Tree_performToolAction_Prefix(Tree __instance, Tool t, int explosion, Vector2 tileLocation)
    {
        try
        {
            if (
                t != null
                && t.hasEnchantmentOfType<GathererEnchantment>()
                && __instance.growthStage.Value >= 5
                && __instance.hasMoss.Value
            )
            {
                Item moss = ItemRegistry.Create("(O)Moss", 1);
                Game1.createItemDebris(
                    moss,
                    new Vector2(tileLocation.X, tileLocation.Y - 1f) * 64f,
                    -1,
                    __instance.Location,
                    Game1.player.StandingPixel.Y - 32
                );
            }
        }
        catch (Exception err)
        {
            ModEntry.Log($"Error in Tree_performToolAction_Prefix:\n{err}", LogLevel.Error);
        }
    }

    private static void Crop_harvest_Prefix(Crop __instance, ref int __state)
    {
        // save dayOfCurrentPhase to __state, for detecting regrow crops
        __state = __instance.dayOfCurrentPhase.Value;
    }

    private static void Crop_harvest_Postfix(
        Crop __instance,
        bool __result,
        int __state,
        int xTile,
        int yTile,
        HoeDirt soil,
        JunimoHarvester junimoHarvester,
        bool isForcedScytheHarvest
    )
    {
        try
        {
            if (
                (__result || __state != __instance.dayOfCurrentPhase.Value)
                && isForcedScytheHarvest
                && junimoHarvester == null
                && (Game1.player.CurrentTool?.hasEnchantmentOfType<HorticulturistEnchantment>() ?? false)
            )
            {
                string harvestIndex = __instance.indexOfHarvest.Value;
                // special case sunflower seeds
                if (harvestIndex == "431")
                    harvestIndex = "421";
                ParsedItemData obj = ItemRegistry.GetDataOrErrorItem(harvestIndex);
                if (!obj.IsErrorItem && obj.Category == StardewObject.flowersCategory)
                {
                    Item harvestedItem = __instance.programColored.Value
                        ? new ColoredObject(harvestIndex, 1, __instance.tintColor.Value)
                        : ItemRegistry.Create(harvestIndex);
                    Game1.createItemDebris(harvestedItem.getOne(), new Vector2(xTile * 64 + 32, yTile * 64 + 32), -1);
                }
            }
        }
        catch (Exception err)
        {
            ModEntry.Log($"Error in Crop_harvest_Postfix:\n{err}", LogLevel.Error);
        }
    }

    private static IEnumerable<CodeInstruction> GiantCrop_performToolAction_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        try
        {
            CodeMatcher matcher = new(instructions, generator);

            matcher.MatchEndForward(
                new CodeMatch[] { new(OpCodes.Ldarg_1), new(OpCodes.Isinst, typeof(Axe)), new(OpCodes.Brtrue_S) }
            );
            Label lbl = (Label)matcher.Operand;
            matcher
                .Advance(1)
                .Insert(
                    new CodeMatch[]
                    {
                        new(OpCodes.Ldarg_1),
                        new(OpCodes.Call, AccessTools.Method(typeof(GamePatches), nameof(HasCrescentEnchantment))),
                        new(OpCodes.Brtrue_S, lbl),
                    }
                );

            matcher.MatchStartForward(new CodeMatch[] { new(OpCodes.Ldstr, "axchop") });
            matcher.Operand = "clubswipe";

            // matcher.MatchStartForward(new CodeMatch[]{
            //     new(OpCodes.Ldstr, "stumpCrack")
            // });
            // matcher.Operand = "leafrustle";

            return matcher.Instructions();
        }
        catch (Exception err)
        {
            ModEntry.Log($"Error in GiantCrop_performToolAction_Transpiler:\n{err}", LogLevel.Error);
            return instructions;
        }
    }

    private static void MeleeWeapon_DoDamage_Prefix(
        MeleeWeapon __instance,
        GameLocation location,
        int x,
        int y,
        int facingDirection,
        int power,
        Farmer who
    )
    {
        try
        {
            if (!HasCrescentEnchantment(__instance))
                return;
            foreach (
                Vector2 item in CrescentEnchantment.GetCrescentAOE(
                    who.TilePoint,
                    facingDirection,
                    who.FarmerSprite.currentAnimationIndex
                )
            )
            {
                if (
                    location.terrainFeatures.TryGetValue(item, out var value)
                    && value.performToolAction(__instance, 0, item)
                )
                    location.terrainFeatures.Remove(item);
                if (location.objects.TryGetValue(item, out var value2) && value2.performToolAction(__instance))
                    location.objects.Remove(item);
                if (location.performToolAction(__instance, (int)item.X, (int)item.Y))
                    break;
            }
        }
        catch (Exception err)
        {
            ModEntry.Log($"Error in Crop_harvest_Postfix:\n{err}", LogLevel.Error);
        }
    }
}
