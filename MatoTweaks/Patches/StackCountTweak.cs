using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Menus;

namespace MatoTweaks.Patches
{
    internal static class StackCountTweak
    {
        public static void Patch(Harmony patcher)
        {
            // patcher.Patch(
            //     original: AccessTools.Method(typeof(Item), nameof(Item.CompareTo)),
            //     prefix: new HarmonyMethod(AccessTools.Method(typeof(AtravitaItemSortTweak), nameof(Item_CompareTo_Prefix)))
            // );
            try
            {
                patcher.Patch(
                    original: AccessTools.DeclaredMethod(typeof(ShopMenu), nameof(ShopMenu.receiveLeftClick)),
                    transpiler: new HarmonyMethod(typeof(StackCountTweak), nameof(ShopMenu_replaceStackCounts_transpiler))
                );
                patcher.Patch(
                    original: AccessTools.DeclaredMethod(typeof(ShopMenu), nameof(ShopMenu.receiveRightClick)),
                    transpiler: new HarmonyMethod(typeof(StackCountTweak), nameof(ShopMenu_replaceStackCounts_transpiler))
                );
            }
            catch (Exception err)
            {
                ModEntry.Log($"Failed to patch StackCountTweak:\n{err}", LogLevel.Error);
            }
        }

        private static IEnumerable<CodeInstruction> ShopMenu_replaceStackCounts_transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                matcher.MatchStartForward(new CodeMatch[]{
                    new(OpCodes.Ldc_I4_S, (sbyte)25),
                    new(OpCodes.Br_S),
                    new(OpCodes.Ldc_I4, 999)
                });
                matcher.Operand = 24;

                return matcher.Instructions();
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in ShopMenu_receiveLeftClick_transpiler:\n{err}", LogLevel.Error);
                return instructions;
            }
        }
    }
}