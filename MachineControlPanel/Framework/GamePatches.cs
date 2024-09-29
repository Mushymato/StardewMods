using HarmonyLib;
using StardewValley;
using StardewModdingAPI;
using System.Reflection.Emit;
using StardewValley.GameData.Machines;

namespace MachineControlPanel.Framework
{
    /// <summary>Indicate why a rule or input is skipped by this mod</summary>
    internal enum SkipReason
    {
        None = 0,
        Rule = 1,
        Input = 2
    }
    internal static class GamePatches
    {
        /// <summary>Hold skip reason, here is hoping single thread means its safe lol</summary>
        private static SkipReason skipped = SkipReason.None;
        internal static void Apply(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(MachineDataUtility), nameof(MachineDataUtility.CanApplyOutput)),
                    // prefix: new HarmonyMethod(typeof(GamePatches), nameof(MachineDataUtility_CanApplyOutput_Prefix)),
                    transpiler: new HarmonyMethod(typeof(GamePatches), nameof(MachineDataUtility_CanApplyOutput_Transpiler))
                );
                ModEntry.Log($"Applied MachineDataUtility.CanApplyOutput Transpiler", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(SObject), nameof(SObject.PlaceInMachine)),
                    transpiler: new HarmonyMethod(typeof(GamePatches), nameof(SObject_PlaceInMachine_Transpiler))
                );
                ModEntry.Log($"Applied MachineDataUtility.CanApplyOutput Transpiler", LogLevel.Trace);
            }
            catch (Exception err)
            {
                ModEntry.Log($"Failed to patch MachineControlPanel:\n{err}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Check whether a rule or input item should be skipped
        /// </summary>
        /// <param name="trigger2"></param>
        /// <param name="machine"></param>
        /// <param name="rule"></param>
        /// <param name="inputItem"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        private static bool ShouldSkipMachineInput(MachineOutputTriggerRule trigger2, SObject machine, MachineOutputRule rule, Item inputItem)
        {
            RuleIdent ident = new(rule.Id, trigger2.Id);
            if (!ModEntry.TryGetSavedEntry(machine.QualifiedItemId, out ModSaveDataEntry? msdEntry))
                return false;

            if (msdEntry.Rules.Contains(ident))
            {
                ModEntry.LogOnce($"{machine.QualifiedItemId} Rule {ident} disabled.");
                if (skipped != SkipReason.Input)
                    skipped = SkipReason.Rule;
                return true;
            }
            // maybe better to check once in the postfix rather than in the iteration, but eh
            if (inputItem != null && msdEntry.Inputs.Contains(inputItem.QualifiedItemId))
            {
                ModEntry.LogOnce($"{machine.QualifiedItemId} Input {inputItem.QualifiedItemId} disabled.");
                skipped = SkipReason.Input;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check should skip, for <see cref="MachineOutputTrigger.DayUpdate"/>
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="trigger2"></param>
        /// <param name="machine"></param>
        /// <param name="rule"></param>
        /// <param name="inputItem"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        private static bool ShouldSkipMachineInput_DayUpdate(MachineOutputTrigger trigger, MachineOutputTriggerRule trigger2, SObject machine, MachineOutputRule rule, Item inputItem)
        {
            if (trigger.HasFlag(MachineOutputTrigger.DayUpdate) || trigger.HasFlag(MachineOutputTrigger.MachinePutDown))
                return ShouldSkipMachineInput(trigger2, machine, rule, inputItem);
            return false;
        }

        /// <summary>Unset skipped reason</summary>
        private static void MachineDataUtility_CanApplyOutput_Prefix()
        {
            skipped = SkipReason.None;
        }

        /// <summary>
        /// Patch checks in CanApplyOutput
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        private static IEnumerable<CodeInstruction> MachineDataUtility_CanApplyOutput_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                // // track enumeration index
                // // starting at -1 out of laziness
                // LocalBuilder idx = generator.DeclareLocal(typeof(int));
                // matcher.Start().Insert([
                //     new(OpCodes.Ldc_I4, -1),
                //     new(OpCodes.Stloc, idx)
                // ]);

                CodeMatch ldlocAny = new(OpCodes.Ldloc_0);
                ldlocAny.opcodes.Add(OpCodes.Ldloc_1);
                ldlocAny.opcodes.Add(OpCodes.Ldloc_2);
                ldlocAny.opcodes.Add(OpCodes.Ldloc_3);
                ldlocAny.opcodes.Add(OpCodes.Ldloc);
                ldlocAny.opcodes.Add(OpCodes.Ldloc_S);

                // insert just before if (trigger2.RequiredCount > inputItem.Stack)
                matcher.Start()
                .MatchStartForward([
                    new(OpCodes.Brfalse_S),
                    ldlocAny,
                    new(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(MachineOutputTriggerRule), nameof(MachineOutputTriggerRule.RequiredCount))),
                    new(OpCodes.Ldarg_3),
                    new(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Item), nameof(Item.Stack))),
                ]);

                Label lbl = (Label)matcher.Operand; // label to end of loop
                matcher.Advance(1);
                CodeInstruction ldloc = new(matcher.Opcode, matcher.Operand);

                // Check for MachineOutputTrigger.ItemPlacedInMachine
                matcher.Advance(1).InsertAndAdvance([
                    // ldloc from match
                    new(OpCodes.Ldarg_0), // Object machine
                    new(OpCodes.Ldarg_1), // MachineOutputRule rule
                    new(OpCodes.Ldarg_3), // Item inputItem
                    // new(OpCodes.Ldloc, idx), // foreach idx
                    new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(GamePatches), nameof(ShouldSkipMachineInput))),
                    new(OpCodes.Brtrue, lbl),
                    ldloc, // MachineOutputTriggerRule trigger2
                ]);

                // Check for MachineOutputTrigger.DayUpdate
                matcher.MatchStartForward([
                    new(OpCodes.Ldarg_S, (byte)6),
                    new(ldloc.opcode, ldloc.operand),
                    new(OpCodes.Stind_Ref),
                    new(OpCodes.Ldarg_S, (byte)7),
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Stind_I1)
                ]);

                matcher
                .SetAndAdvance(OpCodes.Ldarg_2, null) // MachineOutputTrigger trigger
                .Insert([
                    ldloc.Clone(), // MachineOutputTriggerRule trigger2
                    new(OpCodes.Ldarg_0), // Object machine
                    new(OpCodes.Ldarg_1), // MachineOutputRule rule
                    new(OpCodes.Ldarg_3), // Item inputItem
                    // new(OpCodes.Ldloc, idx), // foreach idx
                    new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(GamePatches), nameof(ShouldSkipMachineInput_DayUpdate))),
                    new(OpCodes.Brtrue, lbl),
                    new(OpCodes.Ldarg_S, (byte)6)
                ]);

                // // increment idx
                // var moveNext = AccessTools.EnumeratorMoveNext(
                //     AccessTools.Method(
                //         typeof(List<MachineOutputTriggerRule>),
                //         nameof(List<MachineOutputTriggerRule>.GetEnumerator)
                //     )
                // );
                // matcher.MatchStartForward([
                //     new(OpCodes.Call, moveNext),
                // ]).Advance(-1);

                // CodeInstruction ldloca = new(matcher.Opcode, matcher.Operand);
                // matcher.SetAndAdvance(OpCodes.Ldloc, idx);
                // matcher.Insert([
                //     new(OpCodes.Ldc_I4, 1),
                //     new(OpCodes.Add),
                //     new(OpCodes.Stloc, idx),
                //     ldloca
                // ]);

                return matcher.Instructions();
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in MachineDataUtility_CanApplyOutput_Transpiler:\n{err}", LogLevel.Error);
                return instructions;
            }
        }

        /// <summary>
        /// Display HUD message for rejecting an input, if <see cref="skipped"/> is set
        /// </summary>
        /// <param name="inputItem"></param>
        /// <param name="who"></param>
        private static void ShowSkippedReasonMessage(Item inputItem, Farmer who)
        {
            switch (skipped)
            {
                case SkipReason.Rule:
                    Game1.showRedMessage(I18n.SkipReason_Rules());
                    who.ignoreItemConsumptionThisFrame = true;
                    break;
                case SkipReason.Input:
                    Game1.showRedMessage(I18n.SkipReason_Inputs(inputItem.DisplayName));
                    who.ignoreItemConsumptionThisFrame = true;
                    break;
            }
            skipped = SkipReason.None;
        }

        /// <summary>
        /// Patch PlaceInMachine to show HUD messages
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        private static IEnumerable<CodeInstruction> SObject_PlaceInMachine_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                matcher.Start()
                .MatchStartForward([
                    new(OpCodes.Stfld, AccessTools.Field(typeof(Farmer), nameof(Farmer.ignoreItemConsumptionThisFrame))),
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Ret),
                    new(OpCodes.Ldarg_3),
                ]);
                matcher.Advance(1);
                // if not reached by jump, go to ret
                matcher.InsertAndAdvance([new(OpCodes.Br, matcher.Labels.Last())]);
                matcher.Insert([
                    new(OpCodes.Ldarg_2),
                    new(OpCodes.Ldarg_S, (sbyte)4),
                    new(OpCodes.Call, AccessTools.Method(typeof(GamePatches), nameof(ShowSkippedReasonMessage))),
                ]);
                matcher.CreateLabel(out Label lbl);

                // change 2 prev false branches to jump to the new label

                matcher.
                MatchEndBackwards([
                    new(OpCodes.Call, AccessTools.Method(
                        typeof(GameStateQuery), nameof(GameStateQuery.CheckConditions),
                        [typeof(string), typeof(GameLocation), typeof(Farmer), typeof(Item), typeof(Random), typeof(HashSet<string>)]
                    )),
                    new(OpCodes.Brfalse_S)
                ]);
                matcher.Operand = lbl;

                matcher.
                MatchEndBackwards([
                    new(OpCodes.Ldfld, AccessTools.Field(typeof(MachineData), nameof(MachineData.InvalidItemMessage))),
                    new(OpCodes.Brfalse_S)
                ]);
                matcher.Operand = lbl;

                return matcher.Instructions();
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in MachineDataUtility_CanApplyOutput_Transpiler:\n{err}", LogLevel.Error);
                return instructions;
            }
        }
    }
}
