using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonSlayer;

/// <summary>
///     Русские названия пород и сопоставление «этот труп — чья порода».
///
///     Названия даны в родительном падеже, потому что подставляются в «убийца …»,
///     «истребитель …», «гроза …» — в именительном там выходит косноязычие.
/// </summary>
public static class SlayerRu
{
    private static readonly Dictionary<SlayerName, string> Genitive = new()
    {
        // Надгруппы (Super) — широкие, зато и убивать их надо много.
        [SlayerName.Silver] = "нежити",
        [SlayerName.Repond] = "человекоподобных",
        [SlayerName.ReptilianDeath] = "рептилий",
        [SlayerName.Exorcism] = "исчадий бездны",
        [SlayerName.ArachnidDoom] = "членистоногих",
        [SlayerName.ElementalBan] = "элементалей",
        [SlayerName.Fey] = "фей",

        // Люди и великаны.
        [SlayerName.OrcSlaying] = "орков",
        [SlayerName.TrollSlaughter] = "троллей",
        [SlayerName.OgreTrashing] = "огров",

        // Рептилии.
        [SlayerName.DragonSlaying] = "драконов",
        [SlayerName.SnakesBane] = "змей",
        [SlayerName.LizardmanSlaughter] = "ящеролюдов",
        [SlayerName.Terathan] = "тератанов",
        [SlayerName.Ophidian] = "офидианов",

        // Бездна.
        [SlayerName.DaemonDismissal] = "демонов",
        [SlayerName.GargoylesFoe] = "гаргулий",
        [SlayerName.BalronDamnation] = "балронов",

        // Членистоногие.
        [SlayerName.SpidersDeath] = "пауков",
        [SlayerName.ScorpionsBane] = "скорпионов",

        // Элементали.
        [SlayerName.FlameDousing] = "огненных элементалей",
        [SlayerName.WaterDissipation] = "водных элементалей",
        [SlayerName.Vacuum] = "воздушных элементалей",
        [SlayerName.ElementalHealth] = "ядовитых элементалей",
        [SlayerName.EarthShatter] = "земляных элементалей",
        [SlayerName.BloodDrinking] = "кровавых элементалей",
        [SlayerName.SummerWind] = "снежных элементалей"
    };

    public static string GenitiveFor(SlayerName name) =>
        Genitive.TryGetValue(name, out var ru) ? ru : name.ToString();

    /// <summary>Есть ли у этой породы русское имя — проверяется тестом на полноту.</summary>
    public static bool HasName(SlayerName name) => Genitive.ContainsKey(name);

    /// <summary>
    ///     Какие породы засчитываются за этот труп. Обычно две: своя (орки) и надгруппа
    ///     (человекоподобные), потому что игрок растит оба звания разом — так и задумано.
    /// </summary>
    public static List<SlayerName> EntriesFor(Mobile m) =>
        m == null ? new List<SlayerName>() : EntriesFor(m.GetType());

    private static readonly Dictionary<Type, SlayerName[]> Cache = new();

    /// <summary>
    ///     То же, но с запоминанием ответа по типу. Нужно ради боя: прибавку к урону считают
    ///     на каждом замахе, а честный перебор — это двадцать семь пород по два десятка
    ///     типов в каждой, и все через IsAssignableFrom. Породы существа не меняются, так
    ///     что ответ считается один раз на тип за всю жизнь сервера.
    /// </summary>
    public static SlayerName[] CachedEntriesFor(Type type)
    {
        if (type == null)
        {
            return Array.Empty<SlayerName>();
        }

        if (!Cache.TryGetValue(type, out var names))
        {
            Cache[type] = names = EntriesFor(type).ToArray();
        }

        return names;
    }

    /// <summary>
    ///     То же, но по типу существа. Отдельная перегрузка не ради красоты: SlayerEntry.Slays
    ///     всё равно смотрит только на тип, а вот создать живого орка в тестах нельзя —
    ///     BaseCreature требует поднятого мира. По типу проверяется без этого.
    /// </summary>
    public static List<SlayerName> EntriesFor(Type type)
    {
        var matched = new List<SlayerName>();

        if (type == null)
        {
            return matched;
        }

        foreach (var group in SlayerGroup.Groups)
        {
            if (Slays(group.Super, type))
            {
                matched.Add(group.Super.Name);
            }

            if (group.Entries == null)
            {
                continue;
            }

            for (var i = 0; i < group.Entries.Length; i++)
            {
                if (Slays(group.Entries[i], type))
                {
                    matched.Add(group.Entries[i].Name);
                }
            }
        }

        return matched;
    }

    /// <summary>Та же проверка, что и в SlayerEntry.Slays, только по типу, а не по мобу.</summary>
    private static bool Slays(SlayerEntry entry, Type type)
    {
        if (entry?.Types == null)
        {
            return false;
        }

        for (var i = 0; i < entry.Types.Length; i++)
        {
            if (entry.Types[i].IsAssignableFrom(type))
            {
                return true;
            }
        }

        return false;
    }
}
