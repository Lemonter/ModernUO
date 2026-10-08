using System.Collections.Generic;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Tracks which weapons have been enchanted by the Mahaon magic hammer: extra damage
///     plus a chance to zap the target with a bolt of lightning on hit. Persisted directly
///     (item references survive world save/load fine, unlike a plain in-memory set).
/// </summary>
public class WeaponEnchantment : GenericPersistence
{
    private static WeaponEnchantment _instance;

    private const double LightningProcChance = 0.15;
    private const int LightningDamage = 15;

    private static readonly HashSet<Item> Enchanted = new();

    public WeaponEnchantment() : base("MahaonWeaponEnchantment", 1)
    {
    }

    public static void Configure()
    {
        _instance = new WeaponEnchantment();
    }

    public static bool IsEnchanted(Item weapon) => Enchanted.Contains(weapon);

    public static void Enchant(Item weapon) => Enchanted.Add(weapon);

    /// <summary>
    ///     Percentage damage bonus this weapon's enchantment grants (fits the same
    ///     percentageBonus scale used elsewhere in BaseWeapon.OnHit).
    /// </summary>
    public static int GetDamageBonus(Item weapon) => IsEnchanted(weapon) ? 20 : 0;

    /// <summary>
    ///     Rolls the lightning proc for an enchanted weapon and applies it directly if it
    ///     hits. Called from BaseWeapon.OnHit after normal damage is resolved.
    /// </summary>
    public static void TryLightningProc(Item weapon, Mobile attacker, Mobile defender)
    {
        if (!IsEnchanted(weapon) || Utility.RandomDouble() >= LightningProcChance)
        {
            return;
        }

        defender.BoltEffect(0);
        AOS.Damage(defender, attacker, LightningDamage, 0, 0, 0, 0, 100);
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(Enchanted.Count);

        foreach (var item in Enchanted)
        {
            writer.Write(item);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var item = reader.ReadEntity<Item>();
            if (item != null)
            {
                Enchanted.Add(item);
            }
        }
    }
}

