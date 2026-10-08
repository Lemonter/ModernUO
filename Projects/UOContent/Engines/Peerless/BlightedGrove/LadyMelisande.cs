using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>The Blighted Grove peerless. Ported from ServUO
/// (Scripts/Mobiles/Bosses/LadyMelisande.cs).
///
/// She was built on AI_Mage at first, for want of AIType.AI_NecroMage; that AI has since been
/// ported (Mobiles/AI/NecroMageAI.cs) and she is back on it.
///
/// Her parrot drop is absent: LootPack.Parrot is commented out upstream in this codebase
/// ("TODO: Uncomment once added Legacy") because the ParrotItem class was never ported. Add
/// the AddLoot(LootPack.Parrot) line when it lands.</summary>
[SerializationGenerator(0, false)]
public partial class LadyMelisande : BasePeerless
{
    private long _nextDrain;
    private long _nextTaunt;

    [Constructible]
    public LadyMelisande() : base(AIType.AI_NecroMage, FightMode.Closest, 10, 1)
    {
        ActiveSpeed = 0.2;
        PassiveSpeed = 0.4;

        Name = "Lady Melisande";
        Body = 0x102;
        BaseSoundID = 451;

        SetStr(400, 1000);
        SetDex(300, 400);
        SetInt(1500, 1700);

        SetHits(100000);

        SetDamage(11, 18);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 40, 60);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 55, 65);
        SetResistance(ResistanceType.Poison, 70, 75);
        SetResistance(ResistanceType.Energy, 70, 80);

        SetSkill(SkillName.Wrestling, 100.0, 120.0);
        SetSkill(SkillName.Tactics, 100.0, 120.0);
        SetSkill(SkillName.MagicResist, 100.0, 120.0);
        SetSkill(SkillName.Magery, 100.0, 120.0);
        SetSkill(SkillName.EvalInt, 100.0, 120.0);
        SetSkill(SkillName.Meditation, 100.0, 120.0);
        SetSkill(SkillName.Necromancy, 100.0, 120.0);
        SetSkill(SkillName.SpiritSpeak, 100.0, 120.0);

        Fame = 25000;
        Karma = -25000;

        _nextDrain = Core.TickCount;
        _nextTaunt = Core.TickCount;
    }

    public override string CorpseName => "a lady melisande corpse";

    public override Poison PoisonImmune => Poison.Lethal;
    public override int TreasureMapLevel => 5;
    public override bool GivesMLMinorArtifact => true;

    public override bool CanSpawnHelpers => true;
    public override int MaxHelpersWaves => 1;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss, 8);
        AddLoot(LootPack.ArcanistScrolls, Utility.RandomMinMax(1, 6));
        AddLoot(LootPack.PeerlessResource, 8);
        AddLoot(LootPack.Talisman, 5);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        c.DropItem(new DiseasedBark());
        c.DropItem(new EternallyCorruptTree());
        // The wine is non-stackable here, so the original's 4-8 is that many bottles.
        var bottles = Utility.RandomMinMax(4, 8);

        for (var i = 0; i < bottles; i++)
        {
            c.DropItem(new MelisandesFermentedWine());
        }

        // One of the three minor artifacts, or none.
        switch (Utility.Random(400))
        {
            case 0:
                {
                    c.DropItem(new MelisandesHairDye());
                    break;
                }
            case 1:
                {
                    c.DropItem(new MelisandesCorrodedHatchet());
                    break;
                }
        }

        // The grove takes her death badly.
        SpawnOnDeath(new Reaper(), 6474, 932, 30);
        SpawnOnDeath(new InsaneDryad(), 6484, 947, 23);
        SpawnOnDeath(new StoneHarpy(), 6494, 959, 20);
    }

    private void SpawnOnDeath(BaseCreature creature, int x, int y, int z)
    {
        var map = Map;

        if (map == null)
        {
            creature.Delete();
            return;
        }

        creature.MoveToWorld(new Point3D(x, y, z), map);
    }

    public override void SpawnHelpers()
    {
        for (var i = 0; i < 4; i++)
        {
            SpawnHelper(new EnslavedSatyr(), 6);
        }
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Combatant == null || !Alive)
        {
            return;
        }

        if (_nextDrain <= Core.TickCount && Hits < HitsMax * 0.1)
        {
            LifeDrain();
        }

        if (_nextTaunt <= Core.TickCount && Hits > HitsMax * 0.5)
        {
            Taunt();
        }
    }

    /// <summary>Below a tenth of her health she starts taking it back out of whoever is in
    /// front of her.</summary>
    private void LifeDrain()
    {
        if (Combatant is not Mobile target || !CanBeHarmful(target, false))
        {
            return;
        }

        DoHarmful(target);

        var drained = Utility.RandomMinMax(10, 40);

        target.FixedParticles(0x374A, 10, 15, 5013, 0x496, 0, EffectLayer.Waist);
        target.PlaySound(0x231);
        target.SendLocalizedMessage(1070848); // You feel your life force being stolen away.

        AOS.Damage(target, this, drained, 0, 0, 0, 0, 100);
        Hits += drained;

        _nextDrain = Core.TickCount + Utility.RandomMinMax(15000, 60000);
    }

    private static readonly int[] _taunts =
    {
        1074818, // You waste your time, fool.
        1074819, // You are no match for me.
        1074820, // You are already dead.
        1074821  // You will never defeat me!
    };

    private void Taunt()
    {
        Say(_taunts.RandomElement());

        _nextTaunt = Core.TickCount + Utility.RandomMinMax(2000, 5000);
    }
}
