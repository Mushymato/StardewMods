using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.Inventories;
using StardewValley.Objects;
using MiscMapActionsProperties.Framework.Wheels;
using StardewValley.Extensions;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace MiscMapActionsProperties.Framework.Buildings
{
    /// <summary>
    /// Add new BuildingData.Metadata mushymato.MMAP/ChestLight_<ChestId> [radius] [color] [type|texture] [offsetX] [offsetY]
    /// Place a light source on a tile, with optional offset
    /// [type|texture] is either a light id (1-10 except for 3) or a texture (must be loaded).
    /// </summary>
    internal static class ChestLight
    {
        internal readonly static string Metadata_ChestLight_Prefix = $"{ModEntry.ModId}/ChestLight.";

        private static readonly ConditionalWeakTable<Chest, BuildingChestLightWatcher> watchers = [];

        internal static void Register(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
        }

        private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            AddBuildingChestLightWatcher(Game1.currentLocation);
        }

        private static void OnWarped(object? sender, WarpedEventArgs e)
        {
            foreach (var kv in watchers)
                kv.Value.Unsubscribe();
            AddBuildingChestLightWatcher(e.NewLocation);
        }

        private static void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            foreach (var kv in watchers)
                kv.Value.Dispose();
            watchers.Clear();
        }

        private static void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            if (e.NamesWithoutLocale.Any(an => an.IsEquivalentTo("Data/Buildings")))
            {
                foreach (var kv in watchers)
                    kv.Value.Dispose();
                watchers.Clear();
                GC.Collect();
            }
        }

        private static void AddBuildingChestLightWatcher(GameLocation location)
        {
            foreach (Building building in location.buildings)
            {
                BuildingData data = building.GetData();
                if (data == null)
                    continue;

                foreach (Chest buildingChest in building.buildingChests)
                {
                    string lightName = $"{Metadata_ChestLight_Prefix}{buildingChest.Name}";
                    if (!data.Metadata.TryGetValue(lightName, out string? lightProps))
                        continue;
                    var watch = watchers.GetValue(buildingChest, (chest) => new BuildingChestLightWatcher(building, chest, lightName, lightProps));
                    watch.Subscribe();
                }
            }
        }
    }

    /// <summary>
    /// Shenanigans for watching building chest changes.
    /// Use with WeakReference or ConditionalWeakTable;
    /// </summary>
    internal sealed class BuildingChestLightWatcher(Building building, Chest chest, string lightName, string lightProps) : IDisposable
    {
        private Building building = building;
        private Chest chest = chest;
        private readonly string lightName = lightName;
        private readonly string lightProps = lightProps;
        internal bool wasDisposed = false;

        ~BuildingChestLightWatcher() => DisposeValues();

        private void DisposeValues()
        {
            if (wasDisposed)
                return;
            chest.Items.OnSlotChanged -= OnSlotChanged;
            building = null!;
            chest = null!;
            wasDisposed = true;
        }

        public void Dispose()
        {
            DisposeValues();
            GC.SuppressFinalize(this);
        }

        public void Subscribe()
        {
            UpdateBuildingLights();
            chest.Items.OnSlotChanged += OnSlotChanged;
        }

        public void Unsubscribe()
        {
            chest.Items.OnSlotChanged -= OnSlotChanged;
        }

        private void OnSlotChanged(Inventory inventory, int index, Item before, Item after)
        {
            UpdateBuildingLights();
        }

        internal void UpdateBuildingLights()
        {
            if (chest.Items.HasAny())
            {
                if (!Game1.currentLightSources.ContainsKey(lightName) &&
                    Light.MakeLightFromProps(lightProps, lightName, new Vector2(building.tileX.Value, building.tileY.Value) * Game1.tileSize) is LightSource light)
                {
                    Game1.currentLightSources.Add(light);
                }
            }
            else
            {
                if (Game1.currentLightSources.ContainsKey(lightName))
                {
                    Game1.currentLightSources.Remove(lightName);
                }
            }
        }
    }
}