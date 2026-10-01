using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonCombat;

public enum TamingSpecialization : byte
{
    CommonBeasts = 0, // Укротитель обычных зверей — ControlSlots 1-2
    MightyBeasts = 1  // Укротитель могучих тварей — ControlSlots 3+
}

/// <summary>
///     Split by BaseCreature.ControlSlots (a real, verifiable difficulty tier already in
///     the codebase) rather than guessing at creature-type categories by name. Bonus:
///     reduces the skill-scaling penalty a freshly tamed creature takes on its first tame
///     (AnimalTaming.ScaleSkills call) — a tamer specialized in the creature's power tier
///     keeps more of what made it worth taming in the first place.
/// </summary>
public static class TamingSpecializationSystem
{
    private static readonly Dictionary<(Mobile, TamingSpecialization), double> Value = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, TamingSpecialization spec) =>
        Value.GetValueOrDefault((m, spec), 0.0);

    private static TamingSpecialization SpecializationFor(int controlSlots) =>
        controlSlots >= 3 ? TamingSpecialization.MightyBeasts : TamingSpecialization.CommonBeasts;

    private static double GetBreadthBonus(Mobile m, TamingSpecialization active)
    {
        var other = active == TamingSpecialization.CommonBeasts
            ? TamingSpecialization.MightyBeasts
            : TamingSpecialization.CommonBeasts;

        return Math.Min(BreadthCap, GetValue(m, other) / BreadthUnitsPerPercent);
    }

    /// <summary>Call right after a successful first tame (BaseCreature.Owners.Count == 0
    /// branch in AnimalTaming.cs, before ScaleSkills runs) — trains and returns how much
    /// to ADD to the normal scale factor (e.g. 0.86 -> 0.86 + bonus), capped so it can
    /// never exceed 1.0 (full, unscaled skills).</summary>
    public static double OnTamed(Mobile tamer, int controlSlots)
    {
        var spec = SpecializationFor(controlSlots);
        var key = (tamer, spec);
        var current = Value.GetValueOrDefault(key, 0.0);

        var cap = MahaonMasteryCapSystem.GetCap(tamer, RuSpecializationName(spec));
        if (current < cap && Utility.RandomDouble() <= GainChance)
        {
            var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
            var newValue = Math.Min(cap, current + gain);
            Value[key] = newValue;
            MahaonSkillTree.NotifyChanged(tamer);
            MahaonSkillTree.AnnounceGain(tamer, RuSpecializationName(spec), newValue - current, newValue);
        }

        var value = GetValue(tamer, spec);
        var breadth = GetBreadthBonus(tamer, spec);

        return (value / 100.0 * 0.10 + breadth / 100.0); // up to +0.10 scale at 100 trained, plus breadth
    }

    public static string RuSpecializationName(TamingSpecialization spec) => spec switch
    {
        TamingSpecialization.CommonBeasts => "Укротитель обычных зверей",
        TamingSpecialization.MightyBeasts => "Укротитель могучих тварей",
        _                                   => spec.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonTamingSpecialization", 1)
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
            MahaonSpecializationPersistenceHelper.Read(reader, Value, r => (TamingSpecialization)r.ReadByte());
        }
    }
}
