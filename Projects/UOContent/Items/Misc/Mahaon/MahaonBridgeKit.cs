using ModernUO.Serialization;
using Server.Systems.MahaonMapEdits;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Плотницкий набор — мосты и настилы через воду и провалы.
///
///     Созидательный инструмент: до него игрок умел только рушить и копать. Кладёт доски на
///     своей высоте, так что мост держит уровень берега, а не уходит в воду вслед за дном.
///
///     Настелить можно только над провалом — водой или землёй заметно ниже. Разреши мы
///     класть доски на ровном месте, вышли бы не мосты, а заасфальтированный мир.
///
///     Доски настоящие: берутся из рюкзака. Разобрать свой мост можно тем же набором,
///     доски при этом возвращаются — но вдвое меньше, чем ушло. Мост можно и взорвать: он
///     разрушим наравне со стенами, и отрезать переправу — половина смысла осады.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonBridgeKit : Item
{
    /// <summary>Разбирать вместо строить.</summary>
    [SerializableField(0)]
    private bool _dismantleMode;

    [Constructible]
    public MahaonBridgeKit() : base(0x1EB8)
    {
        Weight = 6.0;
        Name = "плотницкий набор";
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"Досок на тайл: {MahaonBridging.BoardsPerTile}");
        list.Add(_dismantleMode ? "Режим: разбирать" : "Режим: настилать");
        list.Add("Только над водой или провалом");
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        DismantleMode = !_dismantleMode;
        InvalidateProperties();

        from.SendMessage(0x59, _dismantleMode ? "Теперь разбираешь." : "Теперь настилаешь.");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage(0x22, "Набор должен быть у тебя в рюкзаке.");
            return;
        }

        from.SendMessage(0x3B2, _dismantleMode ? "Что разобрать?" : "Куда настилать?");
        from.Target = new BuildTarget(this);
    }

    private class BuildTarget : Target
    {
        private readonly MahaonBridgeKit _kit;

        public BuildTarget(MahaonBridgeKit kit) : base(4, true, TargetFlags.None) => _kit = kit;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_kit.Deleted || !_kit.IsChildOf(from.Backpack) || targeted is not IPoint3D p || from.Map == null)
            {
                return;
            }

            var map = from.Map;
            var x = p.X;
            var y = p.Y;

            if (!from.InRange(new Point3D(x, y, p.Z), 4))
            {
                from.SendMessage(0x22, "Слишком далеко.");
                return;
            }

            // Пометка одна на стройку и строителя: весь мост откатывается разом, чужие
            // мосты рядом при этом не трогаются.
            var reason = MahaonDemolition.ReasonFor(from, _kit._dismantleMode ? "разбор настила" : "настил");

            if (_kit._dismantleMode)
            {
                var removed = MahaonBridging.Dismantle(map, x, y, reason);

                if (removed == 0)
                {
                    from.SendMessage(0x3B2, "Тут нечего разбирать.");
                    return;
                }

                // Половина досок обратно: разбирать выгоднее, чем бросать, но невыгоднее,
                // чем не строить лишнего.
                var back = removed * MahaonBridging.BoardsPerTile / 2;

                if (back > 0)
                {
                    from.AddToBackpack(new Board(back));
                }

                from.SendMessage(0x59, $"Настил разобран, досок возвращено: {back}.");
                from.PlaySound(0x23D);

                return;
            }

            if (from.Backpack?.GetAmount(typeof(Board)) < MahaonBridging.BoardsPerTile)
            {
                from.SendMessage(0x22, $"Нужно досок: {MahaonBridging.BoardsPerTile}.");
                return;
            }

            // Высоту берём у строителя, а не у цели: мост держит уровень берега.
            var result = MahaonBridging.Build(map, x, y, from.Z, reason);

            switch (result)
            {
                case MahaonBridging.BuildResult.NoGap:
                    from.SendMessage(0x22, "Настилают над водой или провалом, а тут и так земля.");
                    return;

                case MahaonBridging.BuildResult.Occupied:
                    from.SendMessage(0x3B2, "Здесь уже настелено.");
                    return;

                case MahaonBridging.BuildResult.Forbidden:
                    from.SendMessage(0x22, "Здесь строить не дадут.");
                    return;
            }

            from.Backpack.ConsumeTotal(typeof(Board), MahaonBridging.BoardsPerTile);

            from.SendMessage(0x59, "Ты настилаешь доски.");
            from.PlaySound(0x23D);
        }
    }
}
