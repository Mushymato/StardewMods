using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using StardewValley;
using StardewValley.Tools;
using StardewValley.Monsters;
using StardewValley.GameData.Weapons;

namespace ScytheToolEnchantments.Framework.Enchantments
{
    [XmlType($"Mods_mushymato_{nameof(ReaperEnchantment)}")]
    public class ReaperEnchantment : ScytheEnchantment
    {
        public const string InfinityBladeQID = $"(W)62";

        public override string GetName()
        {
            return I18n.Enchantment_Reaper_Name();
        }

        protected override void _ApplyTo(Item item)
        {
            base._ApplyTo(item);
            if (item is MeleeWeapon weapon)
            {
                // Makes scythe deal infinity blade min damage, done like this because SVE buffs infinity blade
                // WeaponData data = weapon.GetData();
                WeaponData data = (WeaponData)ItemRegistry.GetData(InfinityBladeQID).RawData;
                if (data != null)
                {
                    weapon.minDamage.Value = data.MinDamage * 3 / 4;
                    weapon.maxDamage.Value = data.MinDamage * 3 / 4;
                    weapon.critChance.Value = 0.1f + (data.CritChance - 0.1f);
                }
            }
        }

        protected override void _UnapplyTo(Item item)
        {
            base._UnapplyTo(item);
            if (item is MeleeWeapon weapon)
            {
                WeaponData data = weapon.GetData();
                if (data != null)
                {
                    weapon.minDamage.Value = data.MinDamage;
                    weapon.maxDamage.Value = data.MaxDamage;
                    weapon.critChance.Value = data.CritChance;
                }
            }
        }

        protected override void _OnMonsterSlay(Monster m, GameLocation location, Farmer who)
        {
            base._OnMonsterSlay(m, location, who);
            if (DataLoader.Monsters(Game1.content).TryGetValue(m.Name, out var result))
            {
                Vector2 monsterPosition = Utility.PointToVector2(m.StandingPixel);
                Vector2 playerPosition = Utility.PointToVector2(who.StandingPixel);
                // Get another set of drops, like vanilla burglar's ring
                List<string> objects = new();
                string[] objectsSplit = ArgUtility.SplitBySpace(result);
                for (int l = 0; l < objectsSplit.Length; l += 2)
                {
                    if (Random.Shared.NextDouble() < Convert.ToDouble(objectsSplit[l + 1]))
                    {
                        objects.Add(objectsSplit[l]);
                    }
                }
                // Do these drops
                for (int k = 0; k < objects.Count; k++)
                {
                    string objectToAdd = objects[k];
                    if (objectToAdd != null && objectToAdd.StartsWith('-') && int.TryParse(objectToAdd, out var parsedIndex))
                    {
                        location.debris.Add(m.ModifyMonsterLoot(new Debris(Math.Abs(parsedIndex), Random.Shared.Next(1, 4), monsterPosition, playerPosition)));
                    }
                    else
                    {
                        location.debris.Add(m.ModifyMonsterLoot(new Debris(objectToAdd, monsterPosition, playerPosition)));
                    }
                }
            }
        }
    }
}
