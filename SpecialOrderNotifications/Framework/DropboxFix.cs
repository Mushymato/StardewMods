using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;
using StardewValley.SpecialOrders;

namespace SpecialOrderNotifications.Framework
{
    internal record PreviouslyOpenedDonateTile(
        Point Tile,
        int BoxIdx
    );

    internal static class DropboxFix
    {
        private static readonly ConditionalWeakTable<GameLocation, PreviouslyOpenedDonateTile?> previousOpen = [];
        internal static void Register()
        {
            GameLocation.RegisterTileAction(
                "DropBox",
                (location, args, farmer, tile) =>
                {
                    if (!ArgUtility.TryGet(args, 1, out var boxId, out string error, allowBlank: true, "string box_id"))
                    {
                        location.LogTileActionError(args, tile.X, tile.Y, error);
                        return false;
                    }
                    List<SpecialOrder> specialOrders = [];
                    foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
                    {
                        if (specialOrder.UsesDropBox(boxId))
                        {
                            specialOrders.Add(specialOrder);
                        }
                    }
                    if (specialOrders.Any())
                    {
                        var prevOpen = previousOpen.GetValue(location, (location) => null);
                        int boxIdx = 0;
                        if (prevOpen != null && prevOpen.Tile == tile)
                        {
                            boxIdx = prevOpen.BoxIdx + 1;
                            if (boxIdx >= specialOrders.Count)
                                boxIdx = 0;
                        }
                        SpecialOrder order = specialOrders[boxIdx];
                        int minCapacity = order.GetMinimumDropBoxCapacity(boxId);
                        order.donateMutex.RequestLock(delegate
                        {
                            while (order.donatedItems.Count < minCapacity)
                            {
                                order.donatedItems.Add(null);
                            }
                            Game1.activeClickableMenu = new QuestContainerMenu(order.donatedItems, 3, order.HighlightAcceptableItems, order.GetAcceptCount, order.UpdateDonationCounts, order.ConfirmCompleteDonations);
                        });
                        previousOpen.AddOrUpdate(location, new PreviouslyOpenedDonateTile(tile, boxIdx));
                        return true;
                    }
                    return false;
                }
            );
        }
    }
}
