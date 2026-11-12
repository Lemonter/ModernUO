using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonRaids;
using Server.Systems.MahaonShamanism;
using Server.Targeting;

namespace Server.Spells.Shamanism;

// Гоблинское шаманство: ворожба, яды, порча, морок. Условие раздела — инструмент в рюкзаке;
// каждый обряд снимает с него одно использование (играется под музыку).

/// <summary>1. Дурной глаз — цель хуже бьёт и колдует. Посмертный.</summary>
public class EvilEyeSpell : ShamanSpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new("Дурной глаз", "Гыз-гыз, кривой глаз!", 212);

    public EvilEyeSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Goblin;
    public override int Tier => 0;
    public override bool Posthumous => true;
    public override string Description => "Сглаз: ловкость и интеллект цели ниже на 15% на 40 секунд.";

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

        Harm(m);

        var fraction = Math.Min(0.3, 0.15 * Power);
        var duration = ScaleTime(40);
        ShamanEffects.AddStats(m, "Дурной глаз", 0, -(int)Math.Max(1, m.RawDex * fraction), -(int)Math.Max(1, m.RawInt * fraction), duration);

        m.FixedParticles(0x374A, 10, 15, 5028, 0x48F, 0, EffectLayer.Head);
        m.PlaySound(0x1EA);
        m.SendMessage(0x22, "На тебя положили дурной глаз — руки не слушаются.");
    }
}

/// <summary>2. Гнилой туман — облако яда в точке.</summary>
public class RotFogSpell : ShamanSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new("Гнилой туман", "Пф-ф-ф, гниль-гниль!", 212);

    public RotFogSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Goblin;
    public override int Tier => 0;
    public override string Description => "Ядовитое облако 3×3: отравляет всех внутри.";

    public override void OnCast() => Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);

    public void Target(IPoint3D p)
    {
        if (!CheckPoint(p, out var point) || !CheckSequence())
        {
            return;
        }

        var map = Caster.Map;
        var poison = Power >= 1.15 ? Poison.Regular : Poison.Lesser;

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                ShamanFx.Burst(map, ShamanFx.Ground(map, point.X + dx, point.Y + dy), 0x3728, 0x48F, 5042, 25);
            }
        }

        Effects.PlaySound(point, map, 0x231);

        foreach (var m in HarmfulAround(point, 1))
        {
            Harm(m);
            m.ApplyPoison(Caster, poison);
        }
    }
}

/// <summary>3. Морок — ложный двойник, на которого переключаются враги.</summary>
public class PhantomSpell : ShamanSpell
{
    private static readonly SpellInfo _info = new("Морок", "Два гоблина, а где же я?", 212);

    public PhantomSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Goblin;
    public override int Tier => 1;
    public override string Description => "Двойник рядом с тобой; звери и чудища вокруг переключаются на него.";

    public override void OnCast()
    {
        if (CheckSequence())
        {
            var clone = new Clone(Caster);
            clone.MoveToWorld(Caster.Location, Caster.Map);
            clone.FixedParticles(0x376A, 1, 14, 5045, 0x48F, 0, EffectLayer.Waist);
            Caster.PlaySound(0x511);

            foreach (var m in Caster.Map.GetMobilesInRange(Caster.Location, 10))
            {
                if (m is BaseCreature { Controlled: false } bc && bc.Combatant == Caster)
                {
                    bc.Combatant = clone;
                }
            }
        }

        FinishSequence();
    }
}

/// <summary>4. Порча — снаряжение цели разом изнашивается.</summary>
public class HexSpell : ShamanSpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new("Порча", "Ржа на сталь, гниль на кожу!", 212);

    public HexSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Goblin;
    public override int Tier => 1;
    public override string Description => "Всё надетое на цель теряет прочность.";

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

        Harm(m);

        var wear = Math.Max(2, ScaleInt(Utility.RandomMinMax(5, 9)));
        var count = 0;

        foreach (var item in new List<Item>(m.Items))
        {
            if (item is IDurability durable && durable.MaxHitPoints > 0 && item.LootType != LootType.Blessed)
            {
                durable.HitPoints = Math.Max(0, durable.HitPoints - wear);
                count++;
            }
        }

        m.FixedParticles(0x374A, 10, 15, 5021, 0x48F, 0, EffectLayer.Waist);
        m.PlaySound(0x1E1);

        if (count > 0)
        {
            m.SendMessage(0x22, "Снаряжение покрывается ржой и гнилью.");
        }
    }
}

/// <summary>5. Ворожба — видение: скрытые рядом, сильнейший враг вокруг, близость набега.</summary>
public class DivinationSpell : ShamanSpell
{
    private static readonly SpellInfo _info = new("Ворожба", "Кости-кости, скажите правду!", 230);

    public DivinationSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Goblin;
    public override int Tier => 2;
    public override string Description => "Вскрывает скрытых в 12 шагах, называет сильнейшего зверя рядом и время до набега.";

    public override void OnCast()
    {
        if (CheckSequence())
        {
            var range = (int)Math.Round(12 * Power);
            var revealed = 0;
            BaseCreature strongest = null;
            var creatures = 0;

            foreach (var m in Caster.Map.GetMobilesInRange(Caster.Location, range))
            {
                if (m == Caster)
                {
                    continue;
                }

                if (m.Hidden && m.AccessLevel == AccessLevel.Player && m.Alive)
                {
                    m.RevealingAction();
                    m.FixedParticles(0x375A, 9, 20, 5049, EffectLayer.Head);
                    revealed++;
                }

                if (m is BaseCreature { Controlled: false, Summoned: false } bc && bc.Alive)
                {
                    creatures++;

                    if (strongest == null || bc.HitsMax > strongest.HitsMax)
                    {
                        strongest = bc;
                    }
                }
            }

            Caster.FixedParticles(0x375A, 9, 20, 5049, 0x48F, 0, EffectLayer.Head);
            Caster.PlaySound(0x1FD);

            Caster.SendMessage(0x48F, revealed > 0 ? $"Кости выдали скрытых: {revealed}." : "Кости молчат: никто не прячется.");

            if (strongest != null)
            {
                Caster.SendMessage(
                    0x48F,
                    $"Тварей вокруг: {creatures}. Сильнейшая — {strongest.Name}, {Caster.GetDirectionTo(strongest).ToString().ToLowerInvariant()}."
                );
            }

            var raid = RaidEventSystem.GetTimeUntilNextRaid();

            if (raid > TimeSpan.Zero)
            {
                Caster.SendMessage(0x48F, $"До набега на города: около {(int)Math.Ceiling(raid.TotalMinutes)} мин.");
            }
        }

        FinishSequence();
    }
}

/// <summary>6. Плясовая одержимость — все в области пускаются в пляс и не могут двинуться.</summary>
public class DanceManiaSpell : ShamanSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new("Плясовая одержимость", "Пляши, пляши, покуда дух вон!", 212);

    public DanceManiaSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Goblin;
    public override int Tier => 2;
    public override string Description => "Все в радиусе 3 от точки пляшут 4 секунды и не могут ни идти, ни бить.";

    public override void OnCast() => Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);

    public void Target(IPoint3D p)
    {
        if (!CheckPoint(p, out var point) || !CheckSequence())
        {
            return;
        }

        var duration = ScaleTime(4);

        Effects.PlaySound(point, Caster.Map, 0x38);

        foreach (var m in HarmfulAround(point, 3, true))
        {
            Harm(m);

            // Сопротивление магии укорачивает пляску, но не снимает её.
            var resisted = m.Skills.MagicResist.Value / 250.0;
            m.Paralyze(TimeSpan.FromSeconds(duration.TotalSeconds * (1.0 - resisted)));

            m.Animate(32, 5, 1, true, false, 0);
            m.FixedParticles(0x376A, 9, 32, 5005, 0x48F, 0, EffectLayer.Waist);
            m.PublicOverheadMessage(MessageType.Emote, 0x48F, false, "*пускается в пляс*");
        }
    }
}

/// <summary>7. Наваждение — твари в области набрасываются друг на друга, люди теряют рассудок.</summary>
public class DelusionSpell : ShamanSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new("Наваждение", "Брат на брата, клык на клык!", 212);

    private const int Radius = 6;

    public DelusionSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Goblin;
    public override int Tier => 3;
    public override int ComponentCost => 1;
    public override TimeSpan GlobalCooldown => TimeSpan.FromMinutes(30);
    public override string Description => "Ритуал 30 с. 15 секунд в радиусе 6 все бьют всех; людей бросает в оцепенение.";

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
        ShamanFx.Burst(map, point, 0x3728, 0x48F, 5042, 40);
        Effects.PlaySound(point, map, 0x1E1);

        foreach (var m in HarmfulAround(point, Radius))
        {
            if (m.Player)
            {
                Harm(m);
                m.Paralyze(TimeSpan.FromSeconds(2));
                ShamanEffects.AddStats(m, "Наваждение", 0, 0, -(int)Math.Max(1, m.RawInt * 0.2), ScaleTime(30));
                m.SendMessage(0x22, "Голову застилает наваждение: свои кажутся чужими.");
            }
        }

        // Твари переключаются заново каждые три секунды — иначе через миг вернулись бы
        // к шаману.
        Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(3), 5, () => Turn(map, point));
    }

    private void Turn(Map map, Point3D point)
    {
        var creatures = new List<BaseCreature>();

        foreach (var m in map.GetMobilesInRange(point, Radius))
        {
            if (m is BaseCreature bc && bc.Alive && !bc.IsInvulnerable && !bc.Blessed && CanHarm(bc))
            {
                creatures.Add(bc);
            }
        }

        if (creatures.Count < 2)
        {
            return;
        }

        foreach (var bc in creatures)
        {
            BaseCreature other;

            do
            {
                other = creatures[Utility.Random(creatures.Count)];
            } while (other == bc);

            bc.Combatant = other;
            bc.Warmode = true;
            bc.FixedParticles(0x374A, 10, 10, 5028, 0x48F, 0, EffectLayer.Head);
        }
    }
}

/// <summary>
///     8. Мор — болезнь, которая сама перескакивает с цели на цель. Шаман задаёт только
///     начало; дальше она идёт по толпе, пока не выгорит. Своих (группу, гильдию) не трогает.
/// </summary>
public class PestilenceSpell : ShamanSpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new("Мор", "Гниль в кровь, гниль в кость, гниль в род!", 212);

    private const int MaxInfected = 60;
    private const int Ticks = 30; // по две секунды — минута болезни на каждого

    private readonly Dictionary<Mobile, int> _infected = new();
    private int _total;

    public PestilenceSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Goblin;
    public override int Tier => 3;
    public override int ComponentCost => 2;
    public override TimeSpan GlobalCooldown => TimeSpan.FromHours(2);
    public override string Description => "Ритуал 30 с. Болезнь бьёт ядом минуту и перекидывается на всех рядом с больным.";

    public override void OnCast() => Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);

    public void Target(Mobile m)
    {
        if (!CanHarm(m))
        {
            Caster.SendLocalizedMessage(1001018);
            return;
        }

        if (!ShamanUltimates.IsAllowedPlace(Caster.Map, m.Location, out var why))
        {
            Caster.SendMessage(0x22, why);
            return;
        }

        if (!CheckSequence())
        {
            return;
        }

        Infect(m);

        var damage = Math.Max(3, ScaleInt(7));
        Timer.DelayCall(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), Ticks + 30, () => Tick(damage));
    }

    private void Infect(Mobile m)
    {
        if (_infected.ContainsKey(m) || _total >= MaxInfected)
        {
            return;
        }

        _infected[m] = Ticks;
        _total++;

        m.FixedParticles(0x374A, 10, 15, 5021, 0x48F, 0, EffectLayer.Waist);
        m.PlaySound(0x205);
        m.SendMessage(0x22, "Тебя бьёт озноб: мор добрался и до тебя.");
    }

    private void Tick(int damage)
    {
        foreach (var m in new List<Mobile>(_infected.Keys))
        {
            var left = _infected[m];

            if (left <= 0)
            {
                continue;
            }

            _infected[m] = left - 1;

            if (!m.Alive || m.Deleted || m.Map != Caster.Map)
            {
                _infected[m] = 0;
                continue;
            }

            if (!CanHarm(m))
            {
                continue;
            }

            Damage(m, damage, 0, 0, 0, 100, 0);
            m.FixedParticles(0x374A, 1, 10, 5021, 0x48F, 0, EffectLayer.Waist);

            foreach (var near in m.Map.GetMobilesInRange(m.Location, 3))
            {
                if (near != m && !_infected.ContainsKey(near) && CanHarm(near) && Utility.RandomDouble() < 0.4)
                {
                    Harm(near);
                    Infect(near);
                }
            }
        }
    }
}
