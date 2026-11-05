using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonCombat;

public enum TrackingSpecialization : byte
{
    Animals = 0,   // Следопыт зверей
    Monsters = 1,  // Следопыт монстров
    HumanNPCs = 2, // Следопыт НПС
    Players = 3    // Следопыт игроков
}

/// <summary>
///     The vanilla TrackWhatGump already splits into exactly these 4 real categories
///     (Animals/Monsters/Human NPCs/Players — see the button labels in Tracking.cs) so
///     this reuses that existing split instead of inventing a new one. Trained by which
///     category button the player actually picked when the track succeeded.
/// </summary>
public static class TrackingSpecializationSystem
{
    private static readonly ProgressTable<TrackingSpecialization> Value = new("branch:tracking");

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetValue(Mobile m, TrackingSpecialization spec) => Value.GetValueOrDefault((m, spec), 0.0);

    public static void Train(Mobile m, TrackingSpecialization spec)
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

    private static double GetBreadthBonus(Mobile m, TrackingSpecialization active)
    {
        var total = 0.0;

        for (var s = 0; s < 4; s++)
        {
            var spec = (TrackingSpecialization)s;

            if (spec == active)
            {
                continue;
            }

            total += GetValue(m, spec);
        }

        return Math.Min(BreadthCap, total / BreadthUnitsPerPercent);
    }

    /// <summary>
    ///     Эффект при успехе (этап 8): видит этот вид целей дальше — до +50% радиуса на 100,
    ///     плюс ширина. Раньше здесь была прибавка к шансу, которую никто не вызывал, — ветка
    ///     не давала ничего.
    /// </summary>
    public static double RangeScale(Mobile m, TrackingSpecialization spec) =>
        1.0 + GetValue(m, spec) / 100.0 * 0.5 + GetBreadthBonus(m, spec) / 100.0;

    public static string RuSpecializationName(TrackingSpecialization spec) => spec switch
    {
        TrackingSpecialization.Animals   => "Следопыт зверей",
        TrackingSpecialization.Monsters  => "Следопыт монстров",
        TrackingSpecialization.HumanNPCs => "Следопыт НПС",
        TrackingSpecialization.Players   => "Следопыт игроков",
        _                                  => spec.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonTrackingSpecialization", 1)
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
            MahaonSpecializationPersistenceHelper.Read(reader, Value, r => (TrackingSpecialization)r.ReadByte());
        }
    }
}
