using System;
using System.Collections.Generic;
using Server.Spells.Chivalry;

namespace Server.Systems.MahaonCombat;

public enum ChivalrySchool : byte
{
    Healing = 0,  // Школа исцеления — CloseWounds/CleanseByFire/RemoveCurse
    Combat = 1,   // Школа воинской доблести — DivineFury/EnemyOfOne/ConsecrateWeapon/HolyLight/DispelEvil
    Sacrifice = 2 // Школа самопожертвования — NobleSacrifice/SacredJourney
}

/// <summary>Same shape as NecromancySchoolSystem — hooked into the same shared Spell.cs
/// cast-completion point, no new hook needed. All 10 real Chivalry spells checked by
/// class name directly.</summary>
public static class ChivalrySchoolSystem
{
    private static readonly Dictionary<(Mobile, ChivalrySchool), double> SchoolValue = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, ChivalrySchool school) => SchoolValue.GetValueOrDefault((m, school), 0.0);

    private static void Train(Mobile m, ChivalrySchool school)
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

    private static double GetBreadthBonus(Mobile m, ChivalrySchool active)
    {
        var total = 0.0;

        for (var s = 0; s < 3; s++)
        {
            var school = (ChivalrySchool)s;

            if (school == active)
            {
                continue;
            }

            total += GetValue(m, school);
        }

        return Math.Min(BreadthCap, total / BreadthUnitsPerPercent);
    }

    private static ChivalrySchool? SchoolOf(ISpell spell) => spell switch
    {
        CloseWoundsSpell or CleanseByFireSpell or RemoveCurseSpell => ChivalrySchool.Healing,
        DivineFurySpell or EnemyOfOneSpell or ConsecrateWeaponSpell or HolyLightSpell or DispelEvilSpell => ChivalrySchool.Combat,
        NobleSacrificeSpell or SacredJourneySpell => ChivalrySchool.Sacrifice,
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

    /// <summary>Bonus for the Healing school specifically — hooked into wherever
    /// Chivalry's own heal amount is calculated (see the call site in CloseWounds.cs/
    /// CleanseByFire.cs).</summary>
    public static double GetHealScalar(Mobile caster)
    {
        var value = GetValue(caster, ChivalrySchool.Healing);
        var breadth = GetBreadthBonus(caster, ChivalrySchool.Healing);

        return 1.0 + (value / 100.0 * 15.0 + breadth) / 100.0;
    }

    public static string RuSchoolName(ChivalrySchool school) => school switch
    {
        ChivalrySchool.Healing   => "Школа исцеления",
        ChivalrySchool.Combat    => "Школа воинской доблести",
        ChivalrySchool.Sacrifice => "Школа самопожертвования",
        _                          => school.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonChivalrySchool", 1)
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
            MahaonSpecializationPersistenceHelper.Read(reader, SchoolValue, r => (ChivalrySchool)r.ReadByte());
        }
    }
}
