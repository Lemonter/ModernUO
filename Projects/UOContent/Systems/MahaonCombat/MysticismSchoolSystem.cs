using System;
using System.Collections.Generic;
using Server.Spells.Mysticism;

namespace Server.Systems.MahaonCombat;

public enum MysticismSchool : byte
{
    Damage = 0,  // Bombard/EagleStrike/HailStorm/NetherCyclone/SpellPlague
    Utility = 1, // AnimatedWeapon/StoneForm
    Healing = 2  // CleansingWinds
}

/// <summary>Same shape as the other magic-school systems — hooked into the same shared
/// Spell.cs cast-completion point. All 8 real Mysticism spells checked by class name.</summary>
public static class MysticismSchoolSystem
{
    private static readonly Dictionary<(Mobile, MysticismSchool), double> SchoolValue = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, MysticismSchool school) => SchoolValue.GetValueOrDefault((m, school), 0.0);

    private static void Train(Mobile m, MysticismSchool school)
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

    private static double GetBreadthBonus(Mobile m, MysticismSchool active)
    {
        var total = 0.0;

        for (var s = 0; s < 3; s++)
        {
            var school = (MysticismSchool)s;

            if (school == active)
            {
                continue;
            }

            total += GetValue(m, school);
        }

        return Math.Min(BreadthCap, total / BreadthUnitsPerPercent);
    }

    private static MysticismSchool? SchoolOf(ISpell spell) => spell switch
    {
        BombardSpell or EagleStrikeSpell or HailStormSpell or NetherCycloneSpell or SpellPlagueSpell => MysticismSchool.Damage,
        AnimatedWeaponSpell or StoneFormSpell => MysticismSchool.Utility,
        CleansingWindsSpell => MysticismSchool.Healing,
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
        if (SchoolOf(spell) != MysticismSchool.Damage)
        {
            return 1.0;
        }

        var value = GetValue(caster, MysticismSchool.Damage);
        var breadth = GetBreadthBonus(caster, MysticismSchool.Damage);

        return 1.0 + (value / 100.0 * 18.0 + breadth) / 100.0;
    }

    public static string RuSchoolName(MysticismSchool school) => school switch
    {
        MysticismSchool.Damage  => "Школа мистического урона",
        MysticismSchool.Utility => "Школа мистических искусств",
        MysticismSchool.Healing => "Школа очищающих ветров",
        _                         => school.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonMysticismSchool", 1)
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
            MahaonSpecializationPersistenceHelper.Read(reader, SchoolValue, r => (MysticismSchool)r.ReadByte());
        }
    }
}
