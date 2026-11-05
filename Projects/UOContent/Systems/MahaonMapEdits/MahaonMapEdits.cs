using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonMapEdits;

/// <summary>Один статик карты. Координаты — внутри блока, 0..7.</summary>
public readonly record struct MapStatic(ushort Graphic, byte X, byte Y, sbyte Z, short Hue);

/// <summary>Один тайл земли.</summary>
public readonly record struct MapLand(ushort Graphic, sbyte Z);

/// <summary>
///     Изменения карты: снос статики, постройка мостов, рытьё рвов.
///
///     ---- Зачем ----
///
///     Предметами такого не сделать. Добавить статик предметом можно, а убрать
///     нарисованный в карте — нельзя ничем: клиент берёт статику из своих файлов и сервера
///     об этом не спрашивает.
///
///     ---- Два мира, а не журнал ----
///
///     Хранится ровно две вещи, и обе — состояния, а не история:
///
///         ИСХОДНЫЙ мир — слепок блока «как было», снимается при первом касании. Нужен для
///         сброса и для того, чтобы запекание не удвоилось.
///
///         ИЗМЕНЁННЫЙ мир — то, что в блоке сейчас.
///
///     Раньше источником истины был append-only журнал правок, а состояние блока из него
///     выводилось. Замысел был в тонком откате: отменить одну правку, отменить всё, что
///     натворило одно событие. На деле это не окупилось ни разу, зато цена росла сама.
///
///     Цену видно на земляных работах. Борта ямы тянутся волной во всю её глубину, так что
///     ОДИН взмах на глубине d переписывает порядка (2d+1)² тайлов — и каждый ложился в
///     журнал отдельной записью. Яма на сорок вниз — под сотню тысяч записей, канава —
///     миллионы. При этом девять десятых из них бессмысленны: десятая правка одного тайла
///     делает девять предыдущих неотличимыми от мусора, для состояния мира важна только
///     последняя. Журнал же хранил их вечно, сохранение пухло, а разливу воды приходилось
///     проходить его целиком на каждый взмах — то есть копка тормозила тем сильнее, чем
///     больше уже выкопано.
///
///     Два состояния не растут вовсе: сколько ни копай один тайл, он остаётся одним тайлом.
///
///     Что потеряно вместе с журналом: откат отдельной правки и откат по событию. Что
///     осталось: сброс блока и сброс всего мира к исходному — а это и есть то, ради чего
///     откат звали на самом деле.
///
///     Слепок снимается с ЖИВОЙ карты, то есть уже с заплатами EA — так их правки
///     сохраняются сами собой. И он же не даёт запеканию удвоиться: после первого запекания
///     живая карта уже содержит наши правки, и считай мы исходное состояние от неё, второе
///     запекание наложило бы всё повторно.
/// </summary>
public sealed class MahaonMapEdits : GenericPersistence
{
    private static MahaonMapEdits _instance;

    public MahaonMapEdits() : base("MahaonMapEdits", 1)
    {
    }

    public static void Configure() => _instance = new MahaonMapEdits();

    /// <summary>Исходный мир: статика блока «как было».</summary>
    private static readonly Dictionary<(int MapId, int BlockId), List<MapStatic>> OriginalStatics = new();

    /// <summary>Исходный мир: земля блока «как была».</summary>
    private static readonly Dictionary<(int MapId, int BlockId), MapLand[]> OriginalLand = new();

    /// <summary>Изменённый мир. Блока тут нет — значит он не тронут.</summary>
    private static readonly Dictionary<(int MapId, int BlockId), BlockState> Changed = new();

    /// <summary>
    ///     Состояние одного блока. Статика и земля заводятся по отдельности и только при
    ///     первом касании: блок, где копали землю, про статику ничего не знает, и выдавать
    ///     его пустой список за «статики здесь нет» нельзя.
    /// </summary>
    public sealed class BlockState
    {
        public List<MapStatic> Statics;
        public MapLand[] Land;

        public bool StaticsTouched => Statics != null;
        public bool LandTouched => Land != null;
    }

    /// <summary>Изменённый мир целиком. Читают живой слой и запекание.</summary>
    public static IReadOnlyDictionary<(int MapId, int BlockId), BlockState> Current => Changed;

    public static int ChangedBlocks => Changed.Count;

    /// <summary>
    ///     Сообщает, что состояние блока изменилось: (карта, блок). На это подписан живой
    ///     слой — он и переносит изменение в TileMatrix и к игрокам.
    ///
    ///     Событие обязано вызываться и при сбросе, причём даже когда блок из изменённых
    ///     ушёл совсем: иначе снесённое осталось бы снесённым у всех, кто уже стоит рядом.
    /// </summary>
    public static event Action<int, int> BlockChanged;

    public static int BlockIdFor(Map map, int x, int y) => (x >> 3) * (map.Height >> 3) + (y >> 3);

    public static bool TryGetOriginalStatics(int mapId, int blockId, out List<MapStatic> tiles) =>
        OriginalStatics.TryGetValue((mapId, blockId), out tiles);

    public static bool TryGetOriginalLand(int mapId, int blockId, out MapLand[] land) =>
        OriginalLand.TryGetValue((mapId, blockId), out land);

    // ---- Правка ---------------------------------------------------------------------------

    /// <summary>Убирает статику карты в точке. graphic = null — всю, что там есть.</summary>
    public static int RemoveStatics(Map map, int x, int y, int? graphic)
    {
        var state = TouchStatics(map, x, y);
        var ox = (byte)(x & 7);
        var oy = (byte)(y & 7);
        var removed = 0;

        for (var i = state.Statics.Count - 1; i >= 0; i--)
        {
            var s = state.Statics[i];

            if (s.X != ox || s.Y != oy || graphic != null && s.Graphic != graphic.Value)
            {
                continue;
            }

            state.Statics.RemoveAt(i);
            removed++;
        }

        if (removed > 0)
        {
            Touched(map, x, y);
        }

        return removed;
    }

    public static void AddStatic(Map map, int x, int y, int z, int graphic, int hue)
    {
        var state = TouchStatics(map, x, y);

        state.Statics.Add(new MapStatic((ushort)graphic, (byte)(x & 7), (byte)(y & 7), (sbyte)z, (short)hue));

        Touched(map, x, y);
    }

    public static void SetLand(Map map, int x, int y, int graphic, int z)
    {
        var state = TouchLand(map, x, y);

        // Порядок тайлов в блоке земли построчный: индекс = y*8 + x.
        state.Land[(y & 7) * 8 + (x & 7)] = new MapLand((ushort)graphic, (sbyte)z);

        Touched(map, x, y);
    }

    /// <summary>Состояние блока, готовое принять правку статики. Заодно снимает слепок.</summary>
    private static BlockState TouchStatics(Map map, int x, int y)
    {
        var key = (map.MapID, BlockIdFor(map, x, y));
        var state = StateFor(key);

        if (state.Statics != null)
        {
            return state;
        }

        EnsureSnapshotStatics(map, x, y);
        state.Statics = new List<MapStatic>(OriginalStatics[key]);

        return state;
    }

    /// <summary>То же для земли.</summary>
    private static BlockState TouchLand(Map map, int x, int y)
    {
        var key = (map.MapID, BlockIdFor(map, x, y));
        var state = StateFor(key);

        if (state.Land != null)
        {
            return state;
        }

        EnsureSnapshotLand(map, x, y);
        state.Land = (MapLand[])OriginalLand[key].Clone();

        return state;
    }

    private static BlockState StateFor((int MapId, int BlockId) key)
    {
        if (!Changed.TryGetValue(key, out var state))
        {
            Changed[key] = state = new BlockState();
        }

        return state;
    }

    private static void Touched(Map map, int x, int y)
    {
        var blockId = BlockIdFor(map, x, y);

        if (_batch != null)
        {
            _batch.Add((map.MapID, blockId));
            return;
        }

        BlockChanged?.Invoke(map.MapID, blockId);
    }

    // ---- Пачка правок ------------------------------------------------------------------

    private static HashSet<(int MapId, int BlockId)> _batch;

    /// <summary>
    ///     Начинает пачку: событие о задетых блоках уйдёт один раз в EndBatch, а не на
    ///     каждую правку.
    ///
    ///     Нужно земляным работам. Одна копка сглаживает борта ямы и двигает до сотни
    ///     тайлов; без пачки каждый такой тайл рассылал бы всем вокруг целый блок, а
    ///     клиент на каждый пакет пересобирал бы девять кусков карты.
    ///
    ///     Вкладывать пачки нельзя: внутренний EndBatch разошлёт и то, что набрал внешний.
    ///     Вызывать строго парой, EndBatch в finally.
    /// </summary>
    public static void BeginBatch() => _batch ??= new HashSet<(int, int)>();

    public static void EndBatch()
    {
        var batch = _batch;
        _batch = null;

        if (batch == null)
        {
            return;
        }

        foreach (var (mapId, blockId) in batch)
        {
            BlockChanged?.Invoke(mapId, blockId);
        }
    }

    // ---- Сброс ---------------------------------------------------------------------------

    /// <summary>Возвращает блок к исходному виду. Слепок остаётся — он и есть исходная карта.</summary>
    public static bool ResetBlock(Map map, int x, int y)
    {
        var key = (map.MapID, BlockIdFor(map, x, y));

        if (!Changed.Remove(key))
        {
            return false;
        }

        BlockChanged?.Invoke(key.Item1, key.Item2);

        return true;
    }

    /// <summary>Возвращает к исходному виду весь мир. Отвечает числом блоков.</summary>
    public static int ResetAll()
    {
        var blocks = new List<(int MapId, int BlockId)>(Changed.Keys);

        Changed.Clear();

        foreach (var (mapId, blockId) in blocks)
        {
            BlockChanged?.Invoke(mapId, blockId);
        }

        return blocks.Count;
    }

    /// <summary>
    ///     Закрепляет запечённое: изменённый мир становится новым исходным.
    ///
    ///     Это недостающее звено всего замысла. Запекание кладёт правки в файлы карты, но
    ///     из живого слоя их не убирает — и без этого шага каждому входящему шлётся то, что
    ///     у него уже лежит в собственных stadif/mapdif.
    ///
    ///     Делается ОТДЕЛЬНОЙ командой, а не автоматически после запекания, и причина
    ///     важная: между «запекли» и «у всех новые файлы» проходит время. Закрепив раньше,
    ///     мы перестанем слать правки тем, кто файлы ещё не получил, и они увидят старый
    ///     мир, пока сервер будет уверен в новом.
    ///
    ///     После закрепления сброс этих правок невозможен: исходный мир перезаписан.
    /// </summary>
    public static int Commit()
    {
        var affected = new List<(int MapId, int BlockId)>(Changed.Count);

        foreach (var (key, state) in Changed)
        {
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

        Changed.Clear();

        // Оповещаем: живой слой должен перестать считать эти блоки изменёнными. Само
        // состояние при этом не меняется — новый слепок равен тому, что и так стояло.
        foreach (var (mapId, blockId) in affected)
        {
            BlockChanged?.Invoke(mapId, blockId);
        }

        return affected.Count;
    }

    // ---- Чтение ----------------------------------------------------------------------------

    /// <summary>
    ///     Земля в этой точке с учётом правок.
    ///
    ///     Читать её из живой карты нельзя: живой слой можно выключить (mapEdits.live), и
    ///     тогда TileMatrix остаётся нетронутым, а изменённый мир — нет. Всё, что рассуждает
    ///     о текущем состоянии земли (например, глубина ямы), обязано спрашивать здесь,
    ///     иначе при выключенном живом слое копка перестанет накапливаться: каждый удар
    ///     будет читать исходную высоту заново.
    /// </summary>
    public static MapLand LandAt(Map map, int x, int y)
    {
        var key = (map.MapID, BlockIdFor(map, x, y));

        if (Changed.TryGetValue(key, out var state) && state.LandTouched)
        {
            return state.Land[(y & 7) * 8 + (x & 7)];
        }

        var tile = map.Tiles.GetLandTile(x, y);

        return new MapLand((ushort)tile.ID, (sbyte)tile.Z);
    }

    /// <summary>
    ///     Что сейчас лежит в этой точке — с учётом наших правок, а где правок не было, то
    ///     что лежит в самой карте.
    ///
    ///     ---- Чем это было ----
    ///
    ///     Здесь возвращался ПУСТОЙ список, если блока нет среди изменённых. То есть «мы
    ///     блок не трогали» молча означало «статики в нём нет вовсе» — а это почти весь мир.
    ///
    ///     Цена была велика и не видна. Вся вода шарда у берегов нарисована статикой поверх
    ///     песка, и MahaonWaterFlow искал её именно отсюда. Значит воды для него не
    ///     существовало нигде, кроме блоков, где кто-то уже ставил или сносил статику:
    ///     игрок стоял по пояс в море, а сервер отвечал «воды в 24 тайлах нет». Тем же
    ///     слепым местом страдали источники света и решётка занятости.
    /// </summary>
    public static List<MapStatic> StaticsAt(Map map, int x, int y)
    {
        var key = (map.MapID, BlockIdFor(map, x, y));
        var ox = (byte)(x & 7);
        var oy = (byte)(y & 7);
        var result = new List<MapStatic>();

        if (!Changed.TryGetValue(key, out var state) || !state.StaticsTouched)
        {
            // Слепок, если он есть, и ТОЛЬКО потом карта. После закрепления (Commit)
            // слепок и есть текущее состояние: блок ушёл из изменённых, но вернуться к
            // файлу он уже не может.
            if (OriginalStatics.TryGetValue(key, out var snapshot))
            {
                foreach (var s in snapshot)
                {
                    if (s.X == ox && s.Y == oy)
                    {
                        result.Add(s);
                    }
                }

                return result;
            }

            foreach (var t in map.Tiles.GetStaticTiles(x, y))
            {
                result.Add(new MapStatic((ushort)t.ID, ox, oy, (sbyte)t.Z, (short)t.Hue));
            }

            return result;
        }

        foreach (var s in state.Statics)
        {
            if (s.X == ox && s.Y == oy)
            {
                result.Add(s);
            }
        }

        return result;
    }

    /// <summary>Была ли тут вообще правка. Дешевле, чем сравнивать состояния.</summary>
    public static bool IsChanged(Map map, int x, int y) =>
        Changed.ContainsKey((map.MapID, BlockIdFor(map, x, y)));

    // ---- Слепки --------------------------------------------------------------------------

    private static void EnsureSnapshotStatics(Map map, int x, int y)
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

    private const int Version = 1;

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Version);

        WriteStatics(writer, OriginalStatics);
        WriteLand(writer, OriginalLand);

        writer.WriteEncodedInt(Changed.Count);

        foreach (var ((mapId, blockId), state) in Changed)
        {
            writer.WriteEncodedInt(mapId);
            writer.WriteEncodedInt(blockId);

            writer.Write(state.StaticsTouched);

            if (state.StaticsTouched)
            {
                writer.WriteEncodedInt(state.Statics.Count);

                foreach (var s in state.Statics)
                {
                    WriteStatic(writer, s);
                }
            }

            writer.Write(state.LandTouched);

            if (state.LandTouched)
            {
                for (var i = 0; i < 64; i++)
                {
                    writer.Write(state.Land[i].Graphic);
                    writer.Write(state.Land[i].Z);
                }
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        if (version == 0)
        {
            ReadOldJournalFormat(reader);
            return;
        }

        ReadStatics(reader, OriginalStatics);
        ReadLand(reader, OriginalLand);

        var blocks = reader.ReadEncodedInt();

        for (var i = 0; i < blocks; i++)
        {
            var mapId = reader.ReadEncodedInt();
            var blockId = reader.ReadEncodedInt();
            var state = new BlockState();

            if (reader.ReadBool())
            {
                var count = reader.ReadEncodedInt();
                state.Statics = new List<MapStatic>(count);

                for (var t = 0; t < count; t++)
                {
                    state.Statics.Add(ReadStatic(reader));
                }
            }

            if (reader.ReadBool())
            {
                state.Land = new MapLand[64];

                for (var t = 0; t < 64; t++)
                {
                    state.Land[t] = new MapLand(reader.ReadUShort(), reader.ReadSByte());
                }
            }

            Changed[(mapId, blockId)] = state;
        }
    }

    /// <summary>
    ///     Разбирает сохранение с журналом и сворачивает его в изменённый мир.
    ///
    ///     Выполняется один раз: следующее сохранение уйдёт уже новым форматом, и журнала
    ///     в мире не останется. Проигрывать правки надо В ТОМ ЖЕ ПОРЯДКЕ, в каком они
    ///     ложились, — иначе снос и постройка на одном тайле поменяются местами.
    /// </summary>
    private static void ReadOldJournalFormat(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // бывший счётчик номеров

        var entries = reader.ReadEncodedInt();
        var journal = new List<(int MapId, int X, int Y, int Z, int Graphic, int Hue, int Kind)>(entries);

        for (var i = 0; i < entries; i++)
        {
            reader.ReadEncodedInt();  // Id
            reader.ReadDateTime();    // When
            var mapId = reader.ReadEncodedInt();
            var x = reader.ReadEncodedInt();
            var y = reader.ReadEncodedInt();
            var z = reader.ReadEncodedInt();
            var graphic = reader.ReadEncodedInt();
            var hue = reader.ReadEncodedInt();
            var kind = reader.ReadEncodedInt();
            reader.ReadString();      // Reason

            journal.Add((mapId, x, y, z, graphic, hue, kind));
        }

        // Слепки в старом формате лежат ПОСЛЕ журнала, а проигрывать правки без них
        // нельзя: состояние блока начинается именно со слепка.
        ReadStatics(reader, OriginalStatics);
        ReadLand(reader, OriginalLand);

        var replayed = 0;

        foreach (var (mapId, x, y, z, graphic, hue, kind) in journal)
        {
            var map = Map.Maps[mapId];

            if (map == null)
            {
                continue;
            }

            switch (kind)
            {
                case 0: // RemoveStatic — снималась ровно одна плитка с такой графикой и высотой
                {
                    var state = TouchStatics(map, x, y);
                    var ox = (byte)(x & 7);
                    var oy = (byte)(y & 7);

                    for (var i = state.Statics.Count - 1; i >= 0; i--)
                    {
                        var s = state.Statics[i];

                        if (s.X == ox && s.Y == oy && s.Graphic == graphic && s.Z == z)
                        {
                            state.Statics.RemoveAt(i);
                            break;
                        }
                    }

                    break;
                }

                case 1: // AddStatic
                {
                    var state = TouchStatics(map, x, y);
                    state.Statics.Add(
                        new MapStatic((ushort)graphic, (byte)(x & 7), (byte)(y & 7), (sbyte)z, (short)hue)
                    );

                    break;
                }

                case 2: // SetLand
                {
                    var state = TouchLand(map, x, y);
                    state.Land[(y & 7) * 8 + (x & 7)] = new MapLand((ushort)graphic, (sbyte)z);

                    break;
                }
            }

            replayed++;
        }

        if (replayed > 0)
        {
            Utility.PushColor(ConsoleColor.Green);
            Console.WriteLine(
                $"[MapEdits] журнал свёрнут: {replayed} записей -> {Changed.Count} изменённых блоков."
            );
            Utility.PopColor();
        }
    }

    // ---- Мелочи записи --------------------------------------------------------------------

    private static void WriteStatic(IGenericWriter writer, MapStatic s)
    {
        writer.Write(s.Graphic);
        writer.Write(s.X);
        writer.Write(s.Y);
        writer.Write(s.Z);
        writer.Write(s.Hue);
    }

    private static MapStatic ReadStatic(IGenericReader reader) =>
        new(reader.ReadUShort(), reader.ReadByte(), reader.ReadByte(), reader.ReadSByte(), reader.ReadShort());

    private static void WriteStatics(
        IGenericWriter writer, Dictionary<(int MapId, int BlockId), List<MapStatic>> source
    )
    {
        writer.WriteEncodedInt(source.Count);

        foreach (var ((mapId, blockId), tiles) in source)
        {
            writer.WriteEncodedInt(mapId);
            writer.WriteEncodedInt(blockId);
            writer.WriteEncodedInt(tiles.Count);

            foreach (var s in tiles)
            {
                WriteStatic(writer, s);
            }
        }
    }

    private static void ReadStatics(
        IGenericReader reader, Dictionary<(int MapId, int BlockId), List<MapStatic>> target
    )
    {
        var blocks = reader.ReadEncodedInt();

        for (var i = 0; i < blocks; i++)
        {
            var mapId = reader.ReadEncodedInt();
            var blockId = reader.ReadEncodedInt();
            var count = reader.ReadEncodedInt();
            var tiles = new List<MapStatic>(count);

            for (var t = 0; t < count; t++)
            {
                tiles.Add(ReadStatic(reader));
            }

            target[(mapId, blockId)] = tiles;
        }
    }

    private static void WriteLand(
        IGenericWriter writer, Dictionary<(int MapId, int BlockId), MapLand[]> source
    )
    {
        writer.WriteEncodedInt(source.Count);

        foreach (var ((mapId, blockId), land) in source)
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

    private static void ReadLand(
        IGenericReader reader, Dictionary<(int MapId, int BlockId), MapLand[]> target
    )
    {
        var blocks = reader.ReadEncodedInt();

        for (var i = 0; i < blocks; i++)
        {
            var mapId = reader.ReadEncodedInt();
            var blockId = reader.ReadEncodedInt();
            var land = new MapLand[64];

            for (var t = 0; t < 64; t++)
            {
                land[t] = new MapLand(reader.ReadUShort(), reader.ReadSByte());
            }

            target[(mapId, blockId)] = land;
        }
    }
}
