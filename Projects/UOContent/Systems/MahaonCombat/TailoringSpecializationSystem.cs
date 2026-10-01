using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;

namespace Server.Systems.MahaonCombat;

public enum TailoringSpecialization : byte
{
    LeatherArmorer = 0, // Мастер кожаных доспехов — trains crafting Leather-material armor
    Clothier = 1        // Мастер одежды — trains crafting cloth clothing
}

/// <summary>
///     Same shape as BlacksmithSpecializationSystem — hooked into the identical two live
///     CraftItem.CompleteCraft call sites, filtered to DefTailoring specifically so a
///     Blacksmith-forged item never counts here and vice versa. Split matches what
///     Tailoring actually produces in this codebase (checked against DefTailoring.cs's
///     real craft list, not assumed) — real leather ARMOR (BaseArmor) vs decorative cloth
///     CLOTHING (BaseClothing); no bags/containers are craftable via Tailoring here, so
///     no "bag master" specialization exists.
/// </summary>
public static class TailoringSpecializationSystem
{
    private static readonly Dictionary<(Mobile, TailoringSpecialization), double> Value = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, TailoringSpecialization spec) =>
        Value.GetValueOrDefault((m, spec), 0.0);

    private static void Train(Mobile m, TailoringSpecialization spec)
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

    private static double GetBreadthBonus(Mobile m, TailoringSpecialization active)
    {
        var total = 0.0;

        for (var s = 0; s < 2; s++)
        {
            var spec = (TailoringSpecialization)s;

            if (spec == active)
            {
                continue;
            }

            total += GetValue(m, spec);
        }

        return Math.Min(BreadthCap, total / BreadthUnitsPerPercent);
    }

    private static TailoringSpecialization? SpecializationOf(Item item) => item switch
    {
        BaseArmor    => TailoringSpecialization.LeatherArmorer, // BaseShield isn't Tailoring's craft, no conflict here
        BaseClothing => TailoringSpecialization.Clothier,
        _            => null
    };

    /// <summary>Called from both live CraftItem.CompleteCraft overloads, same as
    /// BlacksmithSpecializationSystem — craftSystem filters to Tailoring specifically.</summary>
    public static void OnItemCrafted(Mobile from, Item item, CraftSystem craftSystem)
    {
        if (craftSystem is not DefTailoring)
        {
            return;
        }

        var spec = SpecializationOf(item);

        if (spec == null)
        {
            return;
        }

        Train(from, spec.Value);

        var value = GetValue(from, spec.Value);
        var breadth = GetBreadthBonus(from, spec.Value);
        var bonusPercent = value / 100.0 * 20.0 + breadth; // up to +20% at 100 trained, plus breadth

        if (bonusPercent <= 0)
        {
            return;
        }

        switch (item)
        {
            case BaseArmor armor when armor.MaxHitPoints > 0:
                var newArmorMax = (int)(armor.MaxHitPoints * (1.0 + bonusPercent / 100.0));
                armor.MaxHitPoints = newArmorMax;
                armor.HitPoints = newArmorMax;
                break;

            case BaseClothing clothing when clothing.MaxHitPoints > 0:
                var newClothMax = (int)(clothing.MaxHitPoints * (1.0 + bonusPercent / 100.0));
                clothing.MaxHitPoints = newClothMax;
                clothing.HitPoints = newClothMax;
                break;
        }
    }

    public static string RuSpecializationName(TailoringSpecialization spec) => spec switch
    {
        TailoringSpecialization.LeatherArmorer => "Мастер кожаных доспехов",
        TailoringSpecialization.Clothier       => "Мастер одежды",
        _                                       => spec.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonTailoringSpecialization", 1)
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
            MahaonSpecializationPersistenceHelper.Read(reader, Value, r => (TailoringSpecialization)r.ReadByte());
        }
    }
}
