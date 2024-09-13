using HarmonyLib;
using StardewValley;
using StardewModdingAPI;
using System.Reflection.Emit;
using StardewValley.GameData.Machines;
using SObject = StardewValley.Object;

namespace MachineControlPanel.Framework
{

    internal static class GamePatches
    {
        internal static void Apply(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(MachineDataUtility), nameof(MachineDataUtility.CanApplyOutput)),
                    transpiler: new HarmonyMethod(typeof(GamePatches), nameof(MachineDataUtility_CanApplyOutput_Transpiler))
                );
                ModEntry.Log($"Applied MachineDataUtility.CanApplyOutput Transpiler", LogLevel.Trace);
            }
            catch (Exception err)
            {
                ModEntry.Log($"Failed to patch MachineControlPanel:\n{err}", LogLevel.Error);
            }
        }

        private static bool CheckRuleDisabled(SObject machine, MachineOutputRule rule, MachineOutputTriggerRule trigger2, int idx, Item inputItem)
        {
            RuleIdent ident = new(machine.QualifiedItemId, rule.Id, trigger2.Id, idx);
            if (!trigger2.Trigger.HasFlag(MachineOutputTrigger.ItemPlacedInMachine))
                return false;
            if (ModEntry.SaveData.Disabled.Contains(ident))
            {
                ModEntry.LogOnce($"Rule {ident} disabled.", LogLevel.Trace);
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
                    new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(GamePatches), nameof(CheckRuleDisabled))),
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
    }
}
