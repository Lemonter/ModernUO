using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Targeting;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Одно хранилище всей прокачки поверх навыков: ветки (школы, стили, специализации),
///     мастерство и персональные потолки.
///
///     ---- Что было ----
///
///     Два с половиной десятка систем — школы пяти видов магии, стили оружия, приёмы,
///     ремесленные, воровские, следопытские специализации — устроены одинаково: значение
///     0–100 на (игрок, ветка), тренировка от использования. И каждая хранила его САМА: свой
///     словарь, свой класс сохранения под своим именем. Новая ветка — новый файл сохранения;
///     вопрос «что вообще натренировал этот игрок» — обход двадцати пяти систем.
///
///     ---- Что стало ----
///
///     Значение лежит здесь под устойчивым именем дорожки: «branch:necromancy/Summoning»,
///     «branch:style/Swords/3», «cap/Школа призыва». Системы при этом не переписаны: их словари
///     заменены таблицами (ProgressTable), у которых тот же GetValueOrDefault и тот же
///     индексатор, — логика тренировки и бонусов не тронута ни строкой.
///
///     ---- Переезд ----
///
///     Старые классы сохранения оставлены, но стали односторонними: при загрузке они читают
///     прежние данные и складывают их сюда, а при сохранении пишут пустую запись. То есть
///     первая же загрузка после обновления переносит всё, первое же сохранение закрепляет
///     перенос, и ничего не теряется, даже если между ними сервер упадёт, — тогда перенос
///     просто повторится. Сколько перенесено, сервер скажет в консоли.
/// </summary>
public sealed class MahaonProgress : GenericPersistence
{
    private static MahaonProgress _instance;

    private static readonly Dictionary<Mobile, Dictionary<string, double>> Data = new();

    /// <summary>Сколько значений пришло из старых хранилищ на этой загрузке.</summary>
    private static int _migrated;

    public MahaonProgress() : base("MahaonProgress", 1)
    {
    }

    public static void Configure()
    {
        _instance ??= new MahaonProgress();
        CommandSystem.Register("Progress", AccessLevel.GameMaster, Progress_OnCommand);
    }

    public static void Initialize()
    {
        if (_migrated == 0)
        {
            return;
        }

        Utility.PushColor(ConsoleColor.Green);
        Console.WriteLine(
            $"[Прокачка] перенесено из старых хранилищ: {_migrated} значений, игроков: {Data.Count}."
        );
        Utility.PopColor();
    }

    public static bool TryGet(Mobile m, string track, out double value)
    {
        value = 0.0;

        return m != null && Data.TryGetValue(m, out var tracks) && tracks.TryGetValue(track, out value);
    }

    public static double Get(Mobile m, string track, double fallback = 0.0) =>
        TryGet(m, track, out var value) ? value : fallback;

    public static void Set(Mobile m, string track, double value)
    {
        if (m == null || track == null)
        {
            return;
        }

        if (!Data.TryGetValue(m, out var tracks))
        {
            Data[m] = tracks = new Dictionary<string, double>();
        }

        tracks[track] = value;
    }

    /// <summary>Всё, что натренировал игрок, — для доски мастерства и [Progress.</summary>
    public static IReadOnlyDictionary<string, double> TracksOf(Mobile m) =>
        m != null && Data.TryGetValue(m, out var tracks) ? tracks : EmptyTracks;

    private static readonly Dictionary<string, double> EmptyTracks = new();

    /// <summary>Зовут старые классы сохранения, перекладывая значение сюда.</summary>
    internal static void Migrated(Mobile m, string track, double value)
    {
        Set(m, track, value);
        _migrated++;
    }

    // ---- Сохранение -------------------------------------------------------------------------

    public override void Serialize(IGenericWriter writer) => WriteAll(writer);

    public override void Deserialize(IGenericReader reader) => ReadAll(reader);

    /// <summary>Отдельно от экземпляра: второй экземпляр хранилища в тестах встал бы в реестр
    /// сохранений рядом с настоящим и писался бы в тот же файл.</summary>
    internal static void WriteAll(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        var live = 0;

        foreach (var (m, tracks) in Data)
        {
            if (m?.Deleted == false && tracks.Count > 0)
            {
                live++;
            }
        }

        writer.WriteEncodedInt(live);

        foreach (var (m, tracks) in Data)
        {
            if (m?.Deleted != false || tracks.Count == 0)
            {
                continue;
            }

            writer.Write(m);
            writer.WriteEncodedInt(tracks.Count);

            foreach (var (track, value) in tracks)
            {
                writer.Write(track);
                writer.Write(value);
            }
        }
    }

    /// <summary>
    ///     Сливает, а не заменяет: порядок, в котором загружаются хранилища, не задан, и старые
    ///     классы могут успеть переложить сюда свои данные раньше, чем прочитается это.
    /// </summary>
    internal static void ReadAll(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var players = reader.ReadEncodedInt();

        for (var i = 0; i < players; i++)
        {
            var m = reader.ReadEntity<Mobile>();
            var count = reader.ReadEncodedInt();

            for (var t = 0; t < count; t++)
            {
                var track = reader.ReadString();
                var value = reader.ReadDouble();

                if (m != null && track != null)
                {
                    Set(m, track, value);
                }
            }
        }
    }

    // ---- Команда ------------------------------------------------------------------------------

    [Usage("Progress")]
    [Description("Вся прокачка поверх навыков у указанного игрока: ветки, мастерство, потолки.")]
    private static void Progress_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage(0x3B2, "Чью прокачку показать?");
        e.Mobile.Target = new ProgressTarget();
    }

    private class ProgressTarget : Target
    {
        public ProgressTarget() : base(16, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Mobile who)
            {
                return;
            }

            var tracks = TracksOf(who);

            if (tracks.Count == 0)
            {
                from.SendMessage(0x3B2, $"{who.Name}: ничего не натренировано.");
                return;
            }

            from.SendMessage(0x59, $"{who.Name}: дорожек {tracks.Count}.");

            var sorted = new List<string>(tracks.Keys);
            sorted.Sort(StringComparer.Ordinal);

            foreach (var track in sorted)
            {
                from.SendMessage(0x3B2, $"  {track} = {tracks[track]:0.0#}");
            }
        }
    }
}

/// <summary>
///     Заместитель словаря Dictionary&lt;Mobile, double&gt; у специализации с одной веткой:
///     тот же GetValueOrDefault и тот же индексатор, а значение лежит в MahaonProgress.
/// </summary>
public sealed class ProgressTable
{
    public ProgressTable(string track) => Track = track;

    public string Track { get; }

    public double GetValueOrDefault(Mobile m, double fallback) => MahaonProgress.Get(m, Track, fallback);

    public double this[Mobile m]
    {
        get => MahaonProgress.Get(m, Track);
        set => MahaonProgress.Set(m, Track, value);
    }
}

/// <summary>
///     Заместитель словаря Dictionary&lt;(Mobile, TKey), double&gt;: дорожка — «префикс/ключ».
///
///     Имя дорожки собирается из имени члена перечисления, поэтому ПЕРЕИМЕНОВЫВАТЬ члены
///     перечислений веток нельзя: это ключи сохранения. Добавлять новые — можно.
/// </summary>
public sealed class ProgressTable<TKey> where TKey : notnull
{
    private readonly Dictionary<TKey, string> _tracks = new();

    public ProgressTable(string prefix) => Prefix = prefix;

    public string Prefix { get; }

    /// <summary>Имя дорожки для ключа. Кешируется: спрашивается на каждом ударе.</summary>
    public string TrackOf(TKey key)
    {
        if (!_tracks.TryGetValue(key, out var track))
        {
            _tracks[key] = track = $"{Prefix}/{key}";
        }

        return track;
    }

    public double GetValueOrDefault((Mobile, TKey) key, double fallback) =>
        MahaonProgress.Get(key.Item1, TrackOf(key.Item2), fallback);

    public double this[(Mobile, TKey) key]
    {
        get => MahaonProgress.Get(key.Item1, TrackOf(key.Item2));
        set => MahaonProgress.Set(key.Item1, TrackOf(key.Item2), value);
    }
}

/// <summary>Заместитель словаря с двумя ключами — у стилей это (вид оружия, номер стиля).</summary>
public sealed class ProgressTable<TKey1, TKey2> where TKey1 : notnull where TKey2 : notnull
{
    private readonly Dictionary<(TKey1, TKey2), string> _tracks = new();

    public ProgressTable(string prefix) => Prefix = prefix;

    public string Prefix { get; }

    public string TrackOf(TKey1 key1, TKey2 key2)
    {
        if (!_tracks.TryGetValue((key1, key2), out var track))
        {
            _tracks[(key1, key2)] = track = $"{Prefix}/{key1}/{key2}";
        }

        return track;
    }

    public double GetValueOrDefault((Mobile, TKey1, TKey2) key, double fallback) =>
        MahaonProgress.Get(key.Item1, TrackOf(key.Item2, key.Item3), fallback);

    public double this[(Mobile, TKey1, TKey2) key]
    {
        get => MahaonProgress.Get(key.Item1, TrackOf(key.Item2, key.Item3));
        set => MahaonProgress.Set(key.Item1, TrackOf(key.Item2, key.Item3), value);
    }
}
