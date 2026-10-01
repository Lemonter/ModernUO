using System;
using System.Collections.Generic;
using Server.Spells.Necromancy;

namespace Server.Systems.MahaonCombat;

public enum NecromancySchool : byte
{
    Damage = 0,      // Школа боли — PainSpike/PoisonStrike/Strangle/Wither/BloodOath
    Curse = 1,       // Школа проклятий — CorpseSkin/EvilOmen/MindRot/CurseWeapon
    Summoning = 2,   // Школа призыва — AnimateDead/SummonFamiliar/VengefulSpirit
    Transformation = 3, // Школа перевоплощения — HorrificBeast/LichForm/WraithForm/VampiricEmbrace
    Exorcism = 4     // Школа изгнания — Exorcism (единственное заклинание, но тематически особняком)
}

/// <summary>
///     Same shape as MagerySchoolSystem — passive, no "pick one active" step, trains
///     automatically based on which Necromancy spell you actually cast. Hooked into the
///     SAME shared Spell.cs cast-completion point MagerySchoolSystem already uses (every
///     spell in the game funnels through that one line, not just Magery), so no new hook
///     was needed — just a second categorization table alongside the Magery one.
/// </summary>
public static class NecromancySchoolSystem
{
    private static readonly Dictionary<(Mobile, NecromancySchool), double> SchoolValue = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, NecromancySchool school) => SchoolValue.GetValueOrDefault((m, school), 0.0);

    private static void Train(Mobile m, NecromancySchool school)
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

    private static double GetBreadthBonus(Mobile m, NecromancySchool active)
    {
        var total = 0.0;

        for (var s = 0; s < 5; s++)
        {
            var school = (NecromancySchool)s;

            if (school == active)
            {
                continue;
            }

            total += GetValue(m, school);
        }

        return Math.Min(BreadthCap, total / BreadthUnitsPerPercent);
    }

    private static NecromancySchool? SchoolOf(ISpell spell) => spell switch
    {
        PainSpikeSpell or PoisonStrikeSpell or StrangleSpell or WitherSpell or BloodOathSpell => NecromancySchool.Damage,
        CorpseSkinSpell or EvilOmenSpell or MindRotSpell or CurseWeaponSpell => NecromancySchool.Curse,
        AnimateDeadSpell or SummonFamiliarSpell or VengefulSpiritSpell => NecromancySchool.Summoning,
        HorrificBeastSpell or LichFormSpell or WraithFormSpell or VampiricEmbraceSpell => NecromancySchool.Transformation,
        ExorcismSpell => NecromancySchool.Exorcism,
        _ => null
    };

    /// <summary>Called from the same Spell.cs shared hook MagerySchoolSystem.OnSpellCast
    /// uses — non-Necromancy spells just no-op via the null check.</summary>
    public static void OnSpellCast(Mobile caster, ISpell spell)
    {
        var school = SchoolOf(spell);

        if (school != null)
        {
            Train(caster, school.Value);
        }
    }

    /// <summary>Mana-cost discount, same shape as MagerySchoolSystem's — hooked into the
    /// same Spell.ScaleMana call site alongside it.</summary>
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

    /// <summary>Damage bonus for the Damage school — hooked into SpellHelper.Damage
    /// alongside MagerySchoolSystem's Fire/Destruction bonus, checked via spell TYPE
    /// this time (Necromancy damage spells don't split cleanly by element the way
    /// Magery's do) rather than the elemental-split heuristic.</summary>
    public static double GetDamageScalar(Mobile caster, ISpell spell)
    {
        if (SchoolOf(spell) != NecromancySchool.Damage)
        {
            return 1.0;
        }

        var value = GetValue(caster, NecromancySchool.Damage);
        var breadth = GetBreadthBonus(caster, NecromancySchool.Damage);

        return 1.0 + (value / 100.0 * 18.0 + breadth) / 100.0;
    }

    public static string RuSchoolName(NecromancySchool school) => school switch
    {
        NecromancySchool.Damage         => "Школа боли",
        NecromancySchool.Curse          => "Школа проклятий",
        NecromancySchool.Summoning      => "Школа призыва",
        NecromancySchool.Transformation => "Школа перевоплощения",
        NecromancySchool.Exorcism       => "Школа изгнания",
        _                                 => school.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonNecromancySchool", 1)
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
            MahaonSpecializationPersistenceHelper.Read(reader, SchoolValue, r => (NecromancySchool)r.ReadByte());
        }
    }
}
