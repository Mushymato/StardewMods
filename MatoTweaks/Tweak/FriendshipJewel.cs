using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewValley.Menus;

namespace MatoTweaks.Tweak;

internal static class FriendshipJewel
{
    public static void Patch(Harmony patcher)
    {
        try
        {
            patcher.Patch(
                original: AccessTools.DeclaredMethod(typeof(DialogueBox), nameof(DialogueBox.drawPortrait)),
                transpiler: new HarmonyMethod(typeof(FriendshipJewel), nameof(DialogueBox_drawPortrait_Transpiler))
            );
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to patch FriendshipJewel:\n{err}", LogLevel.Error);
        }
    }

    private static int Return10For8HeartDateableButNotDating(Farmer player, NPC npc)
    {
        CharacterData data = npc.GetData();
        if (player.friendshipData.TryGetValue(npc.Name, out Friendship friendship))
        {
            if (friendship.Points >= 2000 && data.CanBeRomanced && !friendship.IsDating())
                return 10;
            return friendship.Points / 250;
        }
        return 0;
    }

    private static IEnumerable<CodeInstruction> DialogueBox_drawPortrait_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        try
        {
            CodeMatcher matcher = new(instructions, generator);

            // IL_02ef: call class StardewValley.Farmer StardewValley.Game1::get_player()
            // IL_02f4: ldloc.0
            // IL_02f5: callvirt instance string StardewValley.Character::get_Name()
            // IL_02fa: callvirt instance int32 StardewValley.Farmer::getFriendshipHeartLevelForNPC(string)
            matcher
                .MatchEndForward(
                    [
                        new(OpCodes.Call, AccessTools.PropertyGetter(typeof(Game1), nameof(Game1.player))),
                        new((inst) => inst.IsLdloc()),
                        new(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Character), nameof(Character.Name))),
                        new(
                            OpCodes.Callvirt,
                            AccessTools.PropertyGetter(typeof(Farmer), nameof(Farmer.getFriendshipHeartLevelForNPC))
                        ),
                    ]
                )
                .Advance(-1)
                .SetAndAdvance(
                    OpCodes.Call,
                    AccessTools.Method(typeof(FriendshipJewel), nameof(Return10For8HeartDateableButNotDating))
                )
                .SetOpcodeAndAdvance(OpCodes.Nop);

            return matcher.Instructions();
        }
        catch (Exception err)
        {
            ModEntry.Log($"Error in DialogueBox_drawPortrait_Transpiler:\n{err}", LogLevel.Error);
            return instructions;
        }
    }
}
