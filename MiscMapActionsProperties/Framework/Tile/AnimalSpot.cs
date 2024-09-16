using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace MiscMapActionsProperties.Framework.Tile
{
    internal static class AnimalSpot
    {
        internal readonly static string MapProp_BuildingEntryLocation = $"{ModEntry.ModId}_AnimalSpot";

        internal static void Patch(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(FarmAnimal), nameof(FarmAnimal.setRandomPosition)),
                    transpiler: new HarmonyMethod(typeof(AnimalSpot), nameof(FarmAnimal_setRandomPosition_transpiler))
                );
            }
            catch (Exception err)
            {
                ModEntry.Log($"Failed to patch CustomBuildingEntry:\n{err}", LogLevel.Error);
            }
        }

        private static IEnumerable<CodeInstruction> FarmAnimal_setRandomPosition_transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            try
            {
                CodeMatcher matcher = new(instructions, generator);

                matcher.MatchStartForward([
                    new(OpCodes.Ldc_I4_S, (sbyte)25),
                    new(OpCodes.Br_S),
                    new(OpCodes.Ldc_I4, 999)
                ]);
                matcher.Operand = 24;

                return matcher.Instructions();
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in FarmAnimal_setRandomPosition_transpiler:\n{err}", LogLevel.Error);
                return instructions;
            }
        }
    }
}