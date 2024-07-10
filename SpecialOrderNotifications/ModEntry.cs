using HarmonyLib;
using SpecialOrderNotifications.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.SpecialOrders.Objectives;

namespace SpecialOrderNotifications
{
    public class ModEntry : Mod
    {
        private static IMonitor? mon;

        public override void Entry(IModHelper helper)
        {
            mon = Monitor;
            GamePatches.Patch(ModManifest.UniqueID);

            helper.ConsoleCommands.Add(
                "debug_quest_ping",
                "debug_quest_ping current max",
                DebugQuestPing
            );
        }

        public static void Log(string msg, LogLevel level = LogLevel.Debug)
        {
            mon!.Log(msg, level);
        }

        public static void DebugQuestPing(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                Log("Must load save first.", LogLevel.Error);
                return;
            }
            if (int.TryParse(args[0], out int current) && int.TryParse(args[1], out int max))
            {
                QuestPingHelper.PingJunimoKart(current, max);
            }
        }
    }
}
