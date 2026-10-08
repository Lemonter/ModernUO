using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Targeting;

namespace Server.Systems.MahaonMapEdits;

/// <summary>
///     Команды правки карты. Это ГМ-обёртка и отладочная поверхность над
///     MahaonMapEdits — сама игра дёргает те же методы напрямую: бомба бота, осадное
///     орудие, кирка. Правки существуют, чтобы быть следствием происходящего в мире, а
///     командами их удобно проверять и разгребать.
///
///     Откат — не довесок, а условие, на котором всё это вообще стоит включать. Журнал
///     позволяет отменить отдельную правку, всё, что натворило одно событие, или всё разом.
/// </summary>
public static class MahaonMapEditCommands
{
    /// <summary>Чем подписываются правки, сделанные руками.</summary>
    private const string GmReason = "рука ГМ";

    public static void Configure()
    {
        CommandSystem.Register("MapRemove", AccessLevel.Administrator, MapRemove_OnCommand);
        CommandSystem.Register("MapAdd", AccessLevel.Administrator, MapAdd_OnCommand);
        CommandSystem.Register("MapLand", AccessLevel.Administrator, MapLand_OnCommand);
        CommandSystem.Register("MapEdits", AccessLevel.Administrator, MapEdits_OnCommand);
        CommandSystem.Register("MapHere", AccessLevel.Administrator, MapHere_OnCommand);
        CommandSystem.Register("MapUndo", AccessLevel.Administrator, MapUndo_OnCommand);
        CommandSystem.Register("MapBake", AccessLevel.Administrator, MapBake_OnCommand);
        CommandSystem.Register("MapCommit", AccessLevel.Administrator, MapCommit_OnCommand);
    }

    [Usage("MapRemove [радиус]")]
    [Description("Убирает статику карты: снос здания, расчистка скалы.")]
    private static void MapRemove_OnCommand(CommandEventArgs e)
    {
        var radius = e.Length >= 1 ? Math.Clamp(e.GetInt32(0), 0, 16) : 0;

        e.Mobile.SendMessage(0x3B2, $"Укажи, что снести (радиус {radius}).");
        e.Mobile.Target = new RemoveTarget(radius);
    }

    [Usage("MapAdd <графика> [hue]")]
    [Description("Кладёт статик в карту: доска моста, стена, вода.")]
    private static void MapAdd_OnCommand(CommandEventArgs e)
    {
        if (e.Length < 1)
        {
            e.Mobile.SendMessage(0x22, "Нужна графика: [MapAdd 0x1797");
            return;
        }

        e.Mobile.SendMessage(0x3B2, "Укажи, куда положить.");
        e.Mobile.Target = new AddTarget(e.GetInt32(0), e.Length >= 2 ? e.GetInt32(1) : 0);
    }

    [Usage("MapLand <графика> [радиус]")]
    [Description("Меняет землю: ров, русло, насыпь.")]
    private static void MapLand_OnCommand(CommandEventArgs e)
    {
        if (e.Length < 1)
        {
            e.Mobile.SendMessage(0x22, "Нужна графика земли: [MapLand 0x00A8");
            return;
        }

        var radius = e.Length >= 2 ? Math.Clamp(e.GetInt32(1), 0, 16) : 0;

        e.Mobile.SendMessage(0x3B2, $"Укажи, где менять землю (радиус {radius}).");
        e.Mobile.Target = new LandTarget(e.GetInt32(0), radius);
    }

    [Usage("MapEdits [сколько]")]
    [Description("Журнал правок карты: последние записи и сводка по событиям.")]
    private static void MapEdits_OnCommand(CommandEventArgs e)
    {
        var entries = MahaonMapEdits.Entries;

        if (entries.Count == 0)
        {
            e.Mobile.SendMessage(0x3B2, "Журнал правок пуст.");
            return;
        }

        var byReason = new Dictionary<string, int>();

        foreach (var entry in entries)
        {
            byReason.TryGetValue(entry.Reason, out var n);
            byReason[entry.Reason] = n + 1;
        }

        e.Mobile.SendMessage(0x59, $"Правок всего: {entries.Count}, блоков задето: {MahaonMapEdits.Derived.Count}.");

        foreach (var (reason, n) in byReason)
        {
            e.Mobile.SendMessage(0x59, $"  «{reason}»: {n}  —  откат: [MapUndo reason {reason}");
        }

        var show = e.Length >= 1 ? Math.Clamp(e.GetInt32(0), 1, 20) : 5;

        e.Mobile.SendMessage(0x3B2, "Последние:");

        for (var i = Math.Max(0, entries.Count - show); i < entries.Count; i++)
        {
            e.Mobile.SendMessage(0x3B2, $"  {entries[i]}");
        }
    }

    [Usage("MapHere")]
    [Description("Показывает правки в блоке под указанной точкой.")]
    private static void MapHere_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage(0x3B2, "Укажи место.");
        e.Mobile.Target = new HereTarget();
    }

    [Usage("MapUndo <номер | last [N] | reason <текст> | all>")]
    [Description("Откатывает правки карты.")]
    private static void MapUndo_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Length == 0)
        {
            from.SendMessage(0x22, "[MapUndo 42 | [MapUndo last 3 | [MapUndo reason осада | [MapUndo all");
            return;
        }

        var what = e.GetString(0);

        if (what.InsensitiveEquals("all"))
        {
            var n = MahaonMapEdits.UndoAll();
            from.SendMessage(0x59, $"Откачено правок: {n}. Карта вернулась к исходной.");
            WarnBakeNeeded(from);

            return;
        }

        if (what.InsensitiveEquals("last"))
        {
            var count = e.Length >= 2 ? Math.Max(1, e.GetInt32(1)) : 1;
            var n = MahaonMapEdits.UndoLast(count);

            from.SendMessage(n > 0 ? 0x59 : 0x22, n > 0 ? $"Откачено последних правок: {n}." : "Откатывать нечего.");

            if (n > 0)
            {
                WarnBakeNeeded(from);
            }

            return;
        }

        if (what.InsensitiveEquals("reason"))
        {
            if (e.Length < 2)
            {
                from.SendMessage(0x22, "Укажи, что откатывать: [MapUndo reason осада Британии");
                return;
            }

            var reason = string.Join(' ', e.Arguments, 1, e.Arguments.Length - 1);
            var n = MahaonMapEdits.UndoByReason(reason);

            from.SendMessage(
                n > 0 ? 0x59 : 0x22,
                n > 0 ? $"Откачено правок события «{reason}»: {n}." : $"Правок с пометкой «{reason}» нет."
            );

            if (n > 0)
            {
                WarnBakeNeeded(from);
            }

            return;
        }

        if (int.TryParse(what, out var id))
        {
            var ok = MahaonMapEdits.Undo(id);

            from.SendMessage(ok ? 0x59 : 0x22, ok ? $"Правка #{id} откачена." : $"Правки #{id} в журнале нет.");

            if (ok)
            {
                WarnBakeNeeded(from);
            }

            return;
        }

        from.SendMessage(0x22, "Не понял. [MapUndo 42 | last N | reason <текст> | all");
    }

    private static void WarnBakeNeeded(Mobile from)
    {
        from.SendMessage(
            0x35,
            "Если правки уже были запечены — нужно запечь снова ([MapBake), иначе в файлах " +
            "карты останется старое состояние."
        );
    }

    [Usage("MapBake [all]")]
    [Description("Запекает правки в разностные файлы карты. Применится после перезапуска.")]
    private static void MapBake_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var all = e.Length >= 1 && e.GetString(0).InsensitiveEquals("all");

        // Главная мина этой затеи: сервер по умолчанию заплаты не читает. TileMatrixPatch
        // включает их только для клиентов старше 7.0.9, а у нас клиент новее. Без этих
        // настроек запекание отработает, файлы лягут, клиент изменения покажет — а сервер
        // о них знать не будет. Игрок увидит снесённое здание и упрётся в него.
        if (!TileMatrixPatch.PatchStaticsEnabled || !TileMatrixPatch.PatchLandEnabled)
        {
            from.SendMessage(
                0x22,
                "ВНИМАНИЕ: сервер сейчас не читает заплаты карты — " +
                $"статика {(TileMatrixPatch.PatchStaticsEnabled ? "да" : "НЕТ")}, " +
                $"земля {(TileMatrixPatch.PatchLandEnabled ? "да" : "НЕТ")}. " +
                "Включи maps.enableStaticsDiffPatches и maps.enableMapDiffPatches в modernuo.json."
            );
        }

        var maps = new List<Map>();

        if (all)
        {
            foreach (var map in Map.AllMaps)
            {
                if (map != null && map != Map.Internal && map.MapID <= 5)
                {
                    maps.Add(map);
                }
            }
        }
        else if (from.Map != null && from.Map != Map.Internal)
        {
            maps.Add(from.Map);
        }

        var anything = false;

        foreach (var map in maps)
        {
            var result = MahaonMapBaker.Bake(map);

            if (result.Error != null)
            {
                from.SendMessage(0x22, $"{map.Name}: ошибка — {result.Error}");
                anything = true;

                continue;
            }

            if (result.StaticBlocks == 0 && result.LandBlocks == 0)
            {
                continue;
            }

            anything = true;

            from.SendMessage(
                0x59,
                $"{map.Name}: запечено блоков — статика {result.StaticBlocks}, земля {result.LandBlocks}; " +
                $"перенесено чужих заплат {result.CarriedOver}."
            );

            foreach (var file in result.Files)
            {
                from.SendMessage(0x59, $"  {file}");
            }
        }

        if (!anything)
        {
            from.SendMessage(0x3B2, "Запекать нечего.");
            return;
        }

        from.SendMessage(
            0x35,
            "Сервер увидит правки после перезапуска, игроки — когда получат обновлённые " +
            "stadif/mapdif. Исходные файлы сохранены рядом с расширением .mahaon-bak."
        );
    }

    [Usage("MapCommit подтверждаю")]
    [Description("Закрепляет запечённое: журнал очищается, откат становится невозможен.")]
    private static void MapCommit_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var pending = MahaonMapEdits.Entries.Count;

        if (pending == 0)
        {
            from.SendMessage(0x3B2, "Закреплять нечего — журнал пуст.");
            return;
        }

        // Подтверждение словом, а не кнопкой: это единственная необратимая операция во всей
        // системе. После неё снимок «как было» перезаписан, и [MapUndo этих правок уже не
        // вернёт — только новая правка поверх.
        if (e.Length < 1 || !e.GetString(0).InsensitiveEquals("подтверждаю"))
        {
            from.SendMessage(0x22, $"Это необратимо: {pending} правок перестанут откатываться.");
            from.SendMessage(0x22, "Делай это ТОЛЬКО после того, как игроки получили новые stadif/mapdif.");
            from.SendMessage(0x35, "Если уверен: [MapCommit подтверждаю");

            return;
        }

        var committed = MahaonMapEdits.Commit();

        from.SendMessage(
            0x59,
            $"Закреплено правок: {committed}. Журнал пуст, живой слой больше их не рассылает — " +
            "теперь они живут в файлах карты."
        );
    }

    // ---- Цели --------------------------------------------------------------------------

    private class RemoveTarget : Target
    {
        private readonly int _radius;

        public RemoveTarget(int radius) : base(18, true, TargetFlags.None) => _radius = radius;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D p || from.Map == null)
            {
                return;
            }

            var map = from.Map;
            var removed = 0;
            var first = MahaonMapEdits.Entries.Count;

            for (var x = p.X - _radius; x <= p.X + _radius; x++)
            {
                for (var y = p.Y - _radius; y <= p.Y + _radius; y++)
                {
                    if (x >= 0 && y >= 0 && x < map.Width && y < map.Height)
                    {
                        removed += MahaonMapEdits.RemoveStatics(map, x, y, null, GmReason);
                    }
                }
            }

            if (removed == 0)
            {
                from.SendMessage(0x22, "Здесь нет статики карты — возможно, это предметы или сама земля.");
                return;
            }

            from.SendMessage(
                0x59,
                $"Снято статиков: {removed} (правки #{MahaonMapEdits.Entries[first].Id}…" +
                $"#{MahaonMapEdits.Entries[^1].Id}). Откат: [MapUndo last {removed}"
            );
        }
    }

    private class AddTarget : Target
    {
        private readonly int _graphic;
        private readonly int _hue;

        public AddTarget(int graphic, int hue) : base(18, true, TargetFlags.None)
        {
            _graphic = graphic;
            _hue = hue;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D p || from.Map == null)
            {
                return;
            }

            MahaonMapEdits.AddStatic(from.Map, p.X, p.Y, p.Z, _graphic, _hue, GmReason);

            from.SendMessage(
                0x59,
                $"Статик {_graphic:X4} записан в ({p.X}, {p.Y}, {p.Z}) — правка " +
                $"#{MahaonMapEdits.Entries[^1].Id}. Откат: [MapUndo last"
            );
        }
    }

    private class LandTarget : Target
    {
        private readonly int _graphic;
        private readonly int _radius;

        public LandTarget(int graphic, int radius) : base(18, true, TargetFlags.None)
        {
            _graphic = graphic;
            _radius = radius;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D p || from.Map == null)
            {
                return;
            }

            var map = from.Map;
            var changed = 0;

            for (var x = p.X - _radius; x <= p.X + _radius; x++)
            {
                for (var y = p.Y - _radius; y <= p.Y + _radius; y++)
                {
                    if (x >= 0 && y >= 0 && x < map.Width && y < map.Height)
                    {
                        MahaonMapEdits.SetLand(map, x, y, _graphic, p.Z, GmReason);
                        changed++;
                    }
                }
            }

            from.SendMessage(0x59, $"Земля изменена на {changed} тайлах. Откат: [MapUndo last {changed}");
        }
    }

    private class HereTarget : Target
    {
        public HereTarget() : base(18, true, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D p || from.Map == null)
            {
                return;
            }

            var entries = MahaonMapEdits.EntriesInBlock(from.Map, p.X, p.Y);

            if (entries.Count == 0)
            {
                from.SendMessage(0x3B2, "В этом блоке правок нет.");
                return;
            }

            from.SendMessage(0x59, $"Правок в блоке: {entries.Count}");

            foreach (var entry in entries)
            {
                from.SendMessage(0x3B2, $"  {entry} ({entry.When:HH:mm dd.MM})");
            }
        }
    }
}
