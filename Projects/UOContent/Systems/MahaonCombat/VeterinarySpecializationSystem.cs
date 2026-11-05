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
    private static readonly ProgressTable<VeterinarySpecialization> Value = new("branch:veterinary");

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

    /// <summary>
    ///     Воскреситель — эффект при успехе (этап 8, раздел 11.2). Раньше ветка прибавляла до
    ///     +15% к шансу воскрешения, то есть делала работу навыка. Теперь шанс — у навыка, а
    ///     ветка решает, в каком виде питомец вернулся: доля здоровья (до половины на 100) и
    ///     шанс не потерять навыки при воскрешении.
    /// </summary>
    public static double ReviveHitsShare(Mobile healer)
    {
        var value = GetValue(healer, VeterinarySpecialization.Resurrector);
        var breadth = GetBreadthBonus(healer, VeterinarySpecialization.Resurrector);

        return Math.Min(0.6, value / 100.0 * 0.5 + breadth / 100.0);
    }

    /// <summary>Воскрешённый питомец не теряет навыков — шанс равен тренировке Воскресителя.</summary>
    public static bool SparesSkillLoss(Mobile healer) =>
        Utility.RandomDouble() * 100.0 < GetValue(healer, VeterinarySpecialization.Resurrector);

    /// <summary>После удачного воскрешения: вернуть питомцу часть здоровья.</summary>
    public static void OnPetResurrected(Mobile healer, Mobile pet)
    {
        var share = ReviveHitsShare(healer);

        if (share > 0 && pet is { Alive: true })
        {
            pet.Hits = Math.Max(pet.Hits, (int)(pet.HitsMax * share));
        }
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
