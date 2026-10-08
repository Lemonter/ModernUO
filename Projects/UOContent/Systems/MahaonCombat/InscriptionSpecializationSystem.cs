using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Same shape as AlchemySpecializationSystem — scrolls are stackable, so the bonus
///     is a chance at an extra scroll from the same batch rather than a fake "stronger
///     scroll" number (scroll effect is fixed by which spell it is, nothing to scale).
/// </summary>
public static class InscriptionSpecializationSystem
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
        if (craftSystem is not DefInscription || item is not SpellScroll scroll)
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

        if (scroll.Stackable && Utility.RandomDouble() < bonusChance)
        {
            scroll.Amount += 1;
            from.SendMessage(0x59, "Мастерство писаря даёт дополнительный свиток.");
        }
    }

    public const string RuSpecializationName = "Мастер свитков";

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonInscriptionSpecialization", 1)
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
