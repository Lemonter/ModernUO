using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Cooking's real adjustable output: Food.FillFactor — genuinely ties into
///     Systems.MahaonCombat.HungerSystem from earlier this session (bigger FillFactor
///     satisfies hunger more per bite), a real synergy, not a made-up number.
/// </summary>
public static class CookingSpecializationSystem
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

    public static void OnItemCrafted(Mobile from, Item item, CraftSystem craftSystem)
    {
        if (craftSystem is not DefCooking || item is not Food food)
        {
            return;
        }

        var current = Value.GetValueOrDefault(from, 0.0);

        var cap = MahaonMasteryCapSystem.GetCap(from, RuSpecializationName);
        if (current < cap && Utility.RandomDouble() <= GainChance)
        {
            var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
            var newValue = Math.Min(cap, current + gain);
            Value[from] = newValue;
            MahaonSkillTree.NotifyChanged(from);
            MahaonSkillTree.AnnounceGain(from, RuSpecializationName, newValue - current, newValue);
        }

        var value = GetValue(from);

        // +1 FillFactor per 34 points trained, capped at +3 — a real, if modest, boost to
        // how much a single serving fills the eater up.
        var bonus = (int)(value / 34.0);

        if (bonus > 0)
        {
            food.FillFactor += Math.Min(3, bonus);
        }
    }

    public const string RuSpecializationName = "Мастер-повар";

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonCookingSpecialization", 1)
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
