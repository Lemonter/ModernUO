using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/DiabolicalSeaweed.cs).
/// It cannot move, so it drags: once a second everything it can harm within nine tiles and more
/// than one tile away is yanked to its feet and bitten. Gravewater Lake spawns it paired with
/// the Paralithode, thirty spawners' worth.
///
/// Upstream bug fixed: the original skips every mobile whose AccessLevel is Player, which is
/// every player — so the branch below it that tests `m.Player` can never be reached and the
/// creature's whole point does not work on players, only on pets. The test is inverted here to
/// skip staff instead, which is plainly what was meant.
///
/// The original's drain list is a static shared by every seaweed in the world and is cleared at
/// the end of each tick; here it is a pooled list local to the tick, which is the same thing
/// without the shared state.</summary>
[SerializationGenerator(0, false)]
public partial class DiabolicalSeaweed : BaseCreature
{
    private TimerExecutionToken _pullTimer;

    [Constructible]
    public DiabolicalSeaweed() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 129;
        Hue = 1914;

        SetStr(452, 485);
        SetDex(401, 420);
        SetInt(126, 140);

        SetHits(501, 532);

        SetDamage(10, 23);

        SetDamageType(ResistanceType.Physical, 60);
        SetDamageType(ResistanceType.Poison, 40);

        SetResistance(ResistanceType.Physical, 35, 40);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 10, 20);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.MagicResist, 50.1, 54.7);
        SetSkill(SkillName.Tactics, 100.3, 114.8);
        SetSkill(SkillName.Wrestling, 45.1, 59.5);

        Fame = 3000;
        Karma = -3000;

        CantWalk = true;
        VirtualArmor = 60;

        PackItem(
            Utility.Random(8) switch
            {
                0 => new BlueDiamond(),
                1 => new FireRuby(),
                2 => new BrilliantAmber(),
                3 => new PerfectEmerald(),
                4 => new DarkSapphire(),
                5 => new Turquoise(),
                6 => new EcruCitrine(),
                _ => (Item)new WhitePearl()
            }
        );

        PackItem(new ParasiticPlant());
        PackItem(new LuminescentFungi());

        Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), Pull, out _pullTimer);
    }

    public override string CorpseName => "труп водоросли";
    public override string DefaultName => "дьявольская водоросль";

    public override bool CanRummageCorpses => true;

    public override void GenerateLoot() => AddLoot(LootPack.Meager);

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        for (var i = Utility.RandomMinMax(5, 6); i > 1; i--)
        {
            var reagent = Loot.RandomReagent();

            if (reagent != null)
            {
                reagent.Amount = Utility.RandomMinMax(4, 5);
                c.DropItem(reagent);
            }
        }
    }

    public override void OnAfterDelete()
    {
        _pullTimer.Cancel();
        base.OnAfterDelete();
    }

    [AfterDeserialization]
    private void AfterDeserialization() =>
        Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), Pull, out _pullTimer);

    private void Pull()
    {
        if (Deleted || Map == null || Map == Map.Internal)
        {
            return;
        }

        using var toDrain = PooledRefList<Mobile>.Create();

        foreach (var m in Map.GetMobilesInRange(Location, 9))
        {
            if (m is DiabolicalSeaweed || !CanBeHarmful(m) || m.AccessLevel > AccessLevel.Player)
            {
                continue;
            }

            if (Math.Abs(m.X - X) < 2 && Math.Abs(m.Y - Y) < 2)
            {
                continue;
            }

            if (m is BaseCreature { Controlled: true } or BaseCreature { Summoned: true } || m.Player)
            {
                toDrain.Add(m);
            }
        }

        for (var i = 0; i < toDrain.Count; i++)
        {
            var m = toDrain[i];

            DoHarmful(m);

            var loc = new Point3D(X + Utility.RandomMinMax(-1, 1), Y + Utility.RandomMinMax(-1, 1), Z);
            m.MoveToWorld(loc, Map);
            m.Damage(Utility.RandomMinMax(1, 10), this);
        }
    }
}
