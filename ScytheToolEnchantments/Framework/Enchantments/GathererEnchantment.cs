using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;

namespace ScytheToolEnchantments.Framework.Enchantments;

[XmlType($"Mods_mushymato_{nameof(GathererEnchantment)}")]
public class GathererEnchantment : ScytheEnchantment
{
    public override string GetName()
    {
        return I18n.Enchantment_Gatherer_Name();
    }

    public override bool CanApplyTo(Item item)
    {
        return base.CanApplyTo(item) && ModEntry.Config!.EnableGatherer;
    }

    protected override void _OnCutWeed(Vector2 tile_location, GameLocation location, Farmer who)
    {
        base._OnCutWeed(tile_location, location, who);
        if (Random.Shared.NextBool())
            Game1.createItemDebris(
                ItemRegistry.Create("(O)771"),
                new Vector2(tile_location.X * 64f + 32f, tile_location.Y * 64f + 32f),
                -1
            );
    }
}
