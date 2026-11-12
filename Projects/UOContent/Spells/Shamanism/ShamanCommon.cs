using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonMapEdits;

namespace Server.Spells.Shamanism;

/// <summary>
///     Рельеф от вершин: воронка, разлом, холм. Изменения НАВСЕГДА — так решил пользователь,
///     поэтому всё идёт через ту же систему правок карты, что и лопата (MahaonMapEdits), и
///     по её же правилам места (MahaonExcavation.CanDigHere): не в городах, не у домов, не в
///     настоящей воде и не в скале. Откатить можно только тем же, чем откатывают копку
///     (ГМ-команды сброса блока).
///
///     Склоны — по шагу лопаты (MaxStep = 1 на тайл): конус, а не обрыв. По такому склону
///     можно и спуститься в воронку, и подняться на холм, а клиент рисует его так же, как
///     вырытую яму, которую уже проверяли вживую.
/// </summary>
public static class ShamanTerrain
{
    /// <summary>Опускает землю конусом. Возвращает число изменённых тайлов.</summary>
    public static int Lower(Map map, Point3D center, int radius, int depth) =>
        Reshape(map, center, radius, d => -Math.Max(0, depth - d));

    /// <summary>Поднимает землю конусом с плоской вершиной в клетку.</summary>
    public static int Raise(Map map, Point3D center, int radius, int height) =>
        Reshape(map, center, radius, d => Math.Max(0, height - Math.Max(0, d - 1)));

    /// <summary>Разлом вдоль линии: глубина на оси, склоны по шагу.</summary>
    public static int Trench(Map map, Point3D start, Point3D end, int depth)
    {
        var changed = 0;

        MahaonMapEdits.BeginBatch();

        try
        {
            var plan = new Dictionary<(int, int), int>();
            var axis = Line(start, end);

            foreach (var p in axis)
            {
                for (var dx = -depth; dx <= depth; dx++)
                {
                    for (var dy = -depth; dy <= depth; dy++)
                    {
                        var d = Math.Max(Math.Abs(dx), Math.Abs(dy));
                        var cut = Math.Max(0, depth - d);

                        if (cut == 0)
                        {
                            continue;
                        }

                        var key = (p.X + dx, p.Y + dy);

                        if (!plan.TryGetValue(key, out var existing) || existing < cut)
                        {
                            plan[key] = cut;
                        }
                    }
                }
            }

            foreach (var ((x, y), cut) in plan)
            {
                changed += Shift(map, x, y, -cut) ? 1 : 0;
            }

            MahaonWaterFlow.Recalculate(map, start.X, start.Y);
            MahaonWaterFlow.Recalculate(map, end.X, end.Y);
        }
        finally
        {
            MahaonMapEdits.EndBatch();
        }

        Settle(map, start, Math.Max(Distance(start, end), 1) + depth);
        return changed;
    }

    private static int Reshape(Map map, Point3D center, int radius, Func<int, int> delta)
    {
        if (map == null || map == Map.Internal)
        {
            return 0;
        }

        var changed = 0;

        MahaonMapEdits.BeginBatch();

        try
        {
            for (var x = center.X - radius; x <= center.X + radius; x++)
            {
                for (var y = center.Y - radius; y <= center.Y + radius; y++)
                {
                    var dx = x - center.X;
                    var dy = y - center.Y;
                    var d = (int)Math.Round(Math.Sqrt(dx * dx + dy * dy));

                    if (d > radius)
                    {
                        continue;
                    }

                    var shift = delta(d);

                    if (shift != 0 && Shift(map, x, y, shift))
                    {
                        changed++;
                    }
                }
            }

            MahaonWaterFlow.Recalculate(map, center.X, center.Y);
        }
        finally
        {
            MahaonMapEdits.EndBatch();
        }

        Settle(map, center, radius + 1);
        return changed;
    }

    private static bool Shift(Map map, int x, int y, int shift)
    {
        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height || !MahaonExcavation.CanDigHere(map, x, y, out _))
        {
            return false;
        }

        var current = MahaonExcavation.GroundZ(map, x, y);
        var newZ = Math.Clamp(current + shift, MahaonExcavation.MinZ, 120);

        if (newZ == current)
        {
            return false;
        }

        MahaonSuccession.MoveTrees(map, x, y, newZ);
        MahaonMapEdits.SetLand(
            map, x, y,
            shift < 0 ? MahaonSuccession.RandomFurrow() : MahaonSuccession.Bare[Utility.Random(MahaonSuccession.Bare.Length)],
            newZ
        );
        MahaonSuccession.Dug(map, x, y);
        return true;
    }

    /// <summary>
    ///     Ставит мобов и вещи на новую землю. Сервер сам высоту не пересчитывает, и без этого
    ///     стоявший на месте воронки висел бы в воздухе, а на месте холма — сидел бы в нём.
    /// </summary>
    private static void Settle(Map map, Point3D center, int range)
    {
        var mobiles = new List<Mobile>();

        foreach (var m in map.GetMobilesInRange(center, range))
        {
            mobiles.Add(m);
        }

        foreach (var m in mobiles)
        {
            var ground = map.GetAverageZ(m.X, m.Y);

            if (Math.Abs(m.Z - ground) <= 20)
            {
                m.MoveToWorld(new Point3D(m.X, m.Y, ground), map);
            }
        }

        var items = new List<Item>();

        foreach (var item in map.GetItemsInRange(center, range))
        {
            if (item.Movable || item is ShamanFieldItem or Corpse)
            {
                items.Add(item);
            }
        }

        foreach (var item in items)
        {
            var ground = map.GetAverageZ(item.X, item.Y);

            if (Math.Abs(item.Z - ground) <= 20)
            {
                item.Z = ground;
            }
        }
    }

    /// <summary>Расстояние в клетках (как считает UO: по большей из осей).</summary>
    public static int Distance(Point3D a, Point3D b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    /// <summary>Клетки прямой (Брезенхем) от начала до конца включительно.</summary>
    public static List<Point3D> Line(Point3D a, Point3D b)
    {
        var list = new List<Point3D>();
        int x0 = a.X, y0 = a.Y, x1 = b.X, y1 = b.Y;
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            list.Add(new Point3D(x0, y0, a.Z));

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var e2 = 2 * err;

            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return list;
    }

    /// <summary>Точка на расстоянии length от начала в сторону цели.</summary>
    public static Point3D Toward(Point3D from, Point3D to, int length)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var len = Math.Sqrt(dx * dx + dy * dy);

        if (len < 0.5)
        {
            return from;
        }

        return new Point3D(
            from.X + (int)Math.Round(dx / len * length),
            from.Y + (int)Math.Round(dy / len * length),
            from.Z
        );
    }
}

/// <summary>Короткие визуальные приёмы, общие для обрядов.</summary>
public static class ShamanFx
{
    public static void Burst(Map map, Point3D p, int itemId, int hue, int effect = 5052, int duration = 20)
    {
        Effects.SendLocationParticles(
            EffectItem.Create(p, map, EffectItem.DefaultDuration),
            itemId, 10, duration, hue, 0, effect, 0
        );
    }

    /// <summary>Нечто падает с неба в точку.</summary>
    public static void FallFromSky(Map map, Point3D p, int itemId, int hue, int speed = 6)
    {
        var from = new Point3D(p.X - 4, p.Y - 4, p.Z + 80);
        Effects.SendMovingEffect(map, itemId, from, p, speed, 0, false, true, hue);
    }

    public static Point3D Ground(Map map, int x, int y) => new(x, y, map.GetAverageZ(x, y));
}

/// <summary>
///     Временное поле обряда: огонь (жжёт стоящих) или камень (просто стоит стеной). Не
///     переживает перезапуск сервера: через секунду после загрузки удаляет себя.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShamanFieldItem : Item
{
    private ShamanSpell _spell;
    private int _damage;
    private Timer _timer;
    private DateTime _end;

    public ShamanFieldItem(int itemId, Point3D loc, Map map, ShamanSpell spell, TimeSpan duration, int damage, int hue = 0)
        : base(itemId)
    {
        Movable = false;
        Hue = hue;
        _spell = spell;
        _damage = damage;
        _end = Core.Now + duration;

        if (damage > 0)
        {
            Light = LightType.Circle300;
        }

        MoveToWorld(loc, map);

        _timer = Timer.DelayCall(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), Tick);
    }

    public override bool BlocksFit => true;

    private void Tick()
    {
        if (Deleted)
        {
            _timer?.Stop();
            return;
        }

        if (Core.Now >= _end)
        {
            Delete();
            return;
        }

        if (_damage <= 0 || _spell == null)
        {
            return;
        }

        var victims = new List<Mobile>();

        foreach (var m in GetMobilesAt())
        {
            if (m.Z + 16 > Z && Z + 12 > m.Z && _spell.CanHarm(m))
            {
                victims.Add(m);
            }
        }

        foreach (var m in victims)
        {
            _spell.Damage(m, _damage, 0, 100, 0, 0, 0);
            m.PlaySound(0x208);
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _timer?.Stop();
    }

    [AfterDeserialization]
    private void AfterDeserialization() => Timer.DelayCall(Delete);
}
