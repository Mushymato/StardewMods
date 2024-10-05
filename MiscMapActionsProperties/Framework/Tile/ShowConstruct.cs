using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace MiscMapActionsProperties.Framework.Tile
{
    /// <summary>
    /// Add new tile actions mushymato.MMAP_ShowConstruct and mushymato.MMAP_ShowConstructForCurrent
    /// Usage:
    /// - mushymato.MMAP_ShowConstruct <builder>
    /// - mushymato.MMAP_ShowConstructForCurrent <builder>
    /// Shows contruct menu (robin, wizard, and any modded builders) when interacted with.
    /// mushymato.MMAP_ShowConstruct shows a list of locations marked buildable to select, just like vanilla Robin/Wizard
    /// mushymato.MMAP_ShowConstructForCurrent shows the menu for current location only, if the current location is made buildable with map properties
    /// </summary>
    internal static class ShowConstruct
    {
        internal static readonly string TileAction_ShowConstruct = $"{ModEntry.ModId}_ShowConstruct";
        internal static readonly string TileAction_ShowConstructForCurrent = $"{ModEntry.ModId}_ShowConstructForCurrent";
        internal static void Register()
        {
            GameLocation.RegisterTileAction(
                TileAction_ShowConstruct,
                (location, args, farmer, tile) =>
                {
                    if (args.Length < 2)
                    {
                        ModEntry.LogOnce($"Not enough arguments, Usage: {TileAction_ShowConstruct} <builder>", LogLevel.Warn);
                        return false;
                    }
                    try
                    {
                        location.ShowConstructOptions(args[1]);
                        return true;
                    }
                    catch (DivideByZeroException)
                    {
                        ModEntry.LogOnce($"Failed to open construct menu, invalid builder {args[1]}", LogLevel.Error);
                        return false;
                    }
                }
            );

            GameLocation.RegisterTileAction(
                TileAction_ShowConstructForCurrent,
                (location, args, farmer, tile) =>
                {
                    if (args.Length < 2)
                    {
                        ModEntry.LogOnce($"Not enough arguments, Usage: {TileAction_ShowConstructForCurrent} <builder>", LogLevel.Warn);
                        return false;
                    }
                    try
                    {
                        if (location.IsBuildableLocation())
                        {
                            Game1.activeClickableMenu = new CarpenterMenu(args[1], location);
                            return true;
                        }
                        return false;
                    }
                    catch (DivideByZeroException)
                    {
                        ModEntry.LogOnce($"Failed to open construct menu, invalid builder {args[1]}");
                        return false;
                    }
                }
            );
        }
    }
}
