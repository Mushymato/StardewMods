using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Locations;

namespace TheLegendReturns;

public sealed class ModEntry : Mod
{
    public const string ModId = "mushymato.TheLegendReturns";
    internal PerScreen<int> PrevYear = new();

    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Content.AssetRequested += OnAssetRequested;
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        PrevYear.Value = Game1.year;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Game1.year != PrevYear.Value)
            Helper.GameContent.InvalidateCache("Data/Locations");
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Locations"))
        {
            e.Edit(EditAssetLocations, AssetEditPriority.Late);
        }
    }

    private void EditAssetLocations(IAssetData asset)
    {
        IDictionary<string, LocationData> data = asset.AsDictionary<string, LocationData>().Data;
        foreach ((string key, LocationData locData) in data)
        {
            if (!(locData.Fish?.Any() ?? false))
                continue;
            foreach (var fishEntry in locData.Fish)
            {
                if (fishEntry.IsBossFish && fishEntry.CatchLimit > 0)
                {
                    fishEntry.CatchLimit += Game1.year - 1;
                    Monitor.Log(
                        $"Increase catch limit for '{fishEntry.ItemId}' to {fishEntry.CatchLimit}",
                        LogLevel.Debug
                    );
                }
            }
        }
    }
}
