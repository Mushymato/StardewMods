using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
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

public sealed class ModConfig
{
    public KeybindList ToggleKey = KeybindList.Parse($"{SButton.F5}+{SButton.F}");
    public bool Enabled = false;
    public bool ShowAreas = true;
    public bool ShowNumber = true;
    private readonly Color[] DefaultFishZoneColors =
    [
        new(138, 228, 0),
        new(176, 200, 0),
        new(206, 170, 0),
        new(230, 135, 0),
        Color.Black,
        new(248, 89, 0),
    ];
    private Color[] fishZoneColors = [];
    public Color[] FishZoneColors
    {
        get => fishZoneColors?.Length >= 6 ? fishZoneColors : DefaultFishZoneColors;
        set => fishZoneColors = value;
    }
}

public sealed record FishZoneRecord(List<(Vector2, int)> TileDistance, Dictionary<string, FishAreaData>? FishAreas);

public sealed class ModEntry : Mod
{
    public const string ModId = "mushymato.FishZones";

    private readonly Point square = new(Game1.tileSize, Game1.tileSize);
    private readonly Vector2 margin = new(4, 4);
    private FishZoneRecord? FishZoneInfo = null;
    private readonly Queue<GameLocation> FishableLocationQueue = [];
    private PhaseMode Phase = PhaseMode.None;
    private ModConfig Config = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        helper.ConsoleCommands.Add(
            "fishzones.here",
            "Take mapwide screenshot of current map with fish zones",
            ConsoleFishZones
        );
        helper.ConsoleCommands.Add(
            "fishzones.all",
            "Take mapwide screenshot of all maps with fish zones",
            ConsoleFishZonesAll
        );
        helper.ConsoleCommands.Add(
            "fishzones.toggle",
            "Toggle certain settings depending on first arg:\n- fishzones.toggle e : always on fish zones\n- fishzones.toggle n : show numbers on fish zones\n- fishzones.toggle a : show area on fish zones",
            ConsoleToggles
        );
        Helper.Events.Display.RenderedWorld += OnRenderedWorld;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdatedTicked;
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Player.Warped += OnWarped;
    }

    private void ConsoleFishZones(string arg1, string[] arg2)
    {
        SaveFishZones(Game1.currentLocation);
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

    private void ConsoleToggles(string arg1, string[] arg2)
    {
        if (!ArgUtility.TryGet(arg2, 0, out string kind, out string error, allowBlank: false, name: "string kind"))
        {
            Monitor.Log(error, LogLevel.Error);
            return;
        }
        switch (kind)
        {
            case "e":
                ToggleAlwaysOnDisplay();
                Monitor.Log($"Enabled: {Config.Enabled}", LogLevel.Info);
                break;
            case "n":
                Config.ShowNumber = !Config.ShowNumber;
                Helper.WriteConfig(Config);
                Monitor.Log($"ShowNumber: {Config.ShowNumber}", LogLevel.Info);
                break;
            case "a":
                Config.ShowAreas = !Config.ShowAreas;
                Helper.WriteConfig(Config);
                Monitor.Log($"ShowAreas: {Config.ShowAreas}", LogLevel.Info);
                break;
        }
    }

    private bool InitFishZoneInfo(GameLocation location)
    {
        if (location is null || !location.canFishHere())
        {
            FishZoneInfo = null;
            return false;
        }
        List<(Vector2, int)> fishableTiles = IterateMapFishableTiles(location).ToList();
        if (fishableTiles.Count == 0)
        {
            FishZoneInfo = null;
            return false;
        }
        FishZoneInfo = new(fishableTiles, location.GetData()?.FishAreas);
        return true;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Config.Enabled)
            InitFishZoneInfo(Game1.currentLocation);
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (Config.Enabled)
            InitFishZoneInfo(e.NewLocation);
    }

    private void ToggleAlwaysOnDisplay()
    {
        Config.Enabled = !Config.Enabled;
        if (Config.Enabled)
        {
            InitFishZoneInfo(Game1.currentLocation);
        }
        else
        {
            FishZoneInfo = null;
        }
        Helper.WriteConfig(Config);
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (Config.ToggleKey.JustPressed())
        {
            ToggleAlwaysOnDisplay();
        }
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
                borderColor: Config.FishZoneColors[distance],
                backgroundColor: Config.FishZoneColors[distance] * 0.8f
            );
            if (Config.ShowNumber)
                Utility.drawTinyDigits(distance, e.SpriteBatch, local + margin, 4f, 1f, Color.White);
        }

        if (!Config.ShowAreas || FishZoneInfo.FishAreas == null)
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
        var prevFishZoneInfoIsNull = FishZoneInfo == null;
        if (FishZoneInfo == null && !InitFishZoneInfo(location))
            return;

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

        if (prevFishZoneInfoIsNull)
            FishZoneInfo = null;
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
}
