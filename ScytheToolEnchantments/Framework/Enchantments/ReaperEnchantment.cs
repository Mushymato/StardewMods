using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Weapons;
using StardewValley.Monsters;
using StardewValley.Tools;

namespace ScytheToolEnchantments.Framework.Enchantments;

[XmlType($"Mods_mushymato_{nameof(ReaperEnchantment)}")]
public class ReaperEnchantment : ScytheEnchantment
{
    public const string InfinityBladeQID = $"(W)62";

    public override string GetName()
    {
        return I18n.Enchantment_Reaper_Name();
    }

    public override bool CanApplyTo(Item item)
    {
        return base.CanApplyTo(item) && ModEntry.Config!.EnableReaper;
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
                weapon.speed.Value = data.Speed;
                weapon.critChance.Value = Math.Max(0.02f, data.CritChance / 2f);
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
                weapon.speed.Value = data.Speed;
                weapon.critChance.Value = data.CritChance;
            }
        }
    }

    public override void OnMonsterSlay(Monster monster, GameLocation location, Farmer who, bool slainByBomb)
    {
        base.OnMonsterSlay(monster, location, who, slainByBomb);
        if (!slainByBomb && DataLoader.Monsters(Game1.content).TryGetValue(monster.Name, out var result))
        {
            Vector2 monsterPosition = Utility.PointToVector2(monster.StandingPixel);
            Vector2 playerPosition = Utility.PointToVector2(who.StandingPixel);
            // Get another set of drops, like vanilla burglar's ring
            List<string> objects = [];
            string[] objectsSplit = ArgUtility.SplitBySpace(result.Split('/')[6]);
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
                if (
                    objectToAdd != null
                    && objectToAdd.StartsWith('-')
                    && int.TryParse(objectToAdd, out var parsedIndex)
                )
                {
                    location.debris.Add(
                        monster.ModifyMonsterLoot(
                            new Debris(Math.Abs(parsedIndex), Random.Shared.Next(1, 4), monsterPosition, playerPosition)
                        )
                    );
                }
                else
                {
                    location.debris.Add(
                        monster.ModifyMonsterLoot(new Debris(objectToAdd, monsterPosition, playerPosition))
                    );
                }
            }
        }
    }
}
