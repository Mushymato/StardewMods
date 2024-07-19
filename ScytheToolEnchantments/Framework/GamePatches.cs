using System.Text;
using System.Reflection.Emit;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using StardewValley.Enchantments;
using StardewValley.Monsters;
using StardewValley.Characters;
using ScytheToolEnchantments.Framework.Enchantments;
using StardewValley.ItemTypeDefinitions;
using StardewObject = StardewValley.Object;


namespace ScytheToolEnchantments.Framework
{
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
                );
                // tooltip draw
                harmony.Patch(
                    original: AccessTools.Method(typeof(MeleeWeapon), nameof(MeleeWeapon.drawTooltip)),
                    transpiler: new HarmonyMethod(typeof(GamePatches), nameof(MeleeWeapon_drawTooltip_Transpiler))
                );
                harmony.Patch(
                    original: AccessTools.Method(typeof(MeleeWeapon), nameof(MeleeWeapon.getExtraSpaceNeededForTooltipSpecialIcons)),
                    transpiler: new HarmonyMethod(typeof(GamePatches), nameof(MeleeWeapon_getExtraSpaceNeededForTooltipSpecialIcons_Transpiler))
                );
                // these two could be 1 GetAvailableEnchantments transpiler, but weh IL ctor
                harmony.Patch(
                    original: AccessTools.Method(typeof(BaseEnchantment), nameof(BaseEnchantment.GetAvailableEnchantments)),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(BaseEnchantment_GetAvailableEnchantments_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.Method(typeof(BaseEnchantment), nameof(BaseEnchantment.ResetEnchantments)),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(BaseEnchantment_ResetEnchantments_Postfix))
                );
                // enchant effects
                harmony.Patch(
                    original: AccessTools.Method(typeof(Tree), nameof(Tree.performToolAction)),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(Tree_performToolAction_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.Method(typeof(Crop), nameof(Crop.harvest)),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(Crop_harvest_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.Method(typeof(Grass), nameof(Grass.TryDropItemsOnCut)),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(Grass_TryDropItemsOnCut_Postfix))
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

        private static IEnumerable<CodeInstruction> MeleeWeapon_drawTooltip_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);
                matcher.MatchStartForward(new CodeMatch[]{
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Callvirt, AccessTools.Method(typeof(Tool), nameof(Tool.isScythe))),
                    new(OpCodes.Brtrue)
                })
                // .SetOpcodeAndAdvance(OpCodes.Nop)
                // .SetOpcodeAndAdvance(OpCodes.Nop)
                // .SetOpcodeAndAdvance(OpCodes.Nop)
                .Advance(1)
                .SetInstruction(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GamePatches), nameof(IsScytheAndNotIridium))))
                ;

                return matcher.Instructions();
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in MeleeWeapon_drawTooltip_Transpiler:\n{err}", LogLevel.Error);
                return instructions;
            }
        }

        private static IEnumerable<CodeInstruction> MeleeWeapon_getExtraSpaceNeededForTooltipSpecialIcons_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                // vanilla calc for tooltip size is longer than it should be for some reason
                CodeMatcher matcher = new(instructions, generator);
                // IL_0091: ldarg.0
                // IL_0092: callvirt instance bool StardewValley.Tool::isScythe()
                // IL_0097: brtrue.s IL_00a6
                matcher.MatchStartForward(new CodeMatch[]{
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Callvirt, AccessTools.Method(typeof(Tool), nameof(Tool.isScythe))),
                    new(OpCodes.Brtrue_S)
                })
                // .SetOpcodeAndAdvance(OpCodes.Nop)
                // .SetOpcodeAndAdvance(OpCodes.Nop)
                // .SetOpcodeAndAdvance(OpCodes.Nop)
                .Advance(1)
                .SetInstruction(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GamePatches), nameof(IsScytheAndNotIridium))))
                ;

                return matcher.Instructions();
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in MeleeWeapon_getExtraSpaceNeededForTooltipSpecialIcons_Transpiler:\n{err}", LogLevel.Error);
                return instructions;
            }
        }

        private static IEnumerable<CodeInstruction> MeleeWeapon_Forge_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                // should technically check for iridium scythe here, but other scythes don't have enchantments to apply anyways
                CodeMatcher matcher = new(instructions, generator);
                matcher.MatchStartForward(new CodeMatch[]{
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Callvirt, AccessTools.Method(typeof(Tool), nameof(Tool.isScythe))),
                    new(OpCodes.Brfalse_S)
                })
                .SetOpcodeAndAdvance(OpCodes.Nop)
                .SetOpcodeAndAdvance(OpCodes.Nop)
                .SetOpcodeAndAdvance(OpCodes.Br_S)
                ;

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
                __result.Add(new PaleologistEnchantment());
                __result.Add(new GathererEnchantment());
                __result.Add(new HorticulturistEnchantment());
                __result.Add(new ReaperEnchantment());
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
                        Game1.createItemDebris(ItemRegistry.Create("(O)178"), new Vector2(__instance.Tile.X * 64f + 32f, __instance.Tile.Y * 64f + 32f), -1);
                    if (tool.hasEnchantmentOfType<PaleologistEnchantment>())
                        PaleologistEnchantment.DropItems(__instance.Tile);
                }
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in Grass_TryDropItemsOnCut_Prefix:\n{err}", LogLevel.Error);
            }
        }

        private static void Tree_performToolAction_Postfix(Tree __instance, Tool t, int explosion, Vector2 tileLocation)
        {
            try
            {
                if (t.hasEnchantmentOfType<GathererEnchantment>() && __instance.growthStage.Value >= 5 && __instance.hasMoss.Value && Game1.random.NextBool())
                {
                    Item moss = ItemRegistry.Create("(O)Moss", 1);
                    Game1.createItemDebris(moss, new Vector2(tileLocation.X, tileLocation.Y - 1f) * 64f, -1, __instance.Location, Game1.player.StandingPixel.Y - 32);
                }
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in Tree_performToolAction_Prefix:\n{err}", LogLevel.Error);
            }
        }

        private static void Crop_harvest_Postfix(Crop __instance, bool __result, int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester, bool isForcedScytheHarvest)
        {
            try
            {

                if (__result && junimoHarvester == null && Game1.player.CurrentTool.hasEnchantmentOfType<HorticulturistEnchantment>())
                {
                    ParsedItemData obj = ItemRegistry.GetDataOrErrorItem(__instance.indexOfHarvest.Value);
                    if (obj.Category == StardewObject.flowersCategory)
                    {
                        Game1.createItemDebris(ItemRegistry.Create(__instance.indexOfHarvest.Value, 1), new Vector2(xTile * 64 + 32, yTile * 64 + 32), -1);
                    }
                }
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in Tree_performToolAction_Prefix:\n{err}", LogLevel.Error);
            }
        }
    }
}
