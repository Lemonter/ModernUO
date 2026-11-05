using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.BuffIcons;
using Server.Engines.CannedEvil;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     The Eodon world boss, ported from ServUO
///     (Scripts/Services/Expansions/Time Of Legends/Mobiles/DragonTurtle.cs). Its signature attack is
///     the bubble trail: eight coloured bubbles crawl out from the turtle, and any player (or
///     player-owned pet) they touch takes a matching resistance debuff, then a delayed elemental hit.
///
///     Dropped: <c>GetArtifact()</c> returning <c>DragonTurtleEgg</c> — that item is not ported and
///     <see cref="BaseChampion.GetArtifact" /> is not virtual here, so the champion rolls from its
///     (empty) unique/shared/decorative lists instead. Also dropped: <c>TeleportsTo</c> /
///     <c>TeleportDuration</c> / <c>TeleportRange</c>, the teleport-chase knobs, which do not exist in
///     this codebase. <c>SetWeaponAbility(Dismount)</c> becomes <see cref="GetWeaponAbility" /> and
///     <c>SetSpecialAbility(DragonBreath)</c> becomes <see cref="MonsterAbilities.FireBreath" />.
///
///     Two ServUO defects are not reproduced. Its bubble timers closed over the loop variable, so
///     every scheduled step ran with the final index; here the trail is one repeating timer walking a
///     step counter. And hue 117 (energy) fell through its debuff mapping to Physical while 126 — a hue
///     the trail never rolls — was mapped to Energy; both now map to Energy.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DragonTurtle : BaseChampion
{
    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.FireBreath];

    private static readonly Direction[] _directions =
    [
        Direction.North, Direction.Right, Direction.East, Direction.Down,
        Direction.South, Direction.Left, Direction.West, Direction.Up
    ];

    private static readonly int[] _hues = [0, 33, 44, 9, 63, 53, 117];

    private readonly List<Tuple<Point3D, int>> _bubbleLocs = new();
    private readonly Dictionary<Mobile, int> _affected = new();

    private long _nextBubbleWander;
    private long _nextBubbleAttack;
    private int _bubbleStep;
    private int _bubblePathLength;
    private bool _doingBubbles;
    private TimerExecutionToken _bubbleToken;
    private TimerExecutionToken _bubbleClearToken;

    [Constructible]
    public DragonTurtle() : base(AIType.AI_Mage)
    {
        Name = "драконья черепаха";
        Body = 1288;
        BaseSoundID = 362;

        SetStr(750, 800);
        SetDex(185, 240);
        SetInt(487, 562);

        SetDamage(25, 37);

        SetHits(60000);

        SetResistance(ResistanceType.Physical, 75, 85);
        SetResistance(ResistanceType.Fire, 65, 75);
        SetResistance(ResistanceType.Cold, 70, 75);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 65, 75);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Poison, 50);

        SetSkill(SkillName.MagicResist, 90, 120);
        SetSkill(SkillName.Tactics, 200, 110);
        SetSkill(SkillName.Wrestling, 225, 227);

        Fame = 11000;
        Karma = -11000;

        _nextBubbleWander = Core.TickCount + Utility.RandomMinMax(25000, 35000);
        _nextBubbleAttack = Core.TickCount + Utility.RandomMinMax(40000, 60000);
    }

    public override string CorpseName => "труп драконьей черепахи";

    public override ChampionSkullType SkullType => ChampionSkullType.None;

    public override Type[] UniqueList => [];
    public override Type[] SharedList => [];
    public override Type[] DecorativeList => [];
    public override MonsterStatuetteType[] StatueTypes => [];

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.Dismount;
    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override int Meat => 1;
    public override int Hides => 33;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies;
    public override bool ReacquireOnMovement => true;
    public override int TreasureMapLevel => 5;

    public override void GenerateLoot() => AddLoot(LootPack.SuperBoss, 3);

    public override void OnCarve(Mobile from, Corpse corpse, Item with)
    {
        if (corpse != null && !corpse.Carved)
        {
            from.SendLocalizedMessage(1156198); // You cut away some scutes, but they remain on the corpse.
            corpse.DropItem(new DragonTurtleScute(18));
        }

        base.OnCarve(from, corpse, with);
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Combatant == null)
        {
            return;
        }

        if (Core.TickCount - _nextBubbleWander >= 0)
        {
            DoBubble();
            _nextBubbleWander = Core.TickCount + Utility.RandomMinMax(25000, 35000);
        }

        if (Core.TickCount - _nextBubbleAttack >= 0)
        {
            DoBubbleAttack();
            _nextBubbleAttack = Core.TickCount + Utility.RandomMinMax(40000, 60000);
        }
    }

    private bool CanBubble() => Alive && Map != null && Map != Map.Internal;

    public void DoBubble()
    {
        if (!CanBubble() || _doingBubbles)
        {
            return;
        }

        _bubblePathLength = Utility.RandomMinMax(5, 11);
        _bubbleStep = 0;
        _doingBubbles = true;

        _bubbleLocs.Clear();

        for (var i = 0; i < 8; i++)
        {
            _bubbleLocs.Add(new Tuple<Point3D, int>(Location, _hues[Utility.Random(_hues.Length)]));
        }

        Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnBubbleStep, out _bubbleToken);
    }

    private void OnBubbleStep()
    {
        if (!CanBubble())
        {
            StopBubbles();
            return;
        }

        var map = Map;

        for (var j = 0; j < _bubbleLocs.Count; j++)
        {
            var loc = _bubbleLocs[j];

            if (_bubbleStep > 0 && loc.Item1 == Point3D.Zero)
            {
                continue;
            }

            var hue = loc.Item2;
            var d = _directions[j];
            var x = loc.Item1.X;
            var y = loc.Item1.Y;

            if (_bubbleStep > 2 && 0.4 > Utility.RandomDouble())
            {
                d = d == Direction.Up ? Direction.North : d + 1;
            }

            Movement.Movement.Offset(d, ref x, ref y);

            IPoint3D p = new Point3D(x, y, map.GetAverageZ(x, y));
            Spells.SpellHelper.GetSurfaceTop(ref p);

            var newLoc = new Point3D(p);
            var hasMobile = false;

            foreach (var m in map.GetMobilesInRange<PlayerMobile>(newLoc, 0))
            {
                TouchBubble(m, hue, out hasMobile);
            }

            foreach (var m in map.GetMobilesInRange<BaseCreature>(newLoc, 0))
            {
                if (m.GetMaster() is PlayerMobile)
                {
                    TouchBubble(m, hue, out hasMobile);
                }
            }

            if (!hasMobile)
            {
                Effects.SendLocationEffect(newLoc, map, 13920, 20, 10, hue == 0 ? 0 : hue - 1, 5);
                _bubbleLocs[j] = new Tuple<Point3D, int>(newLoc, hue);
            }
        }

        _bubbleStep++;

        if (_bubbleStep >= _bubblePathLength)
        {
            StopBubbles();
            Timer.StartTimer(TimeSpan.FromSeconds(1), ClearBubbles, out _bubbleClearToken);
        }
    }

    private void TouchBubble(Mobile m, int hue, out bool hasMobile)
    {
        hasMobile = false;

        if (m == this || !CanBeHarmful(m))
        {
            return;
        }

        hasMobile = true;

        Effects.SendTargetEffect(m, 13920, 10, 60, hue == 0 ? 0 : hue - 1, 5);

        for (var i = 0; i < _bubbleLocs.Count; i++)
        {
            if (_bubbleLocs[i].Item1 == m.Location)
            {
                _bubbleLocs[i] = new Tuple<Point3D, int>(Point3D.Zero, _bubbleLocs[i].Item2);
            }
        }

        ApplyMod(m, hue);
    }

    private void StopBubbles()
    {
        _bubbleToken.Cancel();
        _doingBubbles = false;
    }

    private void ClearBubbles()
    {
        _doingBubbles = false;
        _bubbleLocs.Clear();
    }

    public void DoBubbleAttack()
    {
        if (!CanBubble())
        {
            return;
        }

        var toget = new List<Mobile>();

        foreach (var m in Map.GetMobilesInRange<PlayerMobile>(Location, 11))
        {
            if (CanBeHarmful(m) && InLOS(m))
            {
                toget.Add(m);
            }
        }

        foreach (var m in Map.GetMobilesInRange<BaseCreature>(Location, 11))
        {
            if (m.GetMaster() is PlayerMobile && CanBeHarmful(m) && InLOS(m))
            {
                toget.Add(m);
            }
        }

        foreach (var mob in toget)
        {
            var hue = _affected.TryGetValue(mob, out var known) ? known : _hues[Utility.Random(_hues.Length)];

            MovingParticles(mob, 13920, 10, 0, false, true, hue == 0 ? 0 : hue - 1, 5, 9502, 14120, 0, 0);

            Timer.StartTimer(TimeSpan.FromSeconds(0.7), () => BubbleHit(mob, hue));
        }
    }

    private void BubbleHit(Mobile mob, int hue)
    {
        if (!Alive || mob == null || mob.Deleted)
        {
            return;
        }

        var type = GetResistanceFromHue(hue);
        var damage = Utility.RandomMinMax(60, 80);

        AOS.Damage(
            mob,
            this,
            damage,
            type == ResistanceType.Physical ? 100 : 0,
            type == ResistanceType.Fire ? 100 : 0,
            type == ResistanceType.Cold ? 100 : 0,
            type == ResistanceType.Poison ? 100 : 0,
            type == ResistanceType.Energy ? 100 : 0
        );
    }

    private void ApplyMod(Mobile m, int hue)
    {
        var type = GetResistanceFromHue(hue);

        _affected[m] = hue;

        var mod = new ResistanceMod(type, "DragonTurtleBubble", -25);
        m.AddResistanceMod(mod);

        (m as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.DragonTurtleDebuff, 1156192, 1156192));

        Timer.StartTimer(TimeSpan.FromSeconds(30), () => RemoveMod(m, mod));
    }

    private void RemoveMod(Mobile m, ResistanceMod mod)
    {
        if (m == null || m.Deleted)
        {
            return;
        }

        m.RemoveResistanceMod(mod);

        (m as PlayerMobile)?.RemoveBuff(BuffIcon.DragonTurtleDebuff);

        _affected.Remove(m);
    }

    private static ResistanceType GetResistanceFromHue(int hue) =>
        hue switch
        {
            0        => ResistanceType.Physical,
            33 or 44 => ResistanceType.Fire,
            9        => ResistanceType.Cold,
            63       => ResistanceType.Poison,
            53 or 117 or 126 => ResistanceType.Energy,
            _        => ResistanceType.Physical
        };

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _bubbleToken.Cancel();
        _bubbleClearToken.Cancel();
        _bubbleLocs.Clear();
        _affected.Clear();
    }
}
