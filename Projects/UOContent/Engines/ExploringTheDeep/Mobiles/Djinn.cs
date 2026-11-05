using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.ExploringTheDeep;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/Djinn.cs). The Ilshenar djinn Zalia
///     points the player at; it summons efreets while it has mana, drops the Aqua Gem, and removes itself
///     after an hour.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Djinn : BaseCreature
{
    private static readonly List<Djinn> _instances = new();

    private TimerExecutionToken _selfDelete;
    private TimerExecutionToken _summonTimer;

    [Constructible]
    public Djinn() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 0x311;
        Hue = 33072;

        SetStr(320, 500);
        SetDex(200, 300);
        SetInt(600, 700);

        SetHits(2003);

        SetDamage(11, 13);

        SetDamageType(ResistanceType.Physical, 0);
        SetDamageType(ResistanceType.Fire, 50);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 60, 70);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 80, 90);

        SetSkill(SkillName.Wrestling, 60.0, 80.0);
        SetSkill(SkillName.Tactics, 60.0, 80.0);
        SetSkill(SkillName.MagicResist, 60.0, 80.0);
        SetSkill(SkillName.Magery, 100.0, 120.0);
        SetSkill(SkillName.EvalInt, 60.0, 110.0);
        SetSkill(SkillName.DetectHidden, 55.0);

        Fame = 15000;
        Karma = -15000;

        _instances.Add(this);

        Timer.StartTimer(TimeSpan.FromMinutes(60.0), Delete, out _selfDelete);
        Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), SummonEfreet, out _summonTimer);
    }

    public static IReadOnlyList<Djinn> Instances => _instances;

    public override string CorpseName => "труп джинна";
    public override string DefaultName => "джинн";

    public override int TreasureMapLevel => 4;

    public static Djinn Spawn(Point3D platLoc, Map platMap)
    {
        if (_instances.Count > 0)
        {
            return null;
        }

        var creature = new Djinn
        {
            Home = platLoc,
            RangeHome = 4
        };

        creature.MoveToWorld(platLoc, platMap);

        return creature;
    }

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich);

    public override void OnDeath(Container c)
    {
        ExploringDeepDrops.Give(this, ExploringDeepChain.ComponentQuest, static () => new AquaGem());

        base.OnDeath(c);
    }

    private void SummonEfreet()
    {
        if (Deleted || Map == null || Map == Map.Internal)
        {
            return;
        }

        foreach (var m in Map.GetMobilesInRange(Location, 10))
        {
            if (m is not PlayerMobile pm || !CanBeHarmful(pm) || Mana < 100)
            {
                continue;
            }

            Mana -= 50;

            var efreet = new Efreet
            {
                Combatant = pm
            };

            efreet.MoveToWorld(
                new Point3D(X + Utility.RandomMinMax(-5, 5), Y + Utility.RandomMinMax(-5, 5), Z), Map
            );
        }
    }

    public override void OnAfterDelete()
    {
        _selfDelete.Cancel();
        _summonTimer.Cancel();
        _instances.Remove(this);

        base.OnAfterDelete();
    }
}
