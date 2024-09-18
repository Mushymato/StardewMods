using HarmonyLib;
using StardewValley;
using StardewModdingAPI;
using System.Reflection.Emit;
using StardewValley.GameData.Machines;

namespace MachineControlPanel.Framework
{
    internal enum SkipReason
    {
        None = 0,
        Rule = 1,
        Input = 2
    }
    internal static class GamePatches
    {
        private static SkipReason skipped = SkipReason.None;
        internal static void Apply(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(MachineDataUtility), nameof(MachineDataUtility.CanApplyOutput)),
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

        private static bool ShouldSkipMachineInput(SObject machine, MachineOutputRule rule, MachineOutputTriggerRule trigger2, int idx, Item inputItem)
        {
            RuleIdent ident = new(rule.Id, trigger2.Id, idx);
            if (!trigger2.Trigger.HasFlag(MachineOutputTrigger.ItemPlacedInMachine))
                return false;
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

        private static IEnumerable<CodeInstruction> MachineDataUtility_CanApplyOutput_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                // track enumeration index
                // starting at -1 out of laziness
                LocalBuilder idx = generator.DeclareLocal(typeof(int));
                matcher.Start().Insert([
                    new(OpCodes.Ldc_I4, -1),
                    new(OpCodes.Stloc, idx)
                ]);

                CodeMatch ldlocAny = new(OpCodes.Ldloc_0);
                ldlocAny.opcodes.Add(OpCodes.Ldloc_1);
                ldlocAny.opcodes.Add(OpCodes.Ldloc_2);
                ldlocAny.opcodes.Add(OpCodes.Ldloc_3);
                ldlocAny.opcodes.Add(OpCodes.Ldloc);
                ldlocAny.opcodes.Add(OpCodes.Ldloc_S);

                // compiler is pretty free spirited about continue, may explode in later builds
                // patching early to not deal with jumps as much
                // foreach (MachineOutputTriggerRule trigger2 in rule.Triggers)
                // if (!trigger2.Trigger.HasFlag(trigger) [PATCH HERE] || (trigger2.Condition != null ...
                matcher.Start()
                .MatchStartForward([
                    new(OpCodes.Brfalse),
                    ldlocAny,
                    new(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(MachineOutputTriggerRule), nameof(MachineOutputTriggerRule.Condition)))
                ]);
                Label lbl = (Label)matcher.Operand;
                matcher.Advance(1);
                CodeInstruction ldloc = new(matcher.Opcode, matcher.Operand);

                matcher.Insert([
                    new(OpCodes.Ldarg_0), // Object machine
                    new(OpCodes.Ldarg_1), // MachineOutputRule rule
                    ldloc, // MachineOutputTriggerRule trigger2
                    new(OpCodes.Ldloc, idx), // foreach idx
                    new(OpCodes.Ldarg_3), // Item inputItem
                    new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(GamePatches), nameof(ShouldSkipMachineInput))),
                    new(OpCodes.Brtrue, lbl)
                ]);

                // IL_00f0: ldloca.s 0
                // IL_00f2: call instance bool valuetype [System.Collections]System.Collections.Generic.List`1/Enumerator<class [StardewValley.GameData]StardewValley.GameData.Machines.MachineOutputTriggerRule>::MoveNext()
                // IL_00f7: brtrue IL_0023
                var moveNext = AccessTools.EnumeratorMoveNext(
                    AccessTools.Method(
                        typeof(List<MachineOutputTriggerRule>),
                        nameof(List<MachineOutputTriggerRule>.GetEnumerator)
                    )
                );
                matcher.MatchStartForward([
                    new(OpCodes.Call, moveNext),
                ]).Advance(-1);

                CodeInstruction ldloca = new(matcher.Opcode, matcher.Operand);
                matcher.SetAndAdvance(OpCodes.Ldloc, idx);
                matcher.Insert([
                    new(OpCodes.Ldc_I4, 1),
                    new(OpCodes.Add),
                    new(OpCodes.Stloc, idx),
                    ldloca
                ]);


                return matcher.Instructions();
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in MachineDataUtility_CanApplyOutput_Transpiler:\n{err}", LogLevel.Error);
                return instructions;
            }
        }

        private static void ShowSkippedReasonMessage(Item inputItem, Farmer who)
        {
            switch (skipped)
            {
                case SkipReason.Rule:
                    Game1.showRedMessage(I18n.SkipReason_Rule());
                    who.ignoreItemConsumptionThisFrame = true;
                    break;
                case SkipReason.Input:
                    Game1.showRedMessage(I18n.SkipReason_Inputs(inputItem.DisplayName));
                    who.ignoreItemConsumptionThisFrame = true;
                    break;
            }
            skipped = SkipReason.None;
        }

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

                ModEntry.Log($"====matcher at {matcher.Pos}====");
                for (int i = -9; i < 9; i++)
                {
                    ModEntry.Log($"inst {i}: {matcher.InstructionAt(i)}");
                }

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
