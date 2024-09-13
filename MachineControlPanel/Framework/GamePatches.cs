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

        private static bool CheckRuleDisabled(SObject machine, MachineOutputRule rule, MachineOutputTriggerRule trigger2, Item inputItem)
        {
            RuleIdent ident = new(machine.QualifiedItemId, rule.Id, trigger2.Id);
            if (!trigger2.Trigger.HasFlag(MachineOutputTrigger.ItemPlacedInMachine))
                return false;
            if (ModEntry.SaveData.Disabled.Contains(ident))
            {
                ModEntry.Log($"Rule {ident} disabled.", LogLevel.Debug);
                return true;
            }
            return false;
        }

        private static IEnumerable<CodeInstruction> MachineDataUtility_CanApplyOutput_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);


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
                CodeInstruction ldloc = matcher.Instruction;

                matcher.Insert([
                    new(OpCodes.Ldarg_0), // Object machine
                    new(OpCodes.Ldarg_1), // MachineOutputRule rule
                    new(ldloc.opcode, ldloc.operand), // MachineOutputTriggerRule trigger2
                    new(OpCodes.Ldarg_3), // Item inputItem
                    new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(GamePatches), nameof(CheckRuleDisabled))),
                    new(OpCodes.Brtrue_S, lbl)
                ]);

                ModEntry.Log($"====matcher at {matcher.Pos}====");
                for (int i = -10; i < 10; i++)
                {
                    ModEntry.Log($"inst {i}: {matcher.InstructionAt(i)}");
                }

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
