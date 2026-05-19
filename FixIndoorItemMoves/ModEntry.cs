using System.Diagnostics;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Buildings;

namespace FixIndoorItemMoves;

public sealed class ModEntry : Mod
{
#if DEBUG
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif

    public const string ModId = "mushymato.FixIndoorItemMoves";
    private static IMonitor mon = null!;
    internal static IModHelper help = null!;

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;
        help = helper;

        Harmony harmony = new(ModId);
        harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Building), nameof(Building.FinishConstruction)),
            transpiler: new HarmonyMethod(typeof(ModEntry), nameof(Building_FinishConstruction_Transpiler))
        );
    }

    private static IEnumerable<CodeInstruction> Building_FinishConstruction_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        try
        {
            CodeMatcher matcher = new(instructions, generator);

            // IL_00af: ldarg.0
            // IL_00b0: ldc.i4.1
            // IL_00b1: ldc.i4.0
            // IL_00b2: callvirt instance class [StardewValley.GameData]StardewValley.GameData.Buildings.BuildingData StardewValley.Buildings.Building::ReloadBuildingData(bool, bool)
            matcher.MatchStartForward([
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Ldc_I4_0),
                new(
                    OpCodes.Callvirt,
                    AccessTools.DeclaredMethod(typeof(Building), nameof(Building.ReloadBuildingData))
                ),
                new(OpCodes.Pop),
            ]);
            matcher.RemoveInstructions(5);

            return matcher.Instructions();
        }
        catch (Exception err)
        {
            Log($"Error in Toolbar_draw_Transpiler:\n{err}", LogLevel.Error);
            return instructions;
        }
    }

    /// <summary>SMAPI static monitor Log wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void Log(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon.Log(msg, level);
    }

    /// <summary>SMAPI static monitor LogOnce wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void LogOnce(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon.LogOnce(msg, level);
    }

    /// <summary>SMAPI static monitor Log wrapper, debug only</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    [Conditional("DEBUG")]
    internal static void LogDebug(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon.Log(msg, level);
    }
}
