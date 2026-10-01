using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonMapEdits;

/// <summary>Один статик в блоке 8x8. Координаты — смещение внутри блока, как в файле.</summary>
public readonly record struct MapStatic(ushort Graphic, byte X, byte Y, sbyte Z, short Hue);

/// <summary>Один тайл земли.</summary>
public readonly record struct MapLand(ushort Graphic, sbyte Z);

public enum MapEditKind
{
    RemoveStatic,
    AddStatic,
    SetLand
}

/// <summary>
///     Одна правка карты. Неизменяемая запись в журнале — вся суть отката в том, что
///     правки не изменяют друг друга, а только накапливаются.
/// </summary>
public sealed class MapEditEntry
{
    public int Id;
    public DateTime When;
    public int MapId;
    public int X;
    public int Y;
    public int Z;
    public int Graphic;
    public int Hue;
    public MapEditKind Kind;

    /// <summary>Откуда взялась правка: «осада Британии», «бот взорвал дом», «рука ГМ».
    /// По этой строке правки и откатываются пачкой.</summary>
    public string Reason = "";

    public override string ToString() =>
        $"#{Id} {Kind} ({X}, {Y}, {Z}) гр.{Graphic:X4} — {Reason}";
}

/// <summary>
///     Изменения карты: снос статики, постройка мостов, рытьё рвов.
///
///     ---- Зачем ----
///
///     Предметами такого не сделать. Добавить статик предметом можно, а убрать
///     нарисованный в карте — нельзя ничем: клиент берёт статику из своих файлов и сервера
///     об этом не спрашивает.
///
///     ---- Почему журнал, а не «текущее состояние» ----
///
///     Первая версия хранила желаемое состояние блока: список того, что в нём должно быть.
///     Работало, но откатить позволяло только блок целиком — а блок это 8x8, и в него
///     запросто попадают и снесённая стена, и чужой мост, поставленный неделей раньше.
///
///     Теперь источник истины — журнал правок, а состояние блока из него ВЫВОДИТСЯ:
///     берём слепок «как было» и проигрываем правки по порядку. Отсюда бесплатно получаются
///     три вещи: откат любой отдельной правки, откат всего, что сделало одно событие (по
///     Reason), и возможность посмотреть, кто вообще это натворил.
///
///     Слепок снимается при первом касании блока и с ЖИВОЙ карты, то есть уже с заплатами
///     EA — так их правки сохраняются сами собой. И он же не даёт запеканию удвоиться:
///     после первого запекания живая карта уже содержит наши правки, и считай мы состояние
///     от неё, второе запекание наложило бы всё повторно.
/// </summary>
public sealed class MahaonMapEdits : GenericPersistence
{
    private static MahaonMapEdits _instance;

    public MahaonMapEdits() : base("MahaonMapEdits", 1)
    {
    }

    public static void Configure() => _instance = new MahaonMapEdits();

    /// <summary>Журнал. Порядок = порядок применения.</summary>
    private static readonly List<MapEditEntry> Journal = new();

    private static int _nextId = 1;

    /// <summary>Слепки «как было», по (карта, блок). Снимаются один раз и живут вечно:
    /// без них откат не к чему возвращать.</summary>
    private static readonly Dictionary<(int MapId, int BlockId), List<MapStatic>> OriginalStatics = new();

    private static readonly Dictionary<(int MapId, int BlockId), MapLand[]> OriginalLand = new();

    /// <summary>Выведенное состояние. Сбрасывается при любой правке и откате.</summary>
    private static Dictionary<(int MapId, int BlockId), BlockState> _derived;

    public sealed class BlockState
    {
        public List<MapStatic> Statics;
        public MapLand[] Land;
        public bool StaticsTouched;
        public bool LandTouched => Land != null;
    }

    public static IReadOnlyList<MapEditEntry> Entries => Journal;

    /// <summary>
    ///     Сообщает, что состояние блока изменилось: (карта, блок). На это подписан живой
    ///     слой — он и переносит изменение в TileMatrix и к игрокам.
    ///
    ///     Событие обязано вызываться и при откате, причём даже когда блок из правок
    ///     ушёл совсем: иначе отменённый снос остался бы снесённым у всех, кто уже стоит
    ///     рядом, — а это ровно то, ради чего откат и делался.
    /// </summary>
    public static event Action<int, int> BlockChanged;

    public static int BlockIdFor(Map map, int x, int y) => (x >> 3) * (map.Height >> 3) + (y >> 3);

    /// <summary>Слепок «как было» — нужен живому слою, чтобы возвращать блок при откате.</summary>
    public static bool TryGetOriginalStatics(int mapId, int blockId, out List<MapStatic> tiles) =>
        OriginalStatics.TryGetValue((mapId, blockId), out tiles);

    public static bool TryGetOriginalLand(int mapId, int blockId, out MapLand[] land) =>
        OriginalLand.TryGetValue((mapId, blockId), out land);

    private static void NotifyBlocks(IEnumerable<(int MapId, int BlockId)> blocks)
    {
        if (BlockChanged == null)
        {
            return;
        }

        foreach (var (mapId, blockId) in blocks)
        {
            BlockChanged(mapId, blockId);
        }
    }

    /// <summary>Блоки, которых касаются эти записи журнала.</summary>
    private static HashSet<(int, int)> BlocksOf(IEnumerable<MapEditEntry> entries)
    {
        var set = new HashSet<(int, int)>();

        foreach (var e in entries)
        {
            var map = Map.Maps[e.MapId];

            if (map != null)
            {
                set.Add((e.MapId, BlockIdFor(map, e.X, e.Y)));
            }
        }

        return set;
    }

    // ---- Запись правок ----------------------------------------------------------------

    /// <summary>Убирает статику карты в точке. graphic = null — всю, что там есть.</summary>
    public static int RemoveStatics(Map map, int x, int y, int? graphic, string reason)
    {
        EnsureSnapshot(map, x, y);

        var present = StaticsAt(map, x, y);
        var removed = 0;

        foreach (var s in present)
        {
            if (graphic != null && s.Graphic != graphic.Value)
            {
                continue;
            }

            Append(
                new MapEditEntry
                {
                    MapId = map.MapID, X = x, Y = y, Z = s.Z,
                    Graphic = s.Graphic, Kind = MapEditKind.RemoveStatic, Reason = reason
                }
            );

            removed++;
        }

        return removed;
    }

    public static void AddStatic(Map map, int x, int y, int z, int graphic, int hue, string reason)
    {
        EnsureSnapshot(map, x, y);

        Append(
            new MapEditEntry
            {
                MapId = map.MapID, X = x, Y = y, Z = z,
                Graphic = graphic, Hue = hue, Kind = MapEditKind.AddStatic, Reason = reason
            }
        );
    }

    public static void SetLand(Map map, int x, int y, int graphic, int z, string reason)
    {
        EnsureSnapshotLand(map, x, y);

        Append(
            new MapEditEntry
            {
                MapId = map.MapID, X = x, Y = y, Z = z,
                Graphic = graphic, Kind = MapEditKind.SetLand, Reason = reason
            }
        );
    }

    private static void Append(MapEditEntry entry)
    {
        entry.Id = _nextId++;
        entry.When = Core.Now;

        Journal.Add(entry);

        var map = Map.Maps[entry.MapId];

        if (map == null)
        {
            _derived = null;
            return;
        }

        var key = (entry.MapId, BlockIdFor(map, entry.X, entry.Y));

        // Новую правку накладываем на уже посчитанное состояние, а не пересчитываем всё.
        //
        // Раньше здесь было _derived = null, и это дорого обходилось: следующее же
        // обращение проигрывало ВЕСЬ журнал заново. Один подрыв на полтора десятка тайлов
        // при журнале в тысячи записей — столько же полных проигрываний подряд, и чем
        // дольше живёт сервер, тем медленнее каждый удар киркой. Добавление правки по
        // своей природе наращивающее, так что и считать его надо наращиванием.
        //
        // Откат — другое дело: он выдёргивает запись из середины, и там пересчёт
        // неизбежен. Но откат редок, а правки идут потоком.
        if (_derived != null)
        {
            if (!_derived.TryGetValue(key, out var state))
            {
                state = NewBlockState(key);
                _derived[key] = state;
            }

            Apply(map, state, entry, key);
        }

        BlockChanged?.Invoke(entry.MapId, key.Item2);
    }

    /// <summary>Состояние блока «как было» — отправная точка для проигрывания правок.</summary>
    private static BlockState NewBlockState((int MapId, int BlockId) key) =>
        new()
        {
            Statics = OriginalStatics.TryGetValue(key, out var snapshot)
                ? new List<MapStatic>(snapshot)
                : new List<MapStatic>()
        };

    /// <summary>
    ///     Пересчитать выведенное состояние с нуля. Обычно не нужно — наращивание и откат
    ///     держат его в порядке сами, — но это дешёвая страховка, если однажды покажется,
    ///     что состояние разъехалось с журналом.
    /// </summary>
    public static void Rebuild() => _derived = null;

    // ---- Откат -------------------------------------------------------------------------

    /// <summary>Откатывает одну правку по номеру.</summary>
    public static bool Undo(int id)
    {
        for (var i = 0; i < Journal.Count; i++)
        {
            if (Journal[i].Id != id)
            {
                continue;
            }

            var removed = Journal[i];

            Journal.RemoveAt(i);
            _derived = null;

            NotifyBlocks(BlocksOf(new[] { removed }));

            return true;
        }

        return false;
    }

    /// <summary>Откатывает все правки одного события — «отменить всю осаду».</summary>
    public static int UndoByReason(string reason)
    {
        var doomed = Journal.FindAll(e => e.Reason.Equals(reason, StringComparison.OrdinalIgnoreCase));

        if (doomed.Count == 0)
        {
            return 0;
        }

        var blocks = BlocksOf(doomed);

        Journal.RemoveAll(e => e.Reason.Equals(reason, StringComparison.OrdinalIgnoreCase));
        _derived = null;

        NotifyBlocks(blocks);

        return doomed.Count;
    }

    /// <summary>Откатывает последние N правок.</summary>
    public static int UndoLast(int count)
    {
        var doomed = new List<MapEditEntry>();

        while (doomed.Count < count && Journal.Count > 0)
        {
            doomed.Add(Journal[^1]);
            Journal.RemoveAt(Journal.Count - 1);
        }

        if (doomed.Count == 0)
        {
            return 0;
        }

        _derived = null;
        NotifyBlocks(BlocksOf(doomed));

        return doomed.Count;
    }

    /// <summary>Откатывает всё. Слепки остаются — они и есть исходная карта.</summary>
    public static int UndoAll()
    {
        var count = Journal.Count;
        var blocks = BlocksOf(Journal);

        Journal.Clear();
        _derived = null;

        NotifyBlocks(blocks);

        return count;
    }

    /// <summary>
    ///     Закрепляет запечённое: текущее состояние становится новым «как было», а журнал
    ///     очищается.
    ///
    ///     Это недостающее звено всего замысла. Запекание кладёт правки в файлы карты, но
    ///     из журнала их не убирает — и без этого шага список растёт вечно, а каждому
    ///     входящему шлётся то, что у него уже лежит в собственных stadif/mapdif. Закрепив,
    ///     мы возвращаем живой слой к тому, ради чего он задуман: в нём остаётся только
    ///     свежее, ещё не разошедшееся по игрокам.
    ///
    ///     Делается ОТДЕЛЬНОЙ командой, а не автоматически после запекания, и причина
    ///     важная: между «запекли» и «у всех новые файлы» проходит время. Закрепив раньше,
    ///     мы перестанем слать правки тем, кто файлы ещё не получил, и они увидят старый
    ///     мир, пока сервер будет уверен в новом.
    ///
    ///     После закрепления откат этих правок невозможен: снимок «как было» перезаписан.
    ///     Вернуть можно только новой правкой.
    /// </summary>
    public static int Commit()
    {
        var affected = new HashSet<(int, int)>();

        foreach (var ((mapId, blockId), state) in Derived)
        {
            var key = (mapId, blockId);

            if (state.StaticsTouched)
            {
                OriginalStatics[key] = new List<MapStatic>(state.Statics);
            }

            if (state.LandTouched)
            {
                OriginalLand[key] = (MapLand[])state.Land.Clone();
            }

            affected.Add(key);
        }

        var count = Journal.Count;

        Journal.Clear();
        _derived = null;

        // Оповещаем: живой слой должен перестать считать эти блоки изменёнными. Само
        // состояние при этом не меняется — новый слепок равен тому, что и так стояло.
        NotifyBlocks(affected);

        return count;
    }

    /// <summary>Правки, задевшие этот блок, — чтобы было что показать перед откатом.</summary>
    public static List<MapEditEntry> EntriesInBlock(Map map, int x, int y)
    {
        var blockId = BlockIdFor(map, x, y);
        var list = new List<MapEditEntry>();

        foreach (var e in Journal)
        {
            if (e.MapId == map.MapID && BlockIdFor(map, e.X, e.Y) == blockId)
            {
                list.Add(e);
            }
        }

        return list;
    }

    // ---- Выведенное состояние -----------------------------------------------------------

    /// <summary>Состояние всех задетых блоков после проигрывания журнала.</summary>
    public static Dictionary<(int MapId, int BlockId), BlockState> Derived
    {
        get
        {
            if (_derived != null)
            {
                return _derived;
            }

            _derived = new Dictionary<(int, int), BlockState>();

            // Начинаем со слепков — но только тех блоков, которых журнал ещё касается.
            // Иначе откаченный блок остался бы в выводе и попал в запекание без нужды.
            foreach (var e in Journal)
            {
                var map = Map.Maps[e.MapId];

                if (map == null)
                {
                    continue;
                }

                var key = (e.MapId, BlockIdFor(map, e.X, e.Y));

                if (!_derived.TryGetValue(key, out var state))
                {
                    state = NewBlockState(key);
                    _derived[key] = state;
                }

                Apply(map, state, e, key);
            }

            return _derived;
        }
    }

    private static void Apply(
        Map map, BlockState state, MapEditEntry e, (int MapId, int BlockId) key
    )
    {
        var ox = (byte)(e.X & 7);
        var oy = (byte)(e.Y & 7);

        switch (e.Kind)
        {
            case MapEditKind.RemoveStatic:
                for (var i = state.Statics.Count - 1; i >= 0; i--)
                {
                    var s = state.Statics[i];

                    if (s.X == ox && s.Y == oy && s.Graphic == e.Graphic && s.Z == e.Z)
                    {
                        state.Statics.RemoveAt(i);
                        state.StaticsTouched = true;

                        break; // одна правка убирает один тайл
                    }
                }

                break;

            case MapEditKind.AddStatic:
                state.Statics.Add(new MapStatic((ushort)e.Graphic, ox, oy, (sbyte)e.Z, (short)e.Hue));
                state.StaticsTouched = true;

                break;

            case MapEditKind.SetLand:
                if (state.Land == null)
                {
                    state.Land = OriginalLand.TryGetValue(key, out var snapshot)
                        ? (MapLand[])snapshot.Clone()
                        : new MapLand[64];
                }

                // Порядок тайлов в блоке земли построчный: индекс = y*8 + x.
                state.Land[oy * 8 + ox] = new MapLand((ushort)e.Graphic, (sbyte)e.Z);

                break;
        }
    }

    /// <summary>
    ///     Земля в этой точке с учётом правок.
    ///
    ///     Читать её из живой карты нельзя: живой слой можно выключить (mapEdits.live), и
    ///     тогда TileMatrix остаётся нетронутым, а журнал — нет. Всё, что рассуждает о
    ///     текущем состоянии земли (например, глубина ямы), обязано спрашивать здесь, иначе
    ///     при выключенном живом слое копка перестанет накапливаться: каждый удар будет
    ///     читать исходную высоту заново.
    /// </summary>
    public static MapLand LandAt(Map map, int x, int y)
    {
        var key = (map.MapID, BlockIdFor(map, x, y));

        if (Derived.TryGetValue(key, out var state) && state.LandTouched)
        {
            return state.Land[(y & 7) * 8 + (x & 7)];
        }

        var tile = map.Tiles.GetLandTile(x, y);

        return new MapLand((ushort)tile.ID, (sbyte)tile.Z);
    }

    /// <summary>Что сейчас лежит в этой точке с точки зрения наших правок.</summary>
    public static List<MapStatic> StaticsAt(Map map, int x, int y)
    {
        var key = (map.MapID, BlockIdFor(map, x, y));
        var ox = (byte)(x & 7);
        var oy = (byte)(y & 7);
        var result = new List<MapStatic>();

        var source = Derived.TryGetValue(key, out var state)
            ? state.Statics
            : OriginalStatics.TryGetValue(key, out var snapshot)
                ? snapshot
                : null;

        if (source == null)
        {
            return result;
        }

        foreach (var s in source)
        {
            if (s.X == ox && s.Y == oy)
            {
                result.Add(s);
            }
        }

        return result;
    }

    // ---- Слепки --------------------------------------------------------------------------

    private static void EnsureSnapshot(Map map, int x, int y)
    {
        var key = (map.MapID, BlockIdFor(map, x, y));

        if (OriginalStatics.ContainsKey(key))
        {
            return;
        }

        var tiles = new List<MapStatic>();
        var block = map.Tiles.GetStaticBlock(x >> 3, y >> 3);

        for (var ox = 0; ox < 8; ox++)
        {
            for (var oy = 0; oy < 8; oy++)
            {
                var cell = block?[ox][oy];

                if (cell == null)
                {
                    continue;
                }

                for (var i = 0; i < cell.Length; i++)
                {
                    var t = cell[i];
                    tiles.Add(new MapStatic((ushort)t.ID, (byte)ox, (byte)oy, (sbyte)t.Z, (short)t.Hue));
                }
            }
        }

        OriginalStatics[key] = tiles;
    }

    private static void EnsureSnapshotLand(Map map, int x, int y)
    {
        var key = (map.MapID, BlockIdFor(map, x, y));

        if (OriginalLand.ContainsKey(key))
        {
            return;
        }

        var land = new MapLand[64];
        var block = map.Tiles.GetLandBlock(x >> 3, y >> 3);

        for (var i = 0; i < 64; i++)
        {
            land[i] = block == null || i >= block.Length
                ? new MapLand(0, 0)
                : new MapLand((ushort)block[i].ID, (sbyte)block[i].Z);
        }

        OriginalLand[key] = land;
    }

    // ---- Сохранение ----------------------------------------------------------------------

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(_nextId);

        writer.WriteEncodedInt(Journal.Count);

        foreach (var e in Journal)
        {
            writer.WriteEncodedInt(e.Id);
            writer.Write(e.When);
            writer.WriteEncodedInt(e.MapId);
            writer.WriteEncodedInt(e.X);
            writer.WriteEncodedInt(e.Y);
            writer.WriteEncodedInt(e.Z);
            writer.WriteEncodedInt(e.Graphic);
            writer.WriteEncodedInt(e.Hue);
            writer.WriteEncodedInt((int)e.Kind);
            writer.Write(e.Reason);
        }

        writer.WriteEncodedInt(OriginalStatics.Count);

        foreach (var ((mapId, blockId), tiles) in OriginalStatics)
        {
            writer.WriteEncodedInt(mapId);
            writer.WriteEncodedInt(blockId);
            writer.WriteEncodedInt(tiles.Count);

            foreach (var s in tiles)
            {
                writer.Write(s.Graphic);
                writer.Write(s.X);
                writer.Write(s.Y);
                writer.Write(s.Z);
                writer.Write(s.Hue);
            }
        }

        writer.WriteEncodedInt(OriginalLand.Count);

        foreach (var ((mapId, blockId), land) in OriginalLand)
        {
            writer.WriteEncodedInt(mapId);
            writer.WriteEncodedInt(blockId);

            for (var i = 0; i < 64; i++)
            {
                writer.Write(land[i].Graphic);
                writer.Write(land[i].Z);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version
        _nextId = reader.ReadEncodedInt();

        var entries = reader.ReadEncodedInt();

        for (var i = 0; i < entries; i++)
        {
            Journal.Add(
                new MapEditEntry
                {
                    Id = reader.ReadEncodedInt(),
                    When = reader.ReadDateTime(),
                    MapId = reader.ReadEncodedInt(),
                    X = reader.ReadEncodedInt(),
                    Y = reader.ReadEncodedInt(),
                    Z = reader.ReadEncodedInt(),
                    Graphic = reader.ReadEncodedInt(),
                    Hue = reader.ReadEncodedInt(),
                    Kind = (MapEditKind)reader.ReadEncodedInt(),
                    Reason = reader.ReadString() ?? ""
                }
            );
        }

        var staticBlocks = reader.ReadEncodedInt();

        for (var i = 0; i < staticBlocks; i++)
        {
            var mapId = reader.ReadEncodedInt();
            var blockId = reader.ReadEncodedInt();
            var count = reader.ReadEncodedInt();
            var tiles = new List<MapStatic>(count);

            for (var t = 0; t < count; t++)
            {
                tiles.Add(
                    new MapStatic(
                        reader.ReadUShort(), reader.ReadByte(), reader.ReadByte(),
                        reader.ReadSByte(), reader.ReadShort()
                    )
                );
            }

            OriginalStatics[(mapId, blockId)] = tiles;
        }

        var landBlocks = reader.ReadEncodedInt();

        for (var i = 0; i < landBlocks; i++)
        {
            var mapId = reader.ReadEncodedInt();
            var blockId = reader.ReadEncodedInt();
            var land = new MapLand[64];

            for (var t = 0; t < 64; t++)
            {
                land[t] = new MapLand(reader.ReadUShort(), reader.ReadSByte());
            }

            OriginalLand[(mapId, blockId)] = land;
        }

        _derived = null;
    }
}
