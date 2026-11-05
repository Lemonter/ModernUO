using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Systems.MahaonMapEdits;

// MahaonMapEdits — и пространство имён, и класс в нём; без псевдонима имя разрешается
// в пространство.
using MapEdits = Server.Systems.MahaonMapEdits.MahaonMapEdits;

namespace Server.Systems.MahaonWeather;

/// <summary>
///     Дождевая вода: во что дождь наливается и как потом высыхает.
///
///     ---- Что тут изменилось и почему ----
///
///     Раньше это была чистая декорация: во время дождя рядом с игроками рассыпались
///     кляксы «куда попало». Причём из КРОВАВЫХ ПЯТЕН — потому что вся водяная графика
///     клиента помечена Impassable, об неё спотыкаешься, а перекрашенная в синее кровь
///     хотя бы проходима.
///
///     Оба решения теперь не нужны.
///
///     Во-первых, у земли появился рельеф: лопата роет ямы, и вода в мире стоит на своём
///     уровне (MahaonWaterFlow). Значит дождь обязан наливаться ТУДА, КУДА ВОДА И ТЕЧЁТ —
///     в низины. Лужа посреди бугра выглядит ошибкой, потому что ошибкой и является.
///
///     Во-вторых, проходимая водяная графика в клиенте всё-таки есть: 0x1FA3..0x1FCA,
///     «wave» — Translucent|Wet|Animation, высота 0, Impassable НЕ стоит. Прошлый перебор
///     её не нашёл, потому что искал по слову water в названии. Это настоящая анимированная
///     вода, по которой можно ходить, — ровно то, чем мелкая лужа и должна быть.
///
///     ---- Правила ----
///
///     Наливается только в котловину: тайл, у которого ВСЕ соседи вокруг выше. Дно ямы
///     заливается целиком, борта — нет. Ровное поле котловиной не считается, иначе лужи
///     расползлись бы по всему лугу.
///
///     Дождевая вода ВРЕМЕННАЯ: кончился дождь — через минуту её нет. Рыбы в ней нет тоже
///     (см. HarvestTarget): это лужа, а не пруд. Настоящий пруд получается иначе — копкой
///     канавы к реке, и он остаётся.
/// </summary>
public static class PuddleSystem
{
    /// <summary>
    ///     Проходимая анимированная вода. Единственный такой набор в нашей тайлдате:
    ///     Translucent|Wet|Animation без Impassable.
    ///
    ///     Если на глаз покажется слишком «морской пеной» — рядом есть ещё два проходимых
    ///     мокрых набора: болотная рябь 0x320D..0x324A и всплеск 0x5675. Подменяется
    ///     здесь, одной строкой.
    /// </summary>
    private static readonly int[] RainwaterGraphics =
    {
        0x1FA3, 0x1FA4, 0x1FA5, 0x1FA6, 0x1FA7, 0x1FA8, 0x1FA9, 0x1FAA, 0x1FAB, 0x1FAC,
        0x1FAD, 0x1FAE, 0x1FAF, 0x1FB0, 0x1FB1, 0x1FB2, 0x1FB3, 0x1FB4, 0x1FB5, 0x1FB6,
        0x1FB7, 0x1FB8, 0x1FB9, 0x1FBA, 0x1FBB, 0x1FBC, 0x1FBD, 0x1FBE, 0x1FBF, 0x1FC0,
        0x1FC1, 0x1FC2, 0x1FC3, 0x1FC4, 0x1FC5, 0x1FC6, 0x1FC7, 0x1FC8, 0x1FC9, 0x1FCA
    };

    /// <summary>Дождевая ли это графика. Нужно рыбалке: в луже рыбы нет.</summary>
    public static bool IsRainwater(int graphic) => Array.IndexOf(RainwaterGraphics, graphic) >= 0;

    private static readonly TimeSpan SpawnTick = TimeSpan.FromSeconds(6);

    /// <summary>Сколько держится вода после того, как дождь кончился.</summary>
    public static readonly TimeSpan DryingDelay = TimeSpan.FromMinutes(1);

    /// <summary>Окно вокруг игрока, которое просматриваем за такт.</summary>
    private const int SpawnRadius = 10;

    /// <summary>Докуда смотрим, ища борт низины. Два-три тайла — это пологий склон
    /// выкопанной ямы; дальше начинается уже рельеф, а не её край.</summary>
    private const int Rim = 3;

    /// <summary>Сколько тайлов заливаем за один такт на игрока.</summary>
    private const int BudgetPerTick = 60;

    /// <summary>Предохранитель на весь мир.</summary>
    private const int MaxPuddles = 2000;

    private static readonly List<MahaonPuddle> ActivePuddles = new();
    private static readonly HashSet<(int Map, int X, int Y)> Taken = new();

    private static Timer _spawnTimer;

    /// <summary>Номер текущей «погодной эпохи»: по нему отложенное высыхание понимает,
    /// что дождь успел начаться заново, и отменяет себя само.</summary>
    private static int _epoch;

    public static int Count => ActivePuddles.Count;

    public static void Configure()
    {
        WeatherSystem.OnWeatherChanged += OnWeatherChanged;

        CommandSystem.Register("Rainwater", AccessLevel.GameMaster, Rainwater_OnCommand);

        WarnAboutImpassableGraphics();
    }

    /// <summary>
    ///     Погода у нас ПЕРЕЖИВАЕТ перезапуск: она лежит в сохранении. А такт луж
    ///     заводился только по событию смены погоды — значит сервер, поднятый под уже
    ///     идущим дождём, не заводил его никогда, и луж не появлялось вовсе, сколько ни
    ///     лей. Ровно это и было видно в игре.
    ///
    ///     Configure для этого не годится: он выполняется ДО загрузки мира, и погода там
    ///     ещё не прочитана.
    /// </summary>
    [CallPriority(90)]
    public static void Initialize()
    {
        if (Raining(WeatherSystem.Current))
        {
            _spawnTimer ??= Timer.DelayCall(SpawnTick, SpawnTick, SpawnTick_OnTick);
        }
    }

    /// <summary>
    ///     Залить низины вокруг себя прямо сейчас, не дожидаясь дождя.
    ///
    ///     Проверять лужи иначе неудобно: погода у нас не идёт сама, её включает ГМ
    ///     кнопкой, и «я не увидел луж» может означать и что дождя не было, и что низин не
    ///     нашлось. Команда разделяет эти два случая — она отвечает числом.
    /// </summary>
    [Usage("Rainwater [радиус]")]
    [Description("Заливает все низины вокруг тебя дождевой водой, не дожидаясь дождя.")]
    private static void Rainwater_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var radius = e.Length >= 1 ? Math.Clamp(e.GetInt32(0), 1, 40) : SpawnRadius;

        ActivePuddles.RemoveAll(p => p.Deleted);

        var placed = FillAround(from.Map, from.X, from.Y, radius, BudgetPerTick * 4);

        from.SendMessage(
            0x59,
            placed > 0
                ? $"Залито тайлов: {placed}. Всего луж в мире: {ActivePuddles.Count}."
                : $"Низин в {radius} тайлах вокруг не нашлось — заливать нечего."
        );

        if (!Raining(WeatherSystem.Current))
        {
            from.SendMessage(0x3B2, "Дождя сейчас нет, так что эти лужи никто не обновит — высохнут сами.");
        }
    }

    /// <summary>
    ///     Ругается, если в наборе оказалась непроходимая графика.
    ///
    ///     Это не паранойя, а память о конкретной ошибке: здесь стояли водяные статики —
    ///     выбор по названию очевидный, — и все они помечены Impassable. Игроки
    ///     спотыкались о лужи, и заметить это по коду было нельзя: флаг живёт в tiledata, а
    ///     не в исходниках. Тестом такое тоже не поймать — в тестовом хосте tiledata не
    ///     загружен. Проверка на старте, на настоящих данных, единственное надёжное место.
    /// </summary>
    private static void WarnAboutImpassableGraphics()
    {
        foreach (var graphic in RainwaterGraphics)
        {
            if ((TileData.ItemTable[graphic & TileData.MaxItemValue].Flags & TileFlag.Impassable) == 0)
            {
                continue;
            }

            Utility.PushColor(ConsoleColor.Red);
            Console.WriteLine(
                $"[Дождь] графика {graphic:X4} непроходима — об такую лужу игроки будут спотыкаться."
            );
            Utility.PopColor();
        }
    }

    private static bool Raining(MahaonWeather weather) =>
        weather is MahaonWeather.Rain or MahaonWeather.Storm;

    private static void OnWeatherChanged(MahaonWeather weather)
    {
        _epoch++;

        if (Raining(weather))
        {
            _spawnTimer ??= Timer.DelayCall(SpawnTick, SpawnTick, SpawnTick_OnTick);
            return;
        }

        _spawnTimer?.Stop();
        _spawnTimer = null;

        // Не сразу: вода уходит в землю не мгновенно. Если за эту минуту дождь пойдёт
        // снова, эпоха сменится и высыхание отменится само.
        var epoch = _epoch;

        Timer.DelayCall(
            DryingDelay,
            () =>
            {
                if (epoch == _epoch)
                {
                    DryEverything();
                }
            }
        );
    }

    public static void DryEverything()
    {
        foreach (var puddle in ActivePuddles)
        {
            if (!puddle.Deleted)
            {
                puddle.Delete();
            }
        }

        ActivePuddles.Clear();
        Taken.Clear();
    }

    private static void SpawnTick_OnTick()
    {
        ActivePuddles.RemoveAll(p => p.Deleted);

        if (ActivePuddles.Count >= MaxPuddles)
        {
            return;
        }

        foreach (var player in World.Mobiles.Values)
        {
            if (player is not PlayerMobile pm || pm.NetState == null || pm.Map == null || !pm.Alive)
            {
                continue;
            }

            FillAround(pm.Map, pm.X, pm.Y, SpawnRadius, BudgetPerTick);
        }
    }

    /// <summary>
    ///     Заливает все низины в окне вокруг точки. Возвращает, сколько тайлов залито.
    ///
    ///     Просматриваем окно целиком, а не тычем наугад: случайные пробы почти всегда
    ///     попадали в ровное место, и за дождь не находилось ни одной лужи — ровно это и
    ///     было видно в игре.
    /// </summary>
    public static int FillAround(Map map, int cx, int cy, int radius, int budget)
    {
        if (map == null || map == Map.Internal)
        {
            return 0;
        }

        var placed = 0;

        for (var x = cx - radius; x <= cx + radius && placed < budget; x++)
        {
            for (var y = cy - radius; y <= cy + radius && placed < budget; y++)
            {
                if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                {
                    continue;
                }

                if (!HoldsWater(map, x, y) || !IsOpenGround(map, x, y))
                {
                    continue;
                }

                var puddle = new MahaonPuddle(RainwaterGraphics.RandomElement());
                puddle.MoveToWorld(new Point3D(x, y, map.GetAverageZ(x, y)), map);

                ActivePuddles.Add(puddle);
                Taken.Add((map.MapID, x, y));
                placed++;
            }
        }

        return placed;
    }

    /// <summary>
    ///     Держится ли тут вода. Два условия, и оба нужны.
    ///
    ///     ПЕРВОЕ: стечь некуда — ни один из восьми соседей не ниже. Это и есть «углубление»
    ///     в точном смысле: с этого тайла нельзя уйти вниз.
    ///
    ///     ВТОРОЕ: выше нас земля минимум с ДВУХ сторон из четырёх, в пределах Rim. Без
    ///     этого условия залило бы всю ровную карту — на идеально плоском месте ни один
    ///     сосед тоже не ниже. А проверка именно на две стороны отличает низину от подножия
    ///     склона: у подножия холма выше только с одной стороны, в яме или в ложбине — с
    ///     двух и более.
    ///
    ///     Прежнее условие было строже: требовалась замкнутая котловина с плоским дном,
    ///     целиком обведённая бортом. Такого в живом рельефе почти не бывает, и лужи не
    ///     появлялись вовсе.
    /// </summary>
    private static bool HoldsWater(Map map, int x, int y)
    {
        var z = MahaonExcavation.GroundZ(map, x, y);

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                if (GroundAt(map, x + dx, y + dy) is { } nz && nz < z)
                {
                    return false; // вода стечёт сюда
                }
            }
        }

        Span<(int X, int Y)> sides = [(1, 0), (-1, 0), (0, 1), (0, -1)];
        var walls = 0;

        foreach (var (dx, dy) in sides)
        {
            for (var d = 1; d <= Rim; d++)
            {
                if (GroundAt(map, x + dx * d, y + dy * d) is { } nz && nz > z)
                {
                    walls++;
                    break;
                }
            }
        }

        return walls >= 2;
    }

    /// <summary>Высота земли, или null за краем карты. За краем «соседа нет» — это не повод
    /// считать, что туда стечёт.</summary>
    private static int? GroundAt(Map map, int x, int y) =>
        x < 0 || y < 0 || x >= map.Width || y >= map.Height ? null : MahaonExcavation.GroundZ(map, x, y);

    /// <summary>Проходимая земля под открытым небом, без своей воды и без чужого добра.</summary>
    private static bool IsOpenGround(Map map, int x, int y)
    {
        if (Taken.Contains((map.MapID, x, y)))
        {
            return false;
        }

        // Настоящая вода уже стоит — доливать нечего.
        if (MahaonWaterFlow.IsFlooded(map, x, y))
        {
            return false;
        }

        if ((TileData.LandTable[MapEdits.LandAt(map, x, y).Graphic & TileData.MaxLandValue].Flags
             & TileFlag.Impassable) != 0)
        {
            return false;
        }

        // Высота для «влезет ли сюда» — СРЕДНЯЯ по углам тайла, а не высота самого тайла.
        // На склоне это разные числа, и CanFit с requireSurface требует поверхности ровно
        // на запрошенной высоте: на высоте тайла её там нет, и проверка отвергала всё
        // подряд. Форму рельефа (HoldsWater) при этом считаем по высоте тайлов — это она и
        // есть.
        var z = map.GetAverageZ(x, y);
        var loc = new Point3D(x, y, z);

        if (!map.CanFit(x, y, z, 16))
        {
            return false;
        }

        if (Region.Find(loc, map).IsPartOf<DungeonRegion>())
        {
            return false; // под землёй дождя нет
        }

        // Крыша над головой — значит не льёт.
        return map.LineOfSight(new Point3D(x, y, z + 1), new Point3D(x, y, z + 200));
    }
}
