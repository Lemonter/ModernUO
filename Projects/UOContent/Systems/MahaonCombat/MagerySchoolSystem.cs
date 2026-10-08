using System;
using System.Collections.Generic;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Third;
using Server.Spells.Fourth;
using Server.Spells.Fifth;
using Server.Spells.Sixth;
using Server.Spells.Seventh;
using Server.Spells.Eighth;

namespace Server.Systems.MahaonCombat;

public enum MagerySchool : byte
{
    Fire = 0,        // Школа огня — trains on casting fire-dominant damage spells
    Destruction = 1, // Школа разрушения — trains on casting other damage spells
    Summoning = 2,   // Школа призыва — trains on casting summon spells
    Holy = 3,        // Святая магия — trains on casting beneficial spells (buffs/heals)
    Control = 4,     // Школа контроля — trains on casting debuff/control spells
    Utility = 5      // Школа иллюзий и путешествий — trains on casting everything else
}

/// <summary>
///     Same shape as WeaponStyleSystem (train through use, one active choice per player,
///     breadth bonus for training the others) but for the single Magery skill — split
///     into 6 real thematic schools instead of one flat number. Unlike combat styles,
///     there's no "pick one to fight with" moment; MOST schools train passively just by
///     casting spells that belong to them (checked once per cast via a single shared hook
///     in Spell.cs, not touched per-spell), whichever school currently has the most
///     relevant bonus active applies automatically based on what's actually being cast.
///
///     Real hooks, not per-spell edits: every spell funnels through ONE OnCast() call
///     site (Spell.cs's internal cast timer) for training, ONE ScaleMana() for the
///     mana-cost bonus, and ONE SpellHelper.Damage() for the Fire/Destruction damage
///     bonus — the same "single chokepoint" pattern BaseWeapon.OnHit used for combat.
/// </summary>
public static class MagerySchoolSystem
{
    private static readonly Dictionary<(Mobile, MagerySchool), double> SchoolValue = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, MagerySchool school) =>
        SchoolValue.GetValueOrDefault((m, school), 0.0);

    private static void Train(Mobile m, MagerySchool school)
    {
        var key = (m, school);
        var current = SchoolValue.GetValueOrDefault(key, 0.0);

        var cap = MahaonMasteryCapSystem.GetCap(m, RuSchoolName(school));
        if (current >= cap || Utility.RandomDouble() > GainChance)
        {
            return;
        }

        var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
        var newValue = Math.Min(cap, current + gain);
        SchoolValue[key] = newValue;
        MahaonSkillTree.NotifyChanged(m);
        MahaonSkillTree.AnnounceGain(m, RuSchoolName(school), newValue - current, newValue);
    }

    private static double GetBreadthBonus(Mobile m, MagerySchool active)
    {
        var total = 0.0;

        for (var s = 0; s < 6; s++)
        {
            var school = (MagerySchool)s;

            if (school == active)
            {
                continue;
            }

            total += GetValue(m, school);
        }

        return Math.Min(BreadthCap, total / BreadthUnitsPerPercent);
    }

    // Categorization by spell class — checked once, here, rather than touching every
    // individual spell file. Every First-Eighth circle Magery spell is covered.
    private static MagerySchool? SchoolOf(ISpell spell) => spell switch
    {
        // Огонь — fire-dominant damage spells
        FireballSpell or FireFieldSpell or FlameStrikeSpell or MeteorSwarmSpell => MagerySchool.Fire,

        // Разрушение — other offensive/damage spells
        MagicArrowSpell or HarmSpell or LightningSpell or EnergyBoltSpell or ChainLightningSpell
            or ExplosionSpell or ManaDrainSpell or MindBlastSpell or EarthquakeSpell => MagerySchool.Destruction,

        // Призыв — summons and elementals
        SummonCreatureSpell or BladeSpiritsSpell or AirElementalSpell or EarthElementalSpell
            or EnergyVortexSpell or FireElementalSpell or WaterElementalSpell or SummonDaemonSpell
            => MagerySchool.Summoning,

        // Святая магия — buffs and healing
        BlessSpell or StrengthSpell or AgilitySpell or CunningSpell or ReactiveArmorSpell
            or ProtectionSpell or ArchProtectionSpell or MagicReflectSpell or HealSpell
            or GreaterHealSpell or ArchCureSpell or CureSpell => MagerySchool.Holy,

        // Контроль — debuffs, paralysis, poison, dispel
        ClumsySpell or FeeblemindSpell or WeakenSpell or CurseSpell or MassCurseSpell
            or ParalyzeSpell or ParalyzeFieldSpell or PoisonSpell or PoisonFieldSpell
            or DispelSpell or MassDispelSpell or DispelFieldSpell => MagerySchool.Control,

        // Иллюзии/утилиты — everything else: travel, detection, illusion, misc
        CreateFoodSpell or NightSightSpell or MagicTrapSpell or RemoveTrapSpell or TelekinesisSpell
            or TeleportSpell or UnlockSpell or MagicLockSpell or WallOfStoneSpell or RecallSpell
            or IncognitoSpell or InvisibilitySpell or MarkSpell or RevealSpell or GateTravelSpell
            or PolymorphSpell or ResurrectionSpell or EnergyFieldSpell => MagerySchool.Utility,

        _ => null
    };

    /// <summary>Called from the single shared spot in Spell.cs right after OnCast()
    /// completes for every spell in the game — non-Magery spells and unrecognized types
    /// just no-op via the null check.</summary>
    public static void OnSpellCast(Mobile caster, ISpell spell)
    {
        var school = SchoolOf(spell);

        if (school != null)
        {
            Train(caster, school.Value);
        }
    }

    /// <summary>Mana-cost discount for whichever school a given spell belongs to — the
    /// one bonus that applies uniformly across all 6 schools regardless of what kind of
    /// spell it is, hooked into Spell.ScaleMana (also shared by every spell).</summary>
    public static double GetManaCostScalar(Mobile caster, ISpell spell)
    {
        var school = SchoolOf(spell);

        if (school == null)
        {
            return 1.0;
        }

        var value = GetValue(caster, school.Value);
        var breadth = GetBreadthBonus(caster, school.Value);

        // Up to 20% off at 100 trained, plus a sliver more from the breadth bonus —
        // capped so mana can never hit zero from this alone.
        var discount = Math.Min(35.0, value / 100.0 * 20.0 + breadth) / 100.0;
        return 1.0 - discount;
    }

    /// <summary>Fire/Destruction damage bonus — hooked into SpellHelper.Damage, the
    /// shared damage-application point every offensive spell in the game funnels through
    /// (Necromancy/Mysticism/Spellweaving/Chivalry/SkillMasteries included, not just
    /// Magery). Guards on SchoolOf(spell) first — unlike its three siblings
    /// (NecromancySchoolSystem/MysticismSchoolSystem/SpellweavingSchoolSystem), this one
    /// used to take a bare bool with no way to check the caster was even casting a Magery
    /// spell, so any character with trained Fire/Destruction got this bonus applied to
    /// every OTHER school's spell damage too. isFireDominant still comes from the caller
    /// checking which elemental split the spell actually passed in (real per-cast data,
    /// not a guess from the spell's class name) — only used once we've confirmed it's
    /// actually a recognized Magery spell.</summary>
    public static double GetDamageScalar(Mobile caster, ISpell spell, bool isFireDominant)
    {
        if (SchoolOf(spell) == null)
        {
            return 1.0;
        }

        var school = isFireDominant ? MagerySchool.Fire : MagerySchool.Destruction;
        var value = GetValue(caster, school);
        var breadth = GetBreadthBonus(caster, school);

        var bonus = value / 100.0 * 18.0 + breadth; // up to +18% at 100 trained, plus breadth
        return 1.0 + bonus / 100.0;
    }

    public static string RuSchoolName(MagerySchool school) => school switch
    {
        MagerySchool.Fire        => "Школа огня",
        MagerySchool.Destruction => "Школа разрушения",
        MagerySchool.Summoning   => "Школа призыва",
        MagerySchool.Holy        => "Святая магия",
        MagerySchool.Control     => "Школа контроля",
        MagerySchool.Utility     => "Школа иллюзий и путешествий",
        _                        => school.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonMagerySchool", 1)
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
            MahaonSpecializationPersistenceHelper.Read(reader, SchoolValue, r => (MagerySchool)r.ReadByte());
        }
    }
}
