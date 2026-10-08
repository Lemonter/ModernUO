using System;
using System.Collections.Generic;
using Server.Spells.Bushido;
using Server.Spells.Ninjitsu;

namespace Server.Systems.MahaonCombat;

public enum MartialTechnique : byte
{
    // Бусидо — offensive moves confirmed to resolve through BaseWeapon.OnHit's
    // percentageBonus chain (same hook WeaponStyleSystem already uses). Confidence/
    // CounterAttack/Evasion are defensive procs on a DIFFERENT mechanism (triggered by
    // being attacked, not by attacking) and aren't covered here.
    LightningStrike = 0,
    MomentumStrike = 1,
    HonorableExecution = 2,

    // Ниндзюцу — same reasoning; MirrorImage/ShadowJump/AnimalForm are utility/escape
    // moves, not on-hit damage techniques, and aren't covered here.
    Backstab = 3,
    DeathStrike = 4,
    FocusAttack = 5,
    KiAttack = 6,
    SurpriseAttack = 7
}

/// <summary>
///     Not a "school" (these aren't Spell-derived, don't go through Spell.cs at all —
///     SamuraiMove/NinjaMove are a separate SpecialMove-based system) and not really a
///     "style" either (all 8 stay simultaneously available — Bushido/Ninjitsu players
///     pick a move per-swing based on situation, not one permanent active choice). This
///     is closer to per-technique mastery: each of the 8 real offensive moves trains
///     independently from actually landing it, with a breadth bonus across the SAME
///     skill's other moves (Bushido techniques share breadth with each other, Ninjitsu
///     with each other — not cross-skill, since a samurai's foot-work doesn't help a
///     ninja's).
/// </summary>
public static class MartialTechniqueSystem
{
    private static readonly Dictionary<(Mobile, MartialTechnique), double> Value = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static readonly MartialTechnique[] BushidoGroup = { MartialTechnique.LightningStrike, MartialTechnique.MomentumStrike, MartialTechnique.HonorableExecution };

    private static readonly MartialTechnique[] NinjitsuGroup =
    {
        MartialTechnique.Backstab, MartialTechnique.DeathStrike, MartialTechnique.FocusAttack,
        MartialTechnique.KiAttack, MartialTechnique.SurpriseAttack
    };

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, MartialTechnique tech) => Value.GetValueOrDefault((m, tech), 0.0);

    private static void Train(Mobile m, MartialTechnique tech)
    {
        var key = (m, tech);
        var current = Value.GetValueOrDefault(key, 0.0);

        var cap = MahaonMasteryCapSystem.GetCap(m, RuTechniqueName(tech));
        if (current < cap && Utility.RandomDouble() <= GainChance)
        {
            var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
            var newValue = Math.Min(cap, current + gain);
            Value[key] = newValue;
            MahaonSkillTree.NotifyChanged(m);
            MahaonSkillTree.AnnounceGain(m, RuTechniqueName(tech), newValue - current, newValue);
        }
    }

    private static double GetBreadthBonus(Mobile m, MartialTechnique active)
    {
        var group = Array.IndexOf(BushidoGroup, active) >= 0 ? BushidoGroup : NinjitsuGroup;
        var total = 0.0;

        foreach (var tech in group)
        {
            if (tech != active)
            {
                total += GetValue(m, tech);
            }
        }

        return Math.Min(BreadthCap, total / BreadthUnitsPerPercent);
    }

    private static MartialTechnique? TechniqueOf(object move) => move switch
    {
        LightningStrike => MartialTechnique.LightningStrike,
        MomentumStrike => MartialTechnique.MomentumStrike,
        HonorableExecution => MartialTechnique.HonorableExecution,
        Backstab => MartialTechnique.Backstab,
        DeathStrike => MartialTechnique.DeathStrike,
        FocusAttack => MartialTechnique.FocusAttack,
        KiAttack => MartialTechnique.KiAttack,
        SurpriseAttack => MartialTechnique.SurpriseAttack,
        _ => null
    };

    /// <summary>Call from BaseWeapon's percentageBonus chain, right alongside
    /// WeaponStyleSystem — attacker's SpecialMove.GetCurrentMove(attacker) passed
    /// straight through, no re-lookup needed. Trains AND returns the bonus in one call
    /// since (unlike weapon styles) there's no separate "select" step to train on.</summary>
    public static int OnTechniqueLanded(Mobile attacker, object currentMove)
    {
        var tech = TechniqueOf(currentMove);

        if (tech == null)
        {
            return 0;
        }

        Train(attacker, tech.Value);

        var value = GetValue(attacker, tech.Value);
        var breadth = GetBreadthBonus(attacker, tech.Value);

        return (int)(value / 100.0 * 15.0 + breadth); // up to +15% at 100 trained, plus breadth
    }

    public static string RuTechniqueName(MartialTechnique tech) => tech switch
    {
        MartialTechnique.LightningStrike     => "Молниеносный удар",
        MartialTechnique.MomentumStrike      => "Удар инерции",
        MartialTechnique.HonorableExecution  => "Достойная казнь",
        MartialTechnique.Backstab            => "Удар в спину",
        MartialTechnique.DeathStrike         => "Смертельный удар",
        MartialTechnique.FocusAttack         => "Сфокусированная атака",
        MartialTechnique.KiAttack            => "Атака ки",
        MartialTechnique.SurpriseAttack      => "Внезапная атака",
        _                                      => tech.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonMartialTechnique", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            MahaonSpecializationPersistenceHelper.Write(writer, Value, (w, key) => w.Write((byte)key));
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version
            MahaonSpecializationPersistenceHelper.Read(reader, Value, r => (MartialTechnique)r.ReadByte());
        }
    }
}
