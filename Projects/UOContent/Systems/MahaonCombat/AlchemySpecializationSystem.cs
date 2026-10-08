using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Potion effects are baked into their specific type (LesserHeal vs GreaterHeal are
///     different classes, not one type with a scalar), so there's no clean single
///     "potency" number to boost across all potions honestly. What IS real: potions are
///     stackable (Item.Amount), so the bonus here is a chance at an extra potion from the
///     same batch instead of a fake potency multiplier.
/// </summary>
public static class AlchemySpecializationSystem
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
        if (craftSystem is not DefAlchemy || item is not BasePotion potion)
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
        var bonusChance = value / 100.0 * 0.25; // up to 25% chance at 100 trained

        if (potion.Stackable && Utility.RandomDouble() < bonusChance)
        {
            potion.Amount += 1;
            from.SendMessage(0x59, "Мастерство алхимика даёт дополнительное зелье.");
        }
    }

    public const string RuSpecializationName = "Мастер зелий";

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonAlchemySpecialization", 1)
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
