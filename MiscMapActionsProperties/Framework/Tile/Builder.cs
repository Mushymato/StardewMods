﻿using StardewValley;

namespace MiscMapActionsProperties.Framework.Tile
{
    internal static class Builder
    {
        internal static void Register()
        {
            GameLocation.RegisterTileAction(
                $"{ModEntry.ModId}_Builder",
                (location, args, farmer, tile) =>
                {
                    try
                    {
                        if (args.Length == 2)
                            location.ShowConstructOptions(args[1]);
                        else if (args.Length == 3)
                            location.ShowConstructOptions(args[1], int.Parse(args[2]));
                        else
                            return false;
                        return true;
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