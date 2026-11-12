using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonShamanism;

/// <summary>
///     Временные усиления и проклятия обрядов поверх обычных модов движка (StatMod,
///     ResistanceMod). Моды не сохраняются в мир, так что после перезапуска всё это
///     честно спадает само — для эффектов на минуты это ровно то, что нужно.
///
///     Одно имя — один эффект: повторный обряд не складывается со старым, а заменяет его.
/// </summary>
public static class ShamanEffects
{
    private static readonly ResistanceType[] AllResists =
    {
        ResistanceType.Physical, ResistanceType.Fire, ResistanceType.Cold, ResistanceType.Poison, ResistanceType.Energy
    };

    private static readonly Dictionary<(Mobile, string), Timer> ResistTimers = new();

    public static void AddResistCurse(Mobile m, string name, int amount, TimeSpan duration) =>
        AddResists(m, name, AllResists, amount, duration);

    /// <summary>Сопротивления: amount положительный — бафф, отрицательный — проклятие.</summary>
    public static void AddResists(Mobile m, string name, ResistanceType[] types, int amount, TimeSpan duration)
    {
        RemoveResists(m, name);

        foreach (var type in types)
        {
            m.AddResistanceMod(new ResistanceMod(type, $"{name}:{type}", amount));
        }

        ResistTimers[(m, name)] = Timer.DelayCall(duration, () => RemoveResists(m, name));
    }

    public static void RemoveResists(Mobile m, string name)
    {
        if (ResistTimers.Remove((m, name), out var timer))
        {
            timer.Stop();
        }

        foreach (var type in AllResists)
        {
            m.RemoveResistanceMod($"{name}:{type}");
        }
    }

    public static bool HasResists(Mobile m, string name) => ResistTimers.ContainsKey((m, name));

    /// <summary>Доля от базовых характеристик в минус: 0.1 — минус десятая часть.</summary>
    public static void AddStatCurse(Mobile m, string name, double fraction, TimeSpan duration) =>
        AddStats(
            m, name,
            -(int)Math.Max(1, m.RawStr * fraction),
            -(int)Math.Max(1, m.RawDex * fraction),
            -(int)Math.Max(1, m.RawInt * fraction),
            duration
        );

    public static void AddStats(Mobile m, string name, int str, int dex, int intel, TimeSpan duration)
    {
        SetStat(m, StatType.Str, $"{name}:Str", str, duration);
        SetStat(m, StatType.Dex, $"{name}:Dex", dex, duration);
        SetStat(m, StatType.Int, $"{name}:Int", intel, duration);
    }

    private static void SetStat(Mobile m, StatType type, string name, int offset, TimeSpan duration)
    {
        m.RemoveStatMod(name);

        if (offset != 0)
        {
            m.AddStatMod(new StatMod(type, name, offset, duration));
        }
    }
}
