using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonCombat;

public enum MinorSkill : byte
{
    Forensics = 0, // Судмедэкспертиза
    TasteID = 1,   // Дегустация
    Camping = 2,   // Кемпинг
    ItemID = 3     // Оценка предметов
}

/// <summary>
///     Four otherwise-unrelated skills that all happen to share the exact same real
///     mechanic — a CheckSkill/CheckTargetSkill roll over a fixed 0-100 window — so one
///     shared system instead of four near-identical files. Each is independent (no
///     breadth bonus between them; a Forensics investigator gains nothing from being
///     also good at Camping, there's no thematic link like weapon styles/magic schools
///     had). Bonus: widens the low end of that skill's check window, same pattern as
///     Stealing/Snooping specializations.
/// </summary>
public static class MinorSkillSpecializationSystem
{
    private static readonly Dictionary<(Mobile, MinorSkill), double> Value = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, MinorSkill skill) => Value.GetValueOrDefault((m, skill), 0.0);

    public static void Train(Mobile m, MinorSkill skill)
    {
        var key = (m, skill);
        var current = Value.GetValueOrDefault(key, 0.0);

        var cap = MahaonMasteryCapSystem.GetCap(m, RuSkillName(skill));
        if (current < cap && Utility.RandomDouble() <= GainChance)
        {
            var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
            var newValue = Math.Min(cap, current + gain);
            Value[key] = newValue;
            MahaonSkillTree.NotifyChanged(m);
            MahaonSkillTree.AnnounceGain(m, RuSkillName(skill), newValue - current, newValue);
        }
    }

    /// <summary>Points to subtract from the low end of a CheckSkill/CheckTargetSkill
    /// window — up to 15 at 100 trained, no breadth bonus (see class doc).</summary>
    public static double GetWindowBonus(Mobile m, MinorSkill skill) => GetValue(m, skill) / 100.0 * 15.0;

    public static string RuSkillName(MinorSkill skill) => skill switch
    {
        MinorSkill.Forensics => "Криминалист",
        MinorSkill.TasteID   => "Дегустатор",
        MinorSkill.Camping   => "Следопыт-кострожог",
        MinorSkill.ItemID    => "Оценщик",
        _                     => skill.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonMinorSkillSpecialization", 1)
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
            MahaonSpecializationPersistenceHelper.Read(reader, Value, r => (MinorSkill)r.ReadByte());
        }
    }
}
