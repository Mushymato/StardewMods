using System.Text;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;
using xTile.Dimensions;
using DropBoxPair = System.ValueTuple<
    StardewValley.SpecialOrders.SpecialOrder,
    StardewValley.SpecialOrders.Objectives.DonateObjective
>;

namespace SpecialOrderNotifications.Framework;

internal static class DropBoxFix
{
    private const string DROPBOX = "DropBox";

    // this is a queue so that i can rotate through it without keeping an index :)
    private static readonly PerScreen<Dictionary<Point, Queue<DropBoxPair>>?> currentLocationDropBoxPairs = new();
    private static readonly PerScreen<string?> activeMenuSpecialOrderName = new();

    internal static bool PointNearVector2(this Point point, Vector2 vector2) =>
        (point.X == Math.Floor(vector2.X) || point.X == Math.Ceiling(vector2.X))
        && (point.Y == Math.Floor(vector2.Y) || point.Y == Math.Ceiling(vector2.Y));

    internal static void Register(Harmony harmony, IModHelper helper)
    {
        harmony.Patch(
            original: AccessTools.DeclaredMethod(
                typeof(GameLocation),
                nameof(GameLocation.performAction),
                [typeof(string[]), typeof(Farmer), typeof(Location)]
            ),
            prefix: new HarmonyMethod(typeof(DropBoxFix), nameof(GameLocation_performAction_Prefix))
        );
        harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(GameLocation), nameof(GameLocation.ShouldIgnoreAction)),
            postfix: new HarmonyMethod(typeof(DropBoxFix), nameof(GameLocation_ShouldIgnoreAction_Postfix))
        );

        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.Display.MenuChanged += OnMenuChanged;

        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
    }

    private static bool GameLocation_performAction_Prefix(
        GameLocation __instance,
        string[] action,
        Farmer who,
        Location tileLocation,
        ref bool __result
    )
    {
        __result = false;

        if (ArgUtility.Get(action, 0) != DROPBOX || currentLocationDropBoxPairs.Value == null)
            return true;

        Point tile = new(tileLocation.X, tileLocation.Y);
        if (!(currentLocationDropBoxPairs.Value?.TryGetValue(tile, out Queue<DropBoxPair>? dropBoxPairs) ?? false))
            return true;

        ModEntry.Log($"Using custom logic for DropBox at {tile}");

        DropBoxPair pair = dropBoxPairs.Dequeue();
        (SpecialOrder specialOrder, DonateObjective donateObjective) = pair;

        specialOrder.donateMutex.RequestLock(
            delegate
            {
                while (specialOrder.donatedItems.Count < Math.Max(9, donateObjective.minimumCapacity.Value))
                {
                    specialOrder.donatedItems.Add(null);
                }
                activeMenuSpecialOrderName.Value = specialOrder.GetName();
                Game1.activeClickableMenu = new QuestContainerMenu(
                    specialOrder.donatedItems,
                    3,
                    specialOrder.HighlightAcceptableItems,
                    specialOrder.GetAcceptCount,
                    specialOrder.UpdateDonationCounts,
                    specialOrder.ConfirmCompleteDonations
                )
                {
                    exitFunction = () =>
                    {
                        activeMenuSpecialOrderName.Value = null;
                        if (specialOrder.ShouldDisplayAsComplete())
                        {
                            currentLocationDropBoxPairs.Value = GetDonateSpecialOrdersInThisLocation(__instance);
                        }
                        else
                        {
                            dropBoxPairs.Enqueue(pair);
                        }
                    },
                };
            }
        );

        __result = true;
        return false;
    }

    private static void GameLocation_ShouldIgnoreAction_Postfix(
        GameLocation __instance,
        string[] action,
        Farmer who,
        Location tileLocation,
        ref bool __result
    )
    {
        if (!__result)
            return;

        if (currentLocationDropBoxPairs.Value == null || ArgUtility.Get(action, 0) != DROPBOX)
        {
            __result = true;
            return;
        }

        Point tile = new(tileLocation.X, tileLocation.Y);
        __result = !currentLocationDropBoxPairs.Value.ContainsKey(tile);
        return;
    }

    /// <summary>
    /// Gets this tile, or 1 tile below.
    /// </summary>
    /// <param name="vector2"></param>
    /// <returns></returns>
    private static List<Point> GetPossibleDropBoxPoints(
        SpecialOrder specialOrder,
        DonateObjective donateObjective,
        Dictionary<string, List<Point>> dropBoxIdTiles
    )
    {
        if (dropBoxIdTiles.TryGetValue(donateObjective.dropBox.Value, out List<Point>? dropBoxIds))
        {
            // happy case, drop box is valid
            return dropBoxIds;
        }
        else
        {
            // unhappy case, drop box overwritten
            ModEntry.LogOnce(
                $"No Action 'DropBox {donateObjective.dropBox.Value}' matching '{specialOrder.questKey.Value}' in current location, applied workaround.",
                LogLevel.Warn
            );
            return dropBoxIdTiles[string.Empty];
        }
    }

    private static Dictionary<string, List<Point>> GetDropBoxIdTiles(GameLocation location)
    {
        Dictionary<string, List<Point>> dropBoxIdTiles = [];
        dropBoxIdTiles[string.Empty] = [];
        xTile.Layers.Layer layer = location.Map.RequireLayer("Buildings");
        for (int x = 0; x < layer.LayerWidth; x++)
        {
            for (int y = 0; y < layer.LayerHeight; y++)
            {
                if (layer.Tiles[x, y] is not xTile.Tiles.Tile tile)
                    continue;
                if (
                    !tile.Properties.TryGetValue("Action", out string propValue)
                    && !tile.TileIndexProperties.TryGetValue("Action", out propValue)
                )
                    continue;

                string[] dropBoxAction = ArgUtility.SplitBySpace(propValue);
                if (
                    ArgUtility.Get(dropBoxAction, 0) != DROPBOX
                    || !ArgUtility.TryGet(
                        dropBoxAction,
                        1,
                        out string dropBoxId,
                        out _,
                        allowBlank: false,
                        "string dropBoxId"
                    )
                )
                    continue;
                ModEntry.Log(propValue);
                if (!dropBoxIdTiles.ContainsKey(dropBoxId))
                    dropBoxIdTiles[dropBoxId] = [];
                Point pos = new(x, y);
                dropBoxIdTiles[dropBoxId].Add(pos);
                dropBoxIdTiles[string.Empty].Add(pos);
            }
        }
        return dropBoxIdTiles;
    }

    private static Dictionary<Point, Queue<DropBoxPair>>? GetDonateSpecialOrdersInThisLocation(GameLocation location)
    {
        if (location == null || location.Map == null || Game1.player.team.specialOrders == null)
            return null;

        Dictionary<string, List<Point>>? dropBoxIdTiles = null;
        StringBuilder sb = new();

        Dictionary<Point, Queue<DropBoxPair>> dropBoxByPoint = [];
        foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
        {
            if (specialOrder.ShouldDisplayAsComplete())
            {
                continue;
            }
            foreach (OrderObjective objective in specialOrder.objectives)
            {
                if (
                    objective is DonateObjective donateObjective
                    && !string.IsNullOrEmpty(donateObjective.dropBoxGameLocation.Value)
                    && donateObjective.GetDropboxLocationName() == location.Name
                )
                {
                    dropBoxIdTiles ??= GetDropBoxIdTiles(location);
                    sb.Append("DropBox tiles for ");
                    sb.Append(specialOrder.questKey.Value);
                    sb.Append(':');
                    DropBoxPair pair = new(specialOrder, donateObjective);
                    foreach (
                        Point dropBoxPoint in GetPossibleDropBoxPoints(specialOrder, donateObjective, dropBoxIdTiles)
                    )
                    {
                        if (!dropBoxByPoint.ContainsKey(dropBoxPoint))
                            dropBoxByPoint[dropBoxPoint] = [];
                        dropBoxByPoint[dropBoxPoint].Enqueue(pair);
                        sb.Append($" {dropBoxPoint};");
                    }
                    ModEntry.Log(sb.ToString());
                    sb.Clear();
                }
            }
        }
        if (dropBoxByPoint.Count == 0)
            return null;
        return dropBoxByPoint;
    }

    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        currentLocationDropBoxPairs.Value = GetDonateSpecialOrdersInThisLocation(Game1.currentLocation);
    }

    private static void OnWarped(object? sender, WarpedEventArgs e)
    {
        currentLocationDropBoxPairs.Value = GetDonateSpecialOrdersInThisLocation(e.NewLocation);
    }

    private static void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.OldMenu is SpecialOrdersBoard)
        {
            currentLocationDropBoxPairs.Value = GetDonateSpecialOrdersInThisLocation(Game1.currentLocation);
        }
    }

    private static void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (
            Game1.activeClickableMenu is QuestContainerMenu questContainerMenu
            && activeMenuSpecialOrderName.Value != null
        )
        {
            SpriteText.drawStringWithScrollCenteredAt(
                e.SpriteBatch,
                activeMenuSpecialOrderName.Value,
                questContainerMenu.xPositionOnScreen + questContainerMenu.width / 2,
                questContainerMenu.yPositionOnScreen - 64
            );
        }
    }

    private static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (Game1.eventUp || currentLocationDropBoxPairs.Value == null)
            return;
        foreach (Queue<DropBoxPair> orders in currentLocationDropBoxPairs.Value.Values)
        {
            foreach ((_, DonateObjective donateObjective) in orders)
            {
                Vector2 indicatorLocation =
                    donateObjective.dropBoxTileLocation.Value * 64f
                    + new Vector2(
                        7f,
                        (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2)
                    ) * 4f;
                e.SpriteBatch.Draw(
                    Game1.mouseCursors2,
                    Game1.GlobalToLocal(Game1.viewport, indicatorLocation),
                    new Microsoft.Xna.Framework.Rectangle(114, 53, 6, 10),
                    Color.White,
                    0f,
                    new Vector2(1f, 4f),
                    4f,
                    SpriteEffects.None,
                    1f
                );
            }
        }
    }
}
