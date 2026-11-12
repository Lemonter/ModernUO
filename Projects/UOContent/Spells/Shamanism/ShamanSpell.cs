using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonShamanism;

namespace Server.Spells.Shamanism;

/// <summary>Раздел книги шамана. Значения — ключи сохранения специализаций, не переименовывать.</summary>
public enum ShamanTradition : byte
{
    Orc = 0,
    Goblin = 1,
    Ogre = 2,
    Ancient = 3
}

/// <summary>
///     Общая основа шаманских обрядов.
///
///     ---- Ступени ----
///
///     В каждом разделе восемь обрядов, по два на ступень навыка Шаманства: 0 / 30 / 60 /
///     90. Последняя ступень — вершины: им дополнительно нужна специализация раздела
///     (ShamanismSchoolSystem.VertexRequirement), редкий компонент, долгий ритуал, и на них
///     лежит откат на весь сервер (ShamanUltimates).
///
///     ---- Атрибуты ----
///
///     Условие раздела (огонь, музыка, земля, круг — ShamanRequirements) проверяется и в
///     начале каста, и в момент срабатывания.
///
///     ---- Посмертие ----
///
///     Обряды с Posthumous можно творить призраком — но только рядом с якорем Печати души
///     (SoulSeal) и за нить души вместо маны. Базовый Spell мёртвым колдовать не даёт;
///     CastWhileDead снимает этот запрет только для таких обрядов. Призрак не может «вредить»
///     по правилам движка (Mobile.CanBeHarmful требует живого), поэтому вредные проверки тут
///     свои: CanHarm / Harm.
///
///     ---- Сила ----
///
///     Power — множитель, который считается один раз в момент срабатывания: навык,
///     специализация раздела, Кровавая жертва, ночь для древнего раздела. Обряды умножают на
///     него свои базовые числа через Scale.
/// </summary>
public abstract class ShamanSpell : Spell
{
    private static readonly double[] SkillByTier = { 0.0, 30.0, 60.0, 90.0 };
    private static readonly int[] ManaByTier = { 8, 15, 25, 60 };

    private double _power = 1.0;
    private Timer _ritualWatch;

    protected ShamanSpell(Mobile caster, Item scroll, SpellInfo info) : base(caster, scroll, info)
    {
    }

    public abstract ShamanTradition Tradition { get; }

    /// <summary>0..3 — ступень; 3 — вершины.</summary>
    public abstract int Tier { get; }

    /// <summary>Описание для книги: одна-две строки.</summary>
    public abstract string Description { get; }

    /// <summary>Можно творить призраком у якоря Печати души.</summary>
    public virtual bool Posthumous => false;

    /// <summary>Сколько компонентов раздела съедает вершина (у остальных — ноль).</summary>
    public virtual int ComponentCost => 0;

    public bool IsVertex => Tier >= 3;

    /// <summary>Откат вершины на весь сервер (ShamanUltimates).</summary>
    public virtual TimeSpan GlobalCooldown => TimeSpan.FromHours(1);

    public double RequiredSkill => SkillByTier[Math.Clamp(Tier, 0, 3)];

    /// <summary>Колдует ли призрак — с той стороны.</summary>
    public bool FromBeyond => Caster is { Alive: false };

    /// <summary>Множитель силы этого каста. Действует после CheckSequence.</summary>
    public double Power => _power;

    public override SkillName CastSkill => SkillName.Shamanism;
    public override SkillName DamageSkill => SkillName.Shamanism;
    public override bool ClearHandsOnCast => false;
    public override bool CastWhileDead => Posthumous;

    // Призрак виден только при проявлении (SoulSeal.Manifest), а не от каждого жеста.
    public override bool RevealOnCast => !FromBeyond;

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(0.75 + 0.5 * Tier);

    /// <summary>Длительность ритуала вершины: фиксированная, ускорители каста её не трогают.</summary>
    public virtual TimeSpan RitualDuration => TimeSpan.FromSeconds(30);

    public override TimeSpan GetCastDelay() => IsVertex ? RitualDuration : base.GetCastDelay();

    public override int GetMana()
    {
        if (FromBeyond)
        {
            return 0; // призрак платит нитью души, а не маной
        }

        return (int)Math.Round(ManaByTier[Math.Clamp(Tier, 0, 3)] * ShamanismSchoolSystem.ManaScalar(Caster, Tradition));
    }

    public override void GetCastSkills(out double min, out double max)
    {
        var required = RequiredSkill;

        // Вершины — уже окно: на пороге 50%, к 120 — без провалов. Иначе долгий ритуал с
        // редким компонентом проваливался бы через раз.
        if (IsVertex)
        {
            min = required - 30.0;
            max = required + 30.0;
        }
        else
        {
            min = required - 15.0;
            max = required + 45.0;
        }
    }

    // ---- Проверки ----------------------------------------------------------------------

    public override bool CheckCast()
    {
        if (!base.CheckCast())
        {
            return false;
        }

        if (Caster.Skills.Shamanism.Value < RequiredSkill)
        {
            Caster.SendMessage(0x22, $"Для этого обряда нужно Шаманство {RequiredSkill:0}.");
            return false;
        }

        return CheckConditions(true);
    }

    /// <summary>Всё, что должно быть верно и в начале каста, и в момент срабатывания.</summary>
    private bool CheckConditions(bool atStart)
    {
        string why;

        if (FromBeyond)
        {
            if (!Posthumous)
            {
                Caster.SendMessage(0x22, "Этот обряд не творят с той стороны.");
                return false;
            }

            if (!SoulSeal.CanCastFromBeyond(Caster, out why))
            {
                Caster.SendMessage(0x22, why);
                return false;
            }

            return true;
        }

        if (!ShamanRequirements.Check(Caster, Tradition, out why))
        {
            Caster.SendMessage(0x22, why);
            return false;
        }

        if (IsVertex && !ShamanUltimates.CanPerform(this, atStart, out why))
        {
            Caster.SendMessage(0x22, why);
            return false;
        }

        return true;
    }

    public override bool CheckSequence()
    {
        if (!CheckConditions(false))
        {
            DoFizzle();
            return false;
        }

        if (!base.CheckSequence())
        {
            return false;
        }

        _power = ComputePower();

        if (FromBeyond)
        {
            SoulSeal.OnCastFromBeyond(Caster, this);
        }
        else
        {
            ShamanRequirements.Consume(Caster, Tradition);

            if (IsVertex)
            {
                ShamanUltimates.OnPerformed(this);
            }
        }

        return true;
    }

    private double ComputePower()
    {
        var skill = Caster.Skills.Shamanism.Value;

        // 0.6 у новичка, 1.0 на сотне, 1.08 на 120: ступень решает, ЧТО ты можешь, а навык
        // — насколько сильно.
        var power = 0.6 + 0.4 * Math.Min(skill, 120.0) / 100.0;

        power *= ShamanismSchoolSystem.PowerScalar(Caster, Tradition);

        if (this is not BloodSacrificeSpell && BloodSacrificeSpell.ConsumeEmpower(Caster))
        {
            power *= 1.5;
            Caster.SendMessage(0x21, "Кровь предков вливается в обряд.");
        }

        if (Tradition == ShamanTradition.Ancient && IsNight(Caster))
        {
            power *= 1.2;
        }

        return power;
    }

    public static bool IsNight(Mobile m)
    {
        if (m.Map == null || m.Map == Map.Internal)
        {
            return false;
        }

        Clock.GetTime(m.Map, m.X, m.Y, out int hours, out int _);
        return hours >= 22 || hours < 5;
    }

    // ---- Ритуал вершин -----------------------------------------------------------------

    public override void OnBeginCast()
    {
        base.OnBeginCast();

        if (!IsVertex)
        {
            return;
        }

        ShamanUltimates.AnnounceBegin(this);

        // На шарде урон каст не сбивает (Spell.OnCasterHurt больше никто не зовёт), но ритуал
        // вершины обещан сбиваемым — иначе противнику нечем ответить на тридцать секунд
        // неподвижного шамана. Следим за здоровьем сами.
        var lastHits = Caster.Hits;
        _ritualWatch = Timer.DelayCall(
            TimeSpan.FromSeconds(0.5),
            TimeSpan.FromSeconds(0.5),
            () =>
            {
                if (State != SpellState.Casting || Caster.Spell != this || Caster.Deleted)
                {
                    _ritualWatch?.Stop();
                    return;
                }

                if (Caster.Hits < lastHits)
                {
                    _ritualWatch?.Stop();
                    Caster.SendMessage(0x22, "Боль сбивает тебя с ритма обряда.");
                    Disturb(DisturbType.Hurt);
                    return;
                }

                lastHits = Caster.Hits;

                if (Utility.RandomDouble() < 0.3)
                {
                    RitualPulse();
                }
            }
        );
    }

    /// <summary>Эффект, пока идёт ритуал вершины — чтобы его было видно издалека.</summary>
    protected virtual void RitualPulse()
    {
        Caster.FixedParticles(0x3709, 10, 30, 5052, RitualHue, 0, EffectLayer.Waist);
    }

    protected int RitualHue => Tradition switch
    {
        ShamanTradition.Orc     => 0x489,
        ShamanTradition.Goblin  => 0x48F,
        ShamanTradition.Ogre    => 0x455,
        ShamanTradition.Ancient => 0x4F2,
        _                       => 0
    };

    public override void FinishSequence()
    {
        _ritualWatch?.Stop();
        base.FinishSequence();
    }

    // ---- Помощники для обрядов ------------------------------------------------------

    protected double Scale(double value) => value * _power;

    protected int ScaleInt(double value) => (int)Math.Round(value * _power);

    protected TimeSpan ScaleTime(double seconds) => TimeSpan.FromSeconds(seconds * _power);

    /// <summary>
    ///     Можно ли этому мобу навредить. Живой шаман — по обычным правилам движка; призрак
    ///     — по своим (движок считает, что мёртвый вредить не может вовсе).
    /// </summary>
    public bool CanHarm(Mobile m)
    {
        if (m == null || m.Deleted || !m.Alive || m == Caster || m.Blessed || m.Map != Caster.Map)
        {
            return false;
        }

        if (!FromBeyond)
        {
            return Caster.CanBeHarmful(m, false) && SpellHelper.ValidIndirectTarget(Caster, m);
        }

        if (m.AccessLevel > AccessLevel.Player && m.AccessLevel > Caster.AccessLevel)
        {
            return false;
        }

        return m is not BaseCreature { IsInvulnerable: true } && SpellHelper.ValidIndirectTarget(Caster, m);
    }

    /// <summary>Отметить вредное действие: агрессия, крим и т.п. — только у живого шамана.</summary>
    public void Harm(Mobile m)
    {
        if (!FromBeyond)
        {
            Caster.DoHarmful(m);
        }
    }

    /// <summary>Нанести урон от лица обряда (с учётом всех школьных множителей шарда).</summary>
    public void Damage(Mobile m, double damage, int phys, int fire, int cold, int pois, int nrgy)
    {
        Harm(m);
        SpellHelper.Damage(this, TimeSpan.Zero, m, Caster, Math.Max(1, damage), phys, fire, cold, pois, nrgy);
    }

    /// <summary>Все, кому можно навредить, в радиусе от точки.</summary>
    public List<Mobile> HarmfulAround(IPoint3D point, int range, bool requireLos = false)
    {
        var list = new List<Mobile>();
        var map = Caster.Map;

        if (map == null || map == Map.Internal)
        {
            return list;
        }

        var p = new Point3D(point);

        foreach (var m in map.GetMobilesInRange(p, range))
        {
            if (CanHarm(m) && (!requireLos || map.LineOfSight(p, m)))
            {
                list.Add(m);
            }
        }

        return list;
    }

    /// <summary>Свои и союзники в радиусе: сам шаман, его группа, гильдия, питомцы.</summary>
    public List<Mobile> AlliesAround(int range)
    {
        var list = new List<Mobile> { Caster };
        var map = Caster.Map;

        if (map == null || map == Map.Internal)
        {
            return list;
        }

        foreach (var m in map.GetMobilesInRange(Caster.Location, range))
        {
            if (m == Caster || !m.Alive || m.Deleted)
            {
                continue;
            }

            if (IsAlly(m) && Caster.CanBeBeneficial(m, false))
            {
                list.Add(m);
            }
        }

        return list;
    }

    public bool IsAlly(Mobile m)
    {
        if (m is BaseCreature bc && (bc.ControlMaster == Caster || bc.SummonMaster == Caster))
        {
            return true;
        }

        var party = Engines.PartySystem.Party.Get(Caster);

        if (party?.Contains(m) == true)
        {
            return true;
        }

        var guild = SpellHelper.GetGuildFor(Caster);
        var other = SpellHelper.GetGuildFor(m);

        return guild != null && other != null && (guild == other || guild.IsAlly(other));
    }

    /// <summary>Проверка цели-точки: видна, на карте, достаёт.</summary>
    protected bool CheckPoint(IPoint3D p, out Point3D point)
    {
        point = Point3D.Zero;

        if (p == null || Caster.Map == null)
        {
            return false;
        }

        SpellHelper.GetSurfaceTop(ref p);
        point = new Point3D(p);

        if (!Caster.InRange(point, 14))
        {
            Caster.SendLocalizedMessage(500643); // Target is too far away.
            return false;
        }

        if (!Caster.InLOS(point))
        {
            Caster.SendLocalizedMessage(500237); // Target can not be seen.
            return false;
        }

        return true;
    }
}
