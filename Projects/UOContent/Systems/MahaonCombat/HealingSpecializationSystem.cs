using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Same shape as VeterinarySpecializationSystem's LivingHealer half — one
///     specialization, trains on successfully bandaging a HUMAN patient (the
///     Patient.Body.IsMonster/IsAnimal branch is Veterinary's territory, not this one).
///     Two real hooks in the same shared Bandage.cs formula: a small bonus to the
///     success chance itself, and a flat bonus added to the actual heal amount.
/// </summary>
public static class HealingSpecializationSystem
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

    private static void Train(Mobile m)
    {
        var current = Value.GetValueOrDefault(m, 0.0);

        var cap = MahaonMasteryCapSystem.GetCap(m, RuSpecializationName);
        if (current < cap && Utility.RandomDouble() <= GainChance)
        {
            var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
            var newValue = Math.Min(cap, current + gain);
            Value[m] = newValue;
            MahaonSkillTree.NotifyChanged(m);
            MahaonSkillTree.AnnounceGain(m, RuSpecializationName, newValue - current, newValue);
        }
    }

    /// <summary>Read-only — call BEFORE rolling the bandage success chance.</summary>
    public static double GetSuccessChanceBonus(Mobile healer) => GetValue(healer) / 100.0 * 0.10; // up to +10% at 100 trained

    /// <summary>Call once healing has actually succeeded — trains AND returns a flat
    /// amount to add to toHeal, scaled by trained value.</summary>
    public static double OnHumanHealed(Mobile healer)
    {
        Train(healer);
        return GetValue(healer) / 100.0 * 5.0; // up to +5 healed at 100 trained
    }

    public const string RuSpecializationName = "Целитель людей";

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonHealingSpecialization", 1)
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
