using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;
using MachineControlPanel.Framework.UI;
using SObject = StardewValley.Object;

namespace MachineControlPanel.Framework
{
    internal class MachineControl(IModHelper helper, IMonitor monitor, ModConfig config)
    {
        internal bool ShowPanel(ICursorPosition cursor)
        {

            if (Game1.activeClickableMenu == null && config.ControlPanelKey.JustPressed())
            {
                // ICursorPosition.GrabTile is unreliable with gamepad controls. Instead recreate game logic.
                Vector2 cursorTile = Game1.currentCursorTile;
                Point tile = Utility.tileWithinRadiusOfPlayer((int)cursorTile.X, (int)cursorTile.Y, 1, Game1.player)
                    ? cursorTile.ToPoint()
                    : Game1.player.GetGrabTile().ToPoint();
                SObject? bigCraftable = Game1.player.currentLocation.getObjectAtTile(tile.X, tile.Y, ignorePassables: true);
                if (bigCraftable != null && bigCraftable.bigCraftable.Value)
                {
                    return ShowPanelFor(bigCraftable);
                }
            }
            return false;
        }

        internal bool ShowPanelFor(SObject bigCraftable)
        {
            if (bigCraftable.GetMachineData() is not MachineData machine)
                return false;

            if (machine.IsIncubator || machine.OutputRules == null || !machine.AllowFairyDust)
                return false;

            // DebugPrintMachineRules(machine);

            Game1.activeClickableMenu = new RuleMenu(new RuleHelper(bigCraftable, machine));

            return true;
        }

        internal void Log(string msg, LogLevel level = LogLevel.Debug)
        {
            monitor.Log(msg, level);
        }

        internal void DebugPrintMachineRules(MachineData machine)
        {
            foreach (MachineOutputRule rule in machine.OutputRules)
            {
                int minutesUntilReady = -1;
                if (rule.MinutesUntilReady >= 0 || rule.DaysUntilReady >= 0)
                {
                    minutesUntilReady = (rule.DaysUntilReady >= 0) ? Utility.CalculateMinutesUntilMorning(Game1.timeOfDay, rule.DaysUntilReady) : rule.MinutesUntilReady;
                }
                Log("\n");
                Log($"Rule: {rule.Id} (minutes: {minutesUntilReady})");

                foreach (MachineOutputTriggerRule trigger in rule.Triggers)
                {
                    Log($"Trigger: {trigger.Trigger}");
                    Log($"\tRequiredItemId: {trigger.RequiredItemId}");
                    Log($"\tRequiredTags: {trigger.RequiredTags}");
                    Log($"\tRequiredCount: {trigger.RequiredCount}");
                    Log($"\tCondition: {trigger.Condition}");
                }

                if (rule.OutputItem == null)
                    continue;

                foreach (MachineItemOutput output in rule.OutputItem)
                {
                    Log($"Output: {output.Id}");
                    if (output.OutputMethod == null)
                    {
                        Log($"\tItemId: {output.ItemId}");
                        Log($"\tMinStack: {output.MinStack}, MaxStack: {output.MaxStack}, Quality: {output.Quality}");
                        Log($"\tCondition: {output.Condition}");
                        Log($"\tPerItemCondition: {output.PerItemCondition}");
                    }
                    else
                    {
                        Log($"\tOutputMethod: {output.OutputMethod}");
                    }
                }
            }
        }
    }
}