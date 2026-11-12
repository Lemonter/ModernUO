using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.Systems.MahaonShamanism;
using Server.Systems.MahaonWeather;
using Server.Targeting;

namespace Server.Spells.Shamanism;

// Древнее шаманство: предки, небо, духи, ветер. Условие раздела — ритуальный круг; ночью
// раздел сильнее на пятую часть (ShamanSpell.ComputePower).

/// <summary>1. Шёпот предков — духи выдают скрытых рядом. Посмертный.</summary>
public class AncestorWhisperSpell : ShamanSpell
{
    private static readonly SpellInfo _info = new("Шёпот предков", "Деды, шепните…", 230);

    public AncestorWhisperSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ancient;
    public override int Tier => 0;
    public override bool Posthumous => true;
    public override string Description => "Вскрывает всех скрытых в 8 шагах от тебя.";

    public override void OnCast()
    {
        if (CheckSequence())
        {
            var range = (int)Math.Round(8 * Power);
            var found = 0;

            foreach (var m in Caster.Map.GetMobilesInRange(Caster.Location, range))
            {
                if (m != Caster && m.Hidden && m.Alive && m.AccessLevel == AccessLevel.Player)
                {
                    m.RevealingAction();
                    m.FixedParticles(0x375A, 9, 20, 5049, 0x4F2, 0, EffectLayer.Head);
                    found++;
                }
            }

            Effects.PlaySound(Caster.Location, Caster.Map, 0x1FD);
            Caster.SendMessage(0x4F2, found > 0 ? $"Предки шепчут: рядом прятались {found}." : "Предки молчат: рядом никого.");
        }

        FinishSequence();
    }
}

/// <summary>2. Порыв ветра — отбрасывает цель; по ветру — дальше и больнее.</summary>
public class WindGustSpell : ShamanSpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new("Порыв ветра", "Ветер, гони!", 212);

    public WindGustSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ancient;
    public override int Tier => 0;
    public override string Description => "Отбрасывает цель на 3 шага (по ветру — на 5) и бьёт холодом.";

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

        var dx = m.X - Caster.X;
        var dy = m.Y - Caster.Y;

        // По ветру: направление ветра на шарде — 0 восток, 90 север (то есть −Y).
        var angle = Math.Atan2(-dy, dx) * 180.0 / Math.PI;
        var diff = Math.Abs(((angle - WindManager.Direction) % 360 + 540) % 360 - 180);
        var downwind = WindManager.Strength > 0.3f && diff < 45;

        var distance = downwind ? 5 : 3;
        var damage = Scale(Utility.RandomMinMax(6, 10)) * (downwind ? 1.3 : 1.0);

        Damage(m, damage, 0, 0, 100, 0, 0);
        Push(m, dx, dy, distance);

        m.FixedParticles(0x3728, 10, 15, 5042, 0x4F2, 0, EffectLayer.Waist);
        Caster.PlaySound(0x5C6);

        if (downwind)
        {
            Caster.SendMessage(0x4F2, "Ветер подхватывает твой порыв.");
        }
    }

    private static void Push(Mobile m, int dx, int dy, int distance)
    {
        if (m is BaseCreature { CantWalk: true } || m.Map == null)
        {
            return;
        }

        var map = m.Map;
        var end = ShamanTerrain.Toward(m.Location, new Point3D(m.X + dx, m.Y + dy, m.Z), distance);
        var last = m.Location;

        foreach (var step in ShamanTerrain.Line(m.Location, end))
        {
            if (step.X == m.X && step.Y == m.Y)
            {
                continue;
            }

            var z = map.GetAverageZ(step.X, step.Y);

            if (Math.Abs(z - last.Z) > 10 || !map.CanSpawnMobile(step.X, step.Y, z))
            {
                break;
            }

            last = new Point3D(step.X, step.Y, z);
        }

        if (last != m.Location)
        {
            m.MoveToWorld(last, map);
        }
    }
}

/// <summary>3. Дух-хранитель — предок встаёт за спиной союзника и принимает часть ударов.</summary>
public class GuardianSpiritSpell : ShamanSpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new("Дух-хранитель", "Встань за спиной, предок.", 230);

    public GuardianSpiritSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ancient;
    public override int Tier => 1;
    public override string Description => "Себе или союзнику: +8 ко всем сопротивлениям на две минуты.";

    public override void OnCast() => Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Beneficial);

    public void Target(Mobile m)
    {
        if (!CheckBSequence(m))
        {
            return;
        }

        ShamanEffects.AddResists(
            m, "Дух-хранитель",
            new[] { ResistanceType.Physical, ResistanceType.Fire, ResistanceType.Cold, ResistanceType.Poison, ResistanceType.Energy },
            ScaleInt(8), ScaleTime(120)
        );

        m.FixedParticles(0x376A, 9, 32, 5030, 0x4F2, 0, EffectLayer.Waist);
        m.PlaySound(0x1F2);
        m.SendMessage(0x4F2, "За твоей спиной встаёт дух-хранитель.");
    }
}

/// <summary>
///     4. Изгнание — рвёт связь чужих душ с миром (Печать души) и жжёт нежить. Главный ответ
///     на посмертных шаманов: якорь и призрак в радиусе — печать рвётся.
/// </summary>
public class BanishmentSpell : ShamanSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new("Изгнание", "Прочь, в нижний мир!", 212);

    public BanishmentSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ancient;
    public override int Tier => 1;
    public override string Description => "Радиус 5: рвёт чужие печати души (и якоря, и призраков) и жжёт нежить.";

    public override void OnCast() => Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);

    public void Target(IPoint3D p)
    {
        if (!CheckPoint(p, out var point) || !CheckSequence())
        {
            return;
        }

        var map = Caster.Map;
        ShamanFx.Burst(map, point, 0x375A, 0x4F2, 5049, 30);
        Effects.PlaySound(point, map, 0x1FB);

        var torn = SoulSeal.Banish(map, point, 5, Caster);

        if (torn > 0)
        {
            Caster.SendMessage(0x4F2, torn == 1 ? "Ты обрываешь чужую печать души." : $"Ты обрываешь чужие печати души: {torn}.");
        }

        var silver = SlayerGroup.GetEntryByName(SlayerName.Silver);

        foreach (var m in HarmfulAround(point, 5))
        {
            if (silver?.Slays(m) == true)
            {
                Damage(m, Scale(Utility.RandomMinMax(30, 45)), 0, 0, 0, 0, 100);
                m.FixedParticles(0x3709, 10, 30, 5052, 0x4F2, 0, EffectLayer.LeftFoot);
            }
        }
    }
}

/// <summary>5. Печать души — якорь для посмертных обрядов (см. SoulSeal).</summary>
public class SoulSealSpell : ShamanSpell
{
    private static readonly SpellInfo _info = new("Печать души", "Держи меня, земля, и после смерти.", 230);

    public SoulSealSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ancient;
    public override int Tier => 2;
    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(8);
    public override string Description => "Ставит якорь души: после смерти колдуешь посмертное рядом с ним. Сутки или пока не разобьют.";

    public override bool CheckCast()
    {
        if (!base.CheckCast())
        {
            return false;
        }

        if (Caster.Region?.GetRegion<GuardedRegion>() != null || BaseHouse.FindHouseAt(Caster) != null)
        {
            Caster.SendMessage(0x22, "Якорь души не ставят в городах и домах — только в диких землях.");
            return false;
        }

        return true;
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            // Вид якоря — по сильнейшей специализации шамана.
            var look = ShamanTradition.Ancient;
            var best = -1.0;

            for (var t = ShamanTradition.Orc; t <= ShamanTradition.Ancient; t++)
            {
                var value = ShamanismSchoolSystem.GetValue(Caster, t);

                if (value > best)
                {
                    best = value;
                    look = t;
                }
            }

            var anchor = new SoulSealAnchor(Caster, look);
            // Якорь — на клетку перед шаманом, а не под ноги: на своём же якоре потом
            // неудобно и стоять, и колдовать.
            var front = Caster.Location;
            Movement.Movement.Offset(Caster.Direction & Direction.Mask, ref front);
            front = new Point3D(front.X, front.Y, Caster.Map.GetAverageZ(front.X, front.Y));

            if (Math.Abs(front.Z - Caster.Z) > 8 || !Caster.Map.CanFit(front, 16, false, false))
            {
                front = Caster.Location;
            }

            anchor.MoveToWorld(front, Caster.Map);

            ShamanFx.Burst(Caster.Map, Caster.Location, 0x3728, 0x482, 5042, 40);
            Caster.PlaySound(0x482);
            Caster.SendMessage(0x482, $"Душа запечатана. Нитей: {anchor.Threads}. Умрёшь — колдуй у якоря, не дальше {SoulSeal.AnchorRange} шагов.");
        }

        FinishSequence();
    }
}

/// <summary>6. Зов грозы — молнии бьют по врагам в области несколько секунд.</summary>
public class ThunderCallSpell : ShamanSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new("Зов грозы", "Небо, ударь!", 212);

    public ThunderCallSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ancient;
    public override int Tier => 2;
    public override string Description => "Пять молний за десять секунд по врагам в радиусе 8 от точки.";

    public override void OnCast() => Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);

    public void Target(IPoint3D p)
    {
        if (!CheckPoint(p, out var point) || !CheckSequence())
        {
            return;
        }

        var damage = Scale(Utility.RandomMinMax(20, 28));

        Timer.DelayCall(
            TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(2), 5,
            () =>
            {
                var targets = HarmfulAround(point, 8);

                if (targets.Count == 0)
                {
                    return;
                }

                var m = targets[Utility.Random(targets.Count)];
                m.BoltEffect(0);
                Damage(m, damage, 0, 0, 0, 0, 100);
            }
        );
    }
}

/// <summary>
///     7. Великая буря — гроза над половиной фасета на пять минут. Погода и ветер — общие для
///     всего мира (MahaonWeather), молнии — только над половиной карты, где стоит шаман, и
///     только на открытой местности: не в городах, не в домах, не в подземельях.
/// </summary>
public class GreatStormSpell : ShamanSpell
{
    private static readonly SpellInfo _info = new("Великая буря", "Небо, расколись над полмиром!", 212);

    private static readonly TimeSpan Duration = TimeSpan.FromMinutes(5);
    private static bool _raging;

    public GreatStormSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ancient;
    public override int Tier => 3;
    public override int ComponentCost => 2;
    public override TimeSpan GlobalCooldown => TimeSpan.FromHours(6);
    public override TimeSpan RitualDuration => TimeSpan.FromSeconds(60);
    public override string Description => "Ритуал 60 с. Гроза на полкарты на 5 минут: молнии бьют по открытой местности.";

    public override bool CheckCast()
    {
        if (!base.CheckCast())
        {
            return false;
        }

        if (_raging)
        {
            Caster.SendMessage(0x22, "Буря уже бушует.");
            return false;
        }

        return true;
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            Begin();
        }

        FinishSequence();
    }

    private void Begin()
    {
        _raging = true;

        var map = Caster.Map;
        var eastHalf = Caster.X >= map.Width / 2;
        var damage = Power;
        var oldWeather = WeatherSystem.Current;
        var oldDir = WindManager.Direction;
        var oldStrength = WindManager.Strength;
        var oldGust = WindManager.GustStrength;

        WeatherSystem.SetWeather(MahaonWeather.Storm);
        WindManager.TransitionTo(Utility.Random(360), WindManager.MaxStrength, WindManager.MaxGust, TimeSpan.FromSeconds(20));
        World.Broadcast(0x4F2, false, $"Небо темнеет над {(eastHalf ? "восточной" : "западной")} половиной мира. Великая буря.");

        var ticks = (int)(Duration.TotalSeconds / 2);

        Timer.DelayCall(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), ticks, () => Strike(map, eastHalf, damage));

        Timer.DelayCall(
            Duration + TimeSpan.FromSeconds(3),
            () =>
            {
                _raging = false;
                WeatherSystem.SetWeather(oldWeather);
                WindManager.TransitionTo(oldDir, oldStrength, oldGust, TimeSpan.FromSeconds(30));
                World.Broadcast(0x4F2, false, "Великая буря стихает.");
            }
        );
    }

    private void Strike(Map map, bool eastHalf, double power)
    {
        var candidates = new List<Mobile>();

        foreach (var ns in NetState.Instances)
        {
            var player = ns.Mobile;

            if (player?.Map != map || (player.X >= map.Width / 2) != eastHalf || !Outdoors(player))
            {
                continue;
            }

            foreach (var m in map.GetMobilesInRange(player.Location, 12))
            {
                // Персонал не мишень всемирного события: ГМ, оказавшийся в поле, не должен
                // ловить молнии чужого обряда.
                if (!candidates.Contains(m) && m.AccessLevel == AccessLevel.Player && Outdoors(m) && CanHarm(m))
                {
                    candidates.Add(m);
                }
            }
        }

        // Каждому, кто на открытом месте, — свой шанс; не больше восьми ударов за раз.
        var strikes = 0;

        foreach (var m in candidates)
        {
            if (strikes >= 8 || Utility.RandomDouble() > 0.12)
            {
                continue;
            }

            strikes++;
            m.BoltEffect(0);
            Damage(m, Utility.RandomMinMax(20, 40) * power, 0, 0, 0, 0, 100);
        }
    }

    private static bool Outdoors(Mobile m) =>
        m.Alive && m.Region?.GetRegion<GuardedRegion>() == null && m.Region?.GetRegion<DungeonRegion>() == null &&
        BaseHouse.FindHouseAt(m) == null;
}

/// <summary>
///     8. Тризна — поминальный обряд: павшие союзники рядом встают живыми, а мёртвые враги
///     поднимаются духами и бьются за шамана две минуты.
/// </summary>
public class FuneralFeastSpell : ShamanSpell
{
    private static readonly SpellInfo _info = new("Тризна", "Встаньте, павшие, — пир не окончен!", 230);

    private const int Radius = 8;
    private const int MaxSpirits = 5;

    private static readonly HashSet<Corpse> Used = new();

    public FuneralFeastSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override ShamanTradition Tradition => ShamanTradition.Ancient;
    public override int Tier => 3;
    public override int ComponentCost => 1;
    public override TimeSpan GlobalCooldown => TimeSpan.FromHours(1);
    public override string Description => "Ритуал 30 с. Воскрешает павших союзников в радиусе 8; до 5 трупов врагов встают духами.";

    public override void OnCast()
    {
        if (CheckSequence())
        {
            Feast();
        }

        FinishSequence();
    }

    private void Feast()
    {
        var map = Caster.Map;
        ShamanFx.Burst(map, Caster.Location, 0x376A, 0x4F2, 5030, 40);
        Effects.PlaySound(Caster.Location, map, 0x214);

        var revived = 0;

        var nearby = new List<Mobile>();

        foreach (var m in map.GetMobilesInRange(Caster.Location, Radius))
        {
            nearby.Add(m);
        }

        foreach (var m in nearby)
        {
            if (m != Caster && !m.Alive && m.Player && IsAlly(m))
            {
                m.Resurrect();
                m.Hits = m.HitsMax / 2;
                m.FixedParticles(0x376A, 10, 16, 5030, 0x4F2, 0, EffectLayer.Waist);
                m.SendMessage(0x4F2, "Тризна по тебе не состоялась — ты снова жив.");
                revived++;
            }
        }

        var risen = 0;

        var items = new List<Item>();

        foreach (var item in map.GetItemsInRange(Caster.Location, Radius))
        {
            items.Add(item);
        }

        foreach (var item in items)
        {
            if (risen >= MaxSpirits)
            {
                break;
            }

            if (item is not Corpse { Owner: BaseCreature owner } corpse || Used.Contains(corpse) || owner.IsInvulnerable)
            {
                continue;
            }

            Used.Add(corpse);

            var spirit = new FallenSpirit(owner, Power);

            if (BaseCreature.Summon(spirit, Caster, corpse.Location, 0x482, TimeSpan.FromMinutes(2)))
            {
                risen++;
            }
            else
            {
                spirit.Delete();
            }
        }

        Caster.SendMessage(0x4F2, $"Тризна: воскрешено {revived}, духов поднято {risen}.");
    }
}

/// <summary>Дух павшего врага, поднятый Тризной.</summary>
[SerializationGenerator(0, false)]
public partial class FallenSpirit : BaseCreature
{
    public FallenSpirit() : base(AIType.AI_Melee)
    {
    }

    public FallenSpirit(BaseCreature original, double power) : base(AIType.AI_Melee)
    {
        Body = original.Body;
        Hue = 0x4001;
        Name = $"дух: {original.Name}";

        var hits = Math.Clamp((int)(original.HitsMax * 0.6 * power), 30, 500);
        SetStr(Math.Clamp(original.RawStr, 50, 400));
        SetDex(Math.Clamp(original.RawDex, 50, 150));
        SetInt(30);
        SetHits(hits);
        SetDamage(Math.Clamp(hits / 20, 5, 25), Math.Clamp(hits / 12, 8, 35));
        SetDamageType(ResistanceType.Cold, 100);

        SetResistance(ResistanceType.Physical, 50);
        SetResistance(ResistanceType.Cold, 70);
        SetResistance(ResistanceType.Poison, 100);

        SetSkill(SkillName.Tactics, 90.0);
        SetSkill(SkillName.Wrestling, 90.0);
        SetSkill(SkillName.MagicResist, 70.0);

        ControlSlots = 0;
    }

    public override bool DeleteCorpseOnDeath => true;
    public override bool BleedImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override double DispelDifficulty => 100.0;
    public override double DispelFocus => 40.0;
}
