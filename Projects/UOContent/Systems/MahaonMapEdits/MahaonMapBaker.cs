using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Systems.MahaonMapEdits;

/// <summary>
///     Запекание правок карты в штатные разностные файлы ультимы.
///
///     ---- Формат, снятый с TileMatrixPatch ----
///
///     Статика, три файла на фасет:
///       stadifl{N}.mul  — указатель: подряд int32 blockId, по одному на заплату;
///                         blockId = blockX * BlockHeight + blockY.
///       stadifi{N}.mul  — справочник: на каждую заплату int32 offset, length, extra.
///       stadif{N}.mul   — данные: по offset лежит length байт, это length/7 записей
///                         статика (id:ushort, x:byte, y:byte, z:sbyte, hue:short).
///
///     Снос здания делается через справочник: offset = -1 (или length = 0) означает, что
///     блок ПУСТ. Это единственный способ убрать статику, нарисованную в карте, — предметом
///     её не тронуть.
///
///     Земля, два файла:
///       mapdifl{N}.mul  — указатель: подряд int32 blockId.
///       mapdif{N}.mul   — данные: на каждую заплату 4 байта заголовка (движок их
///                         пропускает) и 192 байта — 64 тайла по (id:ushort, z:sbyte).
///
///     ---- Что делает запекание ----
///
///     Переписывает эти файлы целиком: сначала переносит все заплаты, которых мы не
///     касались, затем дописывает свои блоки. Перенос обязателен — в stadif лежат
///     собственные правки EA, и записав туда только своё, мы стёрли бы куски карты.
///
///     Перед первой записью рядом кладётся .mahaon-bak. Мы пишем в каталог клиента, и
///     ошибка здесь — это испорченная карта у всех разом.
///
///     ---- Чего запекание НЕ делает ----
///
///     Не применяет ничего на лету. Сервер прочитает заплаты при следующем запуске, клиент
///     — при следующем своём. То есть это выпуск обновления, а не событие в игре: мост
///     появится у всех сразу, но после перезапуска, а не в ту секунду, когда его достроили.
/// </summary>
public static class MahaonMapBaker
{
    private const int BlockRecord = 12; // offset + length + extra
    private const int StaticRecord = 7;
    private const int LandBlockBytes = 192; // 64 тайла по 3 байта
    private const string BackupSuffix = ".mahaon-bak";

    public sealed class BakeResult
    {
        public int StaticBlocks;
        public int LandBlocks;
        public int CarriedOver;
        public readonly List<string> Files = new();
        public string Error;
    }

    /// <summary>Запекает правки одного фасета.</summary>
    public static BakeResult Bake(Map map)
    {
        var result = new BakeResult();

        var blocks = new Dictionary<int, MahaonMapEdits.BlockState>();

        foreach (var ((mapId, blockId), state) in MahaonMapEdits.Derived)
        {
            if (mapId == map.MapID)
            {
                blocks[blockId] = state;
            }
        }

        if (blocks.Count == 0)
        {
            return result;
        }

        var fileIndex = TileMatrix.Pre6000ClientSupport && map.MapID == 1 ? 0 : map.FileIndex;
        var blockHeight = map.Height >> 3;

        try
        {
            BakeStatics(fileIndex, blockHeight, blocks, result);
            BakeLand(fileIndex, blocks, result);
        }
        catch (Exception e)
        {
            result.Error = e.Message;
        }

        return result;
    }

    // ---- Статика ---------------------------------------------------------------------

    private static void BakeStatics(
        int fileIndex, int blockHeight, Dictionary<int, MahaonMapEdits.BlockState> blocks, BakeResult result
    )
    {
        var ours = new Dictionary<int, List<MapStatic>>();

        foreach (var (blockId, edit) in blocks)
        {
            if (edit.StaticsTouched)
            {
                ours[blockId] = edit.Statics;
            }
        }

        if (ours.Count == 0)
        {
            return;
        }

        var idxPath = DataPath($"stadifl{fileIndex}.mul");
        var lookupPath = DataPath($"stadifi{fileIndex}.mul");
        var dataPath = DataPath($"stadif{fileIndex}.mul");

        // Уже существующие заплаты — их надо унести с собой, иначе сотрём правки EA.
        var ordered = new List<(int BlockId, List<MapStatic> Tiles)>();

        if (File.Exists(idxPath) && File.Exists(lookupPath) && File.Exists(dataPath))
        {
            var carried = ParseStaticPatch(
                File.ReadAllBytes(idxPath), File.ReadAllBytes(lookupPath), File.ReadAllBytes(dataPath), ours.Keys
            );

            result.CarriedOver += carried.Count;

            foreach (var (blockId, tiles) in carried)
            {
                ordered.Add((blockId, tiles));
            }
        }

        foreach (var (blockId, tiles) in ours)
        {
            ordered.Add((blockId, tiles));
            result.StaticBlocks++;
        }

        var (index, lookup, data) = BuildStaticPatch(ordered);

        WriteFile(idxPath, index, result);
        WriteFile(lookupPath, lookup, result);
        WriteFile(dataPath, data, result);
    }

    /// <summary>
    ///     Собирает три файла заплаты статики. Вынесено отдельно и без файловых операций
    ///     именно затем, чтобы это можно было прогнать тестом: ошибка на байт здесь — это
    ///     испорченная карта у всех игроков разом, а заметна она станет не сразу.
    ///
    ///     Пустой список тайлов у блока — не пустая запись, а offset = -1: так движок
    ///     понимает «блок пуст», и так сносится здание.
    /// </summary>
    public static (byte[] Index, byte[] Lookup, byte[] Data) BuildStaticPatch(
        IReadOnlyList<(int BlockId, List<MapStatic> Tiles)> blocks
    )
    {
        var index = new List<byte>();
        var lookup = new List<byte>();
        var data = new List<byte>();

        foreach (var (blockId, tiles) in blocks)
        {
            index.AddRange(BitConverter.GetBytes(blockId));

            if (tiles == null || tiles.Count == 0)
            {
                lookup.AddRange(BitConverter.GetBytes(-1));
                lookup.AddRange(BitConverter.GetBytes(0));
                lookup.AddRange(BitConverter.GetBytes(0));
                continue;
            }

            lookup.AddRange(BitConverter.GetBytes(data.Count));
            lookup.AddRange(BitConverter.GetBytes(tiles.Count * StaticRecord));
            lookup.AddRange(BitConverter.GetBytes(0));

            foreach (var s in tiles)
            {
                data.AddRange(BitConverter.GetBytes(s.Graphic));
                data.Add(s.X);
                data.Add(s.Y);
                data.Add(unchecked((byte)s.Z));
                data.AddRange(BitConverter.GetBytes(s.Hue));
            }
        }

        return (index.ToArray(), lookup.ToArray(), data.ToArray());
    }

    /// <summary>
    ///     Разбирает заплату статики тем же способом, что и TileMatrixPatch. Используется и
    ///     при переносе чужих заплат, и в тестах как обратная сторона BuildStaticPatch.
    /// </summary>
    public static Dictionary<int, List<MapStatic>> ParseStaticPatch(
        byte[] index, byte[] lookup, byte[] data, ICollection<int> skip = null
    )
    {
        var blocks = new Dictionary<int, List<MapStatic>>();
        var count = Math.Min(index.Length / 4, lookup.Length / BlockRecord);

        for (var i = 0; i < count; i++)
        {
            var blockId = BitConverter.ToInt32(index, i * 4);

            if (skip?.Contains(blockId) == true || blocks.ContainsKey(blockId))
            {
                continue;
            }

            var offset = BitConverter.ToInt32(lookup, i * BlockRecord);
            var length = BitConverter.ToInt32(lookup, i * BlockRecord + 4);
            var tiles = new List<MapStatic>();

            if (offset >= 0 && length > 0 && offset + length <= data.Length)
            {
                for (var t = 0; t < length / StaticRecord; t++)
                {
                    var p = offset + t * StaticRecord;

                    tiles.Add(
                        new MapStatic(
                            BitConverter.ToUInt16(data, p),
                            data[p + 2],
                            data[p + 3],
                            unchecked((sbyte)data[p + 4]),
                            BitConverter.ToInt16(data, p + 5)
                        )
                    );
                }
            }

            blocks[blockId] = tiles;
        }

        return blocks;
    }

    // ---- Земля -----------------------------------------------------------------------

    private static void BakeLand(
        int fileIndex, Dictionary<int, MahaonMapEdits.BlockState> blocks, BakeResult result
    )
    {
        var ours = new Dictionary<int, MapLand[]>();

        foreach (var (blockId, edit) in blocks)
        {
            if (edit.LandTouched)
            {
                ours[blockId] = edit.Land;
            }
        }

        if (ours.Count == 0)
        {
            return;
        }

        var idxPath = DataPath($"mapdifl{fileIndex}.mul");
        var dataPath = DataPath($"mapdif{fileIndex}.mul");

        var ordered = new List<(int BlockId, MapLand[] Land)>();

        if (File.Exists(idxPath) && File.Exists(dataPath))
        {
            var carried = ParseLandPatch(File.ReadAllBytes(idxPath), File.ReadAllBytes(dataPath), ours.Keys);
            result.CarriedOver += carried.Count;

            foreach (var (blockId, land) in carried)
            {
                ordered.Add((blockId, land));
            }
        }

        foreach (var (blockId, land) in ours)
        {
            ordered.Add((blockId, land));
            result.LandBlocks++;
        }

        var (index, data) = BuildLandPatch(ordered);

        WriteFile(idxPath, index, result);
        WriteFile(dataPath, data, result);
    }

    /// <summary>
    ///     Собирает заплату земли. Справочника у mapdif нет: записи лежат подряд и
    ///     сопоставляются с указателем по порядку — вот это и есть то место, где легко
    ///     ошибиться и сдвинуть всю карту на один блок.
    /// </summary>
    public static (byte[] Index, byte[] Data) BuildLandPatch(
        IReadOnlyList<(int BlockId, MapLand[] Land)> blocks
    )
    {
        var index = new List<byte>();
        var data = new List<byte>();

        foreach (var (blockId, land) in blocks)
        {
            index.AddRange(BitConverter.GetBytes(blockId));
            data.AddRange(BitConverter.GetBytes(0)); // заголовок, движок его пропускает

            for (var i = 0; i < 64; i++)
            {
                data.AddRange(BitConverter.GetBytes(land[i].Graphic));
                data.Add(unchecked((byte)land[i].Z));
            }
        }

        return (index.ToArray(), data.ToArray());
    }

    public static Dictionary<int, MapLand[]> ParseLandPatch(
        byte[] index, byte[] data, ICollection<int> skip = null
    )
    {
        var blocks = new Dictionary<int, MapLand[]>();
        var stride = 4 + LandBlockBytes;
        var count = Math.Min(index.Length / 4, data.Length / stride);

        for (var i = 0; i < count; i++)
        {
            var blockId = BitConverter.ToInt32(index, i * 4);

            if (skip?.Contains(blockId) == true || blocks.ContainsKey(blockId))
            {
                continue;
            }

            var land = new MapLand[64];
            var p = i * stride + 4;

            for (var t = 0; t < 64; t++)
            {
                land[t] = new MapLand(
                    BitConverter.ToUInt16(data, p + t * 3),
                    unchecked((sbyte)data[p + t * 3 + 2])
                );
            }

            blocks[blockId] = land;
        }

        return blocks;
    }

    // ---- Общее -----------------------------------------------------------------------

    /// <summary>
    ///     Путь к файлу карты. Если файла ещё нет (заплат для этого фасета не было вовсе),
    ///     Core.FindDataFile вернёт null — тогда кладём рядом с остальными данными.
    /// </summary>
    private static string DataPath(string name)
    {
        var existing = Core.FindDataFile(name, false);

        if (existing != null)
        {
            return existing;
        }

        // Ориентируемся на заведомо существующий файл карты — он лежит в нужном каталоге.
        var anchor = Core.FindDataFile("tiledata.mul", false);

        return anchor == null ? name : Path.Combine(Path.GetDirectoryName(anchor)!, name);
    }

    /// <summary>Пишет файл, один раз сохранив исходный рядом. Портим чужой каталог —
    /// страховка обязательна.</summary>
    private static void WriteFile(string path, byte[] bytes, BakeResult result)
    {
        var backup = path + BackupSuffix;

        if (File.Exists(path) && !File.Exists(backup))
        {
            File.Copy(path, backup);
        }

        File.WriteAllBytes(path, bytes);
        result.Files.Add($"{Path.GetFileName(path)} ({bytes.Length} б)");
    }
}
