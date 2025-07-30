using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Tools;

namespace FishZones;

public sealed class ModEntry : Mod
{
    public const string ModId = "mushymato.FishZones";

    private static readonly Color[] FishZoneColors =
    [
        new(138, 228, 0),
        new(176, 200, 0),
        new(206, 170, 0),
        new(230, 135, 0),
        Color.Black,
        new(248, 89, 0),
    ];
    private readonly Point square = new(Game1.tileSize, Game1.tileSize);
    private readonly Vector2 margin = new(4, 4);
    private List<(Vector2, int)>? FishZoneData = null;
    private readonly Queue<GameLocation> FishableLocationQueue = [];
    private bool JustWarped = false;
    private bool WillCapture = false;
    private bool WillWarp = false;

    public override void Entry(IModHelper helper)
    {
        helper.ConsoleCommands.Add(
            "fishzones",
            "Take mapwide screenshot of current map with fish zones",
            ConsoleFishZones
        );
        helper.ConsoleCommands.Add(
            "fishzones.all",
            "Take mapwide screenshot of all maps with fish zones",
            ConsoleFishZonesAll
        );
        Helper.Events.Display.RenderedWorld += OnRenderedWorld;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdatedTicked;
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (FishZoneData == null)
            return;
        foreach ((Vector2 pos, int distance) in FishZoneData)
        {
            Vector2 local = Game1.GlobalToLocal(pos * Game1.tileSize);
            Utility.DrawSquare(
                e.SpriteBatch,
                new(local.ToPoint(), square),
                4,
                borderColor: FishZoneColors[distance] * 0.5f,
                backgroundColor: FishZoneColors[distance]
            );
            Utility.drawTinyDigits(distance, e.SpriteBatch, local + margin, 4f, 1f, Color.White);
        }
    }

    internal static IEnumerable<(Vector2, int)> IterateMapFishableTiles(GameLocation location)
    {
        xTile.Layers.Layer layer = location.map.RequireLayer("Back");
        for (int x = 0; x < layer.LayerWidth; x++)
        {
            for (int y = 0; y < layer.LayerHeight; y++)
            {
                if (layer.Tiles[x, y] is null)
                    continue;
                if (!location.isTileFishable(x, y))
                    continue;
                yield return new(new(x, y), FishingRod.distanceToLand(x, y, location));
            }
        }
    }

    private void SaveFishZones(GameLocation location)
    {
        if (location == null)
            return;
        FishZoneData = IterateMapFishableTiles(location).ToList();
        if (FishZoneData.Count == 0)
            return;
        string text = Game1.game1.takeMapScreenshot(
            0.25f,
            $"fishzone_{SaveGame.FilterFileName(Game1.player.Name)}_{location.NameOrUniqueName}",
            null
        );
        if (text != null)
        {
            Monitor.Log(
                $"Saved screenshot to '{Path.Combine(Game1.game1.GetScreenshotFolder(), text)}'.",
                LogLevel.Info
            );
        }
        else
        {
            Monitor.Log($"Failed to take screenshot.", LogLevel.Error);
        }
        FishZoneData = null;
    }

    private void ConsoleFishZones(string arg1, string[] arg2)
    {
        GameLocation currentLoc = Game1.currentLocation;
        if (currentLoc is null || !currentLoc.canFishHere())
            return;
        SaveFishZones(currentLoc);
    }

    private void DoNextFishableLocation()
    {
        if (!FishableLocationQueue.TryDequeue(out GameLocation? nextLoc))
        {
            Game1.warpHome();
            return;
        }
        Game1.warpingForForcedRemoteEvent = true;
        int x = 0;
        int y = 0;
        Utility.getDefaultWarpLocation(nextLoc.NameOrUniqueName, ref x, ref y);
        LocationRequest locationRequest = new(nextLoc.NameOrUniqueName, false, nextLoc);
        locationRequest.OnWarp += OnWarpToFishableLocation;
        Game1.warpFarmer(locationRequest, x, y, 2);
    }

    private void OnWarpToFishableLocation()
    {
        JustWarped = true;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (WillWarp)
        {
            DoNextFishableLocation();
            WillWarp = false;
        }
        else if (JustWarped && Game1.player.CanMove && Game1.currentLocation != null)
        {
            JustWarped = false;
            WillCapture = true;
            Game1.outdoorLight = Color.White;
            Game1.ambientLight = Color.White;
        }
    }

    private void OnOneSecondUpdatedTicked(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        if (WillCapture)
        {
            SaveFishZones(Game1.currentLocation);
            WillCapture = false;
            WillWarp = true;
        }
    }

    private void ConsoleFishZonesAll(string arg1, string[] arg2)
    {
        Utility.ForEachLocation(loc =>
        {
            if (loc.canFishHere() && IterateMapFishableTiles(loc).Any())
            {
                Monitor.Log($"Enqueue {loc.NameOrUniqueName}", LogLevel.Info);
                FishableLocationQueue.Enqueue(loc);
            }
            return true;
        });
        DoNextFishableLocation();
    }
}
