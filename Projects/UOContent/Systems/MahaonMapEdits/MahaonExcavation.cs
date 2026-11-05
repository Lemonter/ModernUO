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
///     дальше — глубже, до самого дна карты. Воду сама копка не наливает никогда: вода
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
    /// <summary>
    ///     Ниже этой высоты земля не опускается.
    ///
    ///     Это не игровое правило, а предел самой карты: высота тайла хранится одним
    ///     байтом со знаком, и выйти за него нельзя физически. Прежний игровой предел
    ///     («пять единиц от целины») снят: он не давал довести канаву до воды там, где
    ///     берег заметно выше реки, а это как раз обычный случай.
    /// </summary>
    public const int MinZ = -127;





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

    /// <summary>
    ///     Насколько далеко от дома земля считается укреплённой.
    ///
    ///     Под домом копать было нельзя и раньше — иначе он повисал бы над ямой. Но
    ///     прогибается не только свой тайл: борта тянутся волной во всю глубину ямы, и
    ///     каньон, вырытый впритык, утащил бы дом за собой. Поэтому запрет отодвинут на
    ///     пять тайлов от границ постройки.
    ///
    ///     Забор отдельно проверять не нужно: он всегда принадлежит дому, а его радиус
    ///     (MahaonHouseFenceSystem.AllowedRadii) не превышает пяти. То есть любая
    ///     огороженная территория целиком лежит внутри этой же полосы — и спрашивать
    ///     реестр заборов, который перебирает все дома шарда, на каждый тайл не придётся.
    /// </summary>
    public const int HouseGuard = 5;

    /// <summary>Есть ли рядом постройка. Через сектора, а не перебором: этот вопрос
    /// задаётся на каждый тайл ямы и на каждый тайл пересчёта разлива.</summary>
    private static bool NearHouse(Map map, int x, int y)
    {
        foreach (var _ in map.GetMultisInRange<BaseHouse>(new Point2D(x, y), HouseGuard))
        {
            return true;
        }

        return false;
    }

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

        if (NearHouse(map, x, y))
        {
            why = "Это укреплённая земля — у дома она не прогибается.";
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
    ///     Копает один тайл. Пометка общая на всю яму, чтобы вся работа откатывалась разом.
    /// </summary>
    public static DigResult Dig(Map map, int x, int y)
    {
        if (!CanDigHere(map, x, y, out _))
        {
            return DigResult.Forbidden;
        }

        // Налитую воду копают дальше: дно под ней настоящее, и яму можно углубить. Море
        // и русло сюда не доходят — их отсекает CanDigHere.

        var current = GroundZ(map, x, y);

        if (current <= MinZ)
        {
            return DigResult.TooDeep;
        }

        var newZ = current - 1;

        // Воду тут не наливаем. Дно ямы — сухая земля, а вода приходит только по
        // рельефу, из настоящего источника (MahaonWaterFlow). Раньше было наоборот:
        // яма на последней лопате сама объявлялась прудом, и пруд появлялся посреди
        // сухого луга.
        MahaonSuccession.MoveTrees(map, x, y, newZ);
        MahaonMapEdits.SetLand(map, x, y, MahaonSuccession.RandomFurrow(), newZ);
        MahaonSuccession.Dug(map, x, y);

        return DigResult.Dug;
    }

    /// <summary>
    ///     Засыпает тайл обратно на единицу. Внутриигровая отмена: игрок правит свою яму
    ///     сам, не дёргая ГМ.
    /// </summary>
    public static bool Fill(Map map, int x, int y)
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
            : (ushort)MahaonSuccession.Furrows[0];

        var newZ = current + 1;

        var back = newZ >= original;

        MahaonSuccession.MoveTrees(map, x, y, newZ);

        // Вернулись на исходную высоту — но не на исходный вид: свежая засыпка это голый
        // грунт, а дёрн нарастает сам, за пару сезонов (MahaonSuccession). Мгновенная
        // трава на месте только что заваленной ямы и была тем, что выдавало подделку.
        MahaonMapEdits.SetLand(
            map, x, y,
            back ? MahaonSuccession.Bare[Utility.Random(MahaonSuccession.Bare.Length)] : MahaonSuccession.RandomFurrow(),
            newZ);

        if (back)
        {
            MahaonSuccession.Filled(map, x, y);
        }
        else
        {
            MahaonSuccession.Dug(map, x, y);
        }

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
    ///     Один взмах лопатой по земле. Копать или закапывать — решает РЕЖИМ, выбранный
    ///     игроком в меню лопаты (MahaonDigMode), а не лопата за него.
    ///
    ///     Раньше направление выводилось из журнала правок: что тут делали в прошлый раз,
    ///     то и продолжаем, а на дне разворачиваемся сами. Это значило, что посреди работы
    ///     лопата молча меняла смысл клика, и предсказать её можно было только зная
    ///     историю тайла.
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

        var filling = MahaonDigMode.Of(from) == DigMode.Fill;
        var result = DigResult.Dug;

        // Одна копка двигает не только свой тайл, но и борта ямы. Без пачки каждый такой
        // тайл рассылал бы всем вокруг отдельный блок карты.
        MahaonMapEdits.BeginBatch();

        try
        {
            if (filling)
            {
                if (!Fill(map, x, y))
                {
                    from.SendMessage(0x3B2, "Здесь и так ровно.");
                    return false;
                }
            }
            else
            {
                result = Dig(map, x, y);

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

            Smooth(from, map, x, y, GroundZ(map, x, y), !filling);

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

        Hint(from, map, x, y, filling);

        Wear(from, tool);

        return true;
    }

    /// <summary>
    ///     Тянет борта за тайлом, который только что переехал на новую высоту, чтобы
    ///     получилось углубление, а не ступенька: сосед, отставший больше чем на MaxStep,
    ///     едет следом, и так волной наружу, пока разница не сойдёт.
    ///
    ///     Ширину борта мы НЕ ограничиваем, и это осознанно: при MaxStep в единицу ширина
    ///     ямы равна её глубине, так что глубокая яма — это воронка во всю глубину, то есть
    ///     каньон. Так и задумано; обрыв вместо склона выглядел бы обрезанным.
    ///
    ///     Платим за это объёмом журнала: один взмах на глубине d переписывает порядка
    ///     (2d+1)² тайлов, и каждый — отдельной записью.
    ///
    ///     Тянем строго в свою сторону. Копка опускает борта, засыпка их поднимает — иначе
    ///     копка у края чужого пруда начала бы этот пруд засыпать.
    ///
    ///     Природный обрыв рядом (скала, берег, край плато) не трогаем вовсе: ему нельзя
    ///     ни ниже предела, ни выше целины, так что он отсеивается сам. Он и так рисуется
    ///     склоном — срезать гору лопатой никто не просил.
    /// </summary>
    private static void Smooth(Mobile from, Map map, int x, int y, int z, bool lowering)
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

                    if (want < MinZ || want > OriginalZ(map, nx, ny))
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

            MahaonSuccession.MoveTrees(map, px, py, pz);
            MahaonMapEdits.SetLand(map, px, py, MahaonMapEdits.LandAt(map, px, py).Graphic, pz);
        }
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

            // Пень — наших же рук дело: его MoveTrees переставляет на новую высоту вместе
            // с землёй. Считать его преградой значило бы, что первое же срубленное копкой
            // дерево навсегда запирает свой тайл.
            if (MahaonSuccession.IsStump(tile.ID))
            {
                continue;
            }

            // Дерево не преграда, а будущий пень: MoveTrees сводит его до того, как земля
            // поедет.
            if (MahaonSuccession.IsTree(tile.ID))
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

    /// <summary>Докуда смотрим в поисках воды ради подсказки. Дальше это уже не «рядом».</summary>
    private const int HintLook = 12;

    /// <summary>
    ///     Одна строка о том, почему в яме сухо, прямо во время копки.
    ///
    ///     Раньше система молчала: вода либо приходила, либо нет, и отличить «копай
    ///     глубже» от «канава перегорожена» можно было только ГМ-командой. А это два
    ///     совершенно разных действия, и игрок, выбрав не то, копает впустую.
    ///
    ///     Полный разбор (MahaonWaterFlow.Explain) тут не зовём: он повторяет весь обход
    ///     разлива, а взмах и так уже один раз его посчитал. Подсказка довольствуется
    ///     уровнем ближайшей воды — этого хватает, чтобы различить два случая.
    /// </summary>
    private static void Hint(Mobile from, Map map, int x, int y, bool filling)
    {
        if (filling || IsWater(map, x, y))
        {
            return;
        }

        if (!MahaonWaterFlow.NearestWater(map, x, y, HintLook, out var level, out var distance))
        {
            return; // воды рядом нет — подсказывать не о чем, это просто яма
        }

        var ground = GroundZ(map, x, y);

        from.SendMessage(
            0x3B2,
            ground < level
                ? $"Вода в {distance} тайлах, её уровень {level}, дно у тебя ниже — значит канава где-то перегорожена."
                : $"Вода в {distance} тайлах, её уровень {level}. Дно {ground} — выше воды, копай ниже {level}."
        );
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
