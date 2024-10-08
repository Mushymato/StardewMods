using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Locations;
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
            helper.Events.Player.Warped += OnWarped;
            helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
        }

        private static void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (HasHoeDirtOverride(e.NewLocation))
            {
                ModifyHoeDirtTextureForLocation(e.NewLocation);
                e.NewLocation.terrainFeatures.OnValueAdded += ModifyHoeDirtTexture;
            }
            e.OldLocation.terrainFeatures.OnValueAdded -= ModifyHoeDirtTexture;
        }

        private static bool HasHoeDirtOverride(GameLocation location)
        {
            return (
                location != null
                && location.GetData() is LocationData locData
                && locData.CustomFields is Dictionary<string, string> customFields
                && customFields.ContainsKey(CustomFields_HoeDirtTexture)
            );
        }

        private static void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            if (e.NamesWithoutLocale.Any(an => an.IsEquivalentTo("Data/Locations")))
            {
                hoeDirtTextureCache.Clear();
            }
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
