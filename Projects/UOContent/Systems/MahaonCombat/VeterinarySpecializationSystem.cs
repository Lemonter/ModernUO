using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonCombat;

public enum VeterinarySpecialization : byte
{
    LivingHealer = 0, // Целитель живых питомцев — trains bandaging a living animal
    Resurrector = 1   // Воскреситель — trains successfully resurrecting a dead pet
}

/// <summary>
///     Hooked into the two real branches Bandage.cs already distinguishes (petPatient?.
///     IsDeadPet check) — living-pet healing gets a bonus to the actual Heal() amount,
///     resurrection gets a widened success chance on the existing roll.
/// </summary>
public static class VeterinarySpecializationSystem
{
    private static readonly Dictionary<(Mobile, VeterinarySpecialization), double> Value = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, VeterinarySpecialization spec) =>
        Value.GetValueOrDefault((m, spec), 0.0);

    private static double GetBreadthBonus(Mobile m, VeterinarySpecialization active)
    {
        var other = active == VeterinarySpecialization.LivingHealer
            ? VeterinarySpecialization.Resurrector
            : VeterinarySpecialization.LivingHealer;

        return Math.Min(BreadthCap, GetValue(m, other) / BreadthUnitsPerPercent);
    }

    private static void Train(Mobile m, VeterinarySpecialization spec)
    {
        var key = (m, spec);
        var current = Value.GetValueOrDefault(key, 0.0);

        var cap = MahaonMasteryCapSystem.GetCap(m, RuSpecializationName(spec));
        if (current < cap && Utility.RandomDouble() <= GainChance)
        {
            var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
            var newValue = Math.Min(cap, current + gain);
            Value[key] = newValue;
            MahaonSkillTree.NotifyChanged(m);
            MahaonSkillTree.AnnounceGain(m, RuSpecializationName(spec), newValue - current, newValue);
        }
    }

    /// <summary>Call right before Patient.Heal() when healing a living animal — returns
    /// a flat amount to ADD to toHeal, scaled by trained value + breadth.</summary>
    public static double OnLivingPetHealed(Mobile healer)
    {
        Train(healer, VeterinarySpecialization.LivingHealer);

        var value = GetValue(healer, VeterinarySpecialization.LivingHealer);
        var breadth = GetBreadthBonus(healer, VeterinarySpecialization.LivingHealer);

        return value / 100.0 * 6.0 + breadth / 2.0; // up to +6 healed at 100 trained, plus breadth
    }

    /// <summary>Read-only — call BEFORE rolling the resurrection chance to get the bonus
    /// (0.0-1.0 scale) to add. Does NOT train; call TrainResurrector separately only if
    /// the resurrection actually succeeds, so training matches genuine success like every
    /// other Mahaon specialization, not just an attempt.</summary>
    public static double GetResurrectionChanceBonus(Mobile healer)
    {
        var value = GetValue(healer, VeterinarySpecialization.Resurrector);
        var breadth = GetBreadthBonus(healer, VeterinarySpecialization.Resurrector);

        return (value / 100.0 * 15.0 + breadth) / 100.0; // up to +15% at 100 trained, plus breadth
    }

    public static void TrainResurrector(Mobile healer) => Train(healer, VeterinarySpecialization.Resurrector);

    public static string RuSpecializationName(VeterinarySpecialization spec) => spec switch
    {
        VeterinarySpecialization.LivingHealer => "Целитель живых питомцев",
        VeterinarySpecialization.Resurrector  => "Воскреситель",
        _                                       => spec.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonVeterinarySpecialization", 1)
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
            MahaonSpecializationPersistenceHelper.Read(reader, Value, r => (VeterinarySpecialization)r.ReadByte());
        }
    }
}
