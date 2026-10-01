using ModernUO.Serialization;
using Server.Systems.MahaonMapEdits;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Землекопная лопата — ров, пруд, яма под частокол.
///
///     Отдельный предмет, а не ветка у шахтёрской лопаты, намеренно: там своя система
///     добычи со своими делянками и навыком, и вклиниваться в неё ради другого занятия
///     значит однажды сломать шахты. Здесь дело простое и к добыче отношения не имеет.
///
///     Копает — опускает землю. Копает достаточно — яма набирает воду и становится прудом,
///     в котором ловится рыба (вода помечена Wet, а рыбалка принимает всё мокрое).
///     Засыпает — поднимает обратно, но не выше исходной высоты: это засыпка, а не стройка.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonSpade : Item
{
    [SerializableField(0)]
    private int _usesRemaining;

    /// <summary>Засыпать вместо копать — переключается двойным кликом по самой лопате.</summary>
    [SerializableField(1)]
    private bool _fillMode;

    [Constructible]
    public MahaonSpade() : base(0x0F39)
    {
        Weight = 5.0;
        Name = "землекопная лопата";
        _usesRemaining = 200;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"Осталось использований: {_usesRemaining}");
        list.Add(_fillMode ? "Режим: засыпать" : "Режим: копать");
        list.Add($"Глубже {MahaonExcavation.MaxDepth} яма набирает воду");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage(0x22, "Лопата должна быть у тебя в рюкзаке.");
            return;
        }

        from.SendMessage(0x3B2, _fillMode ? "Что засыпать? (Alt-клик по лопате — сменить режим)" : "Где копать?");
        from.Target = new DigTarget(this);
    }

    /// <summary>Смена режима — по клику с земли, чтобы не занимать двойной клик.</summary>
    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        FillMode = !_fillMode;
        InvalidateProperties();

        from.SendMessage(0x59, _fillMode ? "Теперь засыпаешь." : "Теперь копаешь.");
    }

    private bool ConsumeUse(Mobile from)
    {
        UsesRemaining--;

        if (_usesRemaining > 0)
        {
            return true;
        }

        from.SendMessage(0x22, "Лопата ломается у тебя в руках.");
        Delete();

        return false;
    }

    private class DigTarget : Target
    {
        private readonly MahaonSpade _spade;

        public DigTarget(MahaonSpade spade) : base(2, true, TargetFlags.None) => _spade = spade;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_spade.Deleted || !_spade.IsChildOf(from.Backpack))
            {
                return;
            }

            if (targeted is not IPoint3D p || from.Map == null)
            {
                return;
            }

            var map = from.Map;
            var x = p.X;
            var y = p.Y;

            if (!from.InRange(new Point3D(x, y, p.Z), 2))
            {
                from.SendMessage(0x22, "Слишком далеко.");
                return;
            }

            if (!MahaonExcavation.CanDigHere(map, x, y, out var why))
            {
                from.SendMessage(0x22, why);
                return;
            }

            // Пометка одна на игрока и место: вся яма откатывается разом, а чужие ямы
            // рядом при этом не трогаются.
            var reason = MahaonDemolition.ReasonFor(from, _spade._fillMode ? "засыпка" : "копка");

            if (_spade._fillMode)
            {
                if (!MahaonExcavation.Fill(map, x, y, reason))
                {
                    from.SendMessage(0x3B2, "Здесь и так ровно.");
                    return;
                }

                if (!_spade.ConsumeUse(from))
                {
                    return;
                }

                from.SendMessage(0x59, "Ты засыпаешь яму.");
                from.PlaySound(0x125);

                return;
            }

            var result = MahaonExcavation.Dig(map, x, y, reason);

            switch (result)
            {
                case MahaonExcavation.DigResult.TooDeep:
                    from.SendMessage(0x3B2, "Глубже не выйдет — здесь уже вода.");
                    return;

                case MahaonExcavation.DigResult.Forbidden:
                case MahaonExcavation.DigResult.Blocked:
                    from.SendMessage(0x22, "Здесь не копается.");
                    return;
            }

            if (!_spade.ConsumeUse(from))
            {
                return;
            }

            from.PlaySound(0x125);

            from.SendMessage(
                0x59,
                result == MahaonExcavation.DigResult.Flooded
                    ? "Яма набрала воду — тут теперь можно ловить рыбу."
                    : "Ты выкапываешь землю."
            );
        }
    }
}
