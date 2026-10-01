using System;
using System.IO;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Systems.MahaonWorld;

namespace Server.Systems.MahaonFarming;

/// <summary>
///     Превращает нарисованные в карте поля в настоящие грядки.
///
///     Зачем. Статик карты сервер перекрасить не может — такого пакета в протоколе нет,
///     клиент берёт статику из своих файлов сам. Поэтому поле у мельницы выглядело спелым
///     круглый год и при этом отказывалось собираться не осенью: игрок читал это как баг.
///
///     Как. Клиент перестаёт рисовать тайлы полей (см. MahaonCrops.ConvertibleTiles и его
///     копию в Chunk.cs), а сервер кладёт в те же точки MahaonCropTile — обычный предмет,
///     которому можно менять графику. Дальше работает ровно то же, что у посеянных грядок:
///     росток весной, почти созрел летом, сбор осенью, невидимость зимой.
///
///     Побочный выигрыш: две механики схлопнулись в одну. Раньше своя грядка и поле на
///     карте собирались по-разному — предмет двойным кликом, статик через систему добычи с
///     делянками. Теперь всё это грядки.
///
///     Команда разовая и идемпотентная: повторный запуск ничего не дублирует, потому что
///     перед установкой смотрит, нет ли уже грядки на этом тайле. Запускать после каждого
///     обновления карты (новые поля появятся сами).
/// </summary>
public static class MahaonFieldSeeding
{
    /// <summary>Сколько блоков карты обрабатываем за один заход таймера.</summary>
    private const int BlocksPerTick = 2048;

    private static bool _running;

    public static void Configure()
    {
        CommandSystem.Register("MahaonFields", AccessLevel.Administrator, OnSeedCommand);
        CommandSystem.Register("MahaonFieldsClear", AccessLevel.Administrator, OnClearCommand);
    }

    [Usage("MahaonFields")]
    [Description("Превращает поля, нарисованные в карте, в настоящие сезонные грядки.")]
    private static void OnSeedCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (_running)
        {
            from.SendMessage(0x22, "Засев уже идёт — дождись конца.");
            return;
        }

        // «clear» — первое, что напрашивается набрать после [MahaonFields, и отправлять
        // человека читать справку ради одного слова невежливо.
        if (e.Length >= 1 && e.GetString(0).InsensitiveEquals("clear"))
        {
            OnClearCommand(e);
            return;
        }

        var maps = ResolveMaps(e);

        if (maps.Count == 0)
        {
            from.SendMessage(0x22, $"Не понял, какую карту засевать: «{(e.Length >= 1 ? e.GetString(0) : "")}».");
            from.SendMessage(0x3B2, $"Можно: all, clear, либо фасет — {FacetNames()}");

            return;
        }

        _running = true;
        from.SendMessage(0x59, $"Засеваю поля: карт — {maps.Count}. Это займёт несколько секунд.");

        // По кускам через таймер, а не одним махом: карта — это полмиллиона блоков на
        // фасет, и синхронный проход по шести фасетам подвесил бы сервер на видимое время.
        new SeedTimer(from, maps).Start();
    }

    [Usage("MahaonFieldsClear")]
    [Description("Убирает все грядки, поставленные командой MahaonFields.")]
    private static void OnClearCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var doomed = new List<MahaonCropTile>();

        // Свои и поставленные командой различаем не флагом в сохранении, а тем, где грядка
        // стоит: поставленная командой лежит поверх тайла поля из карты, посеянная игроком
        // — на обычной земле. Заводить ради этого новое поле в сериализации значило бы
        // менять формат сохранения у всех уже существующих грядок.
        foreach (var item in World.Items.Values)
        {
            if (item is MahaonCropTile { Deleted: false } tile && IsSystemField(tile))
            {
                doomed.Add(tile);
            }
        }

        foreach (var tile in doomed)
        {
            tile.Delete();
        }

        from.SendMessage(0x59, $"Убрано грядок: {doomed.Count}. Посеянные на чистой земле не тронуты.");
    }

    private static List<Map> ResolveMaps(CommandEventArgs e)
    {
        var maps = new List<Map>();

        if (e.Length == 0)
        {
            if (e.Mobile.Map != null && e.Mobile.Map != Map.Internal)
            {
                maps.Add(e.Mobile.Map);
            }

            return maps;
        }

        if (e.GetString(0).InsensitiveEquals("all"))
        {
            foreach (var map in Map.AllMaps)
            {
                if (map != null && map != Map.Internal && map.MapID <= 5)
                {
                    maps.Add(map);
                }
            }

            return maps;
        }

        // Через TryParse, а не Parse: Parse бросает FormatException на любом слове, которое
        // не является фасетом, и команда падает в консоль стеком вместо того, чтобы просто
        // сказать человеку, что он опечатался.
        if (Map.TryParse(e.GetString(0), null, out var named) && named != null && named != Map.Internal)
        {
            maps.Add(named);
        }

        return maps;
    }

    /// <summary>Имена фасетов — чтобы подсказать, а не просто отказать.</summary>
    private static string FacetNames()
    {
        var names = new List<string>();

        foreach (var map in Map.AllMaps)
        {
            if (map != null && map != Map.Internal && map.MapID <= 5)
            {
                names.Add(map.Name);
            }
        }

        return string.Join(", ", names);
    }

    /// <summary>
    ///     Ставит грядку на один тайл. Возвращает false, если тут уже стоит грядка — ради
    ///     этого команда и идемпотентна.
    ///
    ///     Культура приходит снаружи, от поля, а не берётся из графики тайла: иначе на
    ///     делянке выходила бы мешанина, ведь декоратор ставил статику как попало.
    /// </summary>
    private static bool TrySeed(Map map, int x, int y, int z, MahaonCropType crop)
    {
        foreach (var existing in map.GetItemsInRange<MahaonCropTile>(new Point3D(x, y, z), 0))
        {
            if (!existing.Deleted && existing.X == x && existing.Y == y)
            {
                return false;
            }
        }

        var tile = new MahaonCropTile(crop);
        tile.MoveToWorld(new Point3D(x, y, z), map);

        return true;
    }

    /// <summary>Найденный в файле карты тайл поля — мировые координаты, а не блочные.</summary>
    public readonly record struct FieldTile(int X, int Y, int Z, int Graphic);

    private const int BlockRecordSize = 12; // lookup(int) + length(uint) + extra(int)
    private const int StaticRecordSize = 7; // id(ushort) + x,y(byte) + z(sbyte) + hue(short)

    /// <summary>
    ///     Достаёт из блока карты все тайлы полей. Вынесено из таймера отдельно, потому что
    ///     здесь сидит вся арифметика разбора staidxN.mul/staticsN.mul, а ошибиться в ней
    ///     можно тихо: перепутаешь порядок блоков — и мир засеется со сдвигом, причём
    ///     правдоподобным. Отдельной функцией её хотя бы можно проверить тестом
    ///     (MahaonFieldSeedingTests), не поднимая сервер и не имея файлов карты под рукой.
    ///
    ///     Раскладка, сверенная с TileMatrix: запись блока в индексе — 12 байт
    ///     (lookup:int, length:uint, extra:int) по смещению (bx * blockHeight + by) * 12;
    ///     запись статика — 7 байт (id:ushort, x:byte, y:byte, z:sbyte, hue:short), где x/y
    ///     это смещение внутри блока 8x8.
    /// </summary>
    public static void ScanBlock(
        byte[] index, byte[] statics, int block, int blockHeight, bool[] wanted, List<FieldTile> found
    )
    {
        var o = block * BlockRecordSize;

        if (o < 0 || o + BlockRecordSize > index.Length)
        {
            return;
        }

        var lookup = BitConverter.ToInt32(index, o);
        var length = BitConverter.ToInt32(index, o + 4);

        if (lookup < 0 || length <= 0 || lookup + length > statics.Length)
        {
            return;
        }

        var bx = block / blockHeight;
        var by = block % blockHeight;

        for (var i = 0; i < length / StaticRecordSize; i++)
        {
            var p = lookup + i * StaticRecordSize;
            var id = BitConverter.ToUInt16(statics, p);

            if (!wanted[id])
            {
                continue;
            }

            found.Add(
                new FieldTile(
                    (bx << 3) + statics[p + 2],
                    (by << 3) + statics[p + 3],
                    (sbyte)statics[p + 4],
                    id
                )
            );
        }
    }

    /// <summary>
    ///     Лежит ли грядка на поле, нарисованном в самой карте.
    ///
    ///     Двумя признаками, а не одним: поверх тайла поля ИЛИ на вспаханной земле. Второе
    ///     добавлено вместе с разбиением на делянки — засев теперь занимает и голую пашню,
    ///     на которой статики нет вовсе. Проверяй мы только статику, такие грядки уборка не
    ///     видела бы, и снести неудачный засев стало бы нечем: пересев на занятые тайлы не
    ///     лезет, так что мешанина осталась бы навсегда.
    /// </summary>
    public static bool IsSystemField(MahaonCropTile tile)
    {
        var map = tile.Map;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        var loc = tile.GetWorldLocation();

        foreach (var st in map.Tiles.GetStaticTiles(loc.X, loc.Y))
        {
            if (Array.IndexOf(MahaonCrops.ConvertibleTiles, st.ID) >= 0)
            {
                return true;
            }
        }

        return MahaonFieldPlots.IsPlotGround(map.Tiles.GetLandTile(loc.X, loc.Y).ID);
    }

    /// <summary>
    ///     Обходит статику фасета, читая staidxN.mul/staticsN.mul напрямую.
    ///
    ///     Через map.Tiles.GetStaticBlock было бы короче, но TileMatrix кэширует каждый
    ///     прочитанный блок навсегда, а тут обход всего мира — под два с половиной миллиона
    ///     блоков на шесть фасетов. Сервер бы материализовал в память всю статику мира и так
    ///     с ней и остался. Последовательное чтение файла ничего не кэширует и идёт быстрее.
    /// </summary>
    private class SeedTimer : Timer
    {
        private readonly Mobile _from;
        private readonly List<Map> _maps;
        private readonly bool[] _wanted = new bool[0x10000];
        private readonly List<FieldTile> _found = new();

        /// <summary>Всё найденное на текущем фасете — до разбиения на поля.</summary>
        private readonly Dictionary<(int X, int Y), (int Z, MahaonCropKind Kind)> _collected = new();

        private int _mapIndex = -1;
        private int _block;
        private int _blockCount;
        private int _blockHeight;
        private int _placed;
        private int _placedOnMap;
        private int _skipped;

        private byte[] _index;
        private byte[] _statics;

        public SeedTimer(Mobile from, List<Map> maps) : base(TimeSpan.Zero, TimeSpan.FromMilliseconds(25))
        {
            _from = from;
            _maps = maps;

            foreach (var id in MahaonCrops.ConvertibleTiles)
            {
                _wanted[id & 0xFFFF] = true;
            }
        }

        protected override void OnTick()
        {
            if (_mapIndex < 0 || _block >= _blockCount)
            {
                if (!AdvanceMap())
                {
                    Finish();
                }

                return;
            }

            var map = _maps[_mapIndex];
            var end = Math.Min(_block + BlocksPerTick, _blockCount);

            for (var b = _block; b < end; b++)
            {
                _found.Clear();
                ScanBlock(_index, _statics, b, _blockHeight, _wanted, _found);

                for (var i = 0; i < _found.Count; i++)
                {
                    var t = _found[i];
                    var info = MahaonCrops.FromTile(t.Graphic);

                    if (info == null)
                    {
                        continue;
                    }

                    // Только собираем. Сажать нельзя, пока не увидим фасет целиком: культура
                    // выбирается на ПОЛЕ, а границы поля станут известны лишь после обхода.
                    _collected[(t.X, t.Y)] = (t.Z, info.Kind);
                }
            }

            _block = end;

            if (_block >= _blockCount)
            {
                PlantCollected(map);
            }
        }

        /// <summary>
        ///     Фасет обойдён — делим собранное на поля и сажаем по одной культуре на поле.
        ///
        ///     Раньше грядка ставилась прямо во время обхода, по графике своего тайла, и на
        ///     делянке выходила мешанина: декоратор ставил статику как попало, а мы честно
        ///     это повторяли. Теперь культура — свойство ПОЛЯ, а поле известно только после
        ///     полного обхода. Заодно в поле попадает смежная пашня, на которой статики не
        ///     нарисовано вовсе: игрок видит прямоугольник вспаханной земли и справедливо
        ///     ждёт, что на нём что-то растёт.
        /// </summary>
        private void PlantCollected(Map map)
        {
            var plots = MahaonFieldPlots.Build(map, _collected);

            foreach (var plot in plots)
            {
                foreach (var (x, y, z) in plot.Tiles)
                {
                    if (TrySeed(map, x, y, z, plot.Crop))
                    {
                        _placed++;
                        _placedOnMap++;
                    }
                    else
                    {
                        _skipped++;
                    }
                }
            }

            _from.SendMessage(
                0x59,
                $"{map.Name}: полей {plots.Count}, грядок поставлено {_placedOnMap}."
            );

            _collected.Clear();
        }

        /// <summary>Переходит к следующему фасету. false — фасеты кончились.</summary>
        private bool AdvanceMap()
        {
            while (++_mapIndex < _maps.Count)
            {
                var map = _maps[_mapIndex];

                // Тот же подбор файла, что делает TileMatrix: на старых клиентах Тrammel
                // берёт статику Фелуки. Разойдёмся с ним — засеем не ту карту.
                var fileIndex = TileMatrix.Pre6000ClientSupport && map.MapID == 1 ? 0 : map.FileIndex;

                var indexPath = Core.FindDataFile($"staidx{fileIndex}.mul", false);
                var staticsPath = Core.FindDataFile($"statics{fileIndex}.mul", false);

                if (indexPath == null || staticsPath == null)
                {
                    _from.SendMessage(0x22, $"{map.Name}: нет файлов статики, пропускаю.");
                    continue;
                }

                _index = File.ReadAllBytes(indexPath);
                _statics = File.ReadAllBytes(staticsPath);
                _blockHeight = map.Height >> 3;
                _blockCount = Math.Min(_index.Length / BlockRecordSize, (map.Width >> 3) * _blockHeight);
                _block = 0;
                _placedOnMap = 0;

                return true;
            }

            return false;
        }

        private void Finish()
        {
            Stop();
            _running = false;
            _index = null;
            _statics = null;

            _from.SendMessage(
                0x59,
                $"Засев закончен. Новых грядок: {_placed}. Уже стояло: {_skipped}."
            );
        }
    }
}
