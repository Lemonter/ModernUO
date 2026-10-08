using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Checked against DefTinkering.cs's real craft list first — mostly component parts
///     (gears, springs, clock parts — no durability, they're just crafting materials for
///     other things) and tools (Scissors etc — no IUsesRemaining/charge system in this
///     codebase to boost either). The one real output with genuine durability is
///     jewelry (rings/earrings, BaseJewel has its own MaxHitPoints), so that's the only
///     specialization here — same "one honest specialization, not a padded set" approach
///     as Carpentry.
/// </summary>
public static class TinkeringSpecializationSystem
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
        if (craftSystem is not DefTinkering || item is not BaseJewel jewel || jewel.MaxHitPoints <= 0)
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
        var bonusPercent = value / 100.0 * 20.0; // up to +20% at 100 trained

        if (bonusPercent <= 0)
        {
            return;
        }

        var newMax = (int)(jewel.MaxHitPoints * (1.0 + bonusPercent / 100.0));
        jewel.MaxHitPoints = newMax;
        jewel.HitPoints = newMax;
    }

    public const string RuSpecializationName = "Ювелир";

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonTinkeringSpecialization", 1)
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
