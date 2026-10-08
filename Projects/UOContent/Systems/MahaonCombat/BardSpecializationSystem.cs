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
///     three real action-skills are what get specializations here, same window-widening
///     mechanic as MinorSkillSpecializationSystem. Breadth bonus kept (unlike Minor
///     skills) since these three genuinely share a "stage presence" theme a real bard
///     would plausibly carry between them.
/// </summary>
public static class BardSpecializationSystem
{
    private static readonly Dictionary<(Mobile, BardSpecialization), double> Value = new();

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

    /// <summary>Points to subtract from the low end of that skill's CheckTargetSkill
    /// window — up to 15 at 100 trained, plus breadth from the other two.</summary>
    public static double GetWindowBonus(Mobile m, BardSpecialization spec) =>
        GetValue(m, spec) / 100.0 * 15.0 + GetBreadthBonus(m, spec);

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
