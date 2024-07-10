using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.SpecialOrders.Objectives;

namespace SpecialOrderNotifications.Framework
{
    public static class GamePatches
    {
        public static void Patch(string modId)
        {
            Harmony harmony = new(modId);
            try
            {
                // can use same transpiler for these two as they have same code
                harmony.Patch(
                    original: AccessTools.Method(typeof(CollectObjective), nameof(CollectObjective.OnItemShipped)),
                    transpiler: new HarmonyMethod(typeof(GamePatches), nameof(Objective_OnIncrementCount_Transpiler))
                );
                harmony.Patch(
                    original: AccessTools.Method(typeof(FishObjective), nameof(FishObjective.OnFishCaught)),
                    transpiler: new HarmonyMethod(typeof(GamePatches), nameof(Objective_OnIncrementCount_Transpiler))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(GiftObjective), nameof(GiftObjective.OnGiftGiven)),
                    transpiler: new HarmonyMethod(typeof(GamePatches), nameof(GiftObjective_OnGiftGiven_Transpiler))
                );
                harmony.Patch(
                    original: AccessTools.Method(typeof(SlayObjective), nameof(SlayObjective.OnMonsterSlain)),
                    transpiler: new HarmonyMethod(typeof(GamePatches), nameof(SlayObjective_OnMonsterSlain_Transpiler))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(JKScoreObjective), nameof(JKScoreObjective.OnNewValue)),
                    prefix: new HarmonyMethod(typeof(GamePatches), nameof(JKScoreObjective_OnNewValue_Prefix)),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(JKScoreObjective_OnNewValue_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.Method(typeof(ReachMineFloorObjective), nameof(ReachMineFloorObjective.OnNewValue)),
                    prefix: new HarmonyMethod(typeof(GamePatches), nameof(ReachMineFloorObjective_OnNewValue_Prefix)),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(ReachMineFloorObjective_OnNewValue_Postfix))
                );
            }
            catch (Exception err)
            {
                ModEntry.Log($"Failed to patch SpecialOrderNotifications(OrderObjectives):\n{err}", LogLevel.Error);
            }

            try
            {
                // can use same transpiler for these two as they have same code
                harmony.Patch(
                    original: AccessTools.DeclaredMethod(typeof(DayTimeMoneyBox), nameof(DayTimeMoneyBox.draw)),
                    transpiler: new HarmonyMethod(typeof(GamePatches), nameof(DayTimeMoneyBox_draw_Transpiler))
                );
            }
            catch (Exception err)
            {
                ModEntry.Log($"Failed to patch SpecialOrderNotifications(DayTimeMoneyBox):\n{err}", LogLevel.Error);
            }
        }

        private static IEnumerable<CodeInstruction> Objective_OnIncrementCount_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                matcher.Start()
                .MatchStartForward(new CodeMatch[]{
                    new(OpCodes.Brtrue_S),
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldarg_2),
                    new(OpCodes.Callvirt, AccessTools.DeclaredPropertyGetter(typeof(Item), nameof(Item.Stack))),
                    new(OpCodes.Callvirt, AccessTools.Method(typeof(OrderObjective), nameof(OrderObjective.IncrementCount))),
                    new(OpCodes.Leave_S)
                })
                .InsertAndAdvance(new CodeInstruction[]{
                    new(OpCodes.Ldarg_0), new(OpCodes.Callvirt, AccessTools.Method(typeof(OrderObjective), nameof(OrderObjective.IsComplete))),
                    new(OpCodes.Or)
                })
                .Advance(5)
                .Insert(new CodeInstruction[]{
                    new(OpCodes.Ldarg_2),
                    new(OpCodes.Ldarg_0), new(OpCodes.Callvirt, AccessTools.Method(typeof(OrderObjective), nameof(OrderObjective.GetCount))),
                    new(OpCodes.Ldarg_0), new(OpCodes.Callvirt, AccessTools.Method(typeof(OrderObjective), nameof(OrderObjective.GetMaxCount))),
                    new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(QuestPingHelper), nameof(QuestPingHelper.PingItem)))
                });

                return matcher.Instructions();
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in Objective_OnIncrementCount_Transpiler:\n{err}", LogLevel.Error);
                return instructions;
            }
        }

        private static IEnumerable<CodeInstruction> SlayObjective_OnMonsterSlain_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                matcher.Start()
                .MatchEndForward(new CodeMatch[]{
                    new(OpCodes.Brfalse_S),
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldc_I4_1),
                    new(OpCodes.Callvirt, AccessTools.Method(typeof(OrderObjective), nameof(OrderObjective.IncrementCount))),
                    // new(OpCodes.Leave_S)
                })
                .InsertAndAdvance(new CodeInstruction[]{
                    new(OpCodes.Ldarg_2),
                })
                .SetInstruction(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GamePatches), nameof(SlayObjectiveIncrementCount))));
                return matcher.Instructions();
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in SlayObjective_OnMonsterSlain_Transpiler:\n{err}", LogLevel.Error);
                return instructions;
            }
        }

        private static void SlayObjectiveIncrementCount(SlayObjective objective, int count, Monster monster)
        {
            if (objective.IsComplete())
                return;
            objective.IncrementCount(count);
            QuestPingHelper.PingMonster(monster, objective.GetCount(), objective.GetMaxCount());
        }

        private static IEnumerable<CodeInstruction> GiftObjective_OnGiftGiven_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                matcher.Start()
                .MatchEndForward(new CodeMatch[]{
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldc_I4_1),
                    new(OpCodes.Callvirt, AccessTools.Method(typeof(OrderObjective), nameof(OrderObjective.IncrementCount))),
                    new(OpCodes.Ret)
                })
                .CreateLabel(out Label lbl)
                .Insert(new CodeInstruction[]{
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Callvirt, AccessTools.Method(typeof(OrderObjective), nameof(OrderObjective.GetCount))),
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Callvirt, AccessTools.Method(typeof(OrderObjective), nameof(OrderObjective.GetMaxCount))),
                    new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(QuestPingHelper), nameof(QuestPingHelper.PingGift)))
                })
                .Advance(-2)
                .InsertAndAdvance(new CodeInstruction[]{
                    // steal the existing Ldarg_0
                    new(OpCodes.Callvirt, AccessTools.Method(typeof(OrderObjective), nameof(OrderObjective.IsComplete))),
                    new(OpCodes.Brtrue_S, lbl),
                    // return the Ldarg_0
                    new(OpCodes.Ldarg_0)
                });

                return matcher.Instructions();
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in GiftObjective_OnGiftGiven_Transpiler:\n{err}", LogLevel.Error);
                return instructions;
            }
        }

        private static bool JKScoreObjective_OnNewValue_Prefix(JKScoreObjective __instance, Farmer who, int new_value)
        {
            try
            {
                return !__instance.IsComplete();
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in JKScoreObjective_OnNewValue_Prefix:\n{err}", LogLevel.Error);
                return true;
            }
        }

        private static void JKScoreObjective_OnNewValue_Postfix(JKScoreObjective __instance, Farmer who, int new_value)
        {
            try
            {
                if (new_value == __instance.GetCount())
                    QuestPingHelper.PingJunimoKart(new_value, __instance.GetMaxCount());
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in JKScoreObjective_OnNewValue_Postfix:\n{err}", LogLevel.Error);
            }
        }

        private static bool ReachMineFloorObjective_OnNewValue_Prefix(JKScoreObjective __instance, Farmer who, int new_value)
        {
            try
            {
                return !__instance.IsComplete();
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in ReachMineFloorObjective_OnNewValue_Prefix:\n{err}", LogLevel.Error);
                return true;
            }
        }

        private static void ReachMineFloorObjective_OnNewValue_Postfix(ReachMineFloorObjective __instance, Farmer who, int new_value)
        {
            try
            {
                if (new_value == __instance.GetCount())
                    QuestPingHelper.PingMineLadder(new_value, __instance.GetMaxCount());
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in ReachMineFloorObjective_OnNewValue_Postfix:\n{err}", LogLevel.Error);
            }
        }

        private static IEnumerable<CodeInstruction> DayTimeMoneyBox_draw_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                matcher
                .Start()
                .MatchEndForward(new CodeMatch[]{
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, AccessTools.Field(typeof(DayTimeMoneyBox), "questNotificationTimer")),
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Ble)
                });
                Label lbl = (Label)matcher.Instruction.operand;

                matcher
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction[]{
                    new(OpCodes.Ldarg_1),
                    new(OpCodes.Ldarg_0), new(OpCodes.Ldfld, AccessTools.Field(typeof(DayTimeMoneyBox), nameof(DayTimeMoneyBox.position))),
                    new(OpCodes.Ldarg_0), new(OpCodes.Ldfld, AccessTools.Field(typeof(DayTimeMoneyBox), "questPingTexture")),
                    new(OpCodes.Ldarg_0), new(OpCodes.Ldfld, AccessTools.Field(typeof(DayTimeMoneyBox), "questPingSourceRect")),
                    new(OpCodes.Ldarg_0), new(OpCodes.Ldfld, AccessTools.Field(typeof(DayTimeMoneyBox), "questPingString")),
                    new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(QuestPingHelper), nameof(QuestPingHelper.DrawQuestPingBox))),
                    new(OpCodes.Br, lbl)
                });

                return matcher.Instructions();
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in CollectObjective_OnItemShipped_Transpiler:\n{err}", LogLevel.Error);
                return instructions;
            }
        }
    }
}