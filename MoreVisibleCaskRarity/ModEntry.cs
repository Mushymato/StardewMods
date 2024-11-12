using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;


namespace MoreVisibleCaskRarity;

public class ModConfig
{
    public int OffsetX { get; set; } = 0;
    public int OffsetY { get; set; } = -50;
}
public class ModEntry : Mod
{
    private static IMonitor? mon;
    private static ModConfig? config;

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;
        config = Helper.ReadConfig<ModConfig>();
        Helper.WriteConfig(config);
        Patch();
    }

    public static void Log(string msg, LogLevel level = LogLevel.Debug)
    {
        mon!.Log(msg, level);
    }

    private void Patch()
    {
        Harmony harmony = new(ModManifest.UniqueID);
        try
        {
            harmony.Patch(
                original: AccessTools.DeclaredMethod(typeof(Cask), nameof(Cask.draw)),
                transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ModEntry), nameof(Cask_draw_Transpiler)))
            );
        }
        catch (Exception err)
        {
            Log($"Failed to patch MoreVisibleCaskRarity:\n{err}", LogLevel.Error);
        }
    }

    private static IEnumerable<CodeInstruction> Cask_draw_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        try
        {
            CodeMatcher matcher = new(instructions, generator);

            // Find the arguments to Vector2 position = new Vector2(X, Y) and add our modded offset
            matcher.Start()
            .MatchEndForward(new CodeMatch[]{
                    new(OpCodes.Ldsfld, AccessTools.Field(typeof(Game1), nameof(Game1.viewport))),
                    new(OpCodes.Ldarg_2),
                    new(OpCodes.Ldc_I4_S, (byte)64),
                    new(OpCodes.Mul),
                    new(OpCodes.Conv_R4)
            })
            .InsertAndAdvance(new CodeInstruction[]{
                    // Just gonna use a constant since no GMCM support anyways
                    // new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ModEntry), nameof(GetOffsetX))),
                    new(OpCodes.Ldc_I4, config!.OffsetX),
                    new(OpCodes.Add)
            })
            .MatchEndForward(new CodeMatch[]{
                    new(OpCodes.Ldarg_3),
                    new(OpCodes.Ldc_I4_S, (byte)64),
                    new(OpCodes.Mul),
                    new(OpCodes.Ldc_I4_S, (byte)64),
                    new(OpCodes.Sub),
                    new(OpCodes.Conv_R4)
            })
            .InsertAndAdvance(new CodeInstruction[]{
                    // Ditto
                    // new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ModEntry), nameof(GetOffsetY))),
                    new(OpCodes.Ldc_I4, config!.OffsetY),
                    new(OpCodes.Add)
            })
            ;

            return matcher.Instructions();
        }
        catch (Exception err)
        {
            Log($"Error in Cask_draw_Transpiler:\n{err}", LogLevel.Error);
            return instructions;
        }
    }
}

