using System;
using System.Collections.Generic;
using Server.Systems.MahaonMapEdits;
using Server.Systems.MahaonShamanism;
using Server.Targeting;

namespace Server.Spells.Shamanism;

// Орочье шаманство: огонь, кровь, ярость. Условие раздела — горящий огонь рядом.

/// <summary>1. Тлеющий уголь — малый огненный снаряд, цель ещё тлеет пару секунд.</summary>
public class EmberSpell : ShamanSpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new("Тлеющий уголь", "Гхар туш!", 212);

    public EmberSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Orc;
    public override int Tier => 0;
    public override string Description => "Огненный снаряд в цель; цель ещё три секунды тлеет.";

    public override void OnCast() => Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);

    public void Target(Mobile m)
    {
        if (!CanHarm(m))
        {
            Caster.SendLocalizedMessage(1001018); // You can not perform negative acts on your target.
            return;
        }

        if (!CheckSequence())
        {
            return;
        }

        SpellHelper.Turn(Caster, m);
        Caster.MovingParticles(m, 0x36D4, 7, 0, false, true, 0x489, 0, 9502, 4019, 0x160, EffectLayer.Waist, 0);
        Caster.PlaySound(0x15E);

        Damage(m, Scale(Utility.RandomMinMax(9, 14)), 0, 100, 0, 0, 0);

        var burn = Math.Max(1, ScaleInt(2));

        for (var i = 1; i <= 3; i++)
        {
            Timer.DelayCall(
                TimeSpan.FromSeconds(i),
                () =>
                {
                    if (CanHarm(m))
                    {
                        m.FixedParticles(0x3709, 1, 10, 5052, 0x489, 0, EffectLayer.Waist);
                        Damage(m, burn, 0, 100, 0, 0, 0);
                    }
                }
            );
        }
    }
}

/// <summary>2. Боевой клич — свои рядом сильнее и быстрее.</summary>
public class WarCrySpell : ShamanSpell
{
    private static readonly SpellInfo _info = new("Боевой клич", "Ваагх-ра!", 263);

    public WarCrySpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Orc;
    public override int Tier => 0;
    public override string Description => "Ты и союзники в 8 шагах: сила и ловкость выше на минуту.";

    public override void OnCast()
    {
        if (CheckSequence())
        {
            var bonus = Math.Max(3, ScaleInt(6));
            var duration = ScaleTime(60);

            Caster.PublicOverheadMessage(MessageType.Emote, 0x21, false, "*ревёт боевой клич*");
            Caster.PlaySound(Caster.Female ? 0x338 : 0x44A);

            foreach (var m in AlliesAround(8))
            {
                ShamanEffects.AddStats(m, "Боевой клич", bonus, bonus, 0, duration);
                m.FixedParticles(0x373A, 10, 15, 5018, 0x489, 0, EffectLayer.Waist);
                m.SendMessage(0x21, "Боевой клич наполняет тебя яростью.");
            }
        }

        FinishSequence();
    }
}

/// <summary>3. Кровавая жертва — платит здоровьем, следующий обряд в полтора раза сильнее.</summary>
public class BloodSacrificeSpell : ShamanSpell
{
    private static readonly SpellInfo _info = new("Кровавая жертва", "Крав гхул ар!", 230);

    private static readonly Dictionary<Mobile, DateTime> Empowered = new();

    public BloodSacrificeSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Orc;
    public override int Tier => 1;
    public override string Description => "Отдаёшь 15% здоровья: следующий обряд (в течение 30 с) сильнее в полтора раза.";

    /// <summary>Есть ли у шамана нерастраченная жертва; если есть — тратит её.</summary>
    public static bool ConsumeEmpower(Mobile m)
    {
        if (!Empowered.Remove(m, out var until))
        {
            return false;
        }

        return until > Core.Now;
    }

    public override bool CheckCast()
    {
        if (!base.CheckCast())
        {
            return false;
        }

        if (Caster.Hits <= Caster.HitsMax * 0.2)
        {
            Caster.SendMessage(0x22, "Ты слишком изранен — крови на жертву не осталось.");
            return false;
        }

        return true;
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            Caster.Hits = Math.Max(1, Caster.Hits - (int)(Caster.HitsMax * 0.15));
            Empowered[Caster] = Core.Now + TimeSpan.FromSeconds(30);

            Caster.FixedParticles(0x377A, 1, 32, 9949, 0x21, 0, EffectLayer.Waist);
            Caster.PlaySound(0x1F1);
            Caster.SendMessage(0x21, "Кровь стекает в огонь. Предки приняли жертву.");
        }

        FinishSequence();
    }
}

/// <summary>4. Огненное кольцо — кольцо огня вокруг шамана. Посмертно — вокруг якоря.</summary>
public class FireRingSpell : ShamanSpell
{
    private static readonly SpellInfo _info = new("Огненное кольцо", "Гхар кругх!", 206);

    public FireRingSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Orc;
    public override int Tier => 1;
    public override bool Posthumous => true;
    public override string Description => "Кольцо огня в два шага вокруг тебя. Посмертно — вокруг якоря печати.";

    public override void OnCast()
    {
        if (CheckSequence())
        {
            var center = FromBeyond && SoulSeal.Find(Caster) is { } anchor ? anchor.GetWorldLocation() : Caster.Location;
            var map = Caster.Map;
            var duration = ScaleTime(12);
            var damage = Math.Max(2, ScaleInt(4));

            for (var dx = -2; dx <= 2; dx++)
            {
                for (var dy = -2; dy <= 2; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != 2)
                    {
                        continue;
                    }

                    var p = ShamanFx.Ground(map, center.X + dx, center.Y + dy);
                    var id = Math.Abs(dx) == 2 ? 0x3996 : 0x398C;
                    new ShamanFieldItem(id, p, map, this, duration, damage);
                }
            }

            Effects.PlaySound(center, map, 0x20C);
        }

        FinishSequence();
    }
}

/// <summary>5. Ярость предков — берсерк: сильнее и крепче, но здоровье уходит.</summary>
public class AncestralRageSpell : ShamanSpell
{
    private static readonly SpellInfo _info = new("Ярость предков", "Ваагх крав мортх!", 263);

    public AncestralRageSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Orc;
    public override int Tier => 2;
    public override string Description => "На 30 с: +сила, +ловкость, +10 к физ. защите; каждую секунду теряешь 2 здоровья.";

    public override void OnCast()
    {
        if (CheckSequence())
        {
            var seconds = 30.0;
            var duration = TimeSpan.FromSeconds(seconds);
            var str = ScaleInt(20);
            var dex = ScaleInt(10);

            ShamanEffects.AddStats(Caster, "Ярость предков", str, dex, 0, duration);
            ShamanEffects.AddResists(Caster, "Ярость предков", new[] { ResistanceType.Physical }, 10, duration);

            Caster.FixedParticles(0x3709, 10, 30, 5052, 0x21, 0, EffectLayer.LeftFoot);
            Caster.PlaySound(0x1F3);
            Caster.PublicOverheadMessage(MessageType.Emote, 0x21, false, "*глаза наливаются кровью*");

            var caster = Caster;
            Timer.DelayCall(
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), (int)seconds,
                () =>
                {
                    if (caster.Alive && caster.Hits > 2)
                    {
                        caster.Hits -= 2;
                    }
                }
            );
        }

        FinishSequence();
    }
}

/// <summary>6. Пламенный вал — волна огня по линии к точке.</summary>
public class FlameWaveSpell : ShamanSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new("Пламенный вал", "Гхар вал тагх!", 212);

    public FlameWaveSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Orc;
    public override int Tier => 2;
    public override string Description => "Огненная волна на 8 шагов в сторону цели; жжёт всех на пути.";

    public override void OnCast() => Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);

    public void Target(IPoint3D p)
    {
        if (!CheckPoint(p, out var point) || !CheckSequence())
        {
            return;
        }

        SpellHelper.Turn(Caster, point);

        var map = Caster.Map;
        var end = ShamanTerrain.Toward(Caster.Location, point, 8);
        var line = ShamanTerrain.Line(Caster.Location, end);
        var damage = Scale(Utility.RandomMinMax(22, 30));
        var hit = new HashSet<Mobile>();

        for (var i = 1; i < line.Count; i++)
        {
            var tile = ShamanFx.Ground(map, line[i].X, line[i].Y);
            var delay = TimeSpan.FromMilliseconds(120 * i);

            Timer.DelayCall(
                delay,
                () =>
                {
                    ShamanFx.Burst(map, tile, 0x3709, 0x489);
                    Effects.PlaySound(tile, map, 0x208);

                    foreach (var m in HarmfulAround(tile, 1))
                    {
                        if (hit.Add(m))
                        {
                            Damage(m, damage, 0, 100, 0, 0, 0);
                        }
                    }
                }
            );
        }
    }
}

/// <summary>7. Пепельный ливень — несколько волн огненного дождя по области.</summary>
public class AshRainSpell : ShamanSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new("Пепельный ливень", "Гхар мортх ул-ракх!", 212);

    public AshRainSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Orc;
    public override int Tier => 3;
    public override int ComponentCost => 1;
    public override TimeSpan GlobalCooldown => TimeSpan.FromMinutes(30);
    public override string Description => "Ритуал 30 с. Пять волн огненного дождя в радиусе 6, пожары.";

    public override void OnCast() => Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);

    public void Target(IPoint3D p)
    {
        if (!CheckPoint(p, out var point))
        {
            return;
        }

        if (!ShamanUltimates.IsAllowedPlace(Caster.Map, point, out var why))
        {
            Caster.SendMessage(0x22, why);
            return;
        }

        if (!CheckSequence())
        {
            return;
        }

        var map = Caster.Map;
        var damage = Scale(Utility.RandomMinMax(26, 34));

        for (var wave = 0; wave < 5; wave++)
        {
            Timer.DelayCall(
                TimeSpan.FromSeconds(1.5 * wave),
                () =>
                {
                    for (var i = 0; i < 6; i++)
                    {
                        var x = point.X + Utility.RandomMinMax(-6, 6);
                        var y = point.Y + Utility.RandomMinMax(-6, 6);
                        var spot = ShamanFx.Ground(map, x, y);

                        ShamanFx.FallFromSky(map, spot, 0x36D4, 0x489, 8);

                        Timer.DelayCall(
                            TimeSpan.FromSeconds(0.6),
                            () =>
                            {
                                ShamanFx.Burst(map, spot, 0x36BD, 0x489);
                                Effects.PlaySound(spot, map, 0x307);

                                foreach (var m in HarmfulAround(spot, 1))
                                {
                                    Damage(m, damage, 0, 100, 0, 0, 0);
                                }

                                if (Utility.RandomDouble() < 0.35)
                                {
                                    new ShamanFieldItem(0x398C, spot, map, this, TimeSpan.FromSeconds(15), ScaleInt(5));
                                }
                            }
                        );
                    }
                }
            );
        }
    }
}

/// <summary>
///     8. Метеорит — главный удар орочьего раздела. Камень с неба: урон по большой области,
///     воронка навсегда, разрушенные постройки рядом, пожар и сам метеорит на дне воронки.
/// </summary>
public class MeteorSpell : ShamanSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new("Метеорит", "Гхар ул-ракх мортх дум!", 212);

    private const int Radius = 6;

    public MeteorSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Orc;
    public override int Tier => 3;
    public override int ComponentCost => 2;
    public override TimeSpan GlobalCooldown => TimeSpan.FromHours(2);
    public override string Description => "Ритуал 30 с. Камень с неба: огромный урон в радиусе 6, воронка навсегда, пожар.";

    public override void OnCast() => Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);

    public void Target(IPoint3D p)
    {
        if (!CheckPoint(p, out var point))
        {
            return;
        }

        if (!ShamanUltimates.IsAllowedPlace(Caster.Map, point, out var why))
        {
            Caster.SendMessage(0x22, why);
            return;
        }

        if (!CheckSequence())
        {
            return;
        }

        var map = Caster.Map;
        var power = Power;

        // Видно заранее: тень и гул за три секунды — шанс отбежать.
        for (var i = 0; i < 3; i++)
        {
            Timer.DelayCall(TimeSpan.FromSeconds(i), () =>
            {
                ShamanFx.Burst(map, point, 0x3728, 0x489, 5042, 30);
                Effects.PlaySound(point, map, 0x2F3);
            });
        }

        Timer.DelayCall(TimeSpan.FromSeconds(2.2), () => ShamanFx.FallFromSky(map, point, 0x36D4, 0x489, 4));

        Timer.DelayCall(TimeSpan.FromSeconds(3), () => Impact(map, point, power));
    }

    private void Impact(Map map, Point3D point, double power)
    {
        Effects.PlaySound(point, map, 0x307);

        for (var i = 0; i < 12; i++)
        {
            var spot = ShamanFx.Ground(map, point.X + Utility.RandomMinMax(-4, 4), point.Y + Utility.RandomMinMax(-4, 4));
            ShamanFx.Burst(map, spot, 0x36BD, 0x489, 5044, 20);
        }

        foreach (var m in HarmfulAround(point, Radius))
        {
            var distance = ShamanTerrain.Distance(point, m.Location);
            var damage = (110 - distance * 14) * power;
            Damage(m, damage, 40, 60, 0, 0, 0);
        }

        if (MahaonDemolition.IsDestructibleArea(map, point))
        {
            MahaonDemolition.Demolish(map, point, 3, MahaonDemolition.ReasonFor(Caster, "Метеорит"));
        }

        ShamanTerrain.Lower(map, point, Radius, Radius);

        // Сам метеорит остаётся лежать на дне — тоже навсегда, статикой карты.
        var bottom = MahaonExcavation.GroundZ(map, point.X, point.Y);
        MahaonMapEdits.AddStatic(map, point.X, point.Y, bottom, 0xA1CE + Utility.Random(4), 0);

        for (var i = 0; i < 10; i++)
        {
            var spot = ShamanFx.Ground(map, point.X + Utility.RandomMinMax(-7, 7), point.Y + Utility.RandomMinMax(-7, 7));
            new ShamanFieldItem(0x398C, spot, map, this, TimeSpan.FromSeconds(30), (int)(5 * power));
        }
    }
}
