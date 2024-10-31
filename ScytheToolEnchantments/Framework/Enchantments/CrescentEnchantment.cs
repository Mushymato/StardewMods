using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using StardewValley;

namespace ScytheToolEnchantments.Framework.Enchantments
{
    [XmlType($"Mods_mushymato_{nameof(CrescentEnchantment)}")]
    public class CrescentEnchantment : ScytheEnchantment
    {
        public override string GetName()
        {
            return I18n.Enchantment_Crescent_Name();
        }

        public override bool CanApplyTo(Item item)
        {
            return base.CanApplyTo(item) && ModEntry.Config!.EnableCrescent;
        }

        /// <summary>
        /// Get tiles to form a crescent shape, following the normal swing order
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="facingDirection"></param>
        /// <param name="currentAnimationIndex"></param>
        /// <returns></returns>
        public static IEnumerable<Vector2> GetCrescentAOE(Point tile, int facingDirection, int currentAnimationIndex)
        {
            int arcOffset = currentAnimationIndex switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                3 => 2,
                4 => 1,
                5 => 0,
                _ => currentAnimationIndex
            };
            int swingOffset = currentAnimationIndex + ((currentAnimationIndex > 2) ? 1 : 0);
            switch (facingDirection)
            {
                case 0:
                    // ..xxx..
                    // .xxxxx.
                    // .xxxxx.
                    // xxxFxxx
                    // xx...xx
                    // x.....x
                    // 0123345
                    swingOffset = -3 + swingOffset;
                    for (int i = 2; i > -1; i--)
                        yield return new Vector2(tile.X + swingOffset, tile.Y + i - arcOffset);
                    if (arcOffset != 0)
                        yield return new Vector2(tile.X + swingOffset, tile.Y - 1 - arcOffset);
                    if (currentAnimationIndex == 2)
                        for (int i = -1; i > -4; i--)
                            yield return new Vector2(tile.X, tile.Y + i);
                    break;
                case 1:
                    // xxx... 0
                    // .xxxx. 1
                    // ..xxxx 2
                    // ..Fxxx 3
                    // ..xxxx 3
                    // .xxxx. 4
                    // xxx... 5
                    swingOffset = -3 + swingOffset;
                    for (int i = -2; i < 1; i++)
                        yield return new Vector2(tile.X + i + arcOffset, tile.Y + swingOffset);
                    if (arcOffset != 0)
                        yield return new Vector2(tile.X + 1 + arcOffset, tile.Y + swingOffset);
                    if (currentAnimationIndex == 2)
                        for (int i = 1; i < 4; i++)
                            yield return new Vector2(tile.X + i, tile.Y);
                    break;
                case 2:
                    // x.....x
                    // xx...xx
                    // xxxFxxx
                    // .xxxxx.
                    // .xxxxx.
                    // ..xxx..
                    // 5433210
                    swingOffset = 3 - swingOffset;
                    for (int i = -2; i < 1; i++)
                        yield return new Vector2(tile.X + swingOffset, tile.Y + i + arcOffset);
                    if (arcOffset != 0)
                        yield return new Vector2(tile.X + swingOffset, tile.Y + 1 + arcOffset);
                    if (currentAnimationIndex == 2)
                        for (int i = 1; i < 4; i++)
                            yield return new Vector2(tile.X, tile.Y + i);
                    break;
                case 3:
                    // ...xxx 0
                    // .xxxx. 1
                    // xxxx.. 2
                    // xxxF.. 3
                    // xxxx.. 3
                    // .xxxx. 4
                    // ...xxx 5
                    swingOffset = -3 + swingOffset;
                    for (int i = 2; i > -1; i--)
                        yield return new Vector2(tile.X + i - arcOffset, tile.Y + swingOffset);
                    if (arcOffset != 0)
                        yield return new Vector2(tile.X - 1 - arcOffset, tile.Y + swingOffset);
                    if (currentAnimationIndex == 2)
                        for (int i = -1; i > -4; i--)
                            yield return new Vector2(tile.X + i, tile.Y);
                    break;
            }

        }
    }
}
