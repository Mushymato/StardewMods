using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using static StardewValley.Objects.Chest;

namespace MatoTweaks.Tweak;

internal static class ChestSize
{
    public const int CHEST_SIZE = 80;
    public const int BIG_CHEST_SIZE = 140;

    public static void Patch(Harmony patcher)
    {
        try
        {
            // make chests bigger
            patcher.Patch(
                original: AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.GetActualCapacity)),
                postfix: new HarmonyMethod(typeof(ChestSize), nameof(Chest_GetActualCapacity_Postfix))
            );
            // grant fridge some behaviors of chest, by actually the chest item
            patcher.Patch(
                original: AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.ShowMenu)),
                transpiler: new HarmonyMethod(typeof(ChestSize), nameof(Chest_ShowMenu_Transpiler))
            );
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to patch ChestSize:\n{err}", LogLevel.Error);
        }
    }

    private static void Chest_GetActualCapacity_Postfix(Chest __instance, ref int __result)
    {
        __result = __instance.SpecialChestType switch
        {
            SpecialChestTypes.BigChest => BIG_CHEST_SIZE,
            _ => CHEST_SIZE,
        };
    }

    private static IEnumerable<CodeInstruction> Chest_ShowMenu_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        try
        {
            CodeMatcher matcher = new(instructions, generator);

            // IL_01e7: ldarg.0
            // IL_01e8: ldfld class Netcode.NetBool StardewValley.Objects.Chest::fridge
            // IL_01ed: callvirt instance !0 class Netcode.NetFieldBase`2<bool, class Netcode.NetBool>::get_Value()
            // IL_01f2: brtrue.s IL_01f7
            // IL_01f4: ldarg.0
            // IL_01f5: br.s IL_01f8
            // IL_01f7: ldnull

            matcher
                .End()
                .MatchEndBackwards(
                    [
                        new(OpCodes.Ldarg_0),
                        new(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(Chest), nameof(Chest.fridge))),
                        new(OpCodes.Callvirt),
                        new(OpCodes.Brtrue_S),
                        new(OpCodes.Ldarg_0),
                        new(OpCodes.Br_S),
                        new(OpCodes.Ldnull),
                    ]
                )
                .SetOpcodeAndAdvance(OpCodes.Ldarg_0);

            return matcher.Instructions();
        }
        catch (Exception err)
        {
            ModEntry.Log($"Error in ShopMenu_receiveLeftClick_transpiler:\n{err}", LogLevel.Error);
            return instructions;
        }
    }
}
