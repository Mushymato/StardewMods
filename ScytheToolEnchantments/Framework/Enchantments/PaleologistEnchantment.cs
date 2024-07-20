using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Internal;

namespace ScytheToolEnchantments.Framework.Enchantments
{
    [XmlType($"Mods_mushymato_{nameof(PalaeontologistEnchantment)}")]
    public class PalaeontologistEnchantment : ScytheEnchantment
    {
        public override string GetName()
        {
            return I18n.Enchantment_Palaeontologist_Name();
        }

        public override bool CanApplyTo(Item item)
        {
            return base.CanApplyTo(item) && ModEntry.Config!.EnablePalaeontologist;
        }

        public static bool TryGetRandomBoneItem([NotNullWhen(true)] out Item? boneItem)
        {
            boneItem = null;
            if (ItemQueryResolver.TryResolve(
                "ALL_ITEMS",
                new ItemQueryContext(Game1.currentLocation, Game1.player, Game1.random),
                ItemQuerySearchMode.RandomOfTypeItem,
                "ITEM_CONTEXT_TAG Target bone_item, !ITEM_CONTEXT_TAG Target id_o_881"
            ).FirstOrDefault()?.Item is not Item item)
                return false;
            boneItem = item;
            return true;
        }

        /// <summary>
        /// Make 0-1 bone fragments when cutting weed, small chance to get artifact
        /// </summary>
        /// <param name="tile_location"></param>
        /// <param name="location"></param>
        /// <param name="who"></param>
        public static void DropItems(Vector2 tile_location)
        {
            if (Random.Shared.NextBool())
            {
                Game1.createItemDebris(ItemRegistry.Create("(O)881"), new Vector2(tile_location.X * 64f + 32f, tile_location.Y * 64f + 32f), -1);
            }
            if (Random.Shared.NextDouble() < 0.05)
            {
                // Game1.createItemDebris(location.tryGetRandomArtifactFromThisLocation(who, Game1.random), new Vector2(tile_location.X * 64f + 32f, tile_location.Y * 64f + 32f), -1);
                if (TryGetRandomBoneItem(out Item? boneItem))
                {
                    Game1.createItemDebris(boneItem, new Vector2(tile_location.X * 64f + 32f, tile_location.Y * 64f + 32f), -1);
                }
            }
        }

        protected override void _OnCutWeed(Vector2 tile_location, GameLocation location, Farmer who)
        {
            base._OnCutWeed(tile_location, location, who);
            DropItems(tile_location);
        }
    }
}
