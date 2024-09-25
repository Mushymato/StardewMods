using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;

namespace MiscMapActionsProperties.Framework.Tile
{
    /// <summary>
    /// Add new tile property mushymato.MMAP_AnimalSpot T
    /// Control where animals go when asked for random position
    /// </summary>
    internal static class AnimalSpot
    {
        internal readonly static string TileProp_AnimalSpot = $"{ModEntry.ModId}_AnimalSpot";
        internal static ConditionalWeakTable<GameLocation, List<Vector2>> animalSpotsCache = [];

        internal static void Patch(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(FarmAnimal), nameof(FarmAnimal.setRandomPosition)),
                    prefix: new HarmonyMethod(typeof(AnimalSpot), nameof(FarmAnimal_setRandomPosition_Prefix))
                );
            }
            catch (Exception err)
            {
                ModEntry.Log($"Failed to patch AnimalSpot:\n{err}", LogLevel.Error);
            }
        }

        private static List<Vector2> GetAnimalSpots(GameLocation location)
        {
            List<Vector2> animalSpots = [];
            var backLayer = location.map.RequireLayer("Back");
            for (int x = 0; x < backLayer.LayerWidth; x++)
            {
                for (int y = 0; y < backLayer.LayerHeight; y++)
                {
                    Vector2 pos = new(x, y);
                    if (pos.Equals(Vector2.Zero))
                        continue;
                    MapTile tile = backLayer.Tiles[x, y];
                    if (tile == null)
                        continue;
                    if (backLayer.Tiles[x, y].Properties.ContainsKey(TileProp_AnimalSpot))
                    {
                        animalSpots.Add(pos);
                    }
                }
            }
            return animalSpots;
        }

        private static bool HasOtherAnimals(FarmAnimal __instance, GameLocation location, Vector2 tile)
        {
            Rectangle bounds = __instance.GetBoundingBox();
            foreach (FarmAnimal animal in location.animals.Values)
                if (animal != __instance && bounds.Intersects(animal.GetBoundingBox()))
                    return true;
            return false;
        }

        private static bool FarmAnimal_setRandomPosition_Prefix(FarmAnimal __instance, GameLocation location)
        {
            try
            {
                __instance.StopAllActions();
                List<Vector2> animalSpots = animalSpotsCache.GetValue(location, GetAnimalSpots);
                Character _base = __instance;
                foreach (Vector2 pos in animalSpots)
                {
                    _base.Position = pos * 64;
                    if (location.Objects.ContainsKey(pos) ||
                        HasOtherAnimals(__instance, location, pos) ||
                        location.isCollidingPosition(_base.GetBoundingBox(), Game1.viewport, isFarmer: false, 0, glider: false, __instance))
                        continue;
                    __instance.SleepIfNecessary();
                    return false;
                }
            }
            catch (Exception err)
            {
                ModEntry.Log($"Error in FarmAnimal_setRandomPosition_Prefix:\n{err}", LogLevel.Error);
            }
            return true;
        }
    }
}