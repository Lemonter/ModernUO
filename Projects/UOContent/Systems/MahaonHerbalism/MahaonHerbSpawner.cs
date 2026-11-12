using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.Systems.MahaonHerbalism;

/// <summary>
///     Сажает дикие травы вокруг игроков.
///
///     ---- Почему вокруг игроков, а не по всей карте ----
///
///     Карта огромная, а ходят по ней в нескольких местах. Засеять её целиком — десятки
///     тысяч предметов в сохранении ради травы, которую никто не увидит. Поэтому трава
///     появляется там, где сейчас кто-то есть, и увядает сама (MahaonWildHerb.Lifetime).
///     Для игрока это неотличимо от «трава растёт везде»: куда бы он ни пошёл, вокруг
///     найдётся что собрать, если местность подходит.
///
///     ---- Без затравки при старте ----
///
///     Никакого Initialize-засева: урок сидеров городов (WildlifeSeeder) — всё, что
///     запускается само при загрузке, однажды запустится на чистом мире и наделает дел.
///     Здесь только таймер, который работает от присутствия игроков.
/// </summary>
public static class MahaonHerbSpawner
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(45);

    /// <summary>Сколько кустов держим в округе одного игрока.</summary>
    private const int TargetNearby = 8;

    /// <summary>Радиус «округи».</summary>
    private const int Radius = 24;

    /// <summary>Ближе этого не сажаем — трава не должна вырастать у игрока под ногами.</summary>
    private const int MinDistance = 6;

    /// <summary>Сколько кустов максимум за один проход на игрока — чтобы не было «вспышки».</summary>
    private const int PerTick = 2;

    /// <summary>Сколько точек пробуем, прежде чем сдаться (город, вода, скалы).</summary>
    private const int Attempts = 24;

    /// <summary>Предохранитель на весь мир.</summary>
    private const int GlobalCap = 4000;

    private static readonly HashSet<MahaonWildHerb> Live = new();

    public static int LiveCount => Live.Count;

    public static void Register(MahaonWildHerb herb) => Live.Add(herb);

    public static void Unregister(MahaonWildHerb herb) => Live.Remove(herb);

    /// <summary>
    ///     Боты в походе за травами. Клиента у бота нет, а трава растёт вокруг тех, кто
    ///     подключён, — без этого списка бот-травник бродил бы по пустому лугу. Отмечаются
    ///     только на время похода, а не всегда: иначе вокруг каждого из сотен ботов мира
    ///     росла бы трава, которую никто не собирает.
    /// </summary>
    private static readonly HashSet<Mobile> Foragers = new();

    public static void AddForager(Mobile m) => Foragers.Add(m);

    public static void RemoveForager(Mobile m) => Foragers.Remove(m);

    public static void Configure()
    {
        Timer.DelayCall(Interval, Interval, Tick);
        CommandSystem.Register("HerbsHere", AccessLevel.GameMaster, HerbsHere_OnCommand);
    }

    /// <summary>
    ///     Проверка без ожидания таймера: досаживает округу до нормы и говорит, что за
    ///     местность под ногами. Без неё вопрос «почему тут ничего не растёт» решается
    ///     угадыванием — урок диагностики полей (MahaonFieldDiagnostics).
    /// </summary>
    private static void HerbsHere_OnCommand(CommandEventArgs e)
    {
        var m = e.Mobile;
        var map = m.Map;
        if (map == null || map == Map.Internal)
        {
            return;
        }

        var planted = 0;
        for (var i = 0; i < TargetNearby; i++)
        {
            planted += FillAround(m, map);
        }

        var night = MahaonHerbs.IsNight(map, m.X, m.Y) ? "ночь" : "день";
        m.SendMessage(
            0x59,
            $"Местность: {MahaonHerbs.HabitatAt(map, m.X, m.Y)}, сезон: {MahaonHerbs.CurrentSeasonMask}, {night}. " +
            $"Посажено: {planted}, кустов в мире: {Live.Count}."
        );
    }

    private static void Tick()
    {
        foreach (var ns in NetState.Instances)
        {
            if (Live.Count >= GlobalCap)
            {
                return;
            }

            if (ns.Mobile is PlayerMobile { Alive: true, Deleted: false } pm && pm.Map is { } map && map != Map.Internal)
            {
                FillAround(pm, map);
            }
        }

        Foragers.RemoveWhere(m => m.Deleted);

        foreach (var m in Foragers)
        {
            if (Live.Count >= GlobalCap)
            {
                return;
            }

            if (m.Alive && m.Map is { } map && map != Map.Internal)
            {
                FillAround(m, map);
            }
        }
    }

    /// <summary>Досадить траву вокруг игрока. Публично — для команды проверки.</summary>
    public static int FillAround(Mobile m, Map map)
    {
        var nearby = 0;
        foreach (var _ in map.GetItemsInRange<MahaonWildHerb>(m.Location, Radius))
        {
            nearby++;
        }

        var planted = 0;
        for (var i = 0; i < Attempts && planted < PerTick && nearby + planted < TargetNearby; i++)
        {
            if (TryPlant(m, map))
            {
                planted++;
            }
        }

        return planted;
    }

    private static bool TryPlant(Mobile m, Map map)
    {
        var angle = Utility.RandomDouble() * Math.PI * 2;
        var dist = MinDistance + Utility.RandomDouble() * (Radius - MinDistance);
        var x = m.X + (int)Math.Round(Math.Cos(angle) * dist);
        var y = m.Y + (int)Math.Round(Math.Sin(angle) * dist);

        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
        {
            return false;
        }

        var habitat = MahaonHerbs.HabitatAt(map, x, y);
        if (habitat == MahaonHabitat.None)
        {
            return false;
        }

        var z = map.GetAverageZ(x, y);
        var p = new Point3D(x, y, z);

        if (!map.CanSpawnMobile(x, y, z) || BaseHouse.FindHouseAt(p, map, 16) != null)
        {
            return false;
        }

        var kind = Pick(map, x, y, habitat);
        if (kind == null)
        {
            return false;
        }

        new MahaonWildHerb(kind.Value).MoveToWorld(p, map);
        return true;
    }

    private static MahaonHerbKind? Pick(Map map, int x, int y, MahaonHabitat habitat)
    {
        var season = MahaonHerbs.CurrentSeasonMask;
        var night = MahaonHerbs.IsNight(map, x, y);
        var total = 0;

        foreach (var info in MahaonHerbs.All)
        {
            if (Fits(info, habitat, season, night))
            {
                total += info.Weight;
            }
        }

        if (total == 0)
        {
            return null;
        }

        var roll = Utility.Random(total);
        foreach (var info in MahaonHerbs.All)
        {
            if (!Fits(info, habitat, season, night))
            {
                continue;
            }

            roll -= info.Weight;
            if (roll < 0)
            {
                return info.Kind;
            }
        }

        return null;
    }

    private static bool Fits(MahaonHerbInfo info, MahaonHabitat habitat, MahaonSeasonMask season, bool night) =>
        (info.Habitat & habitat) != 0 && (info.Seasons & season) != 0 && (!info.NightOnly || night);
}
