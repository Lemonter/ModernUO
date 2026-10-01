using System;
using System.Collections.Generic;
using Server.Spells.Spellweaving;

namespace Server.Systems.MahaonCombat;

public enum SpellweavingSchool : byte
{
    Damage = 0,         // EssenceOfWind/NatureFury/Thunderstorm/WordOfDeath
    Healing = 1,        // GiftOfLife/GiftOfRenewal
    Summoning = 2,       // SummonFey/SummonFiend
    Transformation = 3, // EtherealVoyage/ReaperForm
    WeaponEnchant = 4   // AttuneWeapon/ImmolatingWeapon/ArcaneCircle
}

/// <summary>Same shape as NecromancySchoolSystem/ChivalrySchoolSystem — hooked into the
/// same shared Spell.cs cast-completion point. ArcaneForm and ArcaneSummon&lt;T&gt; are
/// abstract base classes (checked directly), so categorization matches the concrete
/// derived spells (EtherealVoyageSpell/ReaperFormSpell, SummonFeySpell/SummonFiendSpell)
/// instead.</summary>
public static class SpellweavingSchoolSystem
{
    private static readonly Dictionary<(Mobile, SpellweavingSchool), double> SchoolValue = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, SpellweavingSchool school) => SchoolValue.GetValueOrDefault((m, school), 0.0);

    private static void Train(Mobile m, SpellweavingSchool school)
    {
        var key = (m, school);
        var current = SchoolValue.GetValueOrDefault(key, 0.0);

        var cap = MahaonMasteryCapSystem.GetCap(m, RuSchoolName(school));
        if (current < cap && Utility.RandomDouble() <= GainChance)
        {
            var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
            var newValue = Math.Min(cap, current + gain);
            SchoolValue[key] = newValue;
            MahaonSkillTree.NotifyChanged(m);
            MahaonSkillTree.AnnounceGain(m, RuSchoolName(school), newValue - current, newValue);
        }
    }

    private static double GetBreadthBonus(Mobile m, SpellweavingSchool active)
    {
        var total = 0.0;

        for (var s = 0; s < 5; s++)
        {
            var school = (SpellweavingSchool)s;

            if (school == active)
            {
                continue;
            }

            total += GetValue(m, school);
        }

        return Math.Min(BreadthCap, total / BreadthUnitsPerPercent);
    }

    private static SpellweavingSchool? SchoolOf(ISpell spell) => spell switch
    {
        EssenceOfWindSpell or NatureFurySpell or ThunderstormSpell or WordOfDeathSpell => SpellweavingSchool.Damage,
        GiftOfLifeSpell or GiftOfRenewalSpell => SpellweavingSchool.Healing,
        SummonFeySpell or SummonFiendSpell => SpellweavingSchool.Summoning,
        EtherealVoyageSpell or ReaperFormSpell => SpellweavingSchool.Transformation,
        AttuneWeaponSpell or ImmolatingWeaponSpell or ArcaneCircleSpell => SpellweavingSchool.WeaponEnchant,
        _ => null
    };

    public static void OnSpellCast(Mobile caster, ISpell spell)
    {
        var school = SchoolOf(spell);

        if (school != null)
        {
            Train(caster, school.Value);
        }
    }

    public static double GetManaCostScalar(Mobile caster, ISpell spell)
    {
        var school = SchoolOf(spell);

        if (school == null)
        {
            return 1.0;
        }

        var value = GetValue(caster, school.Value);
        var breadth = GetBreadthBonus(caster, school.Value);

        var discount = Math.Min(35.0, value / 100.0 * 20.0 + breadth) / 100.0;
        return 1.0 - discount;
    }

    public static double GetDamageScalar(Mobile caster, ISpell spell)
    {
        if (SchoolOf(spell) != SpellweavingSchool.Damage)
        {
            return 1.0;
        }

        var value = GetValue(caster, SpellweavingSchool.Damage);
        var breadth = GetBreadthBonus(caster, SpellweavingSchool.Damage);

        return 1.0 + (value / 100.0 * 18.0 + breadth) / 100.0;
    }

    public static string RuSchoolName(SpellweavingSchool school) => school switch
    {
        SpellweavingSchool.Damage         => "Школа стихийной ярости",
        SpellweavingSchool.Healing        => "Школа исцеления природой",
        SpellweavingSchool.Summoning      => "Школа призыва фей",
        SpellweavingSchool.Transformation => "Школа перевоплощения",
        SpellweavingSchool.WeaponEnchant  => "Школа зачарования оружия",
        _                                   => school.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonSpellweavingSchool", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            MahaonSpecializationPersistenceHelper.Write(writer, SchoolValue, (w, key) => w.Write((byte)key));
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version
            MahaonSpecializationPersistenceHelper.Read(reader, SchoolValue, r => (SpellweavingSchool)r.ReadByte());
        }
    }
}
