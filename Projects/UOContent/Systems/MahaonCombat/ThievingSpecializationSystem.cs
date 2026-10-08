using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonCombat;

public enum ThievingSpecialization : byte
{
    Pickpocket = 0, // Карманник — trains stealing from a live target (not containers/corpses)
    Locksmith = 1,  // Взломщик — trains successful lockpicking
    Shadow = 2,     // Тень — trains successful Stealth movement
    Poisoner = 3,   // Отравитель — trains successful poison application
    Snoop = 4       // Ищейка — trains successful Snooping
}

/// <summary>
///     Same shape as MagerySchoolSystem/BlacksmithSpecializationSystem — passive, no
///     "pick one active" step, trains automatically based on which thieving action you
///     actually perform successfully. Each of the 5 hooks into a REAL success point in
///     the underlying skill file (Stealing.cs/LockableContainer.cs/Stealth.cs/
///     Poisoning.cs/Snooping.cs) rather than a shared chokepoint — unlike combat/magic/
///     crafting, these 5 skills don't share one central "resolve" method, so each gets
///     its own single-line hook at its own real success point instead.
/// </summary>
public static class ThievingSpecializationSystem
{
    private static readonly Dictionary<(Mobile, ThievingSpecialization), double> Value = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, ThievingSpecialization spec) =>
        Value.GetValueOrDefault((m, spec), 0.0);

    public static void Train(Mobile m, ThievingSpecialization spec)
    {
        var key = (m, spec);
        var current = Value.GetValueOrDefault(key, 0.0);

        var cap = MahaonMasteryCapSystem.GetCap(m, RuSpecializationName(spec));
        if (current >= cap || Utility.RandomDouble() > GainChance)
        {
            return;
        }

        var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
        var newValue = Math.Min(cap, current + gain);
        Value[key] = newValue;
        MahaonSkillTree.NotifyChanged(m);
        MahaonSkillTree.AnnounceGain(m, RuSpecializationName(spec), newValue - current, newValue);
    }

    public static double GetBreadthBonus(Mobile m, ThievingSpecialization active)
    {
        var total = 0.0;

        for (var s = 0; s < 5; s++)
        {
            var spec = (ThievingSpecialization)s;

            if (spec == active)
            {
                continue;
            }

            total += GetValue(m, spec);
        }

        return Math.Min(BreadthCap, total / BreadthUnitsPerPercent);
    }

    /// <summary>Widens a CheckTargetSkill-style success window — used by Карманник
    /// (Stealing from a live target) and Ищейка (Snooping). Returns how many points to
    /// SUBTRACT from the low end of the check range (easier low roll = higher success
    /// chance), scaled by trained value + breadth.</summary>
    public static double GetSkillWindowBonus(Mobile m, ThievingSpecialization spec)
    {
        var value = GetValue(m, spec);
        var breadth = GetBreadthBonus(m, spec);
        return value / 100.0 * 15.0 + breadth; // up to 15 points easier at 100 trained, plus breadth
    }

    /// <summary>Взломщик — chance to skip a lock's trap entirely even if one would have
    /// fired, scaled by trained value + breadth.</summary>
    public static bool TryBypassTrap(Mobile m)
    {
        var value = GetValue(m, ThievingSpecialization.Locksmith);
        var breadth = GetBreadthBonus(m, ThievingSpecialization.Locksmith);
        var chance = (value / 100.0 * 30.0 + breadth) / 100.0; // up to 30% at 100 trained, plus breadth

        return Utility.RandomDouble() < chance;
    }

    /// <summary>Отравитель — reduces the chance of the "grave mistake" self-poison
    /// backfire on a failed poison application, scaled by trained value + breadth.</summary>
    public static double GetSelfPoisonReduction(Mobile m)
    {
        var value = GetValue(m, ThievingSpecialization.Poisoner);
        var breadth = GetBreadthBonus(m, ThievingSpecialization.Poisoner);
        return Math.Min(80.0, value / 100.0 * 50.0 + breadth) / 100.0; // up to 80% reduction at 100 trained, plus breadth
    }

    /// <summary>Тень — extra sneaking steps before Stealth needs to re-check, scaled by
    /// trained value + breadth.</summary>
    public static int GetExtraStealthSteps(Mobile m)
    {
        var value = GetValue(m, ThievingSpecialization.Shadow);
        var breadth = GetBreadthBonus(m, ThievingSpecialization.Shadow);
        return (int)(value / 100.0 * 5.0 + breadth / 2.0); // up to 5 extra steps at 100 trained, plus breadth
    }

    public static string RuSpecializationName(ThievingSpecialization spec) => spec switch
    {
        ThievingSpecialization.Pickpocket => "Карманник",
        ThievingSpecialization.Locksmith  => "Взломщик",
        ThievingSpecialization.Shadow     => "Тень",
        ThievingSpecialization.Poisoner   => "Отравитель",
        ThievingSpecialization.Snoop      => "Ищейка",
        _                                  => spec.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonThievingSpecialization", 1)
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
            MahaonSpecializationPersistenceHelper.Read(reader, Value, r => (ThievingSpecialization)r.ReadByte());
        }
    }
}
