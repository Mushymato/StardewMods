using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;


namespace MiscMapActionsProperties.Framework.Terrain
{
    /// <summary>
    /// Allow mods to change the texture of the hoe dirt for a location via CustomFields
    /// {ModEntry.ModId}/HoeDirt.texture
    /// </summary>
    internal static class HoeDirtOverride
    {
        internal static readonly string CustomFields_HoeDirtTexture = $"{ModEntry.ModId}/HoeDirt.texture";
        private static readonly FieldInfo hoeDirtTexture = typeof(HoeDirt).GetField("texture", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly ConditionalWeakTable<GameLocation, Texture2D> hoeDirtTextureCache = [];

        internal static void Register(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
        }

        private static bool HasHoeDirtOverride(GameLocation location)
        {
            return (
                location.GetData().CustomFields is Dictionary<string, string> customFields
                && customFields.ContainsKey(CustomFields_HoeDirtTexture)
            );
        }

        private static void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (Game1.dayOfMonth == 1)
            {
                Utility.ForEachLocation((GameLocation location) =>
                {
                    if (HasHoeDirtOverride(location))
                        ModifyHoeDirtTextureForLocation(location);
                    return true;
                });
            }
        }

        private static void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            if (e.NamesWithoutLocale.Any(an => an.IsEquivalentTo("Data/Locations")))
            {
                hoeDirtTextureCache.Clear();
            }
        }

        private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            Utility.ForEachLocation((GameLocation location) =>
            {
                if (HasHoeDirtOverride(location))
                {
                    location.terrainFeatures.OnValueAdded += ModifyHoeDirtTexture;
                    ModifyHoeDirtTextureForLocation(location);
                }
                return true;
            });
        }

        private static bool ModifyHoeDirtTextureForLocation(GameLocation location)
        {
            foreach (var kv in location.terrainFeatures.Pairs)
            {
                ModifyHoeDirtTexture(kv.Key, kv.Value);
            }
            return true;
        }

        private static void ModifyHoeDirtTexture(Vector2 tile, TerrainFeature feature)
        {
            if (feature is HoeDirt hoeDirt)
            {
                Texture2D hoeDirtOverride = hoeDirtTextureCache.GetValue(hoeDirt.Location, LoadHoeDirtTexture);
                hoeDirtTexture.SetValue(hoeDirt, hoeDirtOverride);
            }
        }

        private static Texture2D LoadHoeDirtTexture(GameLocation location)
        {
            return Game1.content.Load<Texture2D>(location.GetData().CustomFields[CustomFields_HoeDirtTexture]);
        }
    }
}
