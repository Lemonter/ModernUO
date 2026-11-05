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
///     had). Bonus: an extra effect on SUCCESS (see below) — the success chance itself is
///     the skill's business, not the branch's (COMBAT-PROGRESSION.md, 11.2).
/// </summary>
public static class MinorSkillSpecializationSystem
{
    private static readonly ProgressTable<MinorSkill> Value = new("branch:minor");

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

    // ---- Эффекты при успехе (этап 8, раздел 11.2) ----------------------------------------
    //
    // Раньше ветка сдвигала нижнюю границу проверки навыка — то есть делала его работу. Теперь
    // шанс у навыка, а ветка добавляет к успеху своё:
    //   Криминалист — с шансом узнаёт ещё и убийцу (по трупу);
    //   Дегустатор  — с шансом узнаёт силу яда;
    //   Оценщик     — с шансом опознаёт заодно остальное в той же сумке;
    //   Кострожог   — костёр горит дольше (вдвое на 100).

    /// <summary>Шанс дополнительного эффекта при успехе, 0–100: равен тренировке ветки.</summary>
    public static bool RollExtra(Mobile m, MinorSkill skill) => Utility.RandomDouble() * 100.0 < GetValue(m, skill);

    /// <summary>Во сколько раз дольше горит костёр Кострожога.</summary>
    public static double CampfireScale(Mobile m) => 1.0 + GetValue(m, MinorSkill.Camping) / 100.0;

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
