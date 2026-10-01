using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Checked against DefCarpentry.cs's real craft list before writing this — Carpentry
///     here is mostly furniture (chairs, tables, decorations), which turned out to have
///     NO durability system at all (WoodenChair etc derive straight from Item, no
///     MaxHitPoints) — nothing real to hook a bonus into, so no "furniture master"
///     specialization exists rather than inventing a fake one. The only Carpentry output
///     with genuine durability is weapons/shields (QuarterStaff, WoodenShield), so that's
///     the one real specialization here — single-specialization systems are fine, no
///     rule says every craft needs multiple.
/// </summary>
public static class CarpentrySpecializationSystem
{
    private static readonly Dictionary<Mobile, double> Value = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m) => Value.GetValueOrDefault(m, 0.0);

    public static void OnItemCrafted(Mobile from, Item item, CraftSystem craftSystem)
    {
        if (craftSystem is not DefCarpentry)
        {
            return;
        }

        // Only weapons (QuarterStaff) and shields (WoodenShield, via BaseShield : BaseArmor)
        // have real durability to boost — everything else Carpentry makes is furniture
        // with no MaxHitPoints system, so it just doesn't match either case below.
        int maxHp;

        switch (item)
        {
            case BaseWeapon weapon when weapon.MaxHitPoints > 0:
                maxHp = weapon.MaxHitPoints;
                break;
            case BaseArmor armor when armor.MaxHitPoints > 0: // covers BaseShield too
                maxHp = armor.MaxHitPoints;
                break;
            default:
                return;
        }

        var current = Value.GetValueOrDefault(from, 0.0);

        var cap = MahaonMasteryCapSystem.GetCap(from, RuSpecializationName);
        if (current < cap && Utility.RandomDouble() <= GainChance)
        {
            var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
            var newValue = Math.Min(cap, current + gain);
            Value[from] = newValue;
            MahaonSkillTree.NotifyChanged(from);
            MahaonSkillTree.AnnounceGain(from, RuSpecializationName, newValue - current, newValue);
        }

        var value = GetValue(from);
        var bonusPercent = value / 100.0 * 20.0; // up to +20% at 100 trained — no breadth bonus, single specialization

        if (bonusPercent <= 0)
        {
            return;
        }

        var newMax = (int)(maxHp * (1.0 + bonusPercent / 100.0));

        switch (item)
        {
            case BaseWeapon weapon:
                weapon.MaxHitPoints = newMax;
                weapon.HitPoints = newMax;
                break;
            case BaseArmor armor:
                armor.MaxHitPoints = newMax;
                armor.HitPoints = newMax;
                break;
        }
    }

    public const string RuSpecializationName = "Мастер оружия и щитов";

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonCarpentrySpecialization", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            MahaonSpecializationPersistenceHelper.Write(writer, Value);
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version
            MahaonSpecializationPersistenceHelper.Read(reader, Value);
        }
    }
}
