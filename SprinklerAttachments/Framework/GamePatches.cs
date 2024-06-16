using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HarmonyLib;
using StardewValley;
using StardewObject = StardewValley.Object;
using StardewModdingAPI;
using StardewValley.Objects;
using System.Reflection.Emit;

namespace SprinklerAttachments.Framework
{

    internal static class GamePatches
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private static IMonitor mon;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        internal static void Apply(Harmony harmony, IMonitor monitor)
        {
            mon = monitor;
            try
            {
                harmony.Patch(
                    original: AccessTools.DeclaredMethod(typeof(StardewObject), nameof(StardewObject.performObjectDropInAction)),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(Object_performObjectDropInAction_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.DeclaredMethod(typeof(StardewObject), nameof(StardewObject.checkForAction)),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(Object_checkForAction_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.DeclaredMethod(typeof(StardewObject), nameof(StardewObject.updateWhenCurrentLocation)),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(Object_updateWhenCurrentLocation_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.DeclaredMethod(typeof(StardewObject), nameof(StardewObject.GetModifiedRadiusForSprinkler)),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(Object_GetModifiedRadiusForSprinkler_PostFix))
                );
                // harmony.Patch(
                //     original: AccessTools.Method(typeof(StardewObject), nameof(StardewObject.draw), new Type[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
                //     // postfix: new HarmonyMethod(typeof(GamePatches), nameof(Object_draw_Postfix)),
                //     transpiler: new HarmonyMethod(typeof(GamePatches), nameof(Object_draw_Transpiler))
                // );
                harmony.Patch(
                    original: AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.GetActualCapacity)),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(Chest_GetActualCapacity_Postfix))
                );

            }
            catch (Exception err)
            {
                mon.Log($"Failed to patch SprinklerAttachments:\n{err}", LogLevel.Error);
            }
        }

        private static void Object_performObjectDropInAction_Postfix(StardewObject __instance, Item dropInItem, bool probe, Farmer who, ref bool __result, bool returnFalseIfItemConsumed = false)
        {
            try
            {
                if (!__result && SprinklerAttachment.TryAttachToSprinkler(__instance, dropInItem, probe))
                    __result = true;
            }
            catch (Exception err)
            {
                mon.Log($"Error in Object_performObjectDropInAction_Postfix:\n{err}", LogLevel.Error);
            }
        }

        private static void Object_checkForAction_Postfix(StardewObject __instance, Farmer who, ref bool __result, bool justCheckingForActivity = false)
        {
            try
            {
                if (!__result && SprinklerAttachment.CheckForAction(__instance, who, justCheckingForActivity))
                    __result = true;
            }
            catch (Exception err)
            {
                mon.Log($"Error in Object_checkForAction_Postfix:\n{err}", LogLevel.Error);
            }
        }

        private static void Object_GetModifiedRadiusForSprinkler_PostFix(StardewObject __instance, ref int __result)
        {
            try
            {
                mon.Log($"Vanilla radius: {__result}");
                __result = SprinklerAttachment.GetModifiedRadiusForSprinkler(__instance, __result);
                mon.Log($"Mod radius: {__result}");
            }
            catch (Exception err)
            {
                mon.Log($"Error in Object_GetModifiedRadiusForSprinkler_PostFix:\n{err}", LogLevel.Error);
            }
        }

        private static void Object_updateWhenCurrentLocation_Postfix(StardewObject __instance, GameTime time)
        {
            try
            {
                SprinklerAttachment.UpdateWhenCurrentLocation(__instance, time);
            }
            catch (Exception err)
            {
                mon.Log($"Error in Object_updateWhenCurrentLocation_Postfix:\n{err}", LogLevel.Error);
            }
        }

        private static void Object_draw_Postfix(StardewObject __instance, SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
        {
            try
            {
                SprinklerAttachment.DrawAttachment(__instance, spriteBatch, x, y, alpha: alpha);
            }
            catch (Exception err)
            {
                mon.Log($"Error in Object_draw_Postfix:\n{err}", LogLevel.Error);
            }
        }

        private static IEnumerable<CodeInstruction> Object_draw_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool inSprinklerBlock = false;
            bool inHeldObjectBlock = false;
            int startIdx = -1;
            int endIdx = -1;
            List<CodeInstruction> codes = new(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && ((MethodInfo)codes[i].operand).Name == "IsSprinkler")
                {
                    mon.Log($"{codes[i].opcode}: {codes[i].operand} lbl:{codes[i].labels.Count}", LogLevel.Info);
                    mon.Log($"Method: {((MethodInfo)codes[i].operand).Name}", LogLevel.Info);
                    inSprinklerBlock = true;
                }

                if (inSprinklerBlock && codes[i].opcode == OpCodes.Ldfld && ((FieldInfo)codes[i].operand).Name == "heldObject")
                {
                    mon.Log($"{codes[i].opcode}: {codes[i].operand} lbl:{codes[i].labels.Count}", LogLevel.Info);
                    mon.Log($"Field: {((FieldInfo)codes[i].operand).Name} (type:{((FieldInfo)codes[i].operand).MemberType})", LogLevel.Info);
                    inHeldObjectBlock = true;
                }

                if (inSprinklerBlock && inHeldObjectBlock)
                {
                    if (codes[i].opcode == OpCodes.Brfalse_S)
                    {
                        mon.Log($"{codes[i].opcode}: {codes[i].operand} lbl:{codes[i].labels.Count}", LogLevel.Info);
                        mon.Log($"{((Label)codes[i].operand)}", LogLevel.Info);

                        break;
                    }
                }
            }

            return instructions;
        }

        private static void Chest_GetActualCapacity_Postfix(Chest __instance, ref int __result)
        {
            try
            {
                __result = SprinklerAttachment.GetActualCapacity(__instance, __result);
            }
            catch (Exception err)
            {
                mon.Log($"Error in Chest_GetActualCapacity_Postfix:\n{err}", LogLevel.Error);
            }
        }
    }
}
