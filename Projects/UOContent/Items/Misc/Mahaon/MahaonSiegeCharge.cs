using System;
using ModernUO.Serialization;
using Server.Systems.MahaonMapEdits;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Подрывной заряд — первый способ менять карту руками игрока, а не командой ГМ.
///
///     Ставится на землю, тлеет несколько секунд и сносит стены и крыши вокруг. Снесённое
///     уходит в журнал правок одной пометкой, так что при нужде весь подрыв отменяется
///     целиком: [MapUndo reason подрыв: Вася в 14:32.
///
///     Фитиль не для красоты. Во-первых, он даёт уйти — взрыв бьёт и по живым. Во-вторых,
///     несколько секунд между «поставил» и «сработало» — это окно, в которое ГМ успевает
///     вмешаться, если на сервере что-то идёт не так.
///
///     Сносится только постройка и только вне охраняемых городов — см. MahaonDemolition,
///     там же и обоснование правил.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonSiegeCharge : Item
{
    /// <summary>Радиус разрушения в тайлах.</summary>
    public const int BlastRadius = 3;

    /// <summary>Сколько тлеет фитиль.</summary>
    public static readonly TimeSpan FuseDelay = TimeSpan.FromSeconds(5);

    /// <summary>Урон живым в эпицентре; с расстоянием спадает.</summary>
    private const int MaxDamage = 40;

    [SerializableField(0)]
    private bool _armed;

    [Constructible]
    public MahaonSiegeCharge() : base(0x0E7F)
    {
        Weight = 20.0;
        Name = "подрывной заряд";
        Hue = 0x21;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"Радиус разрушения: {BlastRadius}");
        list.Add("Рушит стены и крыши. Не работает в городах.");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_armed)
        {
            from.SendMessage(0x22, "Фитиль уже горит.");
            return;
        }

        if (!IsChildOf(from.Backpack) && !(Parent == null && from.InRange(GetWorldLocation(), 2)))
        {
            from.SendMessage(0x22, "Заряд должен быть у тебя в рюкзаке или под рукой.");
            return;
        }

        from.SendMessage(0x3B2, "Куда заложить заряд?");
        from.Target = new PlaceTarget(this);
    }

    private void Arm(Mobile from)
    {
        _armed = true;

        Name = "подрывной заряд (горит фитиль)";
        InvalidateProperties();

        PublicOverheadMessage(MessageType.Regular, 0x21, false, "фитиль зашипел...");
        Effects.PlaySound(GetWorldLocation(), Map, 0x226);

        Timer.StartTimer(FuseDelay, () => Detonate(from), out _);
    }

    private void Detonate(Mobile author)
    {
        if (Deleted)
        {
            return;
        }

        var location = GetWorldLocation();
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            Delete();
            return;
        }

        Effects.PlaySound(location, map, 0x207);
        Effects.SendLocationEffect(location, map, 0x36BD, 20, 10, 0, 0);

        HurtNearby(location, map, author);

        var reason = MahaonDemolition.ReasonFor(author, "подрыв");
        var destroyed = MahaonDemolition.Demolish(map, location, BlastRadius, reason);

        if (destroyed > 0)
        {
            ScatterRubble(location, map, destroyed);

            author?.SendMessage(
                0x59,
                $"Обрушено тайлов постройки: {destroyed}. Отменить весь подрыв: [MapUndo reason {reason}"
            );
        }
        else
        {
            author?.SendMessage(
                0x3B2,
                MahaonDemolition.IsDestructibleArea(map, location)
                    ? "Рушить тут нечего — ни стен, ни крыш рядом."
                    : "В городе стражу такое не порадует: здесь ничего не рушится."
            );
        }

        Delete();
    }

    private static void HurtNearby(Point3D location, Map map, Mobile author)
    {
        foreach (var m in map.GetMobilesInRange<Mobile>(location, BlastRadius))
        {
            if (m.Deleted || !m.Alive)
            {
                continue;
            }

            var distance = m.GetDistanceToSqrt(location);
            var damage = (int)(MaxDamage * (1.0 - distance / (BlastRadius + 1.0)));

            if (damage > 0)
            {
                AOS.Damage(m, author, damage, 0, 100, 0, 0, 0);
            }
        }
    }

    /// <summary>
    ///     Обломки. Нужны не для красоты: снесённая стена иначе исчезает бесследно, и место
    ///     выглядит так, будто дома тут и не было. Обломки — предметы, а не карта, поэтому
    ///     их можно убрать обычной уборкой и они не мешают откату.
    /// </summary>
    private static void ScatterRubble(Point3D location, Map map, int destroyed)
    {
        var pieces = Math.Clamp(destroyed / 3, 1, 8);

        for (var i = 0; i < pieces; i++)
        {
            var x = location.X + Utility.RandomMinMax(-BlastRadius, BlastRadius);
            var y = location.Y + Utility.RandomMinMax(-BlastRadius, BlastRadius);

            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
            {
                continue;
            }

            var rubble = new MahaonRubble();
            rubble.MoveToWorld(new Point3D(x, y, map.GetAverageZ(x, y)), map);
        }
    }

    private class PlaceTarget : Target
    {
        private readonly MahaonSiegeCharge _charge;

        public PlaceTarget(MahaonSiegeCharge charge) : base(3, true, TargetFlags.None) => _charge = charge;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_charge.Deleted || _charge.Armed || targeted is not IPoint3D p || from.Map == null)
            {
                return;
            }

            var location = new Point3D(p.X, p.Y, p.Z);

            if (!from.InRange(location, 3))
            {
                from.SendMessage(0x22, "Слишком далеко.");
                return;
            }

            if (!MahaonDemolition.IsDestructibleArea(from.Map, location))
            {
                from.SendMessage(0x22, "В городе такое не заложишь — стража рядом.");
                return;
            }

            _charge.MoveToWorld(location, from.Map);
            _charge.Arm(from);
        }
    }
}

/// <summary>Обломки после подрыва. Обычный предмет: убирается уборкой и не мешает откату.</summary>
[SerializationGenerator(0, false)]
public partial class MahaonRubble : Item
{
    private static readonly int[] Graphics = { 0x1363, 0x1364, 0x1365, 0x1366, 0x1367 };

    [Constructible]
    public MahaonRubble() : base(Graphics[Utility.Random(Graphics.Length)])
    {
        Movable = false;
        Name = "обломки";
    }
}
