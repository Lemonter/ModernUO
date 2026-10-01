using System;
using System.Buffers;
using System.Collections.Generic;
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
    ///     Сколько блоков выдаём одному входящему игроку.
    ///
    ///     Это не оптимизация, а предохранитель. Всё накопленное уходит игроку одним
    ///     залпом при входе, и если правок наберётся много тысяч (долгая осада, неделя без
    ///     запекания), залп будет соответствующий. Упёршись в потолок, сервер ругается в
    ///     консоль — потому что лечится это не здесь, а запеканием с закреплением.
    /// </summary>
    private const int MaxBlocksPerLogin = 512;

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("mapEdits.live", true);
        MahaonMapEdits.BlockChanged += OnBlockChanged;
        EventSink.WorldLoad += ApplyAllToServer;
        EventSink.Connected += OnConnected;
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

        foreach (var ((mapId, blockId), _) in MahaonMapEdits.Derived)
        {
            ApplyToServer(mapId, blockId);
        }
    }

    private static void OnConnected(Mobile m)
    {
        if (!Enabled || m.NetState == null)
        {
            return;
        }

        // Правки живут у клиента в памяти, значит при каждом входе их надо выдать заново.
        // Это и есть цена отказа от записи в файлы игрока — зато список короткий: всё
        // старое периодически уходит в запекание и отсюда пропадает.
        var sent = 0;

        foreach (var ((mapId, blockId), state) in MahaonMapEdits.Derived)
        {
            if (mapId != m.Map?.MapID)
            {
                continue;
            }

            if (sent >= MaxBlocksPerLogin)
            {
                // Дальше не шлём — но и молчать нельзя: остаток мира игрок увидит
                // неправленым, и это надо чинить не здесь, а запеканием.
                Utility.PushColor(System.ConsoleColor.Yellow);
                Console.WriteLine(
                    $"[MapEdits] {m.Name}: правок больше {MaxBlocksPerLogin}, остальные не отправлены. " +
                    "Пора запечь ([MapBake) и закрепить ([MapCommit)."
                );
                Utility.PopColor();

                break;
            }

            SendBlock(m.NetState, mapId, blockId, state);
            sent++;
        }
    }

    private static void OnBlockChanged(int mapId, int blockId)
    {
        if (!Enabled)
        {
            return;
        }

        ApplyToServer(mapId, blockId);
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

        MahaonMapEdits.Derived.TryGetValue((mapId, blockId), out var state);

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

    private static void BroadcastBlock(int mapId, int blockId)
    {
        MahaonMapEdits.Derived.TryGetValue((mapId, blockId), out var state);

        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile?.Map?.MapID == mapId)
            {
                SendBlock(ns, mapId, blockId, state);
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
