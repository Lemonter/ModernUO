using Server.Commands;
using Server.Targeting;

namespace Server.Systems.MahaonMapEdits;

/// <summary>
///     Что лопата думает про этот тайл.
///
///     Земляные работы держатся на числах, которых в игре не видно: целина, текущее дно,
///     уровень ближайшей воды. Без них непонятно, почему вода не пришла — то ли канава
///     недостаточно глубока, то ли перемычка не прокопана, то ли это вообще чужой пруд.
///     Поэтому сюда и выведено ровно то, на чём считает MahaonExcavation и MahaonWaterFlow.
/// </summary>
public static class MahaonDigInfoCommand
{
    /// <summary>Докуда ищем ближайшую воду, чтобы сказать её уровень.</summary>
    private const int Look = 24;

    public static void Configure()
    {
        CommandSystem.Register("DigInfo", AccessLevel.GameMaster, DigInfo_OnCommand);
    }

    [Usage("DigInfo")]
    [Description("Показывает по тайлу: целину, дно, глубину, уровень ближайшей воды и что сделает следующий взмах.")]
    private static void DigInfo_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage(0x3B2, "Укажи тайл.");
        e.Mobile.Target = new InfoTarget();
    }

    private class InfoTarget : Target
    {
        public InfoTarget() : base(16, true, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D p || from.Map == null)
            {
                return;
            }

            var map = from.Map;
            var x = p.X;
            var y = p.Y;

            var original = MahaonExcavation.OriginalZ(map, x, y);
            var ground = MahaonExcavation.GroundZ(map, x, y);
            var surface = MahaonExcavation.CurrentZ(map, x, y);

            from.SendMessage(0x59, $"Тайл ({x}, {y}) на карте {map.Name}:");
            from.SendMessage(0x3B2, $"  целина {original}, дно {ground}, глубина {original - ground}, предел карты {MahaonExcavation.MinZ}");

            if (surface != ground)
            {
                from.SendMessage(0x3B2, $"  залито водой, её уровень {surface}");
            }

            from.SendMessage(
                0x3B2,
                MahaonExcavation.CanDigHere(map, x, y, out var why)
                    ? $"  твой режим: {MahaonDigMode.Name(MahaonDigMode.Of(from))}"
                    : $"  копать нельзя: {why}"
            );

            if (!MahaonWaterFlow.NearestWater(map, x, y, Look, out var waterLevel, out var distance))
            {
                from.SendMessage(0x3B2, $"  воды в {Look} тайлах нет — яма останется сухой");
                return;
            }

            from.SendMessage(0x3B2, $"  ближайшая вода в {distance} тайлах, её уровень {waterLevel}");

            // Главное число: вода идёт только по тайлам, чья земля СТРОГО ниже её уровня.
            from.SendMessage(
                0x3B2,
                ground < waterLevel
                    ? "  дно ниже воды — по высоте всё в порядке"
                    : $"  дно НЕ ниже воды: чтобы налилось, нужно ниже {waterLevel}"
            );

            // И главный ответ: высоты мало, нужна ещё непрерывная канава. Считаем тем же
            // обходом, что и сам разлив, — чтобы совет не расходился с поведением.
            MahaonWaterFlow.Explain(map, x, y, out var trace);
            from.SendMessage(0x3B2, $"  {trace}");
        }
    }
}
