using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;

namespace FishZones;

public enum PhaseMode
{
    None,
    JustWarped,
    WillCapture,
    WillWarp,
}

public sealed record FishZoneRecord(List<(Vector2, int)> TileDistance, Dictionary<string, FishAreaData>? FishAreas);

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
    private FishZoneRecord? FishZoneInfo = null;
    private readonly Queue<GameLocation> FishableLocationQueue = [];
    private PhaseMode Phase = PhaseMode.None;

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
        if (FishZoneInfo == null)
            return;
        foreach ((Vector2 pos, int distance) in FishZoneInfo.TileDistance)
        {
            Vector2 local = Game1.GlobalToLocal(pos * Game1.tileSize);
            Utility.DrawSquare(
                e.SpriteBatch,
                new(local.ToPoint(), square),
                4,
                borderColor: FishZoneColors[distance],
                backgroundColor: FishZoneColors[distance] * 0.8f
            );
            Utility.drawTinyDigits(distance, e.SpriteBatch, local + margin, 4f, 1f, Color.White);
        }

        if (FishZoneInfo.FishAreas == null)
            return;

        foreach ((string key, FishAreaData fishArea) in FishZoneInfo.FishAreas)
        {
            if (fishArea.Position is not Rectangle posRect)
                continue;
            Vector2 local = Game1.GlobalToLocal(new(posRect.X * Game1.tileSize, posRect.Y * Game1.tileSize));
            Utility.DrawSquare(
                e.SpriteBatch,
                new((int)local.X, (int)local.Y, posRect.Width * Game1.tileSize, posRect.Height * Game1.tileSize),
                8,
                borderColor: Color.White
            );
            string displayName = (fishArea.DisplayName != null) ? TokenParser.ParseText(fishArea.DisplayName) : key;
            Utility.drawBoldText(e.SpriteBatch, displayName, Game1.dialogueFont, local + margin * 3, Color.White, 4);
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
        List<(Vector2, int)> fishableTiles = IterateMapFishableTiles(location).ToList();
        if (fishableTiles.Count == 0)
            return;
        FishZoneInfo = new(fishableTiles, location.GetData()?.FishAreas);
        string text = Game1.game1.takeMapScreenshot(
            0.25f,
            $"fishzone_{SaveGame.FilterFileName(Game1.player.Name)}_{location.NameOrUniqueName}",
            null
        );
        if (text != null)
        {
            Monitor.Log(
                $"Saved fishzone screenshot to '{Path.Combine(Game1.game1.GetScreenshotFolder(), text)}'.",
                LogLevel.Info
            );
        }
        else
        {
            Monitor.Log($"Failed to take screenshot.", LogLevel.Error);
        }
        FishZoneInfo = null;
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
        Phase = PhaseMode.None;
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
        Phase = PhaseMode.JustWarped;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Phase == PhaseMode.WillWarp)
        {
            DoNextFishableLocation();
        }
        else if (Phase == PhaseMode.JustWarped && Game1.player.CanMove && Game1.currentLocation != null)
        {
            Phase = PhaseMode.WillCapture;
            Game1.outdoorLight = Color.White;
            Game1.ambientLight = Color.White;
        }
    }

    private void OnOneSecondUpdatedTicked(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        if (Phase == PhaseMode.WillCapture)
        {
            SaveFishZones(Game1.currentLocation);
            Phase = PhaseMode.WillWarp;
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
            else
            {
                Monitor.Log($"Skip {loc.NameOrUniqueName}", LogLevel.Debug);
            }
            return true;
        });
        DoNextFishableLocation();
    }
}
