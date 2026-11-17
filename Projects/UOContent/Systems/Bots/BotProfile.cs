using System.Collections.Generic;
using System.Diagnostics;

namespace Server.Systems.Bots;

/// <summary>
/// Куда уходит время бота: оценка каждой цели, построение планов, шаги действий, бой. Копит с
/// последнего [BotStats и показывает самые дорогие места — чтобы оптимизировать по цифрам, а не
/// наугад.
/// </summary>
public static class BotProfile
{
    private sealed class Entry
    {
        public long Calls;
        public long Ticks;
        public long MaxTicks;
    }

    private static readonly Dictionary<(string kind, string name), Entry> _entries = new();

    public static long Start() => Stopwatch.GetTimestamp();

    public static void Stop(string section, long start) => Stop(section, null, start);

    public static void Stop(string kind, string name, long start)
    {
        if (!_entries.TryGetValue((kind, name), out var entry))
        {
            _entries[(kind, name)] = entry = new Entry();
        }

        var spent = Stopwatch.GetTimestamp() - start;
        entry.Calls++;
        entry.Ticks += spent;
        if (spent > entry.MaxTicks)
        {
            entry.MaxTicks = spent;
        }
    }

    public static void Reset() => _entries.Clear();

    /// <summary>Подразделы одного вида по числу вызовов: для счётчиков-причин без времени.</summary>
    public static List<(string name, long calls)> Counts(string kind)
    {
        var list = new List<(string, long)>();
        foreach (var ((k, sub), e) in _entries)
        {
            if (k == kind)
            {
                list.Add((sub, e.Calls));
            }
        }

        list.Sort((a, b) => b.Item2.CompareTo(a.Item2));
        return list;
    }

    /// <summary>Самые дорогие места: имя, вызовов, всего мс, мкс на вызов, худший вызов в мс.</summary>
    public static List<(string section, long calls, double totalMs, double usPerCall, double maxMs)> Top(int count)
    {
        var list = new List<(string, long, double, double, double)>();
        foreach (var ((kind, sub), e) in _entries)
        {
            var name = sub == null ? kind : $"{kind}: {sub}";
            var ms = e.Ticks * 1000.0 / Stopwatch.Frequency;
            list.Add((name, e.Calls, ms, e.Calls == 0 ? 0 : ms * 1000 / e.Calls, e.MaxTicks * 1000.0 / Stopwatch.Frequency));
        }

        list.Sort((a, b) => b.Item3.CompareTo(a.Item3));
        if (list.Count > count)
        {
            list.RemoveRange(count, list.Count - count);
        }

        return list;
    }
}
