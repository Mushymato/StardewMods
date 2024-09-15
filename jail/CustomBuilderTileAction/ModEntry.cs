using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace CustomBuilderTileAction;

internal sealed class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        GameLocation.RegisterTileAction(
            ModManifest.UniqueID,
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
                    Monitor.LogOnce($"Failed to open construct menu, invalid builder {args[1]}");
                    return false;
                }
            }
        );
    }
}
