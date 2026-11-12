using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Systems.MahaonMapEdits;
using Server.Systems.MahaonShamanism;
using Server.Targeting;

namespace Server.Spells.Shamanism;

// Огрское шаманство: земля, камень, грубая сила. Условие раздела — стоять на голой земле.

/// <summary>1. Каменная кожа — физическая защита выше.</summary>
public class StoneSkinSpell : ShamanSpell
{
    private static readonly SpellInfo _info = new("Каменная кожа", "Гррум. Камень.", 230);

    public StoneSkinSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ogre;
    public override int Tier => 0;
    public override string Description => "Кожа твердеет: +12 к физической защите на две минуты.";

    public override void OnCast()
    {
        if (CheckSequence())
        {
            ShamanEffects.AddResists(Caster, "Каменная кожа", new[] { ResistanceType.Physical }, ScaleInt(12), ScaleTime(120));
            Caster.FixedParticles(0x375A, 9, 20, 5016, 0x455, 0, EffectLayer.Waist);
            Caster.PlaySound(0x1F7);
        }

        FinishSequence();
    }
}

/// <summary>2. Бросок валуна — тяжёлый камень, цель на миг оглушена.</summary>
public class BoulderThrowSpell : ShamanSpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new("Бросок валуна", "Гррах! Камень летит!", 263);

    public BoulderThrowSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ogre;
    public override int Tier => 0;
    public override string Description => "Валун в цель: физический урон и оглушение на секунду.";

    public override void OnCast() => Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);

    public void Target(Mobile m)
    {
        if (!CanHarm(m))
        {
            Caster.SendLocalizedMessage(1001018);
            return;
        }

        if (!CheckSequence())
        {
            return;
        }

        SpellHelper.Turn(Caster, m);
        Caster.MovingEffect(m, 0x1363, 6, 0, false, false, 0x455, 0);
        Caster.PlaySound(0x13F);

        Timer.DelayCall(
            TimeSpan.FromSeconds(0.4),
            () =>
            {
                if (!CanHarm(m))
                {
                    return;
                }

                Damage(m, Scale(Utility.RandomMinMax(10, 16)), 100, 0, 0, 0, 0);
                m.Paralyze(TimeSpan.FromSeconds(1));
                m.PlaySound(0x13E);
            }
        );
    }
}

/// <summary>3. Топот — удар оземь, все вокруг оглушены.</summary>
public class StompSpell : ShamanSpell
{
    private static readonly SpellInfo _info = new("Топот", "ГРРУМ!", 263);

    public StompSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ogre;
    public override int Tier => 1;
    public override string Description => "Удар оземь: урон и оглушение на полторы секунды всем в 3 шагах.";

    public override void OnCast()
    {
        if (CheckSequence())
        {
            var map = Caster.Map;
            Caster.PlaySound(0x2F3);

            for (var i = 0; i < 6; i++)
            {
                ShamanFx.Burst(map, ShamanFx.Ground(map, Caster.X + Utility.RandomMinMax(-3, 3), Caster.Y + Utility.RandomMinMax(-3, 3)), 0x36B0, 0x455, 5042, 15);
            }

            foreach (var m in HarmfulAround(Caster.Location, 3))
            {
                Damage(m, Scale(Utility.RandomMinMax(10, 15)), 100, 0, 0, 0, 0);
                m.Paralyze(TimeSpan.FromSeconds(1.5));
            }
        }

        FinishSequence();
    }
}

/// <summary>4. Каменная стена — временная стена поперёк направления к точке.</summary>
public class RockWallSpell : ShamanSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new("Каменная стена", "Камень, встань!", 212);

    public RockWallSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ogre;
    public override int Tier => 1;
    public override string Description => "Стена из пяти камней поперёк, держится 20 секунд.";

    public override void OnCast() => Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);

    public void Target(IPoint3D p)
    {
        if (!CheckPoint(p, out var point) || !CheckSequence())
        {
            return;
        }

        var map = Caster.Map;
        var dx = point.X - Caster.X;
        var dy = point.Y - Caster.Y;

        // Стена поперёк линии «шаман — точка»: если цель больше по X, стена тянется по Y.
        var alongY = Math.Abs(dx) >= Math.Abs(dy);
        var duration = ScaleTime(20);

        for (var i = -2; i <= 2; i++)
        {
            var x = point.X + (alongY ? 0 : i);
            var y = point.Y + (alongY ? i : 0);
            var tile = ShamanFx.Ground(map, x, y);

            if (!map.CanFit(tile, 12, true, false))
            {
                continue;
            }

            new ShamanFieldItem(alongY ? 0x0080 : 0x0080, tile, map, this, duration, 0, 0x455);
            ShamanFx.Burst(map, tile, 0x36B0, 0x455, 5042, 10);
        }

        Effects.PlaySound(point, map, 0x1F6);
    }
}

/// <summary>5. Хватка земли — земля держит всех в области. Посмертный.</summary>
public class EarthGripSpell : ShamanSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new("Хватка земли", "Земля, держи!", 212);

    public EarthGripSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ogre;
    public override int Tier => 2;
    public override bool Posthumous => true;
    public override string Description => "Все в радиусе 2 от точки скованы землёй на 4 секунды.";

    public override void OnCast() => Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);

    public void Target(IPoint3D p)
    {
        if (!CheckPoint(p, out var point) || !CheckSequence())
        {
            return;
        }

        var map = Caster.Map;
        var seconds = 4.0 * Power;

        Effects.PlaySound(point, map, 0x2F3);

        foreach (var m in HarmfulAround(point, 2))
        {
            Harm(m);
            var resisted = m.Skills.MagicResist.Value / 250.0;
            m.Paralyze(TimeSpan.FromSeconds(seconds * (1.0 - resisted)));
            m.FixedParticles(0x376A, 9, 32, 5005, 0x455, 0, EffectLayer.LeftFoot);
            m.SendMessage(0x22, "Земля хватает тебя за ноги.");
        }
    }
}

/// <summary>6. Обвал — каменный дождь на точку.</summary>
public class RockfallSpell : ShamanSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new("Обвал", "Гора, падай!", 212);

    public RockfallSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ogre;
    public override int Tier => 2;
    public override string Description => "Камни с неба в радиусе 2: тяжёлый урон и оглушение на секунду.";

    public override void OnCast() => Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);

    public void Target(IPoint3D p)
    {
        if (!CheckPoint(p, out var point) || !CheckSequence())
        {
            return;
        }

        var map = Caster.Map;

        for (var i = 0; i < 5; i++)
        {
            ShamanFx.FallFromSky(map, ShamanFx.Ground(map, point.X + Utility.RandomMinMax(-2, 2), point.Y + Utility.RandomMinMax(-2, 2)), 0x1363 + Utility.Random(4), 0x455, 7);
        }

        Timer.DelayCall(
            TimeSpan.FromSeconds(0.7),
            () =>
            {
                Effects.PlaySound(point, map, 0x221);

                foreach (var m in HarmfulAround(point, 2))
                {
                    Damage(m, Scale(Utility.RandomMinMax(25, 34)), 100, 0, 0, 0, 0);
                    m.Paralyze(TimeSpan.FromSeconds(1));
                }
            }
        );
    }
}

/// <summary>
///     7. Землетрясение — разлом на 15 шагов в сторону точки. Траншея остаётся навсегда,
///     постройки вдоль неё рушатся, всех рядом сбивает с ног.
/// </summary>
public class QuakeSpell : ShamanSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new("Землетрясение", "ГРРУМ-ГРРУМ! Земля, расколись!", 263);

    private const int Length = 15;
    private const int Depth = 4;

    public QuakeSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ogre;
    public override int Tier => 3;
    public override int ComponentCost => 1;
    public override TimeSpan GlobalCooldown => TimeSpan.FromHours(1);
    public override string Description => "Ритуал 30 с. Разлом на 15 шагов: траншея навсегда, урон и сбивание с ног, рушит постройки.";

    public override void OnCast() => Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);

    public void Target(IPoint3D p)
    {
        if (!CheckPoint(p, out var point))
        {
            return;
        }

        var start = ShamanTerrain.Toward(Caster.Location, point, 2);
        var end = ShamanTerrain.Toward(Caster.Location, point, 2 + Length);

        if (!ShamanUltimates.IsAllowedPlace(Caster.Map, end, out var why) ||
            !ShamanUltimates.IsAllowedPlace(Caster.Map, start, out why))
        {
            Caster.SendMessage(0x22, why);
            return;
        }

        if (!CheckSequence())
        {
            return;
        }

        var map = Caster.Map;
        var line = ShamanTerrain.Line(start, end);
        var power = Power;
        var hit = new HashSet<Mobile>();

        for (var i = 0; i < line.Count; i++)
        {
            var tile = ShamanFx.Ground(map, line[i].X, line[i].Y);
            var index = i;

            Timer.DelayCall(
                TimeSpan.FromMilliseconds(150 * i),
                () =>
                {
                    ShamanFx.Burst(map, tile, 0x36B0, 0x455, 5042, 20);
                    Effects.PlaySound(tile, map, 0x2F3);

                    foreach (var m in HarmfulAround(tile, 2))
                    {
                        if (hit.Add(m))
                        {
                            Damage(m, Utility.RandomMinMax(40, 60) * power, 100, 0, 0, 0, 0);
                            m.Paralyze(TimeSpan.FromSeconds(2));
                        }
                    }

                    if (index % 3 == 0 && MahaonDemolition.IsDestructibleArea(map, tile))
                    {
                        MahaonDemolition.Demolish(map, tile, 1, MahaonDemolition.ReasonFor(Caster, "Землетрясение"));
                    }
                }
            );
        }

        Timer.DelayCall(TimeSpan.FromMilliseconds(150 * line.Count + 200), () => ShamanTerrain.Trench(map, start, end, Depth));
    }
}

/// <summary>
///     8. Пробуждение горы — земля встаёт холмом (навсегда), из него выходит каменный исполин
///     и три минуты бьётся за шамана.
/// </summary>
public class MountainAwakeningSpell : ShamanSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new("Пробуждение горы", "Гора, проснись! Гора, встань!", 263);

    private const int Radius = 8;

    public MountainAwakeningSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ogre;
    public override int Tier => 3;
    public override int ComponentCost => 2;
    public override TimeSpan GlobalCooldown => TimeSpan.FromHours(2);
    public override string Description => "Ритуал 30 с. Холм радиусом 8 навсегда; из него выходит каменный исполин на 3 минуты.";

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

        for (var i = 0; i < 4; i++)
        {
            Timer.DelayCall(TimeSpan.FromSeconds(i * 0.7), () =>
            {
                Effects.PlaySound(point, map, 0x2F3);

                for (var k = 0; k < 6; k++)
                {
                    ShamanFx.Burst(map, ShamanFx.Ground(map, point.X + Utility.RandomMinMax(-Radius, Radius), point.Y + Utility.RandomMinMax(-Radius, Radius)), 0x36B0, 0x455, 5042, 20);
                }
            });
        }

        Timer.DelayCall(
            TimeSpan.FromSeconds(3),
            () =>
            {
                ShamanTerrain.Raise(map, point, Radius, Radius);

                var top = ShamanFx.Ground(map, point.X, point.Y);
                var colossus = new StoneColossus(power);

                if (!BaseCreature.Summon(colossus, Caster, top, 0x2F3, TimeSpan.FromMinutes(3)))
                {
                    colossus.Delete();
                    Caster.SendMessage(0x22, "Исполин не может выйти: у тебя слишком много спутников.");
                }
            }
        );
    }
}

/// <summary>Каменный исполин — призыв «Пробуждения горы».</summary>
[SerializationGenerator(0, false)]
public partial class StoneColossus : BaseCreature
{
    public StoneColossus() : this(1.0)
    {
    }

    public StoneColossus(double power) : base(AIType.AI_Melee)
    {
        Body = 14;
        Hue = 0x455;
        BaseSoundID = 268;

        SetStr((int)(400 * power));
        SetDex(60);
        SetInt(40);
        SetHits((int)(600 * power));
        SetDamage(25, 35);
        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 70);
        SetResistance(ResistanceType.Fire, 50);
        SetResistance(ResistanceType.Cold, 50);
        SetResistance(ResistanceType.Poison, 70);
        SetResistance(ResistanceType.Energy, 40);

        SetSkill(SkillName.MagicResist, 90.0);
        SetSkill(SkillName.Tactics, 110.0);
        SetSkill(SkillName.Wrestling, 110.0);

        VirtualArmor = 50;
        ControlSlots = 0; // вершина не должна упираться в питомцев шамана
    }

    public override string DefaultName => "каменный исполин";
    public override string CorpseName => "груда камней";
    public override bool DeleteCorpseOnDeath => true;
    public override bool BleedImmune => true;
    public override double DispelDifficulty => 140.0;
    public override double DispelFocus => 50.0;
}
