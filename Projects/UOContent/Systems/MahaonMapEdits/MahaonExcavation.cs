using System;
using System.Collections.Generic;
using Server.Multis;
using Server.Regions;

namespace Server.Systems.MahaonMapEdits;

/// <summary>
///     Земляные работы: ямы, рвы, пруды.
///
///     Единственное, чего нельзя изобразить предметами. Статик можно поставить поверх
///     земли, но опустить саму землю — только правкой карты, и до сих пор эта половина
///     системы не была задействована вовсе.
///
///     ---- Как это выглядит в игре ----
///
///     Копаешь тайл — он опускается на единицу и превращается в вскопанную землю. Копаешь
///     дальше — глубже, до предела (FloorZ). Воду сама копка не наливает никогда: вода
///     приходит по рельефу из настоящего источника, этим занимается MahaonWaterFlow.
///
///     Засыпать можно обратно — землю поднимают той же лопатой, пока не вернётся исходная
///     высота. Это внутриигровая отмена, отдельная от админского [MapUndo: игрок правит
///     свою яму сам, не дёргая ГМ.
///
///     ---- Где нельзя ----
///
///     В охраняемых городах — по той же причине, что и подрыв. Под чужим домом — иначе
///     дом окажется висящим над ямой. И в скале: прокоп горы — это шахта, у неё свои
///     правила и свой навык, и смешивать их не надо.
///
///     А вот берег, откос и насыпь копать МОЖНО, хотя земля там помечена Impassable. См.
///     CanDigHere: «непроходимо» и «камень» — не одно и то же.
/// </summary>
public static class MahaonExcavation
{
    /// <summary>На сколько единиц ниже исходной земли можно закопаться.</summary>
    public const int MaxDepth = 5;

    /// <summary>Вскопанная земля — вид ямы, пока она не набрала воду.</summary>
    private static readonly int[] DugGraphics = { 0x0009, 0x000A, 0x000B, 0x000C };



    public enum DigResult
    {
        Dug,
        Flooded,
        TooDeep,
        Forbidden,
        Blocked
    }

    /// <summary>Исходная высота тайла — до всех наших правок.</summary>
    public static int OriginalZ(Map map, int x, int y)
    {
        var blockId = MahaonMapEdits.BlockIdFor(map, x, y);

        if (MahaonMapEdits.TryGetOriginalLand(map.MapID, blockId, out var land))
        {
            return land[(y & 7) * 8 + (x & 7)].Z;
        }

        return map.Tiles.GetLandTile(x, y).Z;
    }

    /// <summary>
    ///     Текущая высота тайла с учётом уже выкопанного.
    ///
    ///     Спрашиваем журнал, а не живую карту: живой слой можно выключить, и тогда
    ///     TileMatrix о наших правках не знает, а глубина ямы должна накапливаться всё
    ///     равно. Это же относится к IsWater ниже.
    /// </summary>
    public static int CurrentZ(Map map, int x, int y) => MahaonMapEdits.LandAt(map, x, y).Z;

    /// <summary>
    ///     Высота дна. У нас это всегда высота ЗЕМЛИ, потому что вода в нашей тайлдате —
    ///     полупрозрачная статика поверх земли, а не сама земля (см. MahaonWaterFlow).
    ///
    ///     Раньше разлив перекрашивал землю в воду, высота тайла уходила под поверхность,
    ///     и дно приходилось выкапывать из журнала правок. Отдельный метод оставлен
    ///     нарочно: он называет вслух, что земляная арифметика считает по ДНУ, и если
    ///     представление воды однажды снова поменяется, менять придётся одно место.
    /// </summary>
    public static int GroundZ(Map map, int x, int y) => CurrentZ(map, x, y);

    /// <summary>Стоит ли на тайле вода. Оба слоя: и статика, и земляные водяные тайлы.</summary>
    public static bool IsWater(Map map, int x, int y) => MahaonWaterFlow.IsFlooded(map, x, y);

    /// <summary>Водой помечена САМА ЗЕМЛЯ — то есть это море или река, нарисованные
    /// художником, а не налитое нами. Такое лопатой не трогают.</summary>
    public static bool IsNaturalWater(Map map, int x, int y) =>
        (TileData.LandTable[MahaonMapEdits.LandAt(map, x, y).Graphic & TileData.MaxLandValue].Flags
         & TileFlag.Wet) != 0;

    /// <summary>Можно ли тут вообще копать — вопрос к месту, не к глубине.</summary>
    public static bool CanDigHere(Map map, int x, int y, out string why)
    {
        why = null;

        if (map == null || map == Map.Internal)
        {
            why = "Здесь не копают.";
            return false;
        }

        var z = CurrentZ(map, x, y);
        var point = new Point3D(x, y, z);

        if (Region.Find(point, map)?.GetRegion<GuardedRegion>() != null)
        {
            why = "В городе копать не дадут — стража рядом.";
            return false;
        }

        if (BaseHouse.FindHouseAt(point, map, 16) != null)
        {
            why = "Под домом копать нельзя.";
            return false;
        }

        var graphic = MahaonMapEdits.LandAt(map, x, y).Graphic;
        var flags = TileData.LandTable[graphic & TileData.MaxLandValue].Flags;

        // Вода тоже помечена Impassable, поэтому разбираем её ПЕРВОЙ. Речь именно про
        // землю, помеченную водой: море и русло. Налитая нами вода лежит статикой поверх
        // земли и сюда не попадает вовсе.
        if ((flags & TileFlag.Wet) != 0)
        {
            why = "Это настоящая вода — её лопатой не вычерпать.";
            return false;
        }

        // Отвесная стена: своды пещер, край карты. Это не поверхность, её не «понизить».
        if ((flags & TileFlag.Wall) != 0)
        {
            why = "Это отвесная стена, а не земля.";
            return false;
        }

        // Камень — ТОЛЬКО то, из чего кирка добывает руду, и список берётся у неё же.
        //
        // Раньше тут стоял флаг Impassable, и это была ошибка, стоившая целого берега:
        // в нашей тайлдате непроходимы не только скалы, но и вся переходная земля —
        // откосы (sand 0x001A..0x0032), обрывы (dirt 0x008D..0x00A7), насыпь
        // (embank 0x098C..0x099F). То есть ровно тот склон, с которого канаву к реке и
        // начинают. Лопата объявляла его камнем и отправляла игрока за киркой, а кирка
        // отвечала, что это не гора.
        if (Engines.Harvest.Mining.System.OreAndStone.Validate(graphic, true))
        {
            why = "Тут камень — это работа для шахтёрской кирки, а не для лопаты.";
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Предел глубины на этом тайле: ниже этой высоты лопата не идёт.
    ///
    ///     Обычно это MaxDepth от целины — яма в чистом поле имеет дно. Но РЯДОМ С ВОДОЙ
    ///     предел опускается под её поверхность, и вот почему.
    ///
    ///     Вода разливается только туда, где земля СТРОГО НИЖЕ её уровня (см.
    ///     MahaonWaterFlow). Река у нас лежит на −5, поле над ней на 0, MaxDepth 5 — и
    ///     яма в поле упирается дном ровно в урез воды. По высоте это пляж, а не дно:
    ///     воды в такой яме ноль. Получались две ямы рядом, своя и речная, и соединить
    ///     их было нельзя — ровно на этом месте предел и срабатывал.
    ///
    ///     Поэтому правило игры: сколько видишь воды рядом — на столько и можешь
    ///     подкопаться под неё. Бездонности это не даёт: уровень воды сам по себе не
    ///     опускается, и каждый тайл всё равно стоит отдельного взмаха.
    /// </summary>
    public static int FloorZ(Map map, int x, int y)
    {
        var floor = OriginalZ(map, x, y) - MaxDepth;

        if (NearbyWaterLevel(map, x, y) is { } level && level - 1 < floor)
        {
            floor = level - 1;
        }

        return floor;
    }

    /// <summary>Самая высокая вода на этом тайле или вплотную к нему, если она тут есть.</summary>
    private static int? NearbyWaterLevel(Map map, int x, int y)
    {
        int? best = null;

        Span<(int X, int Y)> around = [(x, y), (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)];

        foreach (var (nx, ny) in around)
        {
            if (nx < 0 || ny < 0 || nx >= map.Width || ny >= map.Height)
            {
                continue;
            }

            if (MahaonWaterFlow.SurfaceZ(map, nx, ny) is { } z && (best == null || z > best.Value))
            {
                best = z;
            }
        }

        return best;
    }

    /// <summary>
    ///     Копает один тайл. Пометка общая на всю яму, чтобы вся работа откатывалась разом.
    /// </summary>
    public static DigResult Dig(Map map, int x, int y, string reason)
    {
        if (!CanDigHere(map, x, y, out _))
        {
            return DigResult.Forbidden;
        }

        // Налитую воду копают дальше: дно под ней настоящее, и яму можно углубить. Море
        // и русло сюда не доходят — их отсекает CanDigHere.

        var current = GroundZ(map, x, y);

        if (current <= FloorZ(map, x, y))
        {
            return DigResult.TooDeep;
        }

        var newZ = current - 1;

        // Воду тут не наливаем. Дно ямы — сухая земля, а вода приходит только по
        // рельефу, из настоящего источника (MahaonWaterFlow). Раньше было наоборот:
        // яма на последней лопате сама объявлялась прудом, и пруд появлялся посреди
        // сухого луга.
        MahaonMapEdits.SetLand(map, x, y, DugGraphics[Utility.Random(DugGraphics.Length)], newZ, reason);

        return DigResult.Dug;
    }

    /// <summary>
    ///     Засыпает тайл обратно на единицу. Внутриигровая отмена: игрок правит свою яму
    ///     сам, не дёргая ГМ.
    /// </summary>
    public static bool Fill(Map map, int x, int y, string reason)
    {
        if (map == null || map == Map.Internal)
        {
            return false;
        }

        var original = OriginalZ(map, x, y);
        var current = GroundZ(map, x, y);

        if (current >= original)
        {
            return false; // выше исходной земли не насыпаем: это уже не засыпка, а стройка
        }

        var blockId = MahaonMapEdits.BlockIdFor(map, x, y);
        var restored = MahaonMapEdits.TryGetOriginalLand(map.MapID, blockId, out var land)
            ? land[(y & 7) * 8 + (x & 7)].Graphic
            : (ushort)DugGraphics[0];

        var newZ = current + 1;

        // Вернулись на исходную высоту — возвращаем и исходный вид. Пока не вернулись,
        // тайл остаётся вскопанным, а не водой: засыпаемый пруд перестаёт быть прудом
        // с первой же лопаты.
        MahaonMapEdits.SetLand(
            map, x, y,
            newZ >= original ? restored : DugGraphics[Utility.Random(DugGraphics.Length)],
            newZ,
            reason
        );

        return true;
    }

    // ---- Что лежит под правками ------------------------------------------------------

    /// <summary>Тайл земли, каким он был до всех наших правок.</summary>
    public static MapLand OriginalLand(Map map, int x, int y)
    {
        var blockId = MahaonMapEdits.BlockIdFor(map, x, y);

        if (MahaonMapEdits.TryGetOriginalLand(map.MapID, blockId, out var land))
        {
            return land[(y & 7) * 8 + (x & 7)];
        }

        var tile = map.Tiles.GetLandTile(x, y);

        return new MapLand((ushort)tile.ID, (sbyte)tile.Z);
    }


    // ---- Лопата в руках игрока ------------------------------------------------------------

    /// <summary>Слово, с которого начинается причина правки: по нему лопата и помнит направление.</summary>
    private const string DigTag = "копка";

    private const string FillTag = "засыпка";

    /// <summary>
    ///     На сколько соседние тайлы могут разойтись по высоте, оставаясь склоном.
    ///
    ///     Высота в ультиме задаётся на тайл, а углы квадрата клиент выводит из высот
    ///     четырёх соседей. Поэтому опустить один тайл посреди ровного места — это не
    ///     ямка, а ступенька в единицу по всему периметру. Чтобы выходило углубление,
    ///     борта надо тянуть за собой; этим и занимается Smooth.
    /// </summary>
    public const int MaxStep = 1;

    /// <summary>
    ///     Один взмах лопатой по земле — и копка, и засыпка, без всяких режимов.
    ///
    ///     Направление лопата берёт из журнала правок: у каждой записи там есть причина, а
    ///     причина у нас начинается со слова «копка» или «засыпка». Поэтому ни переключателя
    ///     в предмете, ни лишнего поля в сохранении не нужно — порядок получается сам:
    ///
    ///         копаешь, копаешь — яма становится глубже и шире;
    ///         дошёл до предела — если рядом есть вода, дно её набирает;
    ///         бьёшь по дну — лопата поворачивает и начинает засыпать;
    ///         засыпал до исходной высоты — следующий взмах снова копает.
    /// </summary>
    public static bool Swing(Mobile from, Item tool, Map map, int x, int y)
    {
        if (from == null || map == null || map == Map.Internal)
        {
            return false;
        }

        if (from.Mounted)
        {
            from.SendMessage(0x22, "С седла копать неудобно — спустись на землю.");
            return false;
        }

        if (!CanDigHere(map, x, y, out var why) || !Free(from, map, x, y, out why))
        {
            from.SendMessage(0x22, why);
            return false;
        }

        var filling = FillsNext(map, x, y);
        var result = DigResult.Dug;

        // Одна копка двигает не только свой тайл, но и борта ямы. Без пачки каждый такой
        // тайл рассылал бы всем вокруг отдельный блок карты.
        MahaonMapEdits.BeginBatch();

        try
        {
            if (filling)
            {
                if (!Fill(map, x, y, MahaonDemolition.ReasonFor(from, FillTag)))
                {
                    from.SendMessage(0x3B2, "Здесь и так ровно.");
                    return false;
                }
            }
            else
            {
                result = Dig(map, x, y, MahaonDemolition.ReasonFor(from, DigTag));

                switch (result)
                {
                    case DigResult.Forbidden:
                    case DigResult.Blocked:
                        from.SendMessage(0x22, "Здесь не копается.");
                        return false;

                    case DigResult.TooDeep:
                        from.SendMessage(0x3B2, "Глубже не выйдет.");
                        return false;
                }
            }

            Smooth(
                from, map, x, y, GroundZ(map, x, y), !filling,
                MahaonDemolition.ReasonFor(from, filling ? FillTag : DigTag)
            );

            // Рельеф изменился — значит вода могла и прийти, и уйти. Пересчитываем
            // разлив вокруг: прокопал канаву к реке — налилось, засыпал перемычку —
            // высохло.
            MahaonWaterFlow.Recalculate(map, x, y);
        }
        finally
        {
            MahaonMapEdits.EndBatch();
        }

        Effect(from, x, y);

        from.SendMessage(
            0x59,
            filling
                ? "Ты засыпаешь яму."
                : IsWater(map, x, y)
                    ? "В яму пошла вода."
                    : "Ты выкапываешь землю."
        );

        Wear(from, tool);

        return true;
    }

    /// <summary>
    ///     Тянет борта за тайлом, который только что переехал на новую высоту, чтобы
    ///     получилось углубление, а не ступенька: сосед, отставший больше чем на MaxStep,
    ///     едет следом, и так волной наружу, пока разница не сойдёт.
    ///
    ///     Тянем строго в свою сторону. Копка опускает борта, засыпка их поднимает — иначе
    ///     копка у края чужого пруда начала бы этот пруд засыпать.
    ///
    ///     Природный обрыв рядом (скала, берег, край плато) не трогаем вовсе: ему нельзя
    ///     ни ниже предела, ни выше целины, так что он отсеивается сам. Он и так рисуется
    ///     склоном — срезать гору лопатой никто не просил.
    /// </summary>
    private static void Smooth(Mobile from, Map map, int x, int y, int z, bool lowering, string reason)
    {
        var plan = new Dictionary<(int X, int Y), int> { [(x, y)] = z };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((x, y));

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            var want = (lowering ? 1 : -1) * MaxStep + plan[(cx, cy)];

            // По восьми направлениям: угол тайла делят и диагональные соседи, так что
            // через диагональ ступенька видна не хуже.
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    var nx = cx + dx;
                    var ny = cy + dy;

                    if (nx < 0 || ny < 0 || nx >= map.Width || ny >= map.Height)
                    {
                        continue;
                    }

                    var key = (nx, ny);
                    var nz = plan.TryGetValue(key, out var planned) ? planned : GroundZ(map, nx, ny);

                    if (lowering ? nz <= want : nz >= want)
                    {
                        continue;
                    }

                    if (want < FloorZ(map, nx, ny) || want > OriginalZ(map, nx, ny))
                    {
                        continue;
                    }

                    if (!CanDigHere(map, nx, ny, out _) || !Free(from, map, nx, ny, out _))
                    {
                        continue;
                    }

                    plan[key] = want;
                    queue.Enqueue(key);
                }
            }
        }

        foreach (var ((px, py), pz) in plan)
        {
            if (px == x && py == y)
            {
                continue; // этот тайл уже поставили Dig или Fill
            }

            MahaonMapEdits.SetLand(map, px, py, MahaonMapEdits.LandAt(map, px, py).Graphic, pz, reason);
        }
    }

    /// <summary>
    ///     Куда повернёт следующий взмах: true — засыпать, false — копать.
    ///
    ///     Засыпаем, если в прошлый раз тут засыпали и засыпать ещё есть что, либо если
    ///     копать уже некуда (дно). Вернулись на исходную высоту — условие гаснет само, и
    ///     взмах снова становится копкой; иначе лопата застряла бы на засыпке навсегда.
    /// </summary>
    public static bool FillsNext(Map map, int x, int y)
    {
        var original = OriginalZ(map, x, y);
        var current = GroundZ(map, x, y);

        if (current >= original)
        {
            return false;
        }

        return LastWasFill(map, x, y) || IsWater(map, x, y) || current <= FloorZ(map, x, y);
    }

    /// <summary>
    ///     Последняя правка земли на этом тайле была засыпкой? Журнал смотрим с конца:
    ///     нам нужна только самая свежая запись про это место.
    /// </summary>
    private static bool LastWasFill(Map map, int x, int y)
    {
        var journal = MahaonMapEdits.Entries;

        for (var i = journal.Count - 1; i >= 0; i--)
        {
            var entry = journal[i];

            if (entry.Kind == MapEditKind.SetLand && entry.MapId == map.MapID && entry.X == x && entry.Y == y)
            {
                return entry.Reason.StartsWith(FillTag, StringComparison.Ordinal);
            }
        }

        return false;
    }

    /// <summary>
    ///     Свободен ли тайл для земляных работ. CanDigHere спрашивает про место вообще,
    ///     этот — про то, что на тайле лежит и стоит прямо сейчас.
    /// </summary>
    private static bool Free(Mobile from, Map map, int x, int y, out string why)
    {
        // На тайле кто-то стоит. Землю под ногами двигать нельзя: мобиль сохраняет свою
        // высоту, а не съезжает вместе с землёй, — и остался бы висеть над ямой. Попутно
        // закрывается очевидный способ погадить соседу.
        foreach (var standing in map.GetMobilesAt(new Point2D(x, y)))
        {
            if (standing.Hidden)
            {
                continue;
            }

            why = standing == from ? "Из-под своих же ног не выкопаешь." : "Там кто-то стоит.";
            return false;
        }

        // Поверх земли нарисована статика — дорога, пол, стена, мост. Двигать землю под
        // ней значит оставить её висеть в воздухе, а то и подрыть стену. Мелочь вроде
        // травы и цветов сюда не попадает: у неё нет ни Surface, ни Wall, ни Impassable.
        var ground = GroundZ(map, x, y);

        foreach (var tile in map.Tiles.GetStaticTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

            if ((data.Flags & (TileFlag.Surface | TileFlag.Wall | TileFlag.Impassable)) == 0)
            {
                continue;
            }

            // Вода — не постройка. Берег, русло и мелкие речки в ультиме нарисованы водяной
            // статикой поверх песка, и она тоже помечена Impassable; принимать её за стену
            // значит запретить копать весь берег, то есть ровно то место, с которого канаву
            // к воде и начинают. Песок под ней опускается, вода остаётся на своём уровне.
            if ((data.Flags & TileFlag.Wet) != 0)
            {
                continue;
            }

            // Подземелье под ногами и мост высоко над головой к этой земле отношения не имеют.
            if (tile.Z + data.CalcHeight < ground - 4 || tile.Z > ground + 20)
            {
                continue;
            }

            why = "Тут поверх земли что-то лежит — лопатой не подступиться.";
            return false;
        }

        why = null;
        return true;
    }

    /// <summary>Замах и звук — те же, что у кирки по руде (Mining: действие 11, звуки 0x125/0x126).</summary>
    private static void Effect(Mobile from, int x, int y)
    {
        from.Direction = from.GetDirectionTo(new Point2D(x, y));
        from.Animate(11, 5, 1, true, false, 0);
        from.PlaySound(Utility.RandomBool() ? 0x125 : 0x126);
    }

    /// <summary>Износ инструмента. Сломался — удаляем, как это делает добыча.</summary>
    private static void Wear(Mobile from, Item tool)
    {
        if (tool is not Items.IUsesRemaining uses)
        {
            return;
        }

        if (uses.UsesRemaining > 0)
        {
            uses.UsesRemaining--;
        }

        if (uses.UsesRemaining < 1)
        {
            tool.Delete();
            from.SendLocalizedMessage(1044038); // You have worn out your tool!
        }
    }
}
