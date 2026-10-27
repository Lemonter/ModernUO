using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Targeting;

namespace Server.Systems.MahaonMapEdits;

/// <summary>
///     Свет, который не проходит через стену.
///
///     ---- Чем это отличается от света ультимы ----
///
///     В ультиме свет — это радиус вокруг источника и один общий «уровень темноты» на игрока
///     (LightCycle). Препятствий он не знает вовсе: факел за каменной стеной освещает тебя
///     так же, как факел в руке, а солнце светит в подвал.
///
///     Здесь свет распространяется ПО КЛЕТКАМ (MahaonVoxels) обходом в ширину с убыванием на
///     единицу за шаг и не проходит там, где клетка помечена Opaque. Поэтому в дом он попадает
///     через проём, в подвал не попадает, а факел освещает коридор, а не всё вокруг сквозь
///     камень.
///
///     ---- Что считает сервер, а что клиент ----
///
///     Рисовать свет — дело клиента, и он будет делать это своим способом (см.
///     VOXEL-WORLD-TASK.md в Unity-проекте). Здесь он нужен для ИГРЫ: спросить, темно ли в
///     этом месте. Скрытность, рост растений, поведение нежити, штраф к стрельбе в темноте —
///     всё это вопросы к серверу, и до сих пор на них нельзя было ответить честно.
///
///     Этот же алгоритм — эталон для клиента. Если клиент посчитает иначе, игроки увидят
///     разный свет в одном и том же месте.
///
///     ---- Яркость, а не темнота ----
///
///     Внутри мы говорим о ЯРКОСТИ: 0 — полная тьма, Max — светло как днём. В протоколе
///     ультимы наоборот, там уровень ТЕМНОТЫ (0 — яркий день, 26 — подземелье), поэтому
///     перевод вынесен в Darkness и нигде больше не смешивается.
/// </summary>
public static class MahaonLight
{
    public const int Max = 15;

    /// <summary>Яркость любого источника света. Радиус из тайлдаты клиента нам тут не нужен:
    /// затухание даёт сам обход, по единице на шаг.</summary>
    public const int SourceBrightness = 10;

    /// <summary>Окно расчёта: столько тайлов в каждую сторону от спрашиваемой точки.</summary>
    public const int Window = 10;

    /// <summary>
    ///     Яркость в точке с учётом перекрытий.
    ///
    ///     Считает честно и потому недёшево: строит решётку вокруг точки и разливает по ней
    ///     свет. Для вопроса «темно ли тут» раз в несколько секунд это нормально, для вопроса
    ///     на каждом шаге каждого моба — нет. Если понадобится часто, кэшировать надо на
    ///     уровне вызывающего, а не здесь: здесь нет понятия «когда это устарело».
    /// </summary>
    public static int Brightness(Map map, Point3D p)
    {
        if (map == null || map == Map.Internal)
        {
            return 0;
        }

        var grid = MahaonVoxels.Around(map, p.X, p.Y, Window);
        var field = Field(grid);

        return At(grid, field, p.X, p.Y, p.Z);
    }

    /// <summary>Уровень ТЕМНОТЫ для протокола ультимы: ноль — яркий день.</summary>
    public static int Darkness(int brightness) =>
        Math.Clamp(LightCycle.NightLevel - brightness * LightCycle.NightLevel / Max, 0, LightCycle.DungeonLevel);

    /// <summary>
    ///     Разливает свет по всей решётке и возвращает яркость каждой клетки.
    ///
    ///     Источники два: небо и всё, что светит само. Небо — не точка, а поток сверху:
    ///     каждую клетку под открытым небом считаем освещённой, пока сверху не встретится
    ///     Opaque. Отсюда и тень от дома получается сама, без отдельного алгоритма теней.
    /// </summary>
    public static int[] Field(MahaonVoxels grid)
    {
        var field = new int[grid.Width * grid.Depth * grid.Height];
        var queue = new Queue<(int X, int Y, int Z)>();

        var sky = SkyBrightness();

        for (var x = grid.X0; x < grid.X0 + grid.Width; x++)
        {
            for (var y = grid.Y0; y < grid.Y0 + grid.Depth; y++)
            {
                // Сверху вниз, пока не упрёмся в непрозрачное: это и есть «под открытым небом».
                for (var z = grid.Z0 + grid.Height - 1; z >= grid.Z0; z--)
                {
                    if (grid.Blocks(x, y, z))
                    {
                        break;
                    }

                    if (sky > 0)
                    {
                        Set(grid, field, x, y, z, sky);
                        queue.Enqueue((x, y, z));
                    }
                }
            }
        }

        foreach (var (x, y, z) in Sources(grid))
        {
            Set(grid, field, x, y, z, SourceBrightness);
            queue.Enqueue((x, y, z));
        }

        // Обход по шести направлениям, а не по двадцати шести: по диагонали свет шёл бы
        // быстрее, чем по прямой, и вытягивался бы крестом.
        while (queue.Count > 0)
        {
            var (x, y, z) = queue.Dequeue();
            var here = At(grid, field, x, y, z);

            if (here <= 1)
            {
                continue;
            }

            Span<(int X, int Y, int Z)> around =
            [
                (x + 1, y, z), (x - 1, y, z),
                (x, y + 1, z), (x, y - 1, z),
                (x, y, z + 1), (x, y, z - 1)
            ];

            foreach (var (nx, ny, nz) in around)
            {
                if (!grid.Contains(nx, ny, nz) || grid.Blocks(nx, ny, nz))
                {
                    continue;
                }

                if (At(grid, field, nx, ny, nz) >= here - 1)
                {
                    continue;
                }

                Set(grid, field, nx, ny, nz, here - 1);
                queue.Enqueue((nx, ny, nz));
            }
        }

        return field;
    }

    /// <summary>Всё, что светит само: и статика карты, и предметы в мире.</summary>
    private static List<(int X, int Y, int Z)> Sources(MahaonVoxels grid)
    {
        var sources = new List<(int X, int Y, int Z)>();

        for (var x = grid.X0; x < grid.X0 + grid.Width; x++)
        {
            for (var y = grid.Y0; y < grid.Y0 + grid.Depth; y++)
            {
                foreach (var s in MahaonMapEdits.StaticsAt(grid.Map, x, y))
                {
                    if ((TileData.ItemTable[s.Graphic & TileData.MaxItemValue].Flags & TileFlag.LightSource) != 0)
                    {
                        sources.Add((x, y, s.Z + 1));
                    }
                }
            }
        }

        var area = new Rectangle2D(grid.X0, grid.Y0, grid.Width, grid.Depth);

        foreach (var item in grid.Map.GetItemsInBounds(area))
        {
            if ((TileData.ItemTable[item.ItemID & TileData.MaxItemValue].Flags & TileFlag.LightSource) != 0)
            {
                sources.Add((item.X, item.Y, item.Z + 1));
            }
        }

        return sources;
    }

    /// <summary>Яркость неба прямо сейчас. Берём из того же цикла суток, что и клиент.</summary>
    private static int SkyBrightness()
    {
        var darkness = Math.Clamp(LightCycle.ComputeLevelFor(null), 0, LightCycle.NightLevel);

        return Max - darkness * Max / Math.Max(1, LightCycle.NightLevel);
    }

    private static int Index(MahaonVoxels grid, int x, int y, int z) =>
        ((x - grid.X0) * grid.Depth + (y - grid.Y0)) * grid.Height + (z - grid.Z0);

    private static int At(MahaonVoxels grid, int[] field, int x, int y, int z) =>
        grid.Contains(x, y, z) ? field[Index(grid, x, y, z)] : 0;

    private static void Set(MahaonVoxels grid, int[] field, int x, int y, int z, int value)
    {
        if (grid.Contains(x, y, z) && field[Index(grid, x, y, z)] < value)
        {
            field[Index(grid, x, y, z)] = value;
        }
    }

    // ---- Команда ---------------------------------------------------------------------------

    public static void Configure()
    {
        CommandSystem.Register("LightInfo", AccessLevel.GameMaster, LightInfo_OnCommand);
    }

    [Usage("LightInfo")]
    [Description("Яркость в точке с учётом стен и крыш — в отличие от штатного уровня темноты.")]
    private static void LightInfo_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage(0x3B2, "Укажи место.");
        e.Mobile.Target = new LightTarget();
    }

    private class LightTarget : Target
    {
        public LightTarget() : base(16, true, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D p || from.Map == null)
            {
                return;
            }

            var point = new Point3D(p.X, p.Y, p.Z);
            var brightness = Brightness(from.Map, point);

            from.SendMessage(0x59, $"({point.X}, {point.Y}, {point.Z}): яркость {brightness} из {Max}.");
            from.SendMessage(
                0x3B2,
                $"  небо сейчас {SkyBrightness()}, штатный уровень темноты у тебя {LightCycle.ComputeLevelFor(from)}"
            );
        }
    }
}
