using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonCombat;

public enum BardSpecialization : byte
{
    Discordance = 0, // Дисгармония — trains successful Discordance
    Peacemaking = 1, // Миротворчество — trains successful Peacemaking
    Provocation = 2  // Провокация — trains successful Provocation
}

/// <summary>
///     Musicianship itself has no independent action file in this codebase — it's only
///     ever checked as a modifier inside Discordance/Peacemaking/Provocation, so those
///     three real action-skills are what get specializations here — each gives an extra
///     effect on success, never a better success chance (see below). Breadth bonus kept (unlike Minor
///     skills) since these three genuinely share a "stage presence" theme a real bard
///     would plausibly carry between them.
/// </summary>
public static class BardSpecializationSystem
{
    private static readonly ProgressTable<BardSpecialization> Value = new("branch:bard");

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, BardSpecialization spec) => Value.GetValueOrDefault((m, spec), 0.0);

    public static void Train(Mobile m, BardSpecialization spec)
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

    private static double GetBreadthBonus(Mobile m, BardSpecialization active)
    {
        var total = 0.0;

        for (var s = 0; s < 3; s++)
        {
            var spec = (BardSpecialization)s;

            if (spec == active)
            {
                continue;
            }

            total += GetValue(m, spec);
        }

        return Math.Min(BreadthCap, total / BreadthUnitsPerPercent);
    }

    // ---- Эффекты при успехе (этап 8, раздел 11.2) ----------------------------------------
    //
    // Раньше ветка сдвигала нижнюю границу проверки — работа навыка. Теперь при успехе:
    //   Мастер дисгармонии  — дисгармония сильнее (до +50% на 100);
    //   Мастер умиротворения — цель успокоена дольше (до +50%);
    //   Мастер провокации    — с шансом инструмент не изнашивается (до 50%).
    // «Ширина» (очки в двух других) добавляет сверху, как и раньше.

    /// <summary>Множитель силы/длительности эффекта при успехе.</summary>
    public static double EffectScale(Mobile m, BardSpecialization spec) =>
        1.0 + GetValue(m, spec) / 100.0 * 0.5 + GetBreadthBonus(m, spec) / 100.0;

    /// <summary>Удачная игра не тратит заряд инструмента — шанс до половины на 100.</summary>
    public static bool SparesInstrument(Mobile m, BardSpecialization spec) =>
        Utility.RandomDouble() * 100.0 < GetValue(m, spec) / 2.0 + GetBreadthBonus(m, spec);

    public static string RuSpecializationName(BardSpecialization spec) => spec switch
    {
        BardSpecialization.Discordance => "Мастер дисгармонии",
        BardSpecialization.Peacemaking => "Мастер умиротворения",
        BardSpecialization.Provocation => "Мастер провокации",
        _                                => spec.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonBardSpecialization", 1)
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
            MahaonSpecializationPersistenceHelper.Read(reader, Value, r => (BardSpecialization)r.ReadByte());
        }
    }
}
