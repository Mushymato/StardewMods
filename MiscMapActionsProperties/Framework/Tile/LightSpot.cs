using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.GameData.Buildings;

namespace MiscMapActionsProperties.Framework.Tile
{
    /// <summary>
    /// Add new tile property mushymato.MMAP_Light [radius] [color] [type]
    /// Place a light source on a tile
    /// </summary>
    internal static class LightSpot
    {
        internal readonly static string TileProp_Light = $"{ModEntry.ModId}_Light";
        internal static void Patch(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(GameLocation), "resetLocalState"),
                    postfix: new HarmonyMethod(typeof(LightSpot), nameof(GameLocation_resetLocalState_Postfix))
                );
            }
            catch (Exception err)
            {
                ModEntry.Log($"Failed to patch LightSpot:\n{err}", LogLevel.Error);
            }

        }

        private static LightSource? MakeMapLightFromProps(string mapName, Vector2 pos, string lightProps)
        {
            string[] args = ArgUtility.SplitBySpace(lightProps ?? "");
            if (!ArgUtility.TryGetOptionalFloat(args, 0, out float radius, out string error, defaultValue: 2f, name: "float radius") ||
                !ArgUtility.TryGetOptional(args, 1, out string colorStr, out error, defaultValue: "White", name: "string color") ||
                !ArgUtility.TryGetOptionalInt(args, 2, out int textureIndex, out error, defaultValue: 1, name: "int textureIndex"))
            {
                ModEntry.Log(error, LogLevel.Error);
                return null;
            }
            Color color = Utility.StringToColor(colorStr) ?? Color.White;
            color = new Color(color.PackedValue ^ 0x00FFFFFF);
            return new LightSource(
                $"{mapName}_{TileProp_Light}_{pos.X}_{pos.Y}",
                textureIndex, pos * Game1.tileSize + new Vector2(Game1.tileSize / 2, Game1.tileSize / 2), radius, color,
                LightSource.LightContext.None
            );
        }

        private static IEnumerable<LightSource> GetMapTileLights(GameLocation location)
        {
            // map front layer lights
            var backLayer = location.map.RequireLayer("Front");
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
                    if (tile.Properties.TryGetValue(TileProp_Light, out string lightProps) &&
                        MakeMapLightFromProps(location.NameOrUniqueName, pos, lightProps) is LightSource light)
                    {
                        yield return light;
                    }
                }
            }

            // map building layer lights
            foreach (Building building in location.buildings)
            {
                BuildingData data = building.GetData();
                foreach (BuildingTileProperty btp in data.TileProperties)
                {
                    if (btp.Name != TileProp_Light || btp.Layer != "Front")
                        continue;
                    string lightProps = btp.Value;
                    // if (MakeMapLightFromProps(location.NameOrUniqueName, Vector2.Zero, lightProps) is not LightSource baseLight)
                    //     continue;
                    for (int i = 0; i < btp.TileArea.Width; i++)
                    {
                        for (int j = 0; j < btp.TileArea.Height; j++)
                        {
                            Vector2 pos = new(building.tileX.Value + btp.TileArea.X + i, building.tileY.Value + btp.TileArea.Y + j);
                            // LightSource light = baseLight.Clone();
                            // light.Id = $"{location.NameOrUniqueName}_{TileProp_Light}_{pos.X}_{pos.Y}";
                            // light.position.Value = pos;
                            // yield return light;
                            if (MakeMapLightFromProps(location.NameOrUniqueName, pos, lightProps) is LightSource light)
                                yield return light;
                        }
                    }
                }
            }
        }

        private static void GameLocation_resetLocalState_Postfix(GameLocation __instance)
        {
            if (__instance.ignoreLights.Value)
                return;
            foreach (LightSource light in GetMapTileLights(__instance))
            {
                Console.WriteLine(light.Id);
                Game1.currentLightSources.Add(light);
            }
        }
    }
}