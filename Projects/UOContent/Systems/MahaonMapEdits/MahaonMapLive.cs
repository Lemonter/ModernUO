using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModernUO.CodeGeneratedEvents;
using Server.Mobiles;
using Server.Network;

namespace Server.Systems.MahaonMapEdits;

/// <summary>
///     Живое применение правок карты: сервер видит их сразу, игроки — без перезахода.
///
///     ---- Почему не UltimaLive ----
///
///     У ультимы есть готовый протокол живой карты, и клиент его умеет. Но ULMapLoader
///     подменяет загрузчик карты ЦЕЛИКОМ и читает мир из распакованных .mul в папке шарда
///     через FileStream. Через этот путь пошёл бы каждый загружаемый кусок, а не только
///     изменённые, — то есть плата берётся всё время, пока игрок ходит. Плюс копия мира на
///     диске у каждого и конвертация из UOP, которого у нас как раз и стоит.
///
///     Здесь вместо этого: правки живут у клиента в памяти и подставляются при сборке
///     куска. Штатный загрузчик остаётся на месте, файлы игрока никто не трогает, в
///     установившемся режиме плата нулевая.
///
///     ---- Сервер ----
///
///     TileMatrix.SetStaticBlock/SetLandBlock публичные, так что серверу достаточно
///     заменить блок в памяти — и новое состояние тут же видно ВЕЗДЕ: проходимость, линия
///     видимости, поиск пути, спавн. Без заплат по десятку мест и без проверок в горячем
///     коде.
///
///     ---- Откат ----
///
///     Когда правка отменена и блок перестаёт быть изменённым, надо вернуть слепок «как
///     было» — и в TileMatrix, и игрокам. Иначе отменённый снос остался бы снесённым у
///     всех, кто уже стоит рядом.
///
///     ---- Кому и когда (по дальности) ----
///
///     Игроку нужны только правки вокруг него, а не все правки карты: стоящему в Магинсии
///     незачем знать, что копают в Миноке. Поэтому:
///       - сервер помнит для каждого соединения, какие блоки оно уже получило и в какой
///         версии (Known);
///       - при входе и при каждом переходе игрока в новый блок 8×8 досылаются правленые блоки
///         в радиусе ViewRadiusBlocks — те, которых клиент не знает или знает устаревшими;
///       - живая правка уходит только тем, кто рядом или уже знает этот блок (знает — значит
///         держит его в памяти, и устаревшим держать нельзя);
///       - ушёл и вернулся — получит свежую версию, если блок без него менялся. Откат правки
///         тоже доходит: для клиента, знающего блок, это просто новая версия (слепок).
///     Клиент правки хранит в памяти до выхода из мира — отдельным журналом на каждый фасет:
///     камни переноса в каждом городе гоняют игроков между картами, и вернувшийся на фасет
///     клиент снова подставляет то, что уже знал. Поэтому и здесь список известного не
///     чистится при смене карты (он и так по паре «карта + блок»), а только при входе в мир.
/// </summary>
public static class MahaonMapLive
{
    private const byte MahaonMultiPacketId = 0xF9;
    private const byte SubtypeMapStatics = 4;
    private const byte SubtypeMapLand = 5;

    /// <summary>Выключатель. Если что-то пойдёт не так — правки останутся в журнале, но
    /// ни в карту, ни игрокам не поедут.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>
    ///     Радиус выдачи правок вокруг игрока, блоков 8×8 по каждой оси. Не обзор в 18 тайлов,
    ///     а дальность ЗАГРУЗКИ клиента Unity: он держит чанки по 20 тайлов на 4 чанка вокруг
    ///     (~80 тайлов). Правка, пришедшая в уже загруженный чанк, проигрывается как свежая
    ///     (вода «наливается» в старый пруд), поэтому радиус с запасом: 12 блоков — 96 тайлов.
    /// </summary>
    private const int ViewRadiusBlocks = 12;

    /// <summary>Как часто проверять, не перешёл ли игрок в другой блок.</summary>
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>Что знает одно соединение: версия каждого полученного блока и где игрок
    /// стоял при последней проверке.</summary>
    private sealed class Known
    {
        public readonly Dictionary<(int MapId, int BlockId), int> Blocks = new();
        public int MapId = -1;
        public int BlockX = int.MinValue;
        public int BlockY = int.MinValue;
    }

    /// <summary>Слабая таблица: ушедшее соединение уносит свою запись с собой.</summary>
    private static readonly ConditionalWeakTable<NetState, Known> KnownByState = new();

    /// <summary>Версия блока растёт на каждой правке — по ней видно, что клиент знает
    /// устаревшее.</summary>
    private static readonly Dictionary<(int MapId, int BlockId), int> Versions = new();

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("mapEdits.live", true);
        MahaonMapEdits.BlockChanged += OnBlockChanged;
        EventSink.WorldLoad += ApplyAllToServer;
        Timer.StartTimer(SweepInterval, SweepInterval, Sweep);
    }

    /// <summary>
    ///     Вход в мир — по PlayerLoginEvent, а не по EventSink.Connected.
    ///
    ///     Connected срабатывает в момент, когда соединение привязывается к персонажу, — ещё
    ///     ДО того, как вышедший из игры персонаж вернётся с Map.Internal на свою карту. Проверка
    ///     «правка для карты игрока» в этот момент отсеивала все правки до единой: клиент входил
    ///     в мир без ям и насыпей, а сервер ставил игрока на дно ямы. Клиент видел себя под
    ///     землёй, и канонический потолок прятал вокруг всю землю — чёрные ромбы под водой.
    ///     Срабатывало не всегда: быстрый перезаход (персонаж ещё в мире) карту не терял.
    ///
    ///     Клиент после входа ничего не помнит, поэтому список известного начинается заново.
    /// </summary>
    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile pm)
    {
        if (!Enabled || pm.NetState == null)
        {
            return;
        }

        var known = KnownByState.GetOrCreateValue(pm.NetState);
        known.Blocks.Clear();
        known.MapId = -1;

        SendNearby(pm, known);
    }

    /// <summary>
    ///     Переносит всё накопленное в живую карту. Вызывается после загрузки мира: правки
    ///     лежат в сохранении, а TileMatrix при старте читает файлы и о них не знает.
    /// </summary>
    public static void ApplyAllToServer()
    {
        if (!Enabled)
        {
            return;
        }

        foreach (var ((mapId, blockId), _) in MahaonMapEdits.Current)
        {
            ApplyToServer(mapId, blockId);
        }
    }

    /// <summary>
    ///     Игрок сдвинулся (PlayerMobile.OnLocationChange / OnMapChange): шаг, рекол, гейт,
    ///     телепорт, смена фасета — всё идёт через MoveToWorld, который зовёт эти хуки уже ПОСЛЕ
    ///     отправки новой позиции. Правки уходят в том же потоке сразу за ней, и клиент
    ///     применяет их до того, как отрисует новое место: без мелькания старой земли и без
    ///     «под землёй» после рекола. Обычный шаг внутри блока отсекается сразу.
    /// </summary>
    public static void OnMoved(Mobile m)
    {
        if (!Enabled || m.NetState == null || m.Map == null || m.Map == Map.Internal)
        {
            return;
        }

        var known = KnownByState.GetOrCreateValue(m.NetState);

        if (known.MapId == m.Map.MapID && known.BlockX == m.X >> 3 && known.BlockY == m.Y >> 3)
        {
            return;
        }

        SendNearby(m, known);
    }

    /// <summary>Обход онлайна — страховка на случай перемещения мимо хуков: кто перешёл в
    /// другой блок или сменил карту — досылаем правки вокруг.</summary>
    private static void Sweep()
    {
        if (!Enabled)
        {
            return;
        }

        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile is not { Deleted: false } m || m.Map == null || m.Map == Map.Internal)
            {
                continue;
            }

            var known = KnownByState.GetOrCreateValue(ns);

            if (known.MapId == m.Map.MapID && known.BlockX == m.X >> 3 && known.BlockY == m.Y >> 3)
            {
                continue;
            }

            SendNearby(m, known);
        }
    }

    /// <summary>
    ///     Правленые блоки вокруг игрока, которых клиент не знает или знает устаревшими. Плюс
    ///     откаченные: блок, которого в журнале больше нет, но клиент держит его правленым, —
    ///     ему уходит слепок.
    /// </summary>
    private static void SendNearby(Mobile m, Known known)
    {
        var ns = m.NetState;
        var map = m.Map;

        if (ns == null || map == null || map == Map.Internal)
        {
            return;
        }

        // Смена фасета: журнал старой карты клиент отложил, а не выбросил — список известного
        // остаётся, меняется только карта, вокруг которой смотрим.
        known.MapId = map.MapID;

        known.BlockX = m.X >> 3;
        known.BlockY = m.Y >> 3;

        var blockHeight = map.Height >> 3;

        foreach (var ((mapId, blockId), state) in MahaonMapEdits.Current)
        {
            if (mapId == map.MapID && IsNear(blockId, blockHeight, known) && IsStale(known, mapId, blockId))
            {
                Deliver(ns, known, mapId, blockId, state);
            }
        }

        // Откаченные рядом — их в журнале нет, но клиент знает их в старой версии.
        List<(int, int)> reverted = null;

        foreach (var ((mapId, blockId), version) in known.Blocks)
        {
            if (mapId == map.MapID && version != VersionOf(mapId, blockId) &&
                !MahaonMapEdits.Current.ContainsKey((mapId, blockId)) && IsNear(blockId, blockHeight, known))
            {
                (reverted ??= new List<(int, int)>()).Add((mapId, blockId));
            }
        }

        if (reverted != null)
        {
            foreach (var (mapId, blockId) in reverted)
            {
                Deliver(ns, known, mapId, blockId, null);
            }
        }
    }

    private static bool IsNear(int blockId, int blockHeight, Known known) =>
        Math.Abs(blockId / blockHeight - known.BlockX) <= ViewRadiusBlocks &&
        Math.Abs(blockId % blockHeight - known.BlockY) <= ViewRadiusBlocks;

    private static int VersionOf(int mapId, int blockId) =>
        Versions.TryGetValue((mapId, blockId), out var version) ? version : 0;

    private static bool IsStale(Known known, int mapId, int blockId) =>
        !known.Blocks.TryGetValue((mapId, blockId), out var version) || version != VersionOf(mapId, blockId);

    private static void Deliver(NetState ns, Known known, int mapId, int blockId, MahaonMapEdits.BlockState state)
    {
        SendBlock(ns, mapId, blockId, state);
        known.Blocks[(mapId, blockId)] = VersionOf(mapId, blockId);
    }

    private static void OnBlockChanged(int mapId, int blockId)
    {
        if (!Enabled)
        {
            return;
        }

        ApplyToServer(mapId, blockId);

        Versions[(mapId, blockId)] = VersionOf(mapId, blockId) + 1;
        BroadcastBlock(mapId, blockId);
    }

    // ---- Сервер ------------------------------------------------------------------------

    /// <summary>Заменяет блок в живой карте — или возвращает слепок, если правок не осталось.</summary>
    private static void ApplyToServer(int mapId, int blockId)
    {
        var map = Map.Maps[mapId];

        if (map == null || map == Map.Internal)
        {
            return;
        }

        var blockHeight = map.Height >> 3;
        var bx = blockId / blockHeight;
        var by = blockId % blockHeight;

        MahaonMapEdits.Current.TryGetValue((mapId, blockId), out var state);

        // Статика.
        var statics = state?.StaticsTouched == true
            ? state.Statics
            : MahaonMapEdits.TryGetOriginalStatics(mapId, blockId, out var snapshot)
                ? snapshot
                : null;

        if (statics != null)
        {
            map.Tiles.SetStaticBlock(bx, by, ToBlock(statics));
        }

        // Земля.
        var land = state?.LandTouched == true
            ? state.Land
            : MahaonMapEdits.TryGetOriginalLand(mapId, blockId, out var landSnapshot)
                ? landSnapshot
                : null;

        if (land != null)
        {
            var tiles = new LandTile[64];

            for (var i = 0; i < 64; i++)
            {
                tiles[i] = new LandTile((short)land[i].Graphic, land[i].Z);
            }

            map.Tiles.SetLandBlock(bx, by, tiles);
        }

        // Walkability is cached per chunk and in the bots' nav graph; both would keep the old
        // terrain: creatures walking into a demolished wall's ghost, bots never using a new bridge.
        Engines.Pathing.Nav.NavSystem.InvalidateArea(map, bx << 3, by << 3, 8, 8);
    }

    /// <summary>Плоский список в форму, которую ждёт TileMatrix: [x][y][тайлы].</summary>
    private static StaticTile[][][] ToBlock(List<MapStatic> statics)
    {
        var lists = new List<StaticTile>[8][];

        for (var x = 0; x < 8; x++)
        {
            lists[x] = new List<StaticTile>[8];

            for (var y = 0; y < 8; y++)
            {
                lists[x][y] = new List<StaticTile>();
            }
        }

        foreach (var s in statics)
        {
            lists[s.X & 7][s.Y & 7].Add(new StaticTile(s.Graphic, s.X, s.Y, s.Z, s.Hue));
        }

        var block = new StaticTile[8][][];

        for (var x = 0; x < 8; x++)
        {
            block[x] = new StaticTile[8][];

            for (var y = 0; y < 8; y++)
            {
                block[x][y] = lists[x][y].ToArray();
            }
        }

        return block;
    }

    // ---- Клиенты -----------------------------------------------------------------------

    /// <summary>
    ///     Живая правка — тем, кто рядом, и тем, кто этот блок уже знает (держит в памяти и
    ///     устаревшим держать не должен). Остальные получат его, когда подойдут.
    /// </summary>
    private static void BroadcastBlock(int mapId, int blockId)
    {
        MahaonMapEdits.Current.TryGetValue((mapId, blockId), out var state);

        var map = Map.Maps[mapId];

        if (map == null)
        {
            return;
        }

        var blockHeight = map.Height >> 3;

        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile?.Map?.MapID != mapId)
            {
                continue;
            }

            var known = KnownByState.GetOrCreateValue(ns);

            if (known.MapId != mapId)
            {
                known.MapId = mapId;
                known.BlockX = ns.Mobile.X >> 3;
                known.BlockY = ns.Mobile.Y >> 3;
            }

            if (known.Blocks.ContainsKey((mapId, blockId)) || IsNear(blockId, blockHeight, known))
            {
                Deliver(ns, known, mapId, blockId, state);
            }
        }
    }

    /// <summary>
    ///     Шлёт блок целиком, а не разницу: клиент всё равно пересобирает кусок, а полное
    ///     состояние избавляет от вопроса «в каком порядке применялись правки». Если правок
    ///     в блоке не осталось — уходит слепок, то есть исходный вид.
    /// </summary>
    private static void SendBlock(NetState ns, int mapId, int blockId, MahaonMapEdits.BlockState state)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var statics = state?.StaticsTouched == true
            ? state.Statics
            : MahaonMapEdits.TryGetOriginalStatics(mapId, blockId, out var snapshot)
                ? snapshot
                : null;

        if (statics != null)
        {
            SendStatics(ns, mapId, blockId, statics);
        }

        var land = state?.LandTouched == true
            ? state.Land
            : MahaonMapEdits.TryGetOriginalLand(mapId, blockId, out var landSnapshot)
                ? landSnapshot
                : null;

        if (land != null)
        {
            SendLand(ns, mapId, blockId, land);
        }
    }

    // [подтип:1][карта:1][блок:4][счётчик:2] затем [x:1][y:1][z:1][графика:2] на тайл
    private static void SendStatics(NetState ns, int mapId, int blockId, List<MapStatic> statics)
    {
        var length = 3 + 1 + 1 + 4 + 2 + statics.Count * 5;
        var writer = new SpanWriter(stackalloc byte[length]);

        writer.Write(MahaonMultiPacketId);
        writer.Write((ushort)0); // место под длину — проставит WritePacketLength()
        writer.Write(SubtypeMapStatics);
        writer.Write((byte)mapId);
        writer.Write(blockId);
        writer.Write((ushort)statics.Count);

        foreach (var s in statics)
        {
            writer.Write(s.X);
            writer.Write(s.Y);
            writer.Write(s.Z);
            writer.Write(s.Graphic);
        }

        writer.WritePacketLength();
        ns.Send(writer.Span);
    }

    // [подтип:1][карта:1][блок:4] затем 64 раза [графика:2][z:1]
    private static void SendLand(NetState ns, int mapId, int blockId, MapLand[] land)
    {
        var length = 3 + 1 + 1 + 4 + 64 * 3;
        var writer = new SpanWriter(stackalloc byte[length]);

        writer.Write(MahaonMultiPacketId);
        writer.Write((ushort)0);
        writer.Write(SubtypeMapLand);
        writer.Write((byte)mapId);
        writer.Write(blockId);

        for (var i = 0; i < 64; i++)
        {
            writer.Write(land[i].Graphic);
            writer.Write(land[i].Z);
        }

        writer.WritePacketLength();
        ns.Send(writer.Span);
    }
}
