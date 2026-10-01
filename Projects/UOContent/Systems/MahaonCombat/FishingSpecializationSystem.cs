using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonCombat;

public enum FishingSpecialization : byte
{
    Shallow = 0, // Рыбак мелководья — trains fishing outside deep water
    Deep = 1     // Рыбак глубин — trains fishing in deep water (SpecialFishingNet.FullValidation)
}

/// <summary>
///     Split by deep-vs-shallow water — a real, already-existing distinction in
///     Fishing.cs (SpecialFishingNet.FullValidation), not a guessed category. Bonus:
///     chance at an extra fish in the same catch, scaled by trained value + breadth —
///     same "stackable item, bonus Amount" pattern as Alchemy/Inscription, since fish
///     don't have a "potency" number to inflate honestly.
/// </summary>
public static class FishingSpecializationSystem
{
    private static readonly Dictionary<(Mobile, FishingSpecialization), double> Value = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, FishingSpecialization spec) =>
        Value.GetValueOrDefault((m, spec), 0.0);

    private static double GetBreadthBonus(Mobile m, FishingSpecialization active)
    {
        var other = active == FishingSpecialization.Shallow ? FishingSpecialization.Deep : FishingSpecialization.Shallow;
        return Math.Min(BreadthCap, GetValue(m, other) / BreadthUnitsPerPercent);
    }

    /// <summary>Call from Fishing.OnHarvestFinished when the catch is a plain stackable
    /// Fish — deepWater comes from the same SpecialFishingNet.FullValidation check
    /// Fishing.cs already runs elsewhere, passed in rather than re-derived here.</summary>
    public static void OnFishCaught(Mobile from, Item caught, bool deepWater)
    {
        if (caught is not Fish fish || !fish.Stackable)
        {
            return;
        }

        var spec = deepWater ? FishingSpecialization.Deep : FishingSpecialization.Shallow;
        var key = (from, spec);
        var current = Value.GetValueOrDefault(key, 0.0);

        var cap = MahaonMasteryCapSystem.GetCap(from, RuSpecializationName(spec));
        if (current < cap && Utility.RandomDouble() <= GainChance)
        {
            var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
            var newValue = Math.Min(cap, current + gain);
            Value[key] = newValue;
            MahaonSkillTree.NotifyChanged(from);
            MahaonSkillTree.AnnounceGain(from, RuSpecializationName(spec), newValue - current, newValue);
        }

        var value = GetValue(from, spec);
        var breadth = GetBreadthBonus(from, spec);
        var bonusChance = (value / 100.0 * 20.0 + breadth) / 100.0; // up to 20% at 100 trained, plus breadth

        if (Utility.RandomDouble() < bonusChance)
        {
            fish.Amount += 1;
        }
    }

    public static string RuSpecializationName(FishingSpecialization spec) => spec switch
    {
        FishingSpecialization.Shallow => "Рыбак мелководья",
        FishingSpecialization.Deep    => "Рыбак глубин",
        _                               => spec.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonFishingSpecialization", 1)
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
            MahaonSpecializationPersistenceHelper.Read(reader, Value, r => (FishingSpecialization)r.ReadByte());
        }
    }
}
